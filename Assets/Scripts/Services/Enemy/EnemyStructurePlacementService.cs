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
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Saves;

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
        private const int CENTRE_PLATFORMS = 2;
        private const int ZONE_PLATFORMS = 1;
        /// <summary>Station ring: one platform from level 2, two from level 4.</summary>
        private const int STATION_PLATFORM_LEVEL = 2;
        private const int STATION_SECOND_PLATFORM_LEVEL = 4;
        /// <summary>Second station: a ring around the home station, on the half away from the map centre.</summary>
        private const float SECOND_STATION_MIN_DISTANCE = 400f;
        private const float SECOND_STATION_MAX_DISTANCE = 700f;
        private const float SECOND_STATION_RING_SPACING = 100f;
        /// <summary>A lost own base closes its station radius plus this distance for the rest of the battle.</summary>
        private const float LOST_BASE_CLOSED_MARGIN = 400f;
        /// <summary>No miner while a known hostile stands this close to the living base.</summary>
        private const float MINER_HOSTILE_RANGE = 1000f;

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
        private readonly EnemyFactionModel _factionModel;
        private readonly IPlayerRelations _relations;
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
        private readonly List<Vector3> _lostBases = new List<Vector3>();
        private IEntity _trackedBase;
        private Vector3 _trackedBaseCenter;
        private float _baseLostAt = float.NegativeInfinity;
        private Vector2? _awayFrom;

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
            EnemyFactionModel factionModel,
            IPlayerRelations relations)
        {
            _owner = owner;
            _productionAccess = productionAccess;
            _recovery = recovery;
            _mapModel = mapModel;
            _zones = zones;
            _zoneSource = zoneSource;
            _world = world;
            _factionModel = factionModel;
            _relations = relations;
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

        /// <summary>
        /// A living own base stands, no known hostile is within <see cref="MINER_HOSTILE_RANGE"/> of it, and the
        /// turtle after a lost base (<see cref="EnemyShipTaskGroupPlanner.TURTLE_SECONDS"/>) is over.
        /// </summary>
        public bool CanPlaceMiner
        {
            get
            {
                if (_recovery.IsActive) return true;
                if (!TryGetStationCenter(out Vector3 centre) ||
                    Time.time < _baseLostAt + EnemyShipTaskGroupPlanner.TURTLE_SECONDS) return false;
                PrepareSearch();
                return !_threats.HasThreatWithin(centre.x, centre.z, MINER_HOSTILE_RANGE);
            }
        }

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
            // The home station ring first, then every held zone gets its first platform before any zone gets another.
            _placementSource = "stationRing";
            if (TryGetStationRingPosition(out position)) return true;
            _placementSource = "heldZone";
            for (int platforms = 0; platforms < MAX_ZONE_PLATFORMS; platforms++)
                if (TryGetZonePosition(platforms, allowExtra, out position)) return true;
            RecordDefenseBlocked(allowExtra);
            position = default;
            return false;
        }

        /// <summary>Platforms ringing the home station: one from level 2, two from level 4.</summary>
        private bool TryGetStationRingPosition(out Vector3 position)
        {
            position = default;
            if (!_productionAccess.HasHostBase || _productionAccess.ProductionHost != _owner.Id) return false;
            int level = _factionModel.CurrentLevel;
            int wanted = level >= STATION_SECOND_PLATFORM_LEVEL ? 2 : level >= STATION_PLATFORM_LEVEL ? 1 : 0;
            if (!TryGetStationCenter(out Vector3 station)) return false;
            float radius = GetStationRadius();
            if (CountPlatforms(station, GetFirstRingRadius(radius) + RING_COUNT * RING_SPACING) >= wanted)
                return false;
            return TryGetPositionNear(station, radius, out position);
        }

        /// <summary>Centre first, then captured relays, then home zones, among held zones with exactly <paramref name="platforms"/> platforms.</summary>
        private bool TryGetZonePosition(int platforms, bool allowExtra, out Vector3 position)
        {
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zoneSource.Zones);
            if (centre != null &&
                TryGetHeldZonePosition(centre, platforms, GetZoneLimit(centre, centre, allowExtra), out position))
                return true;
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
                if (zone != centre && zone.IsCapturable &&
                    TryGetHeldZonePosition(zone, platforms, GetZoneLimit(zone, centre, allowExtra), out position))
                    return true;
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
                if (!zone.IsCapturable &&
                    TryGetHeldZonePosition(zone, platforms, GetZoneLimit(zone, centre, allowExtra), out position))
                    return true;
            position = default;
            return false;
        }

        /// <summary>2 at the centre, 1 elsewhere; up to <see cref="MAX_ZONE_PLATFORMS"/> while ships wait on the unit limit.</summary>
        private static int GetZoneLimit(ReinforcementZonePresenter zone, ReinforcementZonePresenter centre,
            bool allowExtra) =>
            allowExtra ? MAX_ZONE_PLATFORMS : zone == centre ? CENTRE_PLATFORMS : ZONE_PLATFORMS;

        // A contested zone still gets its platform: every candidate point passes the placement safety test.
        private bool TryGetHeldZonePosition(ReinforcementZonePresenter zone, int platforms, int limit,
            out Vector3 position)
        {
            position = default;
            if (zone.Owner != _owner.Id || platforms >= limit || CountZonePlatforms(zone) != platforms) return false;

            // Place around the relay so the platform covers it without blocking the capture sites.
            return TryGetPositionNear(zone.Center, zone.Radius, out position);
        }

        // The placement rings start outside the zone, so the platforms of this zone are found within their reach.
        private int CountZonePlatforms(ReinforcementZonePresenter zone) =>
            CountPlatforms(zone.Center,
                Mathf.Max(2f * zone.Radius, GetFirstRingRadius(zone.Radius) + RING_COUNT * RING_SPACING));

        /// <summary>Own purchasable platforms within reach; satellites and battle asteroids are capture-site facilities.</summary>
        private int CountPlatforms(Vector3 anchor, float reach)
        {
            Vector2 centre = new Vector2(anchor.x, anchor.z);
            int count = 0;
            foreach (AiWorldSnapshot.Entry structure in _world.Structures)
                if (structure.Owner == _owner.Id &&
                    Vector2.Distance(centre, new Vector2(structure.Position.x, structure.Position.z)) <= reach &&
                    structure.Entity.TryGetFacade(out IDefendPlatformTypeFacade platform) &&
                    _factionModel.DefendPlatforms.ContainsKey(platform.Type)) count++;
            return count;
        }

        /// <summary>Why each held zone below its platform limit offered no position; at most one report per interval.</summary>
        private void RecordDefenseBlocked(bool allowExtra)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying || Time.time < _nextDefenseBlockedReport) return;
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zoneSource.Zones);
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
            {
                if (zone.Owner != _owner.Id) continue;
                int limit = GetZoneLimit(zone, centre, allowExtra);
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

        /// <summary>
        /// A ring of <see cref="SECOND_STATION_MIN_DISTANCE"/>–<see cref="SECOND_STATION_MAX_DISTANCE"/> around the
        /// home station on the half away from the map centre (the corner side); the point farthest from known
        /// hostiles wins. None there → the own held outer zone nearest home. Never nearer a hostile station than the
        /// home station is.
        /// </summary>
        public bool TryGetSecondStationPosition(out Vector3 position)
        {
            PrepareSearch();
            _world.EnsureCurrent();
            if (!TryGetStationCenter(out Vector3 home))
            {
                position = default;
                return false;
            }
            Vector2 mapCentre = (_mapModel.SizeRange.Min + _mapModel.SizeRange.Max) * 0.5f;
            Vector2 corner = new Vector2(home.x - mapCentre.x, home.z - mapCentre.y);
            bool found = false;
            float bestDistance = float.NegativeInfinity;
            position = default;
            for (float radius = SECOND_STATION_MIN_DISTANCE; radius <= SECOND_STATION_MAX_DISTANCE;
                 radius += SECOND_STATION_RING_SPACING)
            {
                int count = Mathf.Max(POSITIONS_PER_RING, Mathf.CeilToInt(2f * Mathf.PI * radius / WIDE_SEARCH_SPACING));
                for (int index = 0; index < count; index++)
                {
                    float angle = index * (2f * Mathf.PI / count);
                    Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (Vector2.Dot(offset, corner) <= 0f) continue;
                    Vector3 candidate = new Vector3(home.x + offset.x, 0f, home.z + offset.y);
                    if (!InBounds(candidate, _mapModel.SizeRange) || !IsSecondStationPoint(candidate, home))
                        continue;
                    float hostileDistance = _recovery.GetNearestHostileDistance(candidate);
                    if (hostileDistance <= bestDistance) continue;
                    bestDistance = hostileDistance;
                    position = candidate;
                    found = true;
                }
            }
            if (found)
            {
                _placementSource = "homeCorner";
                return true;
            }

            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zoneSource.Zones);
            ReinforcementZonePresenter nearest = null;
            foreach (ReinforcementZonePresenter zone in _zoneSource.Zones)
                if (zone != centre && zone.IsCapturable && zone.Owner == _owner.Id &&
                    (nearest == null || PlanarDistanceSquared(zone.Center, home) <
                        PlanarDistanceSquared(nearest.Center, home)))
                    nearest = zone;
            _placementSource = "heldZone";
            if (nearest != null && TryGetPositionNear(nearest.Center, nearest.Radius, out position) &&
                !IsNearerHostileStation(position, home))
                return true;
            position = default;
            return false;
        }

        private bool IsSecondStationPoint(Vector3 candidate, Vector3 home) =>
            IsPlaceable(candidate) && !IsNearerHostileStation(candidate, home);

        private bool IsNearerHostileStation(Vector3 candidate, Vector3 home)
        {
            foreach (AiWorldSnapshot.Entry station in _world.Bases)
                if (_relations.IsHostile(_owner.Id, station.Owner) &&
                    PlanarDistanceSquared(candidate, station.Position) < PlanarDistanceSquared(home, station.Position))
                    return true;
            return false;
        }

        private static float PlanarDistanceSquared(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
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
            else if (!TryGetStationCenter(out origin))
            {
                position = default;
                return false;
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
            // Around the base, only the half facing away from known hostiles.
            if (!recovering && _threats.HasThreatWithin(origin.x, origin.z, 2f * MINER_HOSTILE_RANGE))
            {
                var hostile = _threats.GetLocalCentroid(origin.x, origin.z, 2f * MINER_HOSTILE_RANGE);
                _awayFrom = new Vector2(hostile.X - origin.x, hostile.Z - origin.z);
            }
            bool nearOrigin = TryGetPositionNear(origin, originRadius, out position);
            _awayFrom = null;
            if (nearOrigin) return true;

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

            Vector3 origin;
            if (_recovery.IsActive && _recovery.TryGetBuildAnchor(out Vector3 anchor)) origin = anchor;
            else if (!TryGetStationCenter(out origin))
            {
                position = default;
                _hasScoutTarget = false;
                _scoutTarget = position;
                _scoutFrame = frame;
                return false;
            }
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
            _lostBases.Clear();
            _trackedBase = null;
            _baseLostAt = float.NegativeInfinity;
        }

        /// <summary>
        /// Centre of the host's living production base (the station that builds now). A base seen here before and
        /// destroyed since closes its area (<see cref="IsInLostBaseArea"/>) and starts the miner pause.
        /// </summary>
        private bool TryGetStationCenter(out Vector3 centre)
        {
            bool hasBase = _productionAccess.TryGetProductionBase(out IEntity productionBase) &&
                           productionBase.Owner == _productionAccess.ProductionHost;
            if (_trackedBase != null && (!hasBase || productionBase != _trackedBase) &&
                _trackedBase.HealthModel.IsDestroyed)
            {
                _lostBases.Add(_trackedBaseCenter);
                _baseLostAt = Time.time;
                InvalidateSearches();
                _hasPlacement = false;
            }
            if (!hasBase)
            {
                _trackedBase = null;
                centre = default;
                return false;
            }
            centre = productionBase.GetFacade<IEntityTransformFacade>().Transform.position + _stationBounds.center;
            _trackedBase = productionBase;
            _trackedBaseCenter = centre;
            return true;
        }

        private bool IsInLostBaseArea(Vector3 candidate)
        {
            float closed = GetStationRadius() + LOST_BASE_CLOSED_MARGIN;
            foreach (Vector3 lost in _lostBases)
                if (PlanarDistanceSquared(candidate, lost) <= closed * closed) return true;
            return false;
        }

        private bool IsAwayFromHostiles(Vector3 candidate, Vector3 anchor) =>
            _awayFrom == null ||
            Vector2.Dot(new Vector2(candidate.x - anchor.x, candidate.z - anchor.z), _awayFrom.Value) <= 0f;

        private float GetStationRadius() =>
            new Vector2(_stationBounds.extents.x, _stationBounds.extents.z).magnitude;

        private bool IsPlaceable(Vector3 candidate) =>
            !IsNearRecentDestroyedPosition(candidate) &&
            !IsInLostBaseArea(candidate) &&
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
                if (!InBounds(candidate, bounds) || !IsAwayFromHostiles(candidate, anchor)) continue;
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
