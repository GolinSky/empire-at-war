using EmpireAtWar.Services.Vision;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Utils;
using UnityEngine;
using EmpireAtWar.Presenters.ReinforcementZones;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemySiteObjectives : IEnemyCaptureObjectiveSource
    {
        /// <summary>
        /// An owned site stays threatened this long after its last known hostile leaves, so a contact flickering
        /// across the site edge does not flip the fleet between site defense and raiding every evaluation.
        /// </summary>
        private const float THREAT_GRACE_SECONDS = 6f;
        private const float FACILITY_POSITION_TOLERANCE_SQUARED = 1f;
        // Satellites hold a zone by themselves, so their pads are worth a detour in travel-distance units.
        private const float SATELLITE_PAD_PRIORITY = 600f;

        private readonly Dictionary<int, float> _threatenedUntil = new Dictionary<int, float>();
        private readonly Dictionary<ICaptureSite, PlayerId> _knownMineClaims =
            new Dictionary<ICaptureSite, PlayerId>();
        private readonly ICaptureSiteDirectory _sites;
        private readonly ICaptureSitePresence _presence;
        private readonly CaptureSiteData _data;
        private readonly IPlayerRoster _players;
        private readonly PlayerSlot _owner;
        private readonly AiWorldSnapshot _world;
        private readonly IVisionService _vision;
        // Hostile satellites seen once: they cannot move, so they stay targets while they stand.
        private readonly HashSet<long> _knownSatellites = new HashSet<long>();
        private readonly Dictionary<int, int> _pointSites = new Dictionary<int, int>();

        public EnemySiteObjectives(ICaptureSiteDirectory sites, ICaptureSitePresence presence, CaptureSiteData data,
            IPlayerRoster players, PlayerSlot owner, AiWorldSnapshot world, IVisionService vision)
        {
            _world = world;
            _vision = vision;
            _sites = sites;
            _presence = presence;
            _data = data;
            _players = players;
            _owner = owner;
        }

        public bool HasMiningSite(ReinforcementZone zone)
        {
            foreach (ICaptureSite site in _sites.Sites)
                if (site.Kind == CaptureSiteKind.Mining &&
                    EnemyShipTaskGroupPlanner.IsInZone(zone, site.Center)) return true;
            return false;
        }

        public void AddStrategicObjectives(List<EnemyCaptureObjective> objectives, HostileIntelModel intel)
        {
            CountPointSites();
            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                // A free pad (a point of its own on a lane) is work for wings, never for the ship fleet.
                if (IsFreePad(site)) continue;
                // Satellite pads are defended by their own guns and squadrons; they never pull the ship fleet.
                bool threatened = site.Kind != CaptureSiteKind.Satellite && IsThreatened(i, site, intel, Time.time);
                long? blockingFacilityId = null;
                float progress = site.CaptureProgress;
                bool isMine = site.Kind == CaptureSiteKind.Mining;
                var strength = _presence.GetTeamStrengths(site, _owner.Id);
                if (!threatened && !IsCapturable(site) &&
                    TryFindHostileSatellite(site, out long satelliteId, out float satelliteDurability))
                {
                    // A hostile satellite blocks its pad: ships destroy it first, then capture the pad.
                    blockingFacilityId = satelliteId;
                    progress = satelliteDurability;
                }
                else if (!threatened && !IsCapturable(site))
                {
                    if (!isMine || site.FacilityType != SiteFacilityType.Mining ||
                        !_players.IsHostile(_owner.Id, site.Owner) ||
                        site.State != CaptureSiteState.Constructing && site.State != CaptureSiteState.Operational) continue;
                    foreach (HostileSighting mine in intel.Mines)
                    {
                        if (mine.Owner != site.Owner || PlanarGeometry.DistanceSquared(
                            ToVector(mine.Position), site.FacilityPosition) > FACILITY_POSITION_TOLERANCE_SQUARED)
                            continue;
                        blockingFacilityId = mine.EntityId;
                        // Damage to the facility is progress toward opening the site.
                        progress = mine.Hull + mine.Shields;
                        break;
                    }
                    if (!blockingFacilityId.HasValue) continue;
                }
                objectives.Add(new EnemyCaptureObjective($"site:{i}", EnemyCaptureObjectiveType.Facility,
                    site.Owner, ToVector(ToPoint(site.Center)), _data.GetSettings(site.Kind).Radius,
                    progress, threatened, priorityBonus: site.Kind == CaptureSiteKind.Satellite ? SATELLITE_PAD_PRIORITY : 0f,
                    isMine: isMine, blockingFacilityId: blockingFacilityId, hostileStrength: strength.Hostile));
            }
        }

        private void CountPointSites()
        {
            _pointSites.Clear();
            foreach (ICaptureSite site in _sites.Sites)
            {
                _pointSites.TryGetValue(site.PointId, out int count);
                _pointSites[site.PointId] = count + 1;
            }
        }

        /// <summary>A satellite pad that is the only site of its point.</summary>
        private bool IsFreePad(ICaptureSite site) =>
            site.Kind == CaptureSiteKind.Satellite && _pointSites[site.PointId] == 1;

        /// <summary>A known, standing hostile satellite on the pad; its id and remaining hull plus shields.</summary>
        private bool TryFindHostileSatellite(ICaptureSite site, out long id, out float durability)
        {
            id = 0;
            durability = 0f;
            if (site.Kind != CaptureSiteKind.Satellite || !_players.IsHostile(_owner.Id, site.Owner) ||
                site.State != CaptureSiteState.Constructing && site.State != CaptureSiteState.Operational) return false;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _world.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (entry.Owner != site.Owner || entry.Entity.HealthModel.IsDestroyed ||
                    PlanarGeometry.DistanceSquared(entry.Position, site.FacilityPosition) >
                    FACILITY_POSITION_TOLERANCE_SQUARED) continue;
                if (_vision.IsVisible(_owner.Id, entry.Entity)) _knownSatellites.Add(entry.Id);
                if (!_knownSatellites.Contains(entry.Id)) return false;
                id = entry.Id;
                durability = entry.Entity.HealthModel.Hull + entry.Entity.HealthModel.Shields;
                return true;
            }
            return false;
        }

        public void AddSquadronTargets(EnemySquadronCaptureAssignments assignments, HostileIntelModel intel)
        {
            foreach (ICaptureSite site in _sites.Sites)
            {
                Vector3 centre = ToVector(ToPoint(site.Center));
                float radius = _data.GetSettings(site.Kind).Radius;
                // Wings land on mining spots whoever owns the mine: the satellite pads in reach flip from there.
                if (site.Kind == CaptureSiteKind.Mining) assignments.AddMiningSpot(centre, radius);
                var strength = _presence.GetTeamStrengths(site, _owner.Id);
                bool threatened = site.Owner == _owner.Id && strength.Hostile > 0f;
                if (!threatened && !IsCapturable(site)) continue;

                // Whether wings go in is decided by wing power against the defenders, not by this source.
                assignments.AddTarget(centre, threatened, isSatellitePad: site.Kind == CaptureSiteKind.Satellite,
                    isMine: site.Kind == CaptureSiteKind.Mining, captureRadius: radius);
            }
        }

        public (int Allied, int Hostile) CountKnownMineClaims(PlayerId enemy, IVisionService vision)
        {
            int allied = 0;
            int hostile = 0;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (site.Kind != CaptureSiteKind.Mining) continue;
                if (_players.IsAllied(_owner.Id, site.Owner) || vision.IsVisible(_owner.Id, site.Center))
                    _knownMineClaims[site] = site.Owner;
                if (!_knownMineClaims.TryGetValue(site, out PlayerId known)) continue;
                if (_players.IsAllied(_owner.Id, known)) allied++;
                else if (!enemy.IsNone && _players.IsAllied(enemy, known)) hostile++;
            }
            return (allied, hostile);
        }

        private bool IsThreatened(int index, ICaptureSite site, HostileIntelModel intel, float now)
        {
            if (site.Owner != _owner.Id)
            {
                _threatenedUntil.Remove(index);
                return false;
            }
            if (HasKnownHostileInside(site, intel))
            {
                _threatenedUntil[index] = now + THREAT_GRACE_SECONDS;
                return true;
            }
            return _threatenedUntil.TryGetValue(index, out float until) && now < until;
        }

        private bool IsCapturable(ICaptureSite site)
        {
            return (site.State == CaptureSiteState.Neutral || site.State == CaptureSiteState.Owned) &&
                   !_players.IsAllied(_owner.Id, site.Owner);
        }

        private bool HasKnownHostileInside(ICaptureSite site, HostileIntelModel intel)
        {
            foreach (HostileSighting sighting in intel.Sightings)
                if (_players.IsHostile(_owner.Id, sighting.Owner) &&
                    intel.GetConfidence(sighting, Time.time) > 0f && site.Contains(ToVector(sighting.Position)))
                    return true;
            return false;
        }
    }
}
