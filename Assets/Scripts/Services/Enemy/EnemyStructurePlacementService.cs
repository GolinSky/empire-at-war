using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.Stations;
using Unity.Profiling;
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

        private static readonly ProfilerMarker _placementMarker = new ProfilerMarker("EnemyAI.Placement.TryGetPosition");
        private static readonly ProfilerMarker _nearMarker = new ProfilerMarker("EnemyAI.Placement.Near");
        private static readonly ProfilerMarker _zonesMarker = new ProfilerMarker("EnemyAI.Placement.Zones");
        private static readonly ProfilerMarker _mapMarker = new ProfilerMarker("EnemyAI.Placement.Map");
        private static readonly ProfilerMarker _scoutMarker = new ProfilerMarker("EnemyAI.Placement.ScoutTarget");
        private static readonly ProfilerMarker _safetyMarker = new ProfilerMarker("EnemyAI.Placement.IsPlacementSafe");
        private static readonly ProfilerMarker _snapshotMarker = new ProfilerMarker("EnemyAI.Placement.ThreatSnapshot");

        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementSpawnRule _spawnRule;
        private readonly IStructureSpawnClearance _structureClearance;
        private readonly IVisionService _vision;
        private readonly IBattleAreaService _areas;

        private readonly PlayerSlot _owner;
        private readonly IMapModelObserver _mapModel;
        private readonly IProductionAccess _productionAccess;
        private readonly IEnemyRecoveryPolicy _recovery;
        private readonly PlacementThreatSnapshot _threats = new PlacementThreatSnapshot();
        private readonly List<Bounds> _capturedZoneBounds = new List<Bounds>();
        private readonly Queue<Vector3> _recentDestroyedPositions = new Queue<Vector3>();

        private readonly Bounds _stationBounds;
        private readonly float _placementRadius;
        private readonly Func<Vector3, bool> _acceptPlacement;
        private readonly Func<Vector3, bool> _acceptScoutTarget;

        private string _placementSource;
        private int _placementFrame = -1;
        private int _scoutFrame = -1;
        private int _physicsFrame = -1;
        private bool _hasPlacement;
        private Vector3 _placement;
        private Vector3 _placementOrigin;
        private bool _placementRecovering;
        private bool _hasScoutTarget;
        private Vector3 _scoutTarget;
        private int _candidatesTested;
        private int _safetyChecks;

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
            _structureClearance = structureClearance;
            _vision = vision;
            _areas = areas;
            _stationBounds = StructureSpawnClearance.GetSpawnBounds(stationPrefab);
            Vector3 stationReach = _stationBounds.extents + new Vector3(
                Mathf.Abs(_stationBounds.center.x), Mathf.Abs(_stationBounds.center.y),
                Mathf.Abs(_stationBounds.center.z));
            _placementRadius = Mathf.Max(structureClearance.Radius, stationReach.magnitude);
            _acceptPlacement = IsPlaceable;
            _acceptScoutTarget = IsUnexplored;
        }

        public bool IsRecovering => _recovery.IsActive;

        public bool TryGetPosition(out Vector3 position)
        {
            using var sample = _placementMarker.Auto();
            int frame = Time.frameCount;
            if (Application.isPlaying && _placementFrame == frame)
            {
                position = _placement;
                return _hasPlacement;
            }

            _hasPlacement = SearchPlacement(out position);
            _placement = position;
            _placementFrame = frame;
            return _hasPlacement;
        }

        private bool SearchPlacement(out Vector3 position)
        {
            bool recovering = _recovery.IsActive;
            Vector3 origin;
            if (recovering)
            {
                if (!_recovery.TryGetBuildAnchor(out origin))
                {
                    position = default;
                    return false;
                }
            }
            else
            {
                origin = GetStationCenter();
            }

            PrepareSearch();
            if (_hasPlacement && _placementRecovering == recovering && _placementOrigin == origin &&
                InBounds(_placement, _mapModel.SizeRange) && IsPlaceable(_placement))
            {
                position = _placement;
                return true;
            }

            _placementOrigin = origin;
            _placementRecovering = recovering;
            float originRadius = recovering ? 0f : GetStationRadius();
            _placementSource = recovering ? "anchor" : "station";
            if (TryGetPositionNear(origin, originRadius, out position)) return true;

            using (_zonesMarker.Auto())
            {
                if (recovering)
                    _recovery.CopySafeAlliedZoneBounds(_capturedZoneBounds);
                else
                    _zones.CopyOwnedCapturableZoneBounds(_owner.Id, _capturedZoneBounds);

                _placementSource = recovering ? "allyZone" : "ownZone";
                foreach (Bounds zone in _capturedZoneBounds)
                {
                    if (TryGetPositionNear(zone.center, zone.extents.x, out position)) return true;
                }
            }

            _placementSource = "map";
            return TrySearchMap(origin, originRadius, _acceptPlacement, "placement", out position);
        }

        public bool TryGetScoutTarget(out Vector3 position)
        {
            using var sample = _scoutMarker.Auto();
            int frame = Time.frameCount;
            if (Application.isPlaying && _scoutFrame == frame)
            {
                position = _scoutTarget;
                return _hasScoutTarget;
            }

            Vector3 origin = _recovery.IsActive && _recovery.TryGetBuildAnchor(out Vector3 anchor)
                ? anchor : GetStationCenter();
            PrepareSearch();
            _hasScoutTarget = TrySearchMap(origin, GetStationRadius(), _acceptScoutTarget, "scout", out position);
            _scoutTarget = position;
            _scoutFrame = frame;
            return _hasScoutTarget;
        }

        private void PrepareSearch()
        {
            _candidatesTested = 0;
            _safetyChecks = 0;
            if (!Application.isPlaying) _physicsFrame = -1;
            using var sample = _snapshotMarker.Auto();
            _recovery.FillPlacementThreats(_threats);
        }

        private bool IsClear(Vector3 candidate)
        {
            int frame = Time.frameCount;
            if (_physicsFrame != frame)
            {
                Physics.SyncTransforms();
                _physicsFrame = frame;
            }

            return _structureClearance.IsClear(candidate);
        }

        private void InvalidateSearches()
        {
            _placementFrame = -1;
            _scoutFrame = -1;
            _physicsFrame = -1;
        }

        public void RecordPlacement(string request, Vector3 position)
        {
            InvalidateSearches();
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

        public void RecordStationBuilt(Vector3 position)
        {
            InvalidateSearches();
            _recovery.OnStationBuilt(position);
        }

        public void RecordDestroyedPosition(Vector3 position)
        {
            InvalidateSearches();
            if (_recentDestroyedPositions.Count == MAX_RECENT_DESTROYED_POSITIONS)
            {
                _recentDestroyedPositions.Dequeue();
            }

            _recentDestroyedPositions.Enqueue(position);
        }

        public void Reset()
        {
            InvalidateSearches();
            _hasPlacement = false;
            _recentDestroyedPositions.Clear();
        }

        private Vector3 GetStationCenter() =>
            _mapModel.GetStationPosition(_productionAccess.ProductionHost) + _stationBounds.center;

        private float GetStationRadius() =>
            new Vector2(_stationBounds.extents.x, _stationBounds.extents.z).magnitude;

        private bool IsPlaceable(Vector3 candidate) =>
            !IsNearRecentDestroyedPosition(candidate) &&
            _spawnRule.IsOpenForStructure(_owner.Id, candidate) &&
            IsPlacementSafe(candidate) &&
            IsClear(candidate);

        /// <summary>Hidden by fog but not covered by a hostile or neutral blocker, so a scout can open it.</summary>
        private bool IsUnexplored(Vector3 candidate) =>
            !_vision.IsVisible(_owner.Id, candidate) && !_areas.IsBlocked(_owner.Id, candidate) &&
            IsPlacementSafe(candidate);

        private bool IsPlacementSafe(Vector3 candidate)
        {
            using var sample = _safetyMarker.Auto();
            _safetyChecks++;
            return _threats.IsSafe(candidate.x, candidate.z, _placementRadius);
        }

        private bool TryGetPositionNear(Vector3 anchor, float anchorRadius, out Vector3 position)
        {
            using var sample = _nearMarker.Auto();
            float firstRadius = GetFirstRingRadius(anchorRadius);
            for (int ring = 0; ring < RING_COUNT; ring++)
            {
                if (TryGetPositionOnRing(anchor, firstRadius + ring * RING_SPACING, POSITIONS_PER_RING,
                        _acceptPlacement, out position))
                {
                    return true;
                }
            }

            position = default;
            return false;
        }

        /// <summary>Rings growing outward from the station until they leave the map; the nearest match wins.</summary>
        private bool TrySearchMap(Vector3 origin, float originRadius, Func<Vector3, bool> accept, string search,
            out Vector3 position)
        {
            using var sample = _mapMarker.Auto();
            Vector2 min = _mapModel.SizeRange.Min;
            Vector2 max = _mapModel.SizeRange.Max;
            float maxRadius = Vector2.Distance(min, max);
            int rings = 0;
            for (float radius = GetFirstRingRadius(originRadius); radius <= maxRadius; radius += WIDE_SEARCH_SPACING)
            {
                rings++;
                int count = Mathf.Max(POSITIONS_PER_RING,
                    Mathf.CeilToInt(2f * Mathf.PI * radius / WIDE_SEARCH_SPACING));
                if (TryGetPositionOnRing(origin, radius, count, accept, out position))
                {
                    RecordWideSearch(search, rings, true);
                    return true;
                }
            }

            position = default;
            RecordWideSearch(search, rings, false);
            return false;
        }

        private void RecordWideSearch(string search, int rings, bool found)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (rings <= RING_COUNT || !Application.isPlaying) return;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "placement-search",
                $"search={search};rings={rings};candidates={_candidatesTested};safetyChecks={_safetyChecks};source={(found ? "map" : "none")}");
#endif
        }

        private bool TryGetPositionOnRing(Vector3 anchor, float radius, int count, Func<Vector3, bool> accept,
            out Vector3 position)
        {
            var bounds = _mapModel.SizeRange;
            for (int index = 0; index < count; index++)
            {
                float angle = index * (2f * Mathf.PI / count);
                Vector3 candidate = new Vector3(
                    anchor.x + Mathf.Cos(angle) * radius,
                    0f,
                    anchor.z + Mathf.Sin(angle) * radius);
                if (!InBounds(candidate, bounds)) continue;
                _candidatesTested++;
                if (!accept(candidate))
                {
                    continue;
                }

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private bool InBounds(Vector3 candidate, Vector2Range bounds) =>
            candidate.x - _placementRadius >= bounds.Min.x &&
            candidate.x + _placementRadius <= bounds.Max.x &&
            candidate.z - _placementRadius >= bounds.Min.y &&
            candidate.z + _placementRadius <= bounds.Max.y;

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
