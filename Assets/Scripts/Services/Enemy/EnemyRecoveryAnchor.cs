using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Stations;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyRecoveryAnchor : IEnemyRecoveryPolicy
    {
        private const float HOSTILE_MARGIN = 100f;
        private const float UNKNOWN_WEAPON_RANGE = 500f;
        private const float SECTOR_MAX_AGE = 60f;
        private const float MINIMUM_LOST_HOME_RADIUS = 64f;
        private readonly IReinforcementZonesSystem _reinforcementZones;
        private readonly IReinforcementZoneSource _zones;
        private readonly IPlayerRoster _roster;
        private readonly IEntityLocator _entities;
        private readonly IBattleAreaService _areas;
        private readonly IMapModelObserver _map;
        private readonly TeamIntelRegistry _registry;
        private readonly HostileIntelModel _intel;
        private readonly PlayerSlot _owner;
        private readonly IProductionAccess _production;
        private readonly EnemyStationPressureTracker _pressure;
        private readonly EnemyAdvantageSmoother _smoother = new EnemyAdvantageSmoother();
        private readonly EnemyRecoveryLatch _latch = new EnemyRecoveryLatch();
        private readonly EnemyTargetCommitment _commitment = new EnemyTargetCommitment();
        private readonly List<Candidate> _candidates = new List<Candidate>();
        private Candidate _anchor;
        private Vector3 _origin;
        private float _advantage = 1f;
                private bool _stationLost;
        private bool _canCapture;
        private string _rejectedRelay;
        private float _relayRetryAt;
        private float _sampleTime;
        private Func<Vector3, float, bool> _routeSafe;

        public bool IsActive => _latch.IsActive;
        public string DeferredReason => PendingCaptureObjective != null ? "anchorNotOwned" :
            _anchor == null ? "noAnchor" : "noSafeCandidate";
        public EnemyCaptureObjective PendingCaptureObjective => _anchor == null ? null : _anchor.Capture;

        public EnemyRecoveryAnchor(IReinforcementZonesSystem reinforcementZones, IReinforcementZoneSource zones,
            IPlayerRoster roster, IEntityLocator entities, IBattleAreaService areas, IMapModelObserver map,
            TeamIntelRegistry registry, PlayerSlot owner, IProductionAccess production,
            EnemyStationPressureTracker pressure)
        {
            _reinforcementZones = reinforcementZones;
            _zones = zones;
            _roster = roster;
            _entities = entities;
            _areas = areas;
            _map = map;
            _registry = registry;
            _intel = registry.Get(owner.Team);
            _owner = owner;
            _production = production;
            _pressure = pressure;
        }

        public void Update(Vector3 origin, float fleetAdvantage, bool underAttack, bool hasOwnStation,
            float now, Func<Vector3, float, bool> routeSafe, bool canCapture = true, bool hasFleetThreat = true)
        {
            _origin = origin;
            _sampleTime = now;
            _routeSafe = routeSafe;
            _stationLost = !hasOwnStation;
            _canCapture = canCapture;
            _smoother.Sample(fleetAdvantage, fleetAdvantage, now, hasFleetThreat, hasFleetThreat);
            _advantage = _smoother.Fleet;
            if (_latch.Update(now, _advantage, EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage,
                    underAttack || _pressure.HasPressure, !hasOwnStation, _pressure.IsStationCritical))
                RecordMode();
            if (!IsActive) { _anchor = null; return; }
            SelectAnchor(now, hasOwnStation);
        }

        public bool TryGetBuildAnchor(out Vector3 position)
        {
            position = _anchor == null ? default : _anchor.Position;
            return IsActive && _anchor != null && _anchor.Capture == null &&
                   IsCurrentAnchorValid() && IsPlacementSafe(position, 0f) &&
                   SafeCurrentRoute(position, 0f) && !_areas.IsBlocked(_owner.Id, position);
        }

        public Vector3? FindRetreatAnchor(Vector3 origin, bool hasStation,
            Func<Vector3, float, bool> routeSafe)
        {
            if (IsActive) return _anchor == null || !routeSafe(_anchor.Position, 0f) ||
                !IsPlacementSafe(_anchor.Position, 0f) ? null : _anchor.Position;
            if (hasStation && !_pressure.HasPressure &&
                _reinforcementZones.TryGetDefaultZoneCenter(_production.ProductionHost, out Vector3 home) &&
                routeSafe(home, 0f) && IsPlacementSafe(home, 0f)) return home;
            Vector3? best = null;
            float bestScore = float.NegativeInfinity;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                Vector3 position = ToVector(ToPoint(zone.Center));
                if (!zone.IsCapturable || zone.IsContested || !_roster.IsAllied(_owner.Id, zone.Owner) ||
                    !routeSafe(position, zone.Radius) || !IsPlacementSafe(position, zone.Radius)) continue;
                float score = 1f / (1f + Distance(origin, position));
                if (score > bestScore) { best = position; bestScore = score; }
            }
            return best;
        }

        public bool IsPlacementSafe(Vector3 position, float clearance)
        {
            if (IsActive && _stationLost && Distance(position, _map.GetStationPosition(_owner.Id)) <=
                LostHomeRadius() + clearance) return false;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!_roster.IsHostile(_owner.Id, sighting.Owner) || _intel.GetConfidence(sighting, Time.time) <= 0f) continue;
                float range = UNKNOWN_WEAPON_RANGE;
                if (_entities.TryGetEntity(sighting.EntityId, out IEntity entity) &&
                    entity.TryGetFacade(out IUnitCombatObserverFacade combat)) range = combat.WeaponRange;
                if (Distance(position, ToVector(sighting.Position)) <= range + HOSTILE_MARGIN + clearance) return false;
            }
            foreach (IEntity entity in _entities.Entities)
            {
                if (!entity.IsPlayerBase() || !_roster.IsHostile(_owner.Id, entity.Owner) || entity.HealthModel.IsDestroyed) continue;
                float range = entity.TryGetFacade(out IUnitCombatObserverFacade combat) ? combat.WeaponRange : UNKNOWN_WEAPON_RANGE;
                if (Distance(position, entity.GetFacade<IEntityTransformFacade>().Transform.position) <=
                    range + HOSTILE_MARGIN + clearance) return false;
            }
            foreach (EnemyPatrolPoint point in _registry.GetPatrol(_owner.Team).Entries)
            {
                if (point.Kind != "hostileStructure") continue;
                float range = UNKNOWN_WEAPON_RANGE;
                if (point.Id.StartsWith("structure:", StringComparison.Ordinal) &&
                    _entities.TryGetEntity(long.Parse(point.Id.Substring(10)), out IEntity structure) &&
                    structure.TryGetFacade(out IUnitCombatObserverFacade combat)) range = combat.WeaponRange;
                if (Distance(position, ToVector(point.Position)) <= range + HOSTILE_MARGIN + clearance) return false;
            }
            return true;
        }

        public float GetNearestHostileDistance(Vector3 position)
        {
            float nearest = float.PositiveInfinity;
            foreach (HostileSighting sighting in _intel.Sightings)
                if (_roster.IsHostile(_owner.Id, sighting.Owner) && _intel.GetConfidence(sighting, Time.time) > 0f)
                    nearest = Mathf.Min(nearest, Distance(position, ToVector(sighting.Position)));
            foreach (IEntity entity in _entities.Entities)
                if (entity.IsPlayerBase() && _roster.IsHostile(_owner.Id, entity.Owner) && !entity.HealthModel.IsDestroyed)
                    nearest = Mathf.Min(nearest, Distance(position, entity.GetFacade<IEntityTransformFacade>().Transform.position));
            foreach (EnemyPatrolPoint point in _registry.GetPatrol(_owner.Team).Entries)
                if (point.Kind == "hostileStructure") nearest = Mathf.Min(nearest, Distance(position, ToVector(point.Position)));
            return nearest;
        }

        public void CopySafeAlliedZoneBounds(List<Bounds> bounds)
        {
            bounds.Clear();
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                Vector3 position = ToVector(ToPoint(zone.Center));
                if (!zone.IsCapturable || zone.IsContested || !_roster.IsAllied(_owner.Id, zone.Owner) ||
                    !IsPlacementSafe(position, zone.Radius) || !SafeCurrentRoute(position, zone.Radius)) continue;
                bounds.Add(new Bounds(position, Vector3.one * zone.Radius * 2f));
            }
            bounds.Sort((left, right) => Score(_origin, right.center, Weight()).CompareTo(Score(_origin, left.center, Weight())));
        }

        public void OnStationBuilt(Vector3 position)
        {
            if (!IsActive || !IsPlacementSafe(position, 0f)) return;
            _latch.StationBuilt();
            RecordMode();
        }

        public void RejectCaptureObjective(string id, float retryAt)
        {
            _rejectedRelay = id;
            _relayRetryAt = retryAt;
        }

        private void SelectAnchor(float now, bool hasStation)
        {
            _candidates.Clear();
            int rejectedUnsafe = 0;
            if (hasStation && !_pressure.HasPressure &&
                _reinforcementZones.TryGetDefaultZoneCenter(_production.ProductionHost, out Vector3 home))
                Add("home", "home", home, 0f, null, ref rejectedUnsafe);
            for (int i = 0; i < _zones.Zones.Count; i++)
            {
                ReinforcementZonePresenter zone = _zones.Zones[i];
                if (!zone.IsCapturable || zone.IsContested || !_roster.IsAllied(_owner.Id, zone.Owner)) continue;
                string kind = zone.Owner == _owner.Id ? "ownZone" : _roster.Get(zone.Owner).IsAi ? "allyZoneAi" : "allyZoneHuman";
                Add($"zone:{i}", kind, ToVector(ToPoint(zone.Center)), zone.Radius, null, ref rejectedUnsafe);
            }
            foreach (IEntity entity in _entities.Entities)
                if (entity.IsPlayerBase() && _roster.IsAllied(_owner.Id, entity.Owner) && !entity.HealthModel.IsDestroyed)
                    Add($"station:{entity.Id}", "allyStation", entity.GetFacade<IEntityTransformFacade>().Transform.position,
                        0f, null, ref rejectedUnsafe);
            if (_candidates.Count == 0)
                foreach (var point in _registry.GetPatrol(_owner.Team).Entries)
                    if (point.IsSector && point.LastSeenTime >= 0f && now - point.LastSeenTime <= SECTOR_MAX_AGE)
                        Add(point.Id, "emptySector", ToVector(point.Position), 0f, null, ref rejectedUnsafe);
            if (_candidates.Count == 0 && _canCapture)
                for (int i = 0; i < _zones.Zones.Count; i++)
                {
                    ReinforcementZonePresenter zone = _zones.Zones[i];
                    if (!zone.IsCapturable || zone.IsContested || zone.Owner != PlayerId.None) continue;
                    Vector3 position = ToVector(ToPoint(zone.Center));
                    var capture = new EnemyCaptureObjective($"zone:{i}", EnemyCaptureObjectiveType.ReinforcementZone,
                        zone.Owner, position, zone.Radius, zone.CaptureProgress);
                    Add(capture.Id, "neutralRelay", position, zone.Radius, capture, ref rejectedUnsafe);
                }
            Candidate best = null;
            Candidate current = null;
            foreach (Candidate candidate in _candidates)
            {
                if (best == null || candidate.Score > best.Score) best = candidate;
                if (_anchor != null && candidate.Id == _anchor.Id) current = candidate;
            }
            string selected = _commitment.Select(best == null ? null : best.Id, best == null ? 0f : best.Score,
                _anchor == null ? null : _anchor.Id, current == null ? 0f : current.Score, current != null, now);
            _anchor = current != null && current.Id == selected ? current : best;
            if (!_commitment.Changed) return;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "objectiveSwitch",
                $"domain=anchor fromId={_commitment.LastFromId} toId={selected} fromScore={_commitment.LastFromScore:F2} toScore={_commitment.LastToScore:F2} margin=0.20 heldSeconds={_commitment.LastHeldSeconds:F1} cause={_commitment.LastCause}");
            if (_anchor != null)
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "recoveryAnchor",
                    $"kind={_anchor.Kind} id={_anchor.Id} pos={_anchor.Position.x:F1},{_anchor.Position.z:F1} w={Weight():F2} threatDistance={GetNearestHostileDistance(_anchor.Position):F1} oldStationDistance={Distance(_anchor.Position, _map.GetStationPosition(_owner.Id)):F1} candidates={_candidates.Count} rejectedUnsafe={rejectedUnsafe}");
        }

        private void Add(string id, string kind, Vector3 position, float radius, EnemyCaptureObjective capture, ref int rejected)
        {
            if (capture != null && id == _rejectedRelay && _sampleTime < _relayRetryAt ||
                !IsPlacementSafe(position, radius) || !_routeSafe(position, radius) ||
                capture == null && _areas.IsBlocked(_owner.Id, position)) { rejected++; return; }
            _candidates.Add(new Candidate(id, kind, position, Score(_origin, position, Weight()), capture));
        }

        private float Weight() => Mathf.Clamp01(_advantage);
        private float LostHomeRadius()
        {
            float radius = MINIMUM_LOST_HOME_RADIUS;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (!zone.IsCapturable && zone.Owner == _owner.Id) radius = Mathf.Max(radius, zone.Radius);
            return radius;
        }
        private bool SafeCurrentRoute(Vector3 position, float radius) =>
            _routeSafe(position, radius);
        private float Score(Vector3 origin, Vector3 position, float weight)
        {
            float mapSize = Vector2.Distance(_map.SizeRange.Min, _map.SizeRange.Max);
            float near = 1f / (1f + Distance(origin, position) / mapSize);
            float far = Mathf.Min(GetNearestHostileDistance(position) / mapSize, 1f);
            return weight * near + (1f - weight) * far;
        }
        private static float Distance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        private bool IsCurrentAnchorValid()
        {
            if (_anchor.Kind == "emptySector")
            {
                foreach (var point in _registry.GetPatrol(_owner.Team).Entries)
                    if (point.Id == _anchor.Id) return point.LastSeenTime >= 0f && Time.time - point.LastSeenTime <= SECTOR_MAX_AGE;
                return false;
            }
            if (_anchor.Id.StartsWith("zone:", StringComparison.Ordinal))
            {
                int index = int.Parse(_anchor.Id.Substring(5));
                if (index >= _zones.Zones.Count) return false;
                ReinforcementZonePresenter zone = _zones.Zones[index];
                return !zone.IsContested && _roster.IsAllied(_owner.Id, zone.Owner);
            }
            if (_anchor.Id.StartsWith("station:", StringComparison.Ordinal))
                return _entities.TryGetEntity(long.Parse(_anchor.Id.Substring(8)), out IEntity station) &&
                    !station.HealthModel.IsDestroyed && _roster.IsAllied(_owner.Id, station.Owner);
            return !_stationLost && !_pressure.HasPressure;
        }
        private void RecordMode() => EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "recoveryMode",
            $"on={IsActive} trigger={_latch.Trigger} fleetSmoothed={_advantage:F2} threat={_pressure.ThreatX:F1},{_pressure.ThreatZ:F1}");

        private sealed class Candidate
        {
            public string Id { get; }
            public string Kind { get; }
            public Vector3 Position { get; }
            public float Score { get; }
            public EnemyCaptureObjective Capture { get; }
            public Candidate(string id, string kind, Vector3 position, float score, EnemyCaptureObjective capture)
            { Id = id; Kind = kind; Position = position; Score = score; Capture = capture; }
        }
    }
}
