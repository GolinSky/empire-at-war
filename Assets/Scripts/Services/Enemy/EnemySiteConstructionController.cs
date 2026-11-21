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
        private readonly HashSet<ReinforcementZonePresenter> _heldZones = new HashSet<ReinforcementZonePresenter>();
        private readonly HashSet<ReinforcementZonePresenter> _lostZones = new HashSet<ReinforcementZonePresenter>();
        private readonly HashSet<int> _ownedPads = new HashSet<int>();
        private readonly Dictionary<int, List<float>> _padLosses = new Dictionary<int, List<float>>();
        private readonly Dictionary<int, float> _padWaitUntil = new Dictionary<int, float>();

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
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            TrackZoneLosses();
            TrackPadLosses(BattleClock.CurrentElapsed);
            int mineSite = FindMineSite(centre);
            bool mandatorySatellite = _defense.HasMandatoryPad(snapshot);
            _production.PriorityConstructionPending = mandatorySatellite || mineSite >= 0;
            string reason = "";
            SiteFacilityType spotFacility = mineSite >= 0
                ? ChooseSpotFacility(_sites.Sites[mineSite], out reason) : SiteFacilityType.Mining;
            BuildNext(snapshot, mineSite, mandatorySatellite, spotFacility, reason);
        }

        /// <param name="spotFacility">Mine, or a battle asteroid when the spot's zone needs firepower.</param>
        internal void BuildNext(EnemyDefenseSnapshot snapshot, int mineSite, bool mandatorySatellite,
            SiteFacilityType spotFacility = SiteFacilityType.Mining, string reason = "")
        {
            if (_defense.TrySelectMandatory(snapshot, out var required) && StartSatellite(snapshot, required))
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
        }

        private bool StartSatellite(EnemyDefenseSnapshot snapshot, EnemyDefenseChoice choice)
        {
            // One satellite under construction at a time: pads never outbid ships, and more pads (≈ 2.5 × after the
            // pad expansion) would otherwise drain the money as soon as each is captured.
            if (HasSatelliteUnderConstruction())
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
            ReinforcementZonePresenter zone = FindZone(spot);
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

        private bool HasBattleAsteroid(ReinforcementZonePresenter zone)
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
        private bool IsHeldZoneAttacked(ReinforcementZonePresenter zone)
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
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (!zone.IsCapturable) continue;
                if (IsHeld(zone)) _heldZones.Add(zone);
                else if (_heldZones.Remove(zone)) _lostZones.Add(zone);
            }
        }

        private bool IsHeld(ReinforcementZonePresenter zone) => zone.Owner == _owner.Id;

        private ReinforcementZonePresenter FindZone(ICaptureSite site)
        {
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (zone.IsCapturable && EnemyShipTaskGroupPlanner.IsInZone(zone, site.Center)) return zone;
            return null;
        }

        private bool HasHostilesIn(ReinforcementZonePresenter zone)
        {
            foreach (ICaptureSite site in _sites.Sites)
                if (EnemyShipTaskGroupPlanner.IsInZone(zone, site.Center) && _presence.HasHostileUnits(site, _owner.Id))
                    return true;
            return false;
        }

        private int FindMineSite(ReinforcementZonePresenter centre)
        {
            int centreSatellites = 0;
            if (centre != null)
                foreach (ICaptureSite site in _sites.Sites)
                    if (site.Kind == CaptureSiteKind.Satellite && site.Owner == _owner.Id && site.IsOperational &&
                        EnemyShipTaskGroupPlanner.IsInZone(centre, site.Center))
                        centreSatellites++;

            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                if (site.Kind != CaptureSiteKind.Mining || site.Owner != _owner.Id ||
                    site.State != CaptureSiteState.Owned || _presence.HasHostileUnits(site, _owner.Id)) continue;
                if (centre == null || centreSatellites >= EnemyDefenseStrategy.ZONE_SATELLITES ||
                    !EnemyShipTaskGroupPlanner.IsInZone(centre, site.Center))
                    return i;
            }
            return -1;
        }

    }
}
