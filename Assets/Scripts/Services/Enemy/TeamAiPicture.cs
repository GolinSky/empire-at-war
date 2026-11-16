using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.TacticalMarkers;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// Cached per-team strategic picture: confident hostile sightings, stable capture-objective candidates,
    /// and precomputed hostile force compositions near structures and capture objectives.
    /// </summary>
    public sealed class TeamAiPicture
    {
        private const float THREAT_RADIUS = 1000f;
        private const float THREAT_RADIUS_SQUARED = THREAT_RADIUS * THREAT_RADIUS;

        private readonly IPlayerRoster _roster;
        private readonly IReinforcementZoneSource _zones;
        private readonly IReadOnlyList<IEnemyCaptureObjectiveSource> _captureSources;
        private readonly UnitCombatProfileCatalog _catalog;
        private readonly AiWorldSnapshot _worldSnapshot;
        private readonly HostileIntelModel _intel;
        private readonly List<HostileSighting> _confidentSightings = new List<HostileSighting>();
        private readonly List<EnemyCaptureObjective> _baseCaptureObjectives = new List<EnemyCaptureObjective>();
        private readonly List<string> _zoneIds = new List<string>();
        private readonly Dictionary<long, ForceComposition> _structureThreats = new Dictionary<long, ForceComposition>();
        private readonly Dictionary<string, ForceComposition> _objectiveThreats = new Dictionary<string, ForceComposition>();
        private readonly List<ForceComposition> _pooledForces = new List<ForceComposition>();

        private int _usedPoolCount;
        private int _lastRefreshFrame = -1;
        private float _lastRefreshTime = -1f;

        public TeamId Team { get; }
        public PlayerId RepresentativePlayer { get; private set; }
        public IReadOnlyList<HostileSighting> ConfidentSightings => _confidentSightings;
        public IReadOnlyList<EnemyCaptureObjective> BaseCaptureObjectives => _baseCaptureObjectives;

        public TeamAiPicture(
            TeamId team,
            PlayerId representativePlayer,
            HostileIntelModel intel,
            IPlayerRoster roster,
            IReinforcementZoneSource zones,
            IReadOnlyList<IEnemyCaptureObjectiveSource> captureSources,
            UnitCombatProfileCatalog catalog,
            AiWorldSnapshot worldSnapshot)
        {
            Team = team;
            RepresentativePlayer = representativePlayer;
            _intel = intel;
            _roster = roster;
            _zones = zones;
            _captureSources = captureSources;
            _catalog = catalog;
            _worldSnapshot = worldSnapshot;
        }

        public void SetRepresentativePlayer(PlayerId player)
        {
            RepresentativePlayer = player;
        }

        public void EnsureRefreshed(float now)
        {
            int frame = Time.frameCount;
            if (_lastRefreshFrame == frame && Mathf.Approximately(_lastRefreshTime, now))
            {
                return;
            }

            Refresh(now);
        }

        public void Refresh(float now)
        {
            _lastRefreshFrame = Time.frameCount;
            _lastRefreshTime = now;
            _confidentSightings.Clear();
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (_intel.GetConfidence(sighting, now) > 0f &&
                    _roster.IsHostile(RepresentativePlayer, sighting.Owner))
                {
                    _confidentSightings.Add(sighting);
                }
            }

            _baseCaptureObjectives.Clear();
            if (_zones != null)
            {
                IReadOnlyList<ReinforcementZonePresenter> zones = _zones.Zones;
                for (int i = 0; i < zones.Count; i++)
                {
                    ReinforcementZonePresenter zone = zones[i];
                    if (!zone.IsCapturable || _roster.IsAllied(RepresentativePlayer, zone.Owner))
                    {
                        continue;
                    }

                    _baseCaptureObjectives.Add(new EnemyCaptureObjective(
                        GetZoneId(i),
                        EnemyCaptureObjectiveType.ReinforcementZone,
                        zone.Owner,
                        ToVector(ToPoint(zone.Center)),
                        zone.Radius,
                        zone.CaptureProgress));
                }
            }

            if (_captureSources != null)
            {
                for (int i = 0; i < _captureSources.Count; i++)
                {
                    IEnemyCaptureObjectiveSource source = _captureSources[i];
                    if (!(source is AllyAiMarkerSource))
                    {
                        source.AddStrategicObjectives(_baseCaptureObjectives, _intel);
                    }
                }
            }

            _usedPoolCount = 0;
            _structureThreats.Clear();
            _objectiveThreats.Clear();

            if (_worldSnapshot != null)
            {
                IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.Structures;
                for (int i = 0; i < structures.Count; i++)
                {
                    AiWorldSnapshot.Entry entry = structures[i];
                    if (!_roster.IsHostile(RepresentativePlayer, entry.Owner) || entry.IsUnit || entry.IsPlayerBase)
                    {
                        continue;
                    }

                    ForceComposition threat = NextForce();
                    BuildThreatNear(threat, entry.Position);
                    _structureThreats[entry.Id] = threat;
                }
            }

            for (int i = 0; i < _baseCaptureObjectives.Count; i++)
            {
                EnemyCaptureObjective objective = _baseCaptureObjectives[i];
                ForceComposition threat = NextForce();
                BuildThreatNear(threat, objective.Position);
                _objectiveThreats[objective.Id] = threat;
            }
        }

        public bool TryGetStructureThreat(long entityId, out ForceComposition threat) =>
            _structureThreats.TryGetValue(entityId, out threat);

        public bool TryGetObjectiveThreat(string objectiveId, out ForceComposition threat) =>
            _objectiveThreats.TryGetValue(objectiveId, out threat);

        public void BuildKnownFromConfident(ForceComposition force, Predicate<HostileSighting> include)
        {
            force.Clear();
            for (int i = 0; i < _confidentSightings.Count; i++)
            {
                HostileSighting sighting = _confidentSightings[i];
                if (!include(sighting))
                {
                    continue;
                }

                force.Add(_catalog.Get(sighting.UnitTypeId), sighting.Hull, sighting.Shields, sighting.OffenseScale);
            }
        }

        public string GetZoneId(int index)
        {
            while (_zoneIds.Count <= index)
            {
                _zoneIds.Add("zone:" + _zoneIds.Count);
            }

            return _zoneIds[index];
        }

        private void BuildThreatNear(ForceComposition threat, Vector3 center)
        {
            threat.Clear();
            for (int i = 0; i < _confidentSightings.Count; i++)
            {
                HostileSighting sighting = _confidentSightings[i];
                Vector3 offset = ToVector(sighting.Position) - center;
                offset.y = 0f;
                if (offset.sqrMagnitude <= THREAT_RADIUS_SQUARED)
                {
                    threat.Add(_catalog.Get(sighting.UnitTypeId), sighting.Hull, sighting.Shields, sighting.OffenseScale);
                }
            }
        }

        private ForceComposition NextForce()
        {
            if (_usedPoolCount == _pooledForces.Count)
            {
                _pooledForces.Add(new ForceComposition());
            }

            ForceComposition force = _pooledForces[_usedPoolCount++];
            force.Clear();
            return force;
        }
    }
}
