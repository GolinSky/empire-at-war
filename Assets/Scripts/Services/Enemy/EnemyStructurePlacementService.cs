using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ReinforcementZones;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyStructurePlacementService : IEnemyStructurePlacementService
    {
        // Covers the larger defense platform footprint, including its collider offset.
        private const float STRUCTURE_CLEARANCE = 16f;
        private const float MINIMUM_ANCHOR_DISTANCE = 64f;
        private const float MAXIMUM_ANCHOR_DISTANCE = 88f;
        private const float RING_SPACING = 12f;
        private const int POSITIONS_PER_RING = 32;
        private const int MAX_RECENT_DESTROYED_POSITIONS = 10;
        private const float DESTROYED_POSITION_EXCLUSION_RADIUS = 1f;

        private readonly EnemyFactionModel _factionModel;
        private readonly LazyInject<IMapModelObserver> _mapModel;
        private readonly IReinforcementZonesSystem _zones;
        private readonly int _obstacleMask;
        private readonly List<Vector3> _capturedZoneCenters = new List<Vector3>();
        private readonly Queue<Vector3> _recentDestroyedPositions = new Queue<Vector3>();

        public EnemyStructurePlacementService(
            EnemyFactionModel factionModel,
            LazyInject<IMapModelObserver> mapModel,
            IReinforcementZonesSystem zones,
            ILayerService layerService)
        {
            _factionModel = factionModel;
            _mapModel = mapModel;
            _zones = zones;

            _obstacleMask = layerService.GetMask(LayerKey.Player, LayerKey.Enemy, LayerKey.Obstacle);
        }

        public bool TryGetPosition(out Vector3 position)
        {
            Physics.SyncTransforms();
            Vector3 station = _mapModel.Value.GetStationPosition(_factionModel.FactionType);
            if (TryGetPositionNear(station, out position))
            {
                return true;
            }

            _zones.CopyOwnedCapturableZoneCenters(PlayerType.Opponent, _capturedZoneCenters);
            foreach (Vector3 center in _capturedZoneCenters)
            {
                if (TryGetPositionNear(center, out position))
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

        private bool TryGetPositionNear(Vector3 anchor, out Vector3 position)
        {
            var bounds = _mapModel.Value.SizeRange;
            for (float radius = MINIMUM_ANCHOR_DISTANCE;
                 radius <= MAXIMUM_ANCHOR_DISTANCE;
                 radius += RING_SPACING)
            {
                for (int index = 0; index < POSITIONS_PER_RING; index++)
                {
                    float angle = index * (2f * Mathf.PI / POSITIONS_PER_RING);
                    Vector3 candidate = new Vector3(
                        anchor.x + Mathf.Cos(angle) * radius,
                        0f,
                        anchor.z + Mathf.Sin(angle) * radius);
                    if (candidate.x - STRUCTURE_CLEARANCE < bounds.Min.x ||
                        candidate.x + STRUCTURE_CLEARANCE > bounds.Max.x ||
                        candidate.z - STRUCTURE_CLEARANCE < bounds.Min.y ||
                        candidate.z + STRUCTURE_CLEARANCE > bounds.Max.y ||
                        IsNearRecentDestroyedPosition(candidate) ||
                        _zones.IsPositionInAnyZone(candidate, STRUCTURE_CLEARANCE) ||
                        Physics.CheckSphere(candidate, STRUCTURE_CLEARANCE,
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
