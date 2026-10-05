using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Chooses where an AI builds a structure: rings around its station, then around its captured relays.
    /// Every candidate must pass the same structure spawn rule as a player placement.</summary>
    public sealed class EnemyStructurePlacementService : IEnemyStructurePlacementService
    {
        private const float MINIMUM_ANCHOR_DISTANCE = 64f;

        private const int RING_COUNT = 3;

        private const float RING_SPACING = 12f;

        private const int POSITIONS_PER_RING = 32;
        private const int MAX_RECENT_DESTROYED_POSITIONS = 10;

        private const float DESTROYED_POSITION_EXCLUSION_RADIUS = 1f;

        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IStructureSpawnClearance _structureClearance;

        private readonly PlayerSlot _owner;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly List<Bounds> _capturedZoneBounds = new List<Bounds>();
        private readonly Queue<Vector3> _recentDestroyedPositions = new Queue<Vector3>();

        private readonly Bounds _stationBounds;

        public EnemyStructurePlacementService(
            IReinforcementZonesSystem zones,
            IReinforcementSpawnRule spawnRule,
            IStructureSpawnClearance structureClearance,
            LazyInject<IMapModelObserver> mapModel,
            PlayerSlot owner,
            BoxCollider stationPrefab)
        {
            _owner = owner;
            _mapModel = mapModel;
            _zones = zones;
            _spawnRule = spawnRule;
            _structureClearance = structureClearance;
            _stationBounds = StructureSpawnClearance.GetSpawnBounds(stationPrefab);
        }

        public bool TryGetPosition(out Vector3 position)
        {
            Physics.SyncTransforms();
            Vector3 station = _mapModel.Value.GetStationPosition(_owner.Id);
            float stationRadius = new Vector2(_stationBounds.extents.x, _stationBounds.extents.z).magnitude;
            if (TryGetPositionNear(station + _stationBounds.center, stationRadius, out position))
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

            position = default;
            return false;
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

        private bool TryGetPositionNear(Vector3 anchor, float anchorRadius, out Vector3 position)
        {
            var bounds = _mapModel.Value.SizeRange;
            float clearance = _structureClearance.Radius;
            float firstRadius = Mathf.Max(MINIMUM_ANCHOR_DISTANCE,
                anchorRadius + clearance + RING_SPACING);
            for (int ring = 0; ring < RING_COUNT; ring++)
            {
                float radius = firstRadius + ring * RING_SPACING;
                for (int index = 0; index < POSITIONS_PER_RING; index++)
                {
                    float angle = index * (2f * Mathf.PI / POSITIONS_PER_RING);
                    Vector3 candidate = new Vector3(
                        anchor.x + Mathf.Cos(angle) * radius,
                        0f,
                        anchor.z + Mathf.Sin(angle) * radius);
                    if (candidate.x - clearance < bounds.Min.x ||
                        candidate.x + clearance > bounds.Max.x ||
                        candidate.z - clearance < bounds.Min.y ||
                        candidate.z + clearance > bounds.Max.y ||
                        IsNearRecentDestroyedPosition(candidate) ||
                        !_spawnRule.CanSpawnStructure(_owner.Id, candidate))
                    {
                        continue;
                    }

                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

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
