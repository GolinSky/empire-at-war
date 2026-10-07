using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Chooses where an AI builds a structure: rings around its station, then around its captured relays,
    /// then the nearest open spot anywhere it can see. Every candidate must pass the same structure spawn rule as a
    /// player placement. When nothing visible is open, it names the nearest unexplored, unblocked spot to scout.</summary>
    public sealed class EnemyStructurePlacementService : IEnemyStructurePlacementService
    {
        private const float MINIMUM_ANCHOR_DISTANCE = 64f;

        private const int RING_COUNT = 3;

        private const float RING_SPACING = 12f;

        private const int POSITIONS_PER_RING = 32;

        /// <summary>Distance between rings and between candidates on a ring in the map-wide search.</summary>
        private const float WIDE_SEARCH_SPACING = 48f;
        private const int MAX_RECENT_DESTROYED_POSITIONS = 10;

        private const float DESTROYED_POSITION_EXCLUSION_RADIUS = 1f;

        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IStructureSpawnClearance _structureClearance;
        private readonly IVisionService _vision;
        private readonly ISpawnBlockerService _blockers;

        private readonly PlayerSlot _owner;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly List<Bounds> _capturedZoneBounds = new List<Bounds>();
        private readonly Queue<Vector3> _recentDestroyedPositions = new Queue<Vector3>();

        private readonly Bounds _stationBounds;

        public EnemyStructurePlacementService(
            IReinforcementZonesSystem zones,
            IReinforcementSpawnRule spawnRule,
            IStructureSpawnClearance structureClearance,
            IVisionService vision,
            ISpawnBlockerService blockers,
            LazyInject<IMapModelObserver> mapModel,
            PlayerSlot owner,
            BoxCollider stationPrefab)
        {
            _owner = owner;
            _mapModel = mapModel;
            _zones = zones;
            _spawnRule = spawnRule;
            _structureClearance = structureClearance;
            _vision = vision;
            _blockers = blockers;
            _stationBounds = StructureSpawnClearance.GetSpawnBounds(stationPrefab);
        }

        public bool TryGetPosition(out Vector3 position)
        {
            Physics.SyncTransforms();
            Vector3 station = GetStationCenter();
            float stationRadius = GetStationRadius();
            if (TryGetPositionNear(station, stationRadius, out position))
            {
                return true;
            }

            _zones.CopyOwnedCapturableZoneBounds(_owner.Id, _capturedZoneBounds);
            foreach (Bounds zone in _capturedZoneBounds)
            {
                if (TryGetPositionNear(zone.center, zone.extents.x, out position))
                {
                    return true;
                }
            }

            // Neutral relays block building around some stations; any spot the AI sees is better than none.
            return TrySearchMap(station, stationRadius, IsPlaceable, out position);
        }

        public bool TryGetScoutTarget(out Vector3 position)
        {
            return TrySearchMap(GetStationCenter(), GetStationRadius(), IsUnexplored, out position);
        }

        public void RecordDestroyedPosition(Vector3 position)
        {
            if (_recentDestroyedPositions.Count == MAX_RECENT_DESTROYED_POSITIONS)
            {
                _recentDestroyedPositions.Dequeue();
            }

            _recentDestroyedPositions.Enqueue(position);
        }

        public void Reset()
        {
            _recentDestroyedPositions.Clear();
        }

        private Vector3 GetStationCenter() =>
            _mapModel.Value.GetStationPosition(_owner.Id) + _stationBounds.center;

        private float GetStationRadius() =>
            new Vector2(_stationBounds.extents.x, _stationBounds.extents.z).magnitude;

        private bool IsPlaceable(Vector3 candidate) =>
            !IsNearRecentDestroyedPosition(candidate) && _spawnRule.CanSpawnStructure(_owner.Id, candidate);

        /// <summary>Hidden by fog but not covered by a hostile or neutral blocker, so a scout can open it.</summary>
        private bool IsUnexplored(Vector3 candidate) =>
            !_vision.IsVisible(_owner.Id, candidate) && !_blockers.IsBlocked(_owner.Id, candidate);

        private bool TryGetPositionNear(Vector3 anchor, float anchorRadius, out Vector3 position)
        {
            float firstRadius = GetFirstRingRadius(anchorRadius);
            for (int ring = 0; ring < RING_COUNT; ring++)
            {
                if (TryGetPositionOnRing(anchor, firstRadius + ring * RING_SPACING, POSITIONS_PER_RING,
                        IsPlaceable, out position))
                {
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>Rings growing outward from the station until they leave the map; the nearest match wins.</summary>
        private bool TrySearchMap(Vector3 origin, float originRadius, Func<Vector3, bool> accept,
            out Vector3 position)
        {
            Vector2 min = _mapModel.Value.SizeRange.Min;
            Vector2 max = _mapModel.Value.SizeRange.Max;
            float maxRadius = Vector2.Distance(min, max);
            for (float radius = GetFirstRingRadius(originRadius); radius <= maxRadius; radius += WIDE_SEARCH_SPACING)
            {
                int count = Mathf.Max(POSITIONS_PER_RING,
                    Mathf.CeilToInt(2f * Mathf.PI * radius / WIDE_SEARCH_SPACING));
                if (TryGetPositionOnRing(origin, radius, count, accept, out position))
                {
                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool TryGetPositionOnRing(Vector3 anchor, float radius, int count, Func<Vector3, bool> accept,
            out Vector3 position)
        {
            var bounds = _mapModel.Value.SizeRange;
            float clearance = _structureClearance.Radius;
            for (int index = 0; index < count; index++)
            {
                float angle = index * (2f * Mathf.PI / count);
                Vector3 candidate = new Vector3(
                    anchor.x + Mathf.Cos(angle) * radius,
                    0f,
                    anchor.z + Mathf.Sin(angle) * radius);
                if (candidate.x - clearance < bounds.Min.x ||
                    candidate.x + clearance > bounds.Max.x ||
                    candidate.z - clearance < bounds.Min.y ||
                    candidate.z + clearance > bounds.Max.y ||
                    !accept(candidate))
                {
                    continue;
                }

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private float GetFirstRingRadius(float anchorRadius) =>
            Mathf.Max(MINIMUM_ANCHOR_DISTANCE, anchorRadius + _structureClearance.Radius + RING_SPACING);

        private bool IsNearRecentDestroyedPosition(Vector3 candidate)
        {
            float exclusionRadiusSquared =
                DESTROYED_POSITION_EXCLUSION_RADIUS * DESTROYED_POSITION_EXCLUSION_RADIUS;
            foreach (Vector3 destroyedPosition in _recentDestroyedPositions)
            {
                if ((candidate - destroyedPosition).sqrMagnitude <= exclusionRadiusSquared)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
