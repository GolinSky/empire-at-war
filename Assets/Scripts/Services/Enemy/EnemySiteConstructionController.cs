using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using Utilities.ScriptUtils.Time;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Builds mines on safe owned sites as soon as affordable, and the required satellites (three per zone, one per mining point) without waiting for a fleet.
    /// Money beyond the production reserve then buys one repair satellite per armed pad group and fills every remaining
    /// empty owned pad.
    /// A mining spot gets a battle asteroid instead of a mine when its zone needs firepower: a known hostile ship or
    /// <see cref="HELD_ATTACK_WINGS"/> hostile wings are in the held zone, the zone was lost and retaken, or a hostile
    /// is inside it in the first <see cref="OPENING_SECONDS"/>. At most one battle asteroid per zone.
    /// </summary>
    public sealed class EnemySiteConstructionController : ITickable
    {
        private const float DECISION_INTERVAL = 3f;
        public const float OPENING_SECONDS = 120f;
        /// <summary>A pad lost this many times within <see cref="PAD_LOSS_WINDOW"/> waits <see cref="PAD_REBUY_WAIT"/>.</summary>
        public const int PAD_LOSSES = 2;
        public const float PAD_LOSS_WINDOW = 60f;
        public const float PAD_REBUY_WAIT = 45f;
        /// <summary>Hostile wings that count as an attack on a held zone; one hostile ship always does.</summary>
        public const int HELD_ATTACK_WINGS = 3;
        private readonly ICaptureSiteDirectory _sites;
        private readonly ICaptureSitePresence _presence;
        private readonly ICaptureSiteConstruction _construction;
        private readonly ITimer _decisionTimer = TimerFactory.ConstructTimer(DECISION_INTERVAL);
        private readonly PlayerSlot _owner;
        private readonly EnemyDefenseSnapshotBuilder _snapshotBuilder;
        private readonly EnemyStationPressureTracker _stationPressure;
        private readonly EnemyDefenseStrategy _defense;
        private readonly EnemyProductionStrategy _production;
        private readonly IReinforcementZoneSource _zones;
        private readonly TeamIntelRegistry _intel;
        private readonly IPlayerRelations _relations;
        private readonly HashSet<ReinforcementZone> _heldZones = new HashSet<ReinforcementZone>();
        private readonly HashSet<ReinforcementZone> _lostZones = new HashSet<ReinforcementZone>();
        private readonly HashSet<int> _ownedPads = new HashSet<int>();
        private readonly Dictionary<int, List<float>> _padLosses = new Dictionary<int, List<float>>();
        private readonly Dictionary<int, float> _padWaitUntil = new Dictionary<int, float>();
        private readonly Predicate<int> _isPadWaitingNow;
        private string _lastIdlePads;

        public EnemySiteConstructionController(ICaptureSiteDirectory sites, ICaptureSitePresence presence,
            ICaptureSiteConstruction construction,
            PlayerSlot owner, EnemyDefenseSnapshotBuilder snapshotBuilder,
            EnemyDefenseStrategy defense,
            EnemyStationPressureTracker stationPressure, EnemyProductionStrategy production,
            IReinforcementZoneSource zones, TeamIntelRegistry intel, IPlayerRelations relations)
        {
            _intel = intel;
            _relations = relations;
            _owner = owner;
            _sites = sites;
            _presence = presence;
            _construction = construction;
            _snapshotBuilder = snapshotBuilder;
            _defense = defense;
            _stationPressure = stationPressure;
            _production = production;
            _zones = zones;
            _isPadWaitingNow = IsPadWaitingNow;
        }

        public void Tick()
        {
            if (_stationPressure.IsStationCritical || _production.OpeningMiningPending || _production.OpeningLevelReserve > 0f)
            {
                _production.PriorityConstructionPending = false;
                return;
            }
            if (!_decisionTimer.IsComplete) return;
            _decisionTimer.StartTimer();
            if (!_snapshotBuilder.IsReady) return;
            EnemyDefenseSnapshot snapshot = _snapshotBuilder.Build();
            ReinforcementZone centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            TrackZoneLosses();
            TrackPadLosses(BattleClock.CurrentElapsed);
            int mineSite = FindMineSite(centre);
            // P3: a pad in its churn wait asks for nothing, and a satellite already under construction is the one
            // allowed at a time: neither holds the ship money back.
            bool mandatorySatellite = _defense.HasMandatoryPad(snapshot, _isPadWaitingNow);
            _production.PriorityConstructionPending = mineSite >= 0 ||
                mandatorySatellite && !HasSatelliteUnderConstruction();
            string reason = "";
            SiteFacilityType spotFacility = mineSite >= 0
                ? ChooseSpotFacility(_sites.Sites[mineSite], out reason) : SiteFacilityType.Mining;
            BuildNext(snapshot, mineSite, mandatorySatellite, spotFacility, reason);
        }

        /// <param name="spotFacility">Mine, or a battle asteroid when the spot's zone needs firepower.</param>
        internal void BuildNext(EnemyDefenseSnapshot snapshot, int mineSite, bool mandatorySatellite,
            SiteFacilityType spotFacility = SiteFacilityType.Mining, string reason = "")
        {
            bool hasRequired = _defense.TrySelectMandatory(snapshot, out var required);
            if (hasRequired && StartSatellite(snapshot, required))
                return;
            if (mineSite >= 0)
            {
                SiteFacilityType type = spotFacility;
                bool built = _construction.TryStartConstruction(mineSite, _owner.Id, type);
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild",
                    type == SiteFacilityType.Mining ? $"kind=mine;started={built}"
                        : $"kind=battleAsteroid;started={built};reason={reason}");
                return;
            }
            if (!mandatorySatellite && (_defense.TrySelectRepair(snapshot, out var optional) ||
                                        _defense.TrySelectScored(snapshot, out optional) ||
                                        _defense.TrySelectFill(snapshot, out optional)))
                StartSatellite(snapshot, optional);
            else if (!hasRequired) RecordIdlePads(snapshot, mandatorySatellite);
        }

        /// <summary>
        /// Owned empty pads but no satellite chosen: one row per change says how many pads have hostiles on them (no
        /// construction possible there), so a held zone left empty can be explained from a release log (L3).
        /// </summary>
        private void RecordIdlePads(EnemyDefenseSnapshot snapshot, bool mandatorySatellite)
        {
            int hostilePads = 0;
            foreach (EnemyDefensePad pad in snapshot.Pads)
                if (pad.HasHostiles) hostilePads++;
            string key = $"{snapshot.Pads.Count}/{hostilePads}/{mandatorySatellite}";
            if (snapshot.Pads.Count == 0 || key == _lastIdlePads) return;
            _lastIdlePads = key;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild", FormattableString.Invariant(
                $"kind=satellite;started=False;reason=noChoice;emptyPads={snapshot.Pads.Count};hostilePads={hostilePads};mandatory={mandatorySatellite};spare={snapshot.Money - snapshot.ProductionReserve:F0}"));
        }

        private bool StartSatellite(EnemyDefenseSnapshot snapshot, EnemyDefenseChoice choice)
        {
            // One satellite under construction at a time: pads never outbid ships, and more pads (≈ 2.5 × after the
            // pad expansion) would otherwise drain the money as soon as each is captured. A second one starts only
            // from spare money (beyond the production reserve): a rich AI filled a retaken 5-pad centre one pad at a
            // time and lost the pads again still empty (L3, 21:20 log).
            if (HasSatelliteUnderConstruction() && choice.Cost > snapshot.Money - snapshot.ProductionReserve)
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild",
                    $"kind=satellite;started=False;site={choice.SiteIndex};reason=oneWaiting");
                return false;
            }
            if (IsPadWaiting(choice.SiteIndex, BattleClock.CurrentElapsed))
            {
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild",
                    $"kind=satellite;started=False;site={choice.SiteIndex};reason=padChurnWait");
                return false;
            }
            if (_construction.TryStartConstruction(choice.SiteIndex, _owner.Id, choice.Type))
            {
                EnemyAiTelemetry.RecordSatellite(_owner.Id, _owner.Faction, choice, snapshot.Money, _defense.TopChoices);
                return true;
            }

            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "siteBuild",
                $"kind=satellite;started=False;site={choice.SiteIndex};type={choice.Type}");
            return false;
        }

        /// <summary>Mine by default; battle asteroid when the spot's zone needs defence firepower.</summary>
        internal SiteFacilityType ChooseSpotFacility(ICaptureSite spot, out string reason)
        {
            reason = "";
            ReinforcementZone zone = FindZone(spot);
            if (zone == null) return SiteFacilityType.Mining;
            if (HasBattleAsteroid(zone))
            {
                // The retaken zone got its asteroid; every other spot there is a mine.
                _lostZones.Remove(zone);
                return SiteFacilityType.Mining;
            }
            if (IsHeld(zone) && IsHeldZoneAttacked(zone)) reason = "heldUnderAttack";
            else if (_lostZones.Contains(zone)) reason = "retaken";
            else if (BattleClock.CurrentElapsed < OPENING_SECONDS && HasHostilesIn(zone)) reason = "opening";
            return reason.Length > 0 ? SiteFacilityType.BattleAsteroid : SiteFacilityType.Mining;
        }

        private bool HasBattleAsteroid(ReinforcementZone zone)
        {
            foreach (ICaptureSite site in _sites.Sites)
                if (site.Kind == CaptureSiteKind.Mining && site.Owner == _owner.Id &&
                    site.FacilityType == SiteFacilityType.BattleAsteroid &&
                    site.State is CaptureSiteState.Constructing or CaptureSiteState.Operational &&
                    EnemyShipTaskGroupPlanner.IsInZone(zone, site.Center))
                    return true;
            return false;
        }

        /// <summary>A known hostile ship, or at least <see cref="HELD_ATTACK_WINGS"/> hostile wings, inside the zone.</summary>
        private bool IsHeldZoneAttacked(ReinforcementZone zone)
        {
            float now = Time.time;
            int wings = 0;
            HostileIntelModel intel = _intel.Get(_owner.Team);
            foreach (HostileSighting sighting in intel.Sightings)
            {
                if (!_relations.IsHostile(_owner.Id, sighting.Owner) || intel.GetConfidence(sighting, now) <= 0f ||
                    !EnemyShipTaskGroupPlanner.IsInZone(zone, ToVector(sighting.Position))) continue;
                if (sighting.UnitTypeId.IsShip) return true;
                if (sighting.UnitTypeId.IsSquadron && ++wings >= HELD_ATTACK_WINGS) return true;
            }
            return false;
        }

        internal bool HasSatelliteUnderConstruction()
        {
            foreach (ICaptureSite site in _sites.Sites)
                if (site.Kind == CaptureSiteKind.Satellite && site.Owner == _owner.Id &&
                    site.State == CaptureSiteState.Constructing) return true;
            return false;
        }

        /// <summary>A satellite pad lost twice within a minute gets no new satellite for a while.</summary>
        internal bool IsPadWaiting(int siteIndex, float now) =>
            _padWaitUntil.TryGetValue(siteIndex, out float until) && now < until;

        private bool IsPadWaitingNow(int siteIndex) => IsPadWaiting(siteIndex, BattleClock.CurrentElapsed);

        internal void TrackPadLosses(float now)
        {
            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                if (site.Kind != CaptureSiteKind.Satellite) continue;
                if (site.Owner == _owner.Id)
                {
                    _ownedPads.Add(i);
                    continue;
                }
                if (!_ownedPads.Remove(i)) continue;
                if (!_padLosses.TryGetValue(i, out List<float> losses)) _padLosses[i] = losses = new List<float>();
                losses.RemoveAll(time => now - time > PAD_LOSS_WINDOW);
                losses.Add(now);
                if (losses.Count < PAD_LOSSES) continue;
                _padWaitUntil[i] = now + PAD_REBUY_WAIT;
                losses.Clear();
            }
        }

        // A zone held, then lost, is marked; the mark stays once it is retaken.
        private void TrackZoneLosses()
        {
            foreach (ReinforcementZone zone in _zones.Zones)
            {
                if (!zone.IsCapturable) continue;
                if (IsHeld(zone)) _heldZones.Add(zone);
                else if (_heldZones.Remove(zone)) _lostZones.Add(zone);
            }
        }

        private bool IsHeld(ReinforcementZone zone) => zone.Owner == _owner.Id;

        private ReinforcementZone FindZone(ICaptureSite site)
        {
            foreach (ReinforcementZone zone in _zones.Zones)
                if (zone.IsCapturable && EnemyShipTaskGroupPlanner.IsInZone(zone, site.Center)) return zone;
            return null;
        }

        private bool HasHostilesIn(ReinforcementZone zone)
        {
            foreach (ICaptureSite site in _sites.Sites)
                if (EnemyShipTaskGroupPlanner.IsInZone(zone, site.Center) && _presence.HasHostileUnits(site, _owner.Id))
                    return true;
            return false;
        }

        private int FindMineSite(ReinforcementZone centre)
        {
            bool centreReady = centre == null || IsCentreReadyForMines(centre);
            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                if (site.Kind != CaptureSiteKind.Mining || site.Owner != _owner.Id ||
                    site.State != CaptureSiteState.Owned || _presence.HasHostileUnits(site, _owner.Id)) continue;
                if (centreReady || !EnemyShipTaskGroupPlanner.IsInZone(centre, site.Center))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Centre mines wait for the centre's satellites: <see cref="EnemyDefenseStrategy.ZONE_SATELLITES"/> standing
        /// team satellites there, or no own empty centre pad left that could take one now (none owned, all built, or
        /// hostiles on them). Waiting on a pad that cannot be built left an ally's three centre mines unbuilt for
        /// ≈ 900 s (L2, 21:20 log).
        /// </summary>
        private bool IsCentreReadyForMines(ReinforcementZone centre)
        {
            int teamSatellites = 0;
            bool buildablePad = false;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (site.Kind != CaptureSiteKind.Satellite || !EnemyShipTaskGroupPlanner.IsInZone(centre, site.Center))
                    continue;
                if (site.IsOperational && _relations.IsAllied(_owner.Id, site.Owner)) teamSatellites++;
                else if (site.Owner == _owner.Id && site.State == CaptureSiteState.Owned &&
                         !_presence.HasHostileUnits(site, _owner.Id))
                    buildablePad = true;
            }
            return teamSatellites >= EnemyDefenseStrategy.ZONE_SATELLITES || !buildablePad;
        }

    }
}
