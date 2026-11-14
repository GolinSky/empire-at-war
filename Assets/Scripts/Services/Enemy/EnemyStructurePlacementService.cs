using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.Stations;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Searches safe structure positions from the station or recovery anchor, then allied zones and
    /// visible map space. Unexplored safe space supplies scouting targets when building is blocked.</summary>
    public sealed class EnemyStructurePlacementService : IEnemyStructurePlacementService
    {
        private const float MINIMUM_ANCHOR_DISTANCE = 64f;

        private const int RING_COUNT = 3;

        private const float RING_SPACING = 12f;

        private const int POSITIONS_PER_RING = 32;

        /// <summary>Distance between rings and between candidates on a ring in the map-wide search.</summary>
        private const float WIDE_SEARCH_SPACING = 48f;
        private const int MAX_RECENT_DESTROYED_POSITIONS = 10;

        private const float DESTROYED_POSITION_EXCLUSION_RADIUS = 32f;

        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IVisionService _vision;
        private readonly IBattleAreaService _areas;

        private readonly PlayerSlot _owner;
        private readonly IMapModelObserver _mapModel;
        private readonly IProductionAccess _productionAccess;
        private readonly IEnemyRecoveryPolicy _recovery;
        private string _placementSource;
        private readonly List<Bounds> _capturedZoneBounds = new List<Bounds>();
        private readonly Queue<Vector3> _recentDestroyedPositions = new Queue<Vector3>();

        private readonly Bounds _stationBounds;
        private readonly float _placementRadius;

        public EnemyStructurePlacementService(
            IReinforcementZonesSystem zones,
            IReinforcementSpawnRule spawnRule,
            IStructureSpawnClearance structureClearance,
            IVisionService vision,
            IBattleAreaService areas,
            IMapModelObserver mapModel,
            PlayerSlot owner,
            BoxCollider stationPrefab,
            IProductionAccess productionAccess,
            IEnemyRecoveryPolicy recovery)
        {
            _owner = owner;
            _productionAccess = productionAccess;
            _recovery = recovery;
            _mapModel = mapModel;
            _zones = zones;
            _spawnRule = spawnRule;
            _vision = vision;
            _areas = areas;
            _stationBounds = StructureSpawnClearance.GetSpawnBounds(stationPrefab);
            Vector3 stationReach = _stationBounds.extents + new Vector3(
                Mathf.Abs(_stationBounds.center.x), Mathf.Abs(_stationBounds.center.y),
                Mathf.Abs(_stationBounds.center.z));
            _placementRadius = Mathf.Max(structureClearance.Radius, stationReach.magnitude);
        }

        public bool IsRecovering => _recovery.IsActive;

        public bool TryGetPosition(out Vector3 position)
        {
            Physics.SyncTransforms();
            if (_recovery.IsActive)
            {
                if (!_recovery.TryGetBuildAnchor(out Vector3 anchor))
                {
                    position = default;
                    return false;
                }
                _placementSource = "anchor";
                if (TryGetPositionNear(anchor, 0f, out position)) return true;
                _recovery.CopySafeAlliedZoneBounds(_capturedZoneBounds);
                foreach (Bounds zone in _capturedZoneBounds)
                {
                    _placementSource = "allyZone";
                    if (TryGetPositionNear(zone.center, zone.extents.x, out position)) return true;
                }
                _placementSource = "map";
                return TrySearchMap(anchor, 0f, IsPlaceable, out position);
            }
            _placementSource = "station";
            Vector3 station = GetStationCenter();
            float stationRadius = GetStationRadius();
            if (TryGetPositionNear(station, stationRadius, out position))
            {
                return true;
            }

            _zones.CopyOwnedCapturableZoneBounds(_owner.Id, _capturedZoneBounds);
            foreach (Bounds zone in _capturedZoneBounds)
            {
                _placementSource = "ownZone";
                if (TryGetPositionNear(zone.center, zone.extents.x, out position))
                {
                    return true;
                }
            }

            // Search remaining visible, safe space when local rings are occupied.
            _placementSource = "map";
            return TrySearchMap(station, stationRadius, IsPlaceable, out position);
        }

        public bool TryGetScoutTarget(out Vector3 position)
        {
            Vector3 origin = _recovery.IsActive && _recovery.TryGetBuildAnchor(out Vector3 anchor)
                ? anchor : GetStationCenter();
            return TrySearchMap(origin, GetStationRadius(), IsUnexplored, out position);
        }

        public void RecordPlacement(string request, Vector3 position)
        {
            string anchor = _recovery.TryGetBuildAnchor(out Vector3 point)
                ? FormattableString.Invariant($"({point.x:F1} {point.z:F1})") : "none";
            float destroyedDistance = float.PositiveInfinity;
            foreach (Vector3 destroyed in _recentDestroyedPositions)
                destroyedDistance = Mathf.Min(destroyedDistance, Vector2.Distance(
                    new Vector2(position.x, position.z), new Vector2(destroyed.x, destroyed.z)));
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "placement",
                FormattableString.Invariant($"request={request};pos=({position.x:F1} {position.z:F1});source={_placementSource};anchor={anchor};nearestHostile={_recovery.GetNearestHostileDistance(position):F1};nearestDestroyed={destroyedDistance:F1};recoveryMode={_recovery.IsActive}"));
        }

        public void RecordDeferred(string request, float refund) =>
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "placementDeferred",
                FormattableString.Invariant($"request={request};reason={(_recovery.IsActive ? _recovery.DeferredReason : "noSafeCandidate")};refund={refund:F1}"));

        public void RecordStationBuilt(Vector3 position) => _recovery.OnStationBuilt(position);

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
            _mapModel.GetStationPosition(_productionAccess.ProductionHost) + _stationBounds.center;

        private float GetStationRadius() =>
            new Vector2(_stationBounds.extents.x, _stationBounds.extents.z).magnitude;

        private bool IsPlaceable(Vector3 candidate) =>
            !IsNearRecentDestroyedPosition(candidate) &&
            _recovery.IsPlacementSafe(candidate, _placementRadius) &&
            _spawnRule.CanSpawnStructure(_owner.Id, candidate);

        /// <summary>Hidden by fog but not covered by a hostile or neutral blocker, so a scout can open it.</summary>
        private bool IsUnexplored(Vector3 candidate) =>
            !_vision.IsVisible(_owner.Id, candidate) && !_areas.IsBlocked(_owner.Id, candidate) &&
            _recovery.IsPlacementSafe(candidate, _placementRadius);

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
            Vector2 min = _mapModel.SizeRange.Min;
            Vector2 max = _mapModel.SizeRange.Max;
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
            var bounds = _mapModel.SizeRange;
            float clearance = _placementRadius;
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
            Mathf.Max(MINIMUM_ANCHOR_DISTANCE, anchorRadius + _placementRadius + RING_SPACING);

        private bool IsNearRecentDestroyedPosition(Vector3 candidate)
        {
            float exclusionRadiusSquared =
                Mathf.Pow(Mathf.Max(DESTROYED_POSITION_EXCLUSION_RADIUS, _placementRadius), 2f);
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
