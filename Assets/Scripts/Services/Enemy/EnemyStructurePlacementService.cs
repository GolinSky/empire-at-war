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
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Saves;
using static EmpireAtWar.Utils.FormationConversion;

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

        private const int MAX_ZONE_PLATFORMS = 3;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float DEFENSE_BLOCKED_REPORT_INTERVAL = 30f;
        private float _nextDefenseBlockedReport;
#endif

        private static readonly ProfilerMarker _placementMarker = new ProfilerMarker("EnemyAI.Placement.TryGetPosition");
        private static readonly ProfilerMarker _nearMarker = new ProfilerMarker("EnemyAI.Placement.Near");
        private static readonly ProfilerMarker _zonesMarker = new ProfilerMarker("EnemyAI.Placement.Zones");
        private static readonly ProfilerMarker _mapMarker = new ProfilerMarker("EnemyAI.Placement.Map");
        private static readonly ProfilerMarker _scoutMarker = new ProfilerMarker("EnemyAI.Placement.ScoutTarget");
        private static readonly ProfilerMarker _safetyMarker = new ProfilerMarker("EnemyAI.Placement.IsPlacementSafe");
        private static readonly ProfilerMarker _snapshotMarker = new ProfilerMarker("EnemyAI.Placement.ThreatSnapshot");

        private readonly IReinforcementZonesSystem _zones;
        private readonly IReinforcementZoneSource _zoneSource;
        private readonly AiWorldSnapshot _world;
        private readonly EnemyPatrolService _patrol;
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
            IEnemyRecoveryPolicy recovery,
            IReinforcementZoneSource zoneSource,
            AiWorldSnapshot world,
            EnemyPatrolService patrol)
        {
            _owner = owner;
            _productionAccess = productionAccess;
            _recovery = recovery;
            _mapModel = mapModel;
            _zones = zones;
            _zoneSource = zoneSource;
            _world = world;
            _patrol = patrol;
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

        public bool TryGetDefensePosition(bool allowExtra, out Vector3 position)
        {
            PrepareSearch();
            _world.EnsureCurrent();
            _placementSource = "heldZone";
            // Every held zone gets its first platform before any zone gets another.
            int limit = allowExtra ? MAX_ZONE_PLATFORMS : 1;
            for (int platforms = 0; platforms < limit; platforms++)
                if (TryGetZonePosition(platforms, out position)) return true;
            RecordDefenseBlocked(limit);
            position = default;
            return false;
        }

        /// <summary>Centre first, then captured relays, then home zones, among held zones with exactly <paramref name="platforms"/> platforms.</summary>
        private bool TryGetZonePosition(int platforms, out Vector3 position)
        {
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zoneSource.Zones);
            if (centre != null && TryGetHeldZonePosition(centre, platforms, out position)) return true;
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
                if (zone != centre && zone.IsCapturable && TryGetHeldZonePosition(zone, platforms, out position)) return true;
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
                if (!zone.IsCapturable && TryGetHeldZonePosition(zone, platforms, out position)) return true;
            position = default;
            return false;
        }

        private bool TryGetHeldZonePosition(ReinforcementZonePresenter zone, int platforms, out Vector3 position)
        {
            position = default;
            if (zone.Owner != _owner.Id || zone.IsContested || CountZonePlatforms(zone) != platforms) return false;

            // Place around the relay so the platform covers it without blocking the capture sites.
            return TryGetPositionNear(zone.Center, zone.Radius, out position);
        }

        private int CountZonePlatforms(ReinforcementZonePresenter zone)
        {
            // The placement rings start outside the zone, so the platforms of this zone are found within their reach.
            float reach = Mathf.Max(2f * zone.Radius, GetFirstRingRadius(zone.Radius) + RING_COUNT * RING_SPACING);
            Vector2 centre = new Vector2(zone.Center.x, zone.Center.z);
            int count = 0;
            foreach (AiWorldSnapshot.Entry structure in _world.Structures)
                if (structure.Owner == _owner.Id &&
                    Vector2.Distance(centre, new Vector2(structure.Position.x, structure.Position.z)) <= reach &&
                    structure.Entity.TryGetFacade(out IStructureKindFacade type) &&
                    type.Kind == StructureKind.DefendPlatform) count++;
            return count;
        }

        /// <summary>Why each held zone below its platform limit offered no position; at most one report per interval.</summary>
        private void RecordDefenseBlocked(int limit)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying || Time.time < _nextDefenseBlockedReport) return;
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
            {
                if (zone.Owner != _owner.Id || zone.IsContested) continue;
                int platforms = CountZonePlatforms(zone);
                if (platforms >= limit) continue;
                int outOfBounds = 0, notVisible = 0, blocked = 0, excluded = 0, unsafePositions = 0, occupied = 0;
                float firstRadius = GetFirstRingRadius(zone.Radius);
                for (int ring = 0; ring < RING_COUNT; ring++)
                for (int index = 0; index < POSITIONS_PER_RING; index++)
                {
                    float angle = index * (2f * Mathf.PI / POSITIONS_PER_RING);
                    float radius = firstRadius + ring * RING_SPACING;
                    Vector3 candidate = new Vector3(zone.Center.x + Mathf.Cos(angle) * radius, 0f,
                        zone.Center.z + Mathf.Sin(angle) * radius);
                    if (!InBounds(candidate, _mapModel.SizeRange)) outOfBounds++;
                    else if (!_vision.IsVisible(_owner.Id, candidate)) notVisible++;
                    else if (_areas.IsBlocked(_owner.Id, candidate)) blocked++;
                    else if (IsNearRecentDestroyedPosition(candidate) ||
                             !_spawnRule.IsOpenForStructure(_owner.Id, candidate)) excluded++;
                    else if (!IsPlacementSafe(candidate)) unsafePositions++;
                    else if (!IsClear(candidate)) occupied++;
                }
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "defensePlacementBlocked",
                    FormattableString.Invariant($"zone=({zone.Center.x:F1} {zone.Center.z:F1});main={zone.IsMain};capturable={zone.IsCapturable};platforms={platforms};limit={limit};firstRing={firstRadius:F1};candidates={RING_COUNT * POSITIONS_PER_RING};outOfBounds={outOfBounds};notVisible={notVisible};blocked={blocked};excluded={excluded};unsafe={unsafePositions};occupied={occupied}"));
                _nextDefenseBlockedReport = Time.time + DEFENSE_BLOCKED_REPORT_INTERVAL;
            }
#endif
        }

        public bool TryGetSecondStationPosition(out Vector3 position)
        {
            const float MINIMUM_HOME_DISTANCE = 1400f;
            const float MAX_SCOUT_AGE = 45f;
            PrepareSearch();
            Vector3 home = GetStationCenter();
            bool found = false;
            float bestScore = float.NegativeInfinity;
            position = default;
            bool hasHomeZone = _zones.TryGetDefaultZoneCenter(_owner.Id, out Vector3 homeZone);
            foreach (EnemyPatrolPoint point in _patrol.Memory.Entries)
            {
                if (point.LastSeenTime < 0f || point.Staleness(Time.time) > MAX_SCOUT_AGE) continue;
                Vector3 candidate = ToVector(point.Position);
                float distance = Vector3.Distance(home, candidate);
                if (distance < MINIMUM_HOME_DISTANCE || !IsPlaceable(candidate)) continue;
                if (hasHomeZone && Vector3.Distance(homeZone, candidate) < MINIMUM_HOME_DISTANCE) continue;
                bool ownedZone = false;
                foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
                    if (zone.Owner == _owner.Id && !zone.IsContested &&
                        EnemyShipTaskGroupPlanner.IsInZone(zone, candidate))
                    {
                        ownedZone = true;
                        break;
                    }
                float score = (ownedZone ? MINIMUM_HOME_DISTANCE : 0f) +
                    Mathf.Min(MINIMUM_HOME_DISTANCE, _recovery.GetNearestHostileDistance(candidate)) -
                    point.Staleness(Time.time);
                if (score <= bestScore) continue;
                position = candidate;
                bestScore = score;
                found = true;
            }
            if (found) _placementSource = "patrol";
            return found;
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
