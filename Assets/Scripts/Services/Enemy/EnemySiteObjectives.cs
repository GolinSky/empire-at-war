using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Utils;
using UnityEngine;
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
        private readonly ICaptureSiteDirectory _sites;
        private readonly ICaptureSitePresence _presence;
        private readonly CaptureSiteData _data;
        private readonly IPlayerRoster _players;
        private readonly PlayerSlot _owner;

        public EnemySiteObjectives(ICaptureSiteDirectory sites, ICaptureSitePresence presence, CaptureSiteData data,
            IPlayerRoster players, PlayerSlot owner)
        {
            _sites = sites;
            _presence = presence;
            _data = data;
            _players = players;
            _owner = owner;
        }

        public void AddStrategicObjectives(List<EnemyCaptureObjective> objectives, HostileIntelModel intel)
        {
            for (int i = 0; i < _sites.Sites.Count; i++)
            {
                ICaptureSite site = _sites.Sites[i];
                // Satellite pads are defended by their own guns and squadrons; they never pull the ship fleet.
                bool threatened = site.Kind != CaptureSiteKind.Satellite && IsThreatened(i, site, intel, Time.time);
                long? blockingFacilityId = null;
                float progress = site.CaptureProgress;
                bool isMine = site.Kind == CaptureSiteKind.Mining;
                if (!threatened && !IsCapturable(site))
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
                    isMine: isMine, blockingFacilityId: blockingFacilityId));
            }
        }

        public void AddSquadronTargets(EnemySquadronCaptureAssignments assignments)
        {
            foreach (ICaptureSite site in _sites.Sites)
            {
                bool threatened = site.Owner == _owner.Id && _presence.HasHostileUnits(site, _owner.Id);
                if (threatened || IsCapturable(site))
                    assignments.AddTarget(ToVector(ToPoint(site.Center)), threatened);
            }
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
                   !_players.IsAllied(_owner.Id, site.Owner) &&
                   (site.Kind != CaptureSiteKind.Satellite || !PointHasMines(site.PointId) || OwnsPointMines(site.PointId));
        }

        private bool PointHasMines(int pointId)
        {
            foreach (ICaptureSite site in _sites.Sites)
                if (site.Kind == CaptureSiteKind.Mining && site.PointId == pointId) return true;
            return false;
        }

        private bool OwnsPointMines(int pointId)
        {
            bool hasMine = false;
            foreach (ICaptureSite site in _sites.Sites)
            {
                if (site.Kind != CaptureSiteKind.Mining || site.PointId != pointId) continue;
                if (site.Owner != _owner.Id) return false;
                hasMine = true;
            }
            return hasMine;
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
