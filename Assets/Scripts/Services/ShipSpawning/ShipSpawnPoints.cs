using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EmpireAtWar.Services.ShipSpawning
{
    public sealed class ShipSpawnPoints : IShipSpawnPoints
    {
        private const int MAX_RANDOM_SPAWN_ATTEMPTS = 100;
        private const int REAR_SECTOR_ATTEMPTS = 36;
        /// <summary>A forward landing blocks its point this long; the hyperspace jump lasts well under it.</summary>
        public const float FORWARD_LANDING_SECONDS = 2f;
        private static readonly float[] REAR_RING_SCALES = { 1f, 1.5f, 2f, 3f };

        private readonly IReinforcementZoneSource _zoneSource;
        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IPlayerRelations _relations;
        private readonly ReinforcementZoneData _data;
        private readonly IEntityLocator _entities;
        private readonly IShipSpawnClearance _clearance;
        private readonly List<Vector3> _stationPositions = new List<Vector3>();
        private readonly List<(Vector3 Position, float Radius, bool Station)> _targetedAnchors =
            new List<(Vector3, float, bool)>();

        private readonly List<Vector3> _anchors = new List<Vector3>();
        // Forward points: bounds, hazard, vision and hostile block circle, then the hull; no reachability.
        private readonly Func<Vector3, bool> _isForwardOpen;
        private readonly Func<Vector3, bool> _isForwardClear;
        // Hulls fly at per-type heights, so the 3D hull test lets two types land on one point; forward points also
        // keep the top-down footprint of standing ships and of ships still jumping in apart.
        private readonly List<(Vector3 Position, float Radius, float Until)> _forwardLandings =
            new List<(Vector3, float, float)>();
        private PlayerId _forwardOwner;
        private ShipType _forwardShip;
        private float _forwardRadius;

        public ShipSpawnPoints(
            IReinforcementZoneSource zoneSource,
            IReinforcementZonesSystem zones,
            IReinforcementSpawnRule spawnRule,
            IPlayerRelations relations,
            ReinforcementZoneData data,
            IEntityLocator entities, IShipSpawnClearance clearance)
        {
            _entities = entities;
            _clearance = clearance;
            _zoneSource = zoneSource;
            _zones = zones;
            _spawnRule = spawnRule;
            _relations = relations;
            _data = data;
            _isForwardOpen = point => _spawnRule.IsOpen(_forwardOwner, point);
            _isForwardClear = point => _clearance.IsClear(_forwardOwner, _forwardShip, point) && IsPlanarFree(point);
        }

        public bool TryGetRandomSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            // The front line first: relays held by the owner's team, where hostiles cannot arrive.
            _anchors.Clear();
            foreach (ReinforcementZonePresenter relay in _zoneSource.Zones)
            {
                if (_relations.IsAllied(relay.Owner, owner)) _anchors.Add(relay.Center);
            }

            return TryFindOpenPosition(owner, shipType, _data.RelaySpawnBlockRadius, out position) ||
                   TryGetDefaultZoneSpawnPosition(owner, shipType, out position);
        }

        public bool TryGetSpawnPositionNear(PlayerId owner, ShipType shipType, Vector3 target, out Vector3 position)
        {
            _targetedAnchors.Clear();
            CollectStationPositions(owner);
            foreach (Vector3 station in _stationPositions)
                _targetedAnchors.Add((station, _data.HomeAreaRadius, true));
            foreach (ReinforcementZonePresenter relay in _zoneSource.Zones)
                if (_relations.IsAllied(relay.Owner, owner))
                    _targetedAnchors.Add((relay.Center, _data.RelaySpawnBlockRadius, false));
            if (_zones.TryGetDefaultZoneCenter(owner, out Vector3 home))
                _targetedAnchors.Add((home, _data.HomeAreaRadius, false));

            _targetedAnchors.Sort((a, b) =>
            {
                int byDistance = (a.Position - target).sqrMagnitude.CompareTo((b.Position - target).sqrMagnitude);
                return byDistance != 0 ? byDistance : b.Station.CompareTo(a.Station);
            });
            foreach (var anchor in _targetedAnchors)
            {
                _anchors.Clear();
                _anchors.Add(anchor.Position);
                if (TryFindOpenPosition(owner, shipType, anchor.Radius, out position)) return true;
            }
            position = default;
            return false;
        }

        public bool TryGetSpawnPositionNearUnits(PlayerId owner, ShipType shipType, IReadOnlyList<Vector3> units,
            Vector3 awayFrom, out Vector3 position, out ForwardSpawnAttempt attempt)
        {
            _forwardOwner = owner;
            _forwardShip = shipType;
            _forwardRadius = _clearance.GetPlanarRadius(shipType);
            float now = Time.time;
            _forwardLandings.RemoveAll(landing => landing.Until <= now);
            if (!ForwardSpawnRing.TryFind(units, awayFrom, _isForwardOpen, _isForwardClear, out position,
                    out attempt)) return false;
            _forwardLandings.Add((position, _forwardRadius, now + FORWARD_LANDING_SECONDS));
            return true;
        }

        /// <summary>No ship of any height, standing or still landing, overlaps the point seen from above.</summary>
        private bool IsPlanarFree(Vector3 point)
        {
            foreach (var landing in _forwardLandings)
                if (IsWithin(point, landing.Position, _forwardRadius + landing.Radius)) return false;
            foreach (IEntity entity in _entities.Entities)
            {
                if (!entity.TryGetFacade(out IUnitTypeFacade unit) || !unit.UnitTypeId.IsShip ||
                    entity.HealthModel.IsDestroyed) continue;
                Vector3 ship = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (IsWithin(point, ship, _forwardRadius + _clearance.GetPlanarRadius(unit.UnitTypeId.ShipType)))
                    return false;
            }
            return true;
        }

        private static bool IsWithin(Vector3 a, Vector3 b, float distance)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z < distance * distance;
        }

        public float GetHullRadius(ShipType shipType) => _clearance.GetPlanarRadius(shipType);

        public bool TryGetDefaultZoneSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            _anchors.Clear();
            if (_zones.TryGetDefaultZoneCenter(owner, out Vector3 home)) _anchors.Add(home);
            return TryFindOpenPosition(owner, shipType, _data.HomeAreaRadius, out position);
        }

        public bool TryGetDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position)
        {
            float radius = _data.HomeAreaRadius;
            var centroid = threats.GetLocalCentroid(anchor.x, anchor.z, radius);
            Vector2 towardThreat = new Vector2(centroid.X - anchor.x, centroid.Z - anchor.z).normalized;
            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                if (Vector2.Dot(offset, towardThreat) > 0f)
                    offset = Vector2.Reflect(offset, towardThreat);
                position = new Vector3(anchor.x + offset.x, 0f, anchor.z + offset.y);
                if (threats.IsSafe(position.x, position.z, 0f) &&
                    _spawnRule.CanSpawnShip(owner, shipType, position)) return true;
            }

            position = default;
            return false;
        }

        public bool TryGetStationSpawnPosition(PlayerId owner, ShipType shipType, out Vector3 position)
        {
            CollectStationPositions(owner);
            // The station nearest the home zone is the usual arrival point; later ones take over when it is lost or blocked.
            Vector3 reference = _zones.TryGetDefaultZoneCenter(owner, out Vector3 home) ? home : Vector3.zero;
            _stationPositions.Sort((a, b) => (a - reference).sqrMagnitude.CompareTo((b - reference).sqrMagnitude));
            foreach (Vector3 station in _stationPositions)
            {
                _anchors.Clear();
                _anchors.Add(station);
                if (TryFindOpenPosition(owner, shipType, _data.HomeAreaRadius, out position)) return true;
            }

            position = default;
            return false;
        }

        public bool TryGetBestDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position)
        {
            float radius = _data.HomeAreaRadius;
            var centroid = threats.GetLocalCentroid(anchor.x, anchor.z, radius);
            var threatCenter = new Vector2(centroid.X, centroid.Z);
            var fromStation = threatCenter - new Vector2(anchor.x, anchor.z);
            bool found = false;
            float bestDistance = float.NegativeInfinity;
            position = default;
            for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                if (Vector2.Dot(offset, fromStation) > 0f)
                    offset = Vector2.Reflect(offset, fromStation.normalized);
                var candidate = new Vector3(anchor.x + offset.x, 0f, anchor.z + offset.y);
                if (!_spawnRule.CanLaunchStationDefenseShip(owner, shipType, candidate)) continue;
                float distance = (new Vector2(candidate.x, candidate.z) - threatCenter).sqrMagnitude;
                if (distance <= bestDistance) continue;
                bestDistance = distance;
                position = candidate;
                found = true;
            }

            return found;
        }

        public bool TryGetRearSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, bool requireThreatSafety, out Vector3 position)
        {
            float baseRadius = _data.HomeAreaRadius;
            float maxRadius = baseRadius * REAR_RING_SCALES[REAR_RING_SCALES.Length - 1];
            var centroid = threats.GetLocalCentroid(anchor.x, anchor.z, maxRadius);
            Vector2 towardThreat = new Vector2(centroid.X - anchor.x, centroid.Z - anchor.z);
            float threatAngle = towardThreat.sqrMagnitude > 1e-6f
                ? Mathf.Atan2(towardThreat.y, towardThreat.x)
                : 0f;
            float rearAngle = threatAngle + Mathf.PI;

            for (int sector = 0; sector < 3; sector++)
            {
                for (int ring = 0; ring < REAR_RING_SCALES.Length; ring++)
                {
                    float inner = ring == 0 ? 0f : REAR_RING_SCALES[ring - 1] * baseRadius;
                    float outer = REAR_RING_SCALES[ring] * baseRadius;
                    float innerSq = inner * inner;
                    float outerSq = outer * outer;
                    for (int attempt = 0; attempt < REAR_SECTOR_ATTEMPTS; attempt++)
                    {
                        float distance = Mathf.Sqrt(Random.Range(innerSq, outerSq));
                        float angle = SampleSectorAngle(sector, rearAngle, threatAngle);
                        position = new Vector3(
                            anchor.x + Mathf.Cos(angle) * distance,
                            0f,
                            anchor.z + Mathf.Sin(angle) * distance);
                        if ((!requireThreatSafety || threats.IsSafe(position.x, position.z, 0f)) &&
                            _spawnRule.CanSpawnShip(owner, shipType, position))
                        {
                            return true;
                        }
                    }
                }
            }

            position = default;
            return false;
        }

        public bool TryGetWideDefenseSpawnPosition(PlayerId owner, ShipType shipType, Vector3 anchor,
            PlacementThreatSnapshot threats, out Vector3 position)
        {
            float baseRadius = _data.HomeAreaRadius;
            float maxRadius = baseRadius * REAR_RING_SCALES[REAR_RING_SCALES.Length - 1];
            var centroid = threats.GetLocalCentroid(anchor.x, anchor.z, maxRadius);
            var threatCenter = new Vector2(centroid.X, centroid.Z);
            Vector2 towardThreat = threatCenter - new Vector2(anchor.x, anchor.z);
            float threatAngle = towardThreat.sqrMagnitude > 1e-6f
                ? Mathf.Atan2(towardThreat.y, towardThreat.x)
                : 0f;
            float rearAngle = threatAngle + Mathf.PI;

            bool found = false;
            float bestDistance = float.NegativeInfinity;
            position = default;
            for (int sector = 0; sector < 3; sector++)
            {
                for (int ring = REAR_RING_SCALES.Length - 1; ring >= 0; ring--)
                {
                    float inner = ring == 0 ? 0f : REAR_RING_SCALES[ring - 1] * baseRadius;
                    float outer = REAR_RING_SCALES[ring] * baseRadius;
                    float innerSq = inner * inner;
                    float outerSq = outer * outer;
                    for (int attempt = 0; attempt < REAR_SECTOR_ATTEMPTS; attempt++)
                    {
                        float distance = Mathf.Sqrt(Random.Range(innerSq, outerSq));
                        float angle = SampleSectorAngle(sector, rearAngle, threatAngle);
                        var candidate = new Vector3(
                            anchor.x + Mathf.Cos(angle) * distance,
                            0f,
                            anchor.z + Mathf.Sin(angle) * distance);
                        if (!_spawnRule.CanLaunchStationDefenseShip(owner, shipType, candidate)) continue;
                        float threatDistance = (new Vector2(candidate.x, candidate.z) - threatCenter).sqrMagnitude;
                        if (threatDistance <= bestDistance) continue;
                        bestDistance = threatDistance;
                        position = candidate;
                        found = true;
                    }
                }

                if (found) return true;
            }

            return false;
        }

        private static float SampleSectorAngle(int sector, float rearAngle, float threatAngle)
        {
            switch (sector)
            {
                case 0:
                    return rearAngle + Random.Range(-Mathf.PI * 0.5f, Mathf.PI * 0.5f);
                case 1:
                    float side = Random.value < 0.5f ? -1f : 1f;
                    return rearAngle + side * Random.Range(Mathf.PI * 0.5f, Mathf.PI * 0.75f);
                default:
                    return threatAngle + Random.Range(-Mathf.PI * 0.25f, Mathf.PI * 0.25f);
            }
        }

        public void ReserveDefenseLanding(object token, PlayerId owner, ShipType shipType, Vector3 position) =>
            _clearance.ReserveLanding(token, owner, shipType, position);

        public void ReleaseDefenseLanding(object token) => _clearance.ReleaseLanding(token);

        private void CollectStationPositions(PlayerId owner)
        {
            _stationPositions.Clear();
            foreach (IEntity entity in _entities.Entities)
                if (entity.Owner == owner && entity.IsPlayerBase() && !entity.HealthModel.IsDestroyed)
                    _stationPositions.Add(entity.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        private bool TryFindOpenPosition(PlayerId owner, ShipType shipType, float searchRadius, out Vector3 position)
        {
            if (_anchors.Count > 0)
            {
                for (int attempt = 0; attempt < MAX_RANDOM_SPAWN_ATTEMPTS; attempt++)
                {
                    Vector2 offset = Random.insideUnitCircle * searchRadius;
                    position = _anchors[Random.Range(0, _anchors.Count)] + new Vector3(offset.x, 0f, offset.y);
                    position.y = 0f;
                    if (_spawnRule.CanSpawnShip(owner, shipType, position)) return true;
                }
            }

            position = default;
            return false;
        }
    }
}
