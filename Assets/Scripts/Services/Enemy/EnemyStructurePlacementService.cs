using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyStructurePlacementService : IEnemyStructurePlacementService
    {
        private const float MINIMUM_ANCHOR_DISTANCE = 64f;
        private const int RING_COUNT = 3;
        private const float RING_SPACING = 12f;
        private const int POSITIONS_PER_RING = 32;
        private const int MAX_RECENT_DESTROYED_POSITIONS = 10;
        private const float DESTROYED_POSITION_EXCLUSION_RADIUS = 1f;

        private readonly PlayerSlot _owner;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly IReinforcementZonesSystem _zones;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly int _obstacleMask;
        private readonly Bounds _stationBounds;
        private readonly float _structureClearance;
        private readonly List<Bounds> _capturedZoneBounds = new List<Bounds>();
        private readonly Queue<Vector3> _recentDestroyedPositions = new Queue<Vector3>();

        public EnemyStructurePlacementService(
            LazyInject<IMapModelObserver> mapModel,
            IReinforcementZonesSystem zones,
            ICaptureSitesSystem captureSites,
            ILayerService layerService,
            PlayerSlot owner,
            BoxCollider stationPrefab,
            BoxCollider[] structurePrefabs)
        {
            _owner = owner;
            _mapModel = mapModel;
            _zones = zones;
            _captureSites = captureSites;
            _obstacleMask = layerService.GetMask(LayerKey.Unit, LayerKey.Obstacle);
            _stationBounds = GetSpawnBounds(stationPrefab);
            foreach (BoxCollider prefab in structurePrefabs)
            {
                Bounds bounds = GetSpawnBounds(prefab);
                Vector3 reach = bounds.extents + new Vector3(
                    Mathf.Abs(bounds.center.x), Mathf.Abs(bounds.center.y), Mathf.Abs(bounds.center.z));
                // Enclose the entire scaled collider, including its offset from the spawn pivot.
                _structureClearance = Mathf.Max(_structureClearance, reach.magnitude);
            }
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
            float firstRadius = Mathf.Max(MINIMUM_ANCHOR_DISTANCE,
                anchorRadius + _structureClearance + RING_SPACING);
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
                    if (candidate.x - _structureClearance < bounds.Min.x ||
                        candidate.x + _structureClearance > bounds.Max.x ||
                        candidate.z - _structureClearance < bounds.Min.y ||
                        candidate.z + _structureClearance > bounds.Max.y ||
                        IsNearRecentDestroyedPosition(candidate) ||
                        _zones.IsPositionInAnyZone(candidate, _structureClearance) ||
                        _captureSites.IsPositionInAnySite(candidate, _structureClearance) ||
                        Physics.CheckSphere(candidate, _structureClearance,
                            _obstacleMask, QueryTriggerInteraction.Ignore))
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

        private static Bounds GetSpawnBounds(BoxCollider prefab)
        {
            // Entity initialization replaces the prefab position, preserving its rotation and scale.
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero,
                prefab.transform.localRotation, prefab.transform.localScale);
            Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(prefab.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f);
                bounds.Encapsulate(matrix.MultiplyPoint3x4(
                    prefab.center + Vector3.Scale(prefab.size * 0.5f, sign)));
            }

            return bounds;
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
