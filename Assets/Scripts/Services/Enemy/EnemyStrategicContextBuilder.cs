using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Services.TacticalMarkers;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.Saves;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyStrategicContext
    {
        public EnemyStrategicSnapshot Snapshot { get; }
        public float StateHeldSeconds { get; internal set; }
        public IReadOnlyList<IShipEntity> Ships { get; }
        public IReadOnlyList<IShipEntity> AssaultCapitals { get; }
        public Vector3 CaptureTarget { get; }
        public GameEntity EnemyFleetTarget { get; }
        public GameEntity EnemyBaseTarget { get; }
        public GameEntity OwnBase { get; }
        /// <summary>Closest known hostile near the fleet, ships before squadrons; null when none.</summary>
        public GameEntity LocalThreatTarget { get; }
        /// <summary>Closest known undefended hostile structure; null when none.</summary>
        public GameEntity RaidTarget { get; }
        public EnemyCaptureObjective CaptureObjective { get; }
        public GameEntity CaptureBlockingFacility { get; }
        public GameEntity DefenseTarget { get; }
        public string StationPressureReason { get; }
        public EnemyStationPressureTracker StationPressure { get; }
        public Func<Vector3, float, bool> IsDefensePointClear { get; }
        public Vector3? OrderDestination { get; set; }
        public string DefensePointReason { get; set; } = "";
        public Vector3? RetreatAnchor { get; }
        public Vector3? GuardPosition { get; }
        public string RecoveryReason { get; }
        public IReadOnlyDictionary<IShipEntity, GameEntity> Receivers { get; }
        public IReadOnlyDictionary<IShipEntity, ForceComposition> ShipForces { get; }
        public IReadOnlyDictionary<GameEntity, ForceComposition> TargetForces { get; }
        public float VisibleAdvantage { get; }
        public float UnseenInvestment { get; }
        public float EconomyPressure { get; }
        public int HeldShips { get; }
        public int TeamHeldZones { get; }

        public EnemyStrategicContext(
            IReadOnlyList<IShipEntity> ships,
            GameEntity enemyFleetTarget,
            GameEntity enemyBaseTarget,
            GameEntity ownBase,
            EnemyStrategicSnapshot snapshot,
            Vector3 captureTarget,
            IReadOnlyDictionary<IShipEntity, ForceComposition> shipForces,
            IReadOnlyDictionary<GameEntity, ForceComposition> targetForces,
            IReadOnlyDictionary<IShipEntity, GameEntity> receivers = null,
            GameEntity localThreatTarget = null,
            GameEntity raidTarget = null,
            EnemyCaptureObjective captureObjective = null,
            GameEntity defenseTarget = null,
            string stationPressureReason = "",
            Vector3? retreatAnchor = null,
            string recoveryReason = "",
            EnemyStationPressureTracker stationPressure = null,
            Func<Vector3, float, bool> isDefensePointClear = null,
            GameEntity captureBlockingFacility = null,
            Vector3? guardPosition = null,
            IReadOnlyList<IShipEntity> assaultCapitals = null,
            float visibleAdvantage = 0f,
            float unseenInvestment = 0f,
            float economyPressure = 1f,
            int heldShips = 0,
            int teamHeldZones = 0)
        {
            Snapshot = snapshot;
            Ships = ships;
            AssaultCapitals = assaultCapitals;
            CaptureTarget = captureTarget;
            EnemyFleetTarget = enemyFleetTarget;
            EnemyBaseTarget = enemyBaseTarget;
            OwnBase = ownBase;
            Receivers = receivers;
            ShipForces = shipForces;
            TargetForces = targetForces;
            LocalThreatTarget = localThreatTarget;
            RaidTarget = raidTarget;
            CaptureObjective = captureObjective;
            CaptureBlockingFacility = captureBlockingFacility;
            DefenseTarget = defenseTarget;
            StationPressureReason = stationPressureReason;
            RetreatAnchor = retreatAnchor;
            GuardPosition = guardPosition;
            RecoveryReason = recoveryReason;
            StationPressure = stationPressure;
            IsDefensePointClear = isDefensePointClear;
            VisibleAdvantage = visibleAdvantage;
            UnseenInvestment = unseenInvestment;
            EconomyPressure = economyPressure;
            HeldShips = heldShips;
            TeamHeldZones = teamHeldZones;
        }
    }

    public sealed class EnemyStrategicContextBuilder
    {
        private readonly EnemyStationPressureTracker _stationPressure;
        private readonly EnemyRecoveryAnchor _recovery;
        // Task groups share the commander's fleet-wide decision to leave the forward station.
        private bool _preferStrategicObjectives;

        public Vector3? CentrePosition => EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones)?.Center;
        /// <summary>Zone the allocator sent this group to; null for the reserve and for a group that picks freely.</summary>
        public string AssignedZoneId { get; set; }
        /// <summary>The reserve takes no zone itself: the allocator sends forces from it.</summary>
        public bool IsReserve { get; set; }
        /// <summary>Where the reserve gathers for the next zone; null when every zone is served.</summary>
        public Vector3? ReserveStagingPoint { get; set; }
        private Vector3 _strategicCenter;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float CAPTURE_BLOCKED_REPORT_INTERVAL = 15f;
        private readonly Dictionary<string, int> _captureFilterCounts = new Dictionary<string, int>();
        private float _nextCaptureBlockedReport;
#endif
        private const float CAPTURE_STALL_SECONDS = 20f;

        /// <summary>A hostile-strength dip must last this long before the current capture objective is dropped.</summary>
        private const float UNSAFE_GRACE_SECONDS = 3f;

        /// <summary>Hostiles this close to the fleet center can engage it soon: twice the common ship weapon range (500).</summary>
        private const float LOCAL_ENGAGEMENT_RADIUS = 1000f;
        /// <summary>A hostile this far ahead on the way back, and this close to it, blocks a pull-back: weapon range (500).</summary>
        private const float WITHDRAW_BLOCK_REACH = 500f;
        /// <summary>The home gathering point is tried at 1..3 steps of this distance ahead of the station.</summary>
        private const float HOME_GATHER_STEP = 500f;
        private const int HOME_GATHER_STEPS = 3;
        /// <summary>Seconds after station damage during which its attackers are revealed to the team.</summary>
        private const float DAMAGE_REVEAL_SECONDS = 5f;

        private readonly IShipService _shipService;
        private ISet<string> _reservedCaptureObjectives;
        private bool _groupScoped;
        private float _hostileIncome;
        private const float HOSTILE_INCOME_TO_FLEET_SHARE = 0.5f;
        private const float UNSEEN_FLEET_WINDOW = 90f;
        private const float OPENING_UNSEEN_GRACE_SECONDS = 180f;
        private const float MAX_UNSEEN_INVESTMENT_SHARE = 0.5f;
        private const float UNSCOUTED_FLEET_INVESTMENT = 1200f;
        // Travel-distance units, on the SATELLITE_PAD_PRIORITY scale.
        private const float ALLY_BASE_MINE_PENALTY = 600f;
        /// <summary>
        /// Inside a hostile zone the relay is taken before its pads: about one zone hop of travel. Satellites on the
        /// way are fought by the attack-move order.
        /// </summary>
        private const float HOSTILE_RELAY_FIRST_BONUS = 1000f;
        private const float BASE_EXCLUSION_MARGIN = 900f;
        private float _hostileEconomyPressure = 1f;
        private readonly Dictionary<TeamId, int> _enemyTeamZones = new Dictionary<TeamId, int>();
        private readonly HashSet<TeamId> _rosterTeams = new HashSet<TeamId>();
        private readonly HashSet<PlayerId> _livingAllies = new HashSet<PlayerId>();
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly IReadOnlyList<IEnemyCaptureObjectiveSource> _captureSources;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;
        private readonly IPlayerRoster _playerRoster;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly HostileIntelModel _intel;
        private readonly IVisionService _vision;
        private readonly IHazardService _hazards;
        private readonly IReinforcementZoneSource _zones;
        private readonly IShipNavigationService _navigation;
        private readonly ForceComposition _objectiveThreat = new ForceComposition();
        private readonly EnemyZoneObjectiveCollector _zoneObjectives;
        private readonly EnemyZoneEvaluator _zoneEvaluator = new EnemyZoneEvaluator();
        private readonly List<EnemyZoneAssessment> _zoneAssessments = new List<EnemyZoneAssessment>();
        private readonly List<EnemyCaptureObjective> _zoneCandidates = new List<EnemyCaptureObjective>();
        private readonly List<float> _zonePriorities = new List<float>();
        private readonly Dictionary<string, EnemyZoneVerdict> _reportedVerdicts = new Dictionary<string, EnemyZoneVerdict>();
        // Area the group gathers for when no zone is a Go; -1 when it has a capture objective or nothing to take.
        private int _assembleArea = -1;
        private int _selectedArea = -1;
        private bool _behindOnZones;
        private Vector3 _home;
        private ForceComposition _assessedForce;
        // ForceCompositionBuilder consumes these predicates synchronously.
        private readonly Predicate<HostileSighting> _includeDefender;
        private readonly Predicate<HostileSighting> _includeRouteThreat;
        private readonly Predicate<HostileSighting> _includeWithdrawBlocker;
        private readonly Predicate<GameEntity> _includeNearbyOwn;
        private readonly Func<Vector3, bool> _isNearHostileBase;
        private Vector3 _nearbyCenter;
        private float _nearbyRadiusSquared;
        private Vector3 _routeOrigin;
        private Vector3 _routeDestination;
        private Vector3 _routeSegment;
        private float _routeDestinationRadius;
        private bool _routeEscaping;
        private readonly HashSet<long> _stationThreats = new HashSet<long>();
        private readonly List<EnemyCaptureObjective> _captureCandidates = new List<EnemyCaptureObjective>();
        private static readonly Comparison<RankedCapture> RANK_CAPTURES = CompareRankedCaptures;
        private readonly List<RankedCapture> _rankedCaptures = new List<RankedCapture>();
        private EnemyCaptureObjective _captureObjective;
        private readonly EnemyTargetCommitment _captureCommitment = new EnemyTargetCommitment();
        private readonly EnemyAdvantageSmoother _advantageSmoother = new EnemyAdvantageSmoother();
        private float _captureDistance;
        private float _captureProgress;
        private float _captureStallSeconds;
        private float _captureUnsafeSince = -1f;
        private float _captureEvaluatedAt;
        private bool _captureIsExecuting;
        private string _rejectedCaptureId;
        private float _captureRetryAt;
        private string _captureReason;
        private string _objectiveSafetyReason;
        public bool CaptureReservedByOtherGroup { get; private set; }
        public bool RecoveryActive => _recovery.IsActive;
        public bool StationCritical => _stationPressure.IsStationCritical;

        public bool TryGetDefendedStation(out Vector3 position)
        {
            position = default;
            if (!_stationPressure.HasPressure ||
                !_entityLocator.TryGetEntity(_stationPressure.StationId, out GameEntity station) ||
                station.HealthModel.IsDestroyed) return false;
            position = station.GetFacade<IEntityTransformFacade>().Transform.position;
            return true;
        }

        public void FillDefenseDeploymentThreats(PlacementThreatSnapshot threats) =>
            _recovery.FillPlacementThreats(threats);

        public bool TryGetRearDeploymentThreats(Vector3 station, PlacementThreatSnapshot threats)
        {
            FillDefenseDeploymentThreats(threats);
            if (_stationPressure.HasThreatPosition)
            {
                threats.SetApproach(_stationPressure.ThreatX, _stationPressure.ThreatZ);
                return true;
            }
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            if (centre != null)
            {
                threats.SetApproach(centre.Center.x, centre.Center.z);
                return true;
            }
            if (_reinforcementZonesSystem.TryGetDefaultZoneCenter(_owner.Id, out Vector3 home))
            {
                threats.SetApproach(home.x, home.z);
                return true;
            }
            return false;
        }

        public AllyAiMarkerSource AlliedMarkers { get; }
        public TeamAiCooperation TeamCooperation => _teamPicture.Cooperation;
        private TacticalMarker _allyDefenseMarker;
        private readonly List<IShipEntity> _ownShips = new List<IShipEntity>();
        private readonly HashSet<long> _ownShipIds = new HashSet<long>();
        private readonly List<FormationPoint> _formationPositions = new List<FormationPoint>();
        private readonly Dictionary<IShipEntity, GameEntity> _receivers = new Dictionary<IShipEntity, GameEntity>();
        private readonly Dictionary<IShipEntity, ForceComposition> _shipForces = new Dictionary<IShipEntity, ForceComposition>();
        private readonly Dictionary<GameEntity, ForceComposition> _targetForces = new Dictionary<GameEntity, ForceComposition>();
        private readonly List<ForceComposition> _forcePool = new List<ForceComposition>();
        private int _usedForces;
        private float _ionRiskWeight = 1f;
        // Targets stay picked while still valid: a new pick re-orders every committed ship, and each order makes
        // the ship's weapons re-sort their candidates.
        private GameEntity _localThreat;
        private GameEntity _raidTarget;
        private GameEntity _fleetTarget;

        private readonly PlayerSlot _owner;
        private readonly EnemyFactionModel _faction;
        private readonly List<IShipEntity> _assaultCapitals = new List<IShipEntity>();
        private readonly IProductionAccess _productionAccess;
        private readonly AiWorldSnapshot _worldSnapshot;
        private readonly TeamAiPicture _teamPicture;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker FIND_RAID_TARGET_MARKER = new ProfilerMarker("EnemyAI.Context.FindRaidTarget");
        private static readonly ProfilerMarker SELECT_CAPTURE_OBJECTIVE_MARKER = new ProfilerMarker("EnemyAI.Context.SelectCaptureObjective");
        private static readonly ProfilerMarker BUILD_BASE_THREAT_MARKER = new ProfilerMarker("EnemyAI.Context.BuildBaseThreat");
        private static readonly ProfilerMarker FORCE_BUILDS_MARKER = new ProfilerMarker("EnemyAI.Context.ForceBuilds");
        private static readonly ProfilerMarker ADD_TARGET_FORCE_MARKER = new ProfilerMarker("EnemyAI.Context.AddTargetForce");
#endif
        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _ownTeamForce = new ForceComposition();
        private readonly ForceComposition _focusTeamForce = new ForceComposition();
        private readonly ForceComposition _combinedStrikeForce = new ForceComposition();
        private readonly ForceComposition _baseThreatForce = new ForceComposition();
        private readonly ForceComposition _localThreatForce = new ForceComposition();
        private readonly ForceComposition _localOwnForce = new ForceComposition();

        public EnemyStrategicContextBuilder(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IReadOnlyList<IEnemyCaptureObjectiveSource> captureSources,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel,
            IPlayerRoster playerRoster,
            ForceCompositionBuilder forceBuilder,
            TeamIntelRegistry intelRegistry,
            IVisionService vision,
            PlayerSlot owner,
            IProductionAccess productionAccess,
            IHazardService hazards,
            IReinforcementZoneSource zones,
            IShipNavigationService navigation,
            EnemyStationPressureTracker stationPressure,
            EnemyRecoveryAnchor recovery,
            AiWorldSnapshot worldSnapshot,
            TeamAiPictureRegistry teamPictures, EnemyFactionModel faction)
        {
            _vision = vision;
            _hazards = hazards;
            _intel = intelRegistry.Get(owner.Team);
            _forceBuilder = forceBuilder;
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _captureSources = captureSources;
            foreach (IEnemyCaptureObjectiveSource source in captureSources)
                if (source is AllyAiMarkerSource markers) AlliedMarkers = markers;
            _entityLocator = entityLocator;
            _gameModel = gameModel;
            _owner = owner;
            _faction = faction;
            _productionAccess = productionAccess;
            _playerRoster = playerRoster;
            _zones = zones;
            _navigation = navigation;
            _stationPressure = stationPressure;
            _recovery = recovery;
            _worldSnapshot = worldSnapshot;
            _teamPicture = teamPictures.GetOrCreate(owner);
            _includeDefender = IncludeDefender;
            _includeRouteThreat = IncludeRouteThreat;
            _includeWithdrawBlocker = IncludeWithdrawBlocker;
            _includeNearbyOwn = IncludeNearbyOwn;
            _isNearHostileBase = IsNearHostileBase;
            _zoneObjectives = new EnemyZoneObjectiveCollector(zones, playerRoster, worldSnapshot, vision, _teamPicture,
                forceBuilder, owner.Id);
            _assessedForce = _ownForce;
        }

        private EnemyStrategicContextBuilder(EnemyStrategicContextBuilder source)
        {
            _shipService = source._shipService;
            _reinforcementZonesSystem = source._reinforcementZonesSystem;
            _captureSources = source._captureSources;
            AlliedMarkers = source.AlliedMarkers;
            _entityLocator = source._entityLocator;
            _gameModel = source._gameModel;
            _playerRoster = source._playerRoster;
            _forceBuilder = source._forceBuilder;
            _intel = source._intel;
            _vision = source._vision;
            _hazards = source._hazards;
            _zones = source._zones;
            _navigation = source._navigation;
            _owner = source._owner;
            _faction = source._faction;
            _productionAccess = source._productionAccess;
            _stationPressure = source._stationPressure;
            _recovery = source._recovery;
            _worldSnapshot = source._worldSnapshot;
            _teamPicture = source._teamPicture;
            _includeDefender = IncludeDefender;
            _includeRouteThreat = IncludeRouteThreat;
            _includeWithdrawBlocker = IncludeWithdrawBlocker;
            _includeNearbyOwn = IncludeNearbyOwn;
            _isNearHostileBase = IsNearHostileBase;
            _zoneObjectives = new EnemyZoneObjectiveCollector(_zones, _playerRoster, _worldSnapshot, _vision, _teamPicture,
                _forceBuilder, _owner.Id);
            _assessedForce = _ownForce;
        }

        public EnemyStrategicContextBuilder CreateGroupBuilder() => new EnemyStrategicContextBuilder(this);

        /// <summary>
        /// Rates every zone for the whole ship pool as seen from <paramref name="home"/>: the allocator's input.
        /// Zones an allied AI reserved are left out. Borrowed until the next call on this builder.
        /// </summary>
        public IReadOnlyList<EnemyZoneObjective> CollectPoolZones(Vector3 home, float speed)
        {
            _teamPicture.EnsureRefreshed(Time.time);
            _reservedCaptureObjectives = null;
            CollectCaptureObjectives();
            PlayerId focusEnemy = FindFocusEnemy(home);
            GameEntity enemyBase = FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == focusEnemy, home);
            _strategicCenter = enemyBase == null ? home :
                (home + enemyBase.GetFacade<IEntityTransformFacade>().Transform.position) * 0.5f;
            _zoneCandidates.Clear();
            _zonePriorities.Clear();
            foreach (EnemyCaptureObjective candidate in _captureCandidates)
            {
                if (IsNearHostileBase(candidate.Position) && !candidate.IsThreatened ||
                    IsCaptureReserved(candidate.Id) || IsInZoneReservedByAlly(candidate.Position)) continue;
                _zoneCandidates.Add(candidate);
                _zonePriorities.Add(-GetCaptureScore(home, candidate));
            }
            _zoneObjectives.Collect(_zoneCandidates, _zonePriorities, home, speed, _isNearHostileBase);
            return _zoneObjectives.Objectives;
        }

        /// <summary>The reinforcement zone of a pool zone; null for a lone site.</summary>
        public ReinforcementZonePresenter GetPoolZone(int area) => _zoneObjectives.GetZone(area);

        public Vector3 GetPoolZoneCentre(int area) => _zoneObjectives.GetCentre(area);

        public Vector3 GetPoolStagingPoint(int area, Vector3 home) => GetStagingPoint(area, home);

        /// <summary>Where the reserve waits when no zone needs a force: ahead of home, toward the map centre.</summary>
        public Vector3 GetIdleGatherPoint(Vector3 home) => GetHomeGatherPoint(home, CentrePosition ?? home);

        /// <summary>
        /// The nearest safe team-held zone to the area, otherwise a point ahead of home toward it. Never the
        /// station itself: a fleet parked on the station blocks its own deployment.
        /// </summary>
        private Vector3 GetStagingPoint(int area, Vector3 home)
        {
            Vector3 staging = _zoneObjectives.GetStagingPoint(area, home);
            return staging == home ? GetHomeGatherPoint(home, _zoneObjectives.GetCentre(area)) : staging;
        }

        private Vector3 GetHomeGatherPoint(Vector3 home, Vector3 target)
        {
            Vector3 direction = target - home;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1f) return home;
            direction.Normalize();
            Vector3 point = home;
            for (int step = 1; step <= HOME_GATHER_STEPS; step++)
            {
                point = home + direction * (HOME_GATHER_STEP * step);
                if (!IsInOwnDeploymentZone(point)) return point;
            }
            return point;
        }

        private bool IsInOwnDeploymentZone(Vector3 point)
        {
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (!zone.IsCapturable && zone.Owner == _owner.Id && EnemyShipTaskGroupPlanner.IsInZone(zone, point))
                    return true;
            return false;
        }

        /// <summary>Some enemy team holds more uncontested zones than this AI's team.</summary>
        public bool IsBehindOnZones() => IsBehindOnZones(CountTeamHeldZones(_owner.Id, out _));

        /// <summary>The team holds an uncontested outer (non-centre) zone.</summary>
        public bool HoldsOuterZone()
        {
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (zone.IsCapturable && !zone.IsMain && !zone.IsContested && !zone.Owner.IsNone &&
                    _playerRoster.IsAllied(_owner.Id, zone.Owner)) return true;
            return false;
        }

        /// <summary>Economy stands: an outer zone held and at least one own mine running.</summary>
        public bool HasStrongEconomy()
        {
            if (!HoldsOuterZone()) return false;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
                if (structures[i].IsMiningFacility && structures[i].Owner == _owner.Id &&
                    !structures[i].Entity.HealthModel.IsDestroyed) return true;
            return false;
        }

        /// <summary>Where this AI gathers: its primary station, else a station it still owns, else its home zone.</summary>
        public Vector3 GetHome(Vector3 fallback)
        {
            GameEntity ownBase = FindOwnBase(fallback);
            return ownBase != null ? ownBase.GetFacade<IEntityTransformFacade>().Transform.position :
                GetHomeWithoutStation(fallback);
        }

        private Vector3 GetHomeWithoutStation(Vector3 fallback)
        {
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (station.Owner == _owner.Id) return station.Position;
            return _reinforcementZonesSystem.TryGetDefaultZoneCenter(_owner.Id, out Vector3 zone) ? zone : fallback;
        }

        /// <summary>Fills <paramref name="force"/> with the ship at its current strength; false when it is gone.</summary>
        public bool TryBuildShipForce(IShipEntity ship, ForceComposition force)
        {
            force.Clear();
            if (!_entityLocator.TryGetEntity(ship.EntityId, out GameEntity entity) ||
                entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits) return false;
            _forceBuilder.AddUnit(force, entity);
            return true;
        }

        public void RecordZoneAllocation(string details) =>
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "zoneAlloc", details);

        private bool IsInZoneReservedByAlly(Vector3 position)
        {
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
                if (EnemyShipTaskGroupPlanner.IsInZone(zone, position) && IsExpansionZoneReservedByAlly(zone))
                    return true;
            return false;
        }

        public string GetExpansionReservationId(ReinforcementZonePresenter zone)
        {
            for (int i = 0; i < _zones.Zones.Count; i++)
                if (_zones.Zones[i] == zone) return $"expansion:{i}";
            throw new ArgumentException("The zone is not part of this battle.", nameof(zone));
        }

        public bool IsExpansionZoneReservedByAlly(ReinforcementZonePresenter zone) =>
            _teamPicture.Cooperation.IsReservedByAlly(_owner.Id, GetExpansionReservationId(zone));

        /// <summary>
        /// A Defend request for an allied AI station when this player's main force is the team's nearest available
        /// responder and the approach is safe; null otherwise.
        /// </summary>
        public TacticalMarker SelectAllyDefenseRequest(bool recovering)
        {
            if (recovering || _stationPressure.HasPressure ||
                !_teamPicture.Cooperation.TryGetDefenseAssignment(_owner.Id, Time.time, out PlayerId requester, out Vector3 station) ||
                !IsMarkerApproachSafe(station)) return null;
            long id = -100L - requester.Index;
            if (_allyDefenseMarker == null || _allyDefenseMarker.Id != id ||
                (_allyDefenseMarker.Point - station).sqrMagnitude > 1f)
                _allyDefenseMarker = new TacticalMarker(id, TacticalMarkerType.Defend, requester, _owner.Team, station,
                    -1, Time.time, Time.time + TeamAiCooperation.STALE_SECONDS);
            return _allyDefenseMarker;
        }

        private bool IsCaptureReserved(string objectiveId) =>
            _reservedCaptureObjectives != null && _reservedCaptureObjectives.Contains(objectiveId) ||
            _teamPicture.Cooperation.IsReservedByAlly(_owner.Id, objectiveId);

        public bool IsMarkerApproachSafe(Vector3 point)
        {
            _teamPicture.EnsureRefreshed(Time.time);
            return IsObjectiveSafe(ToVector(CalculateFleetCenter(_ownShips)), point, AllyAiMarkerSource.REQUEST_RADIUS, false);
        }

        public bool AssessStation(IReadOnlyList<IShipEntity> ships)
        {
            float now = Time.time;
            _teamPicture.EnsureRefreshed(now);
            Vector3 origin = ToVector(CalculateFleetCenter(ships));
            GameEntity ownBase = FindOwnBase(origin);
            BuildBaseThreat(ownBase);
            if (ownBase != null && _stationPressure.IsStationCritical)
                _teamPicture.Cooperation.RequestDefense(_owner.Id,
                    ownBase.GetFacade<IEntityTransformFacade>().Transform.position, now);
            else _teamPicture.Cooperation.ClearDefense(_owner.Id);
            _groupScoped = false;
            _ownShips.Clear();
            _ownShips.AddRange(ships);
            _forceBuilder.Build(_ownTeamForce, entity => _playerRoster.IsAllied(_owner.Id, entity.Owner));
            _forceBuilder.BuildKnown(_focusTeamForce, _intel, Time.time,
                sighting => _playerRoster.IsHostile(_owner.Id, sighting.Owner));
            BuildOwnNear(_localOwnForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            BuildHostilesNear(_localThreatForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            UpdateRecovery(origin, ownBase, now);
            return _stationPressure.HasPressure;
        }

        /// <summary>The returned collections and their force compositions are borrowed until the next Build on this builder.</summary>
        public EnemyStrategicContext Build(IReadOnlyList<IShipEntity> ships = null,
            ISet<string> reservedCaptureObjectives = null, bool preferStrategicObjectives = false,
            bool unbuiltMinesOnly = false)
        {
            _preferStrategicObjectives = preferStrategicObjectives;
            PlayerId self = _owner.Id;
            _groupScoped = ships != null;
            List<IShipEntity> ownShips;
            if (ships == null) ownShips = GetShips(self);
            else
            {
                _ownShips.Clear();
                for (int i = 0; i < ships.Count; i++)
                {
                    IShipEntity ship = ships[i];
                    if (_entityLocator.TryGetEntity(ship.EntityId, out GameEntity entity) &&
                        !entity.HealthModel.IsDestroyed)
                    {
                        _ownShips.Add(ship);
                    }
                }
                ownShips = _ownShips;
            }
            _reservedCaptureObjectives = reservedCaptureObjectives;
            _ionRiskWeight = 1f;
            _ownShipIds.Clear();
            _assaultCapitals.Clear();
            float assaultInvestment = 0f;
            const float EARLY_FLEET_CREDITS_PER_SHIP = 1000f;
            float fleetInvestment = _faction.CurrentLevel >= 4 ? 0f :
                ownShips.Count * EARLY_FLEET_CREDITS_PER_SHIP;
            foreach (IShipEntity ship in ownShips)
            {
                if (_groupScoped) _ownShipIds.Add(ship.EntityId);
                if (!_entityLocator.TryGetEntity(ship.EntityId, out GameEntity shipEntity)) continue;
                IHealthModelObserver health = shipEntity.HealthModel;
                if (_faction.CurrentLevel >= 4 && shipEntity.TryGetFacade(out IUnitTypeFacade unit) && unit.UnitTypeId.IsShip &&
                    _faction.ShipFactionData.TryGetValue(unit.UnitTypeId.ShipType, out FactionData data))
                {
                    fleetInvestment += data.Price;
                    if (_faction.IsAssaultCapital(unit.UnitTypeId.ShipType, health.ShipClass))
                    {
                        _assaultCapitals.Add(ship);
                        assaultInvestment += data.Price;
                    }
                }
                float durability = health.Hull + health.Shields;
                if (durability <= 0f) continue;
                float capitalWeight = health.ShipClass is ShipClass.Capital or ShipClass.BattleCruiser or ShipClass.Dreadnought ? 2f : 1f;
                _ionRiskWeight = Mathf.Max(_ionRiskWeight, 1f + capitalWeight * health.Shields / durability);
            }
            Vector3 origin = ToVector(CalculateFleetCenter(ownShips));
            GameEntity ownBase = FindOwnBase(origin);
            Vector3 home = ownBase != null ? ownBase.GetFacade<IEntityTransformFacade>().Transform.position :
                GetHomeWithoutStation(origin);
            PlayerId focusEnemy = FindFocusEnemy(home);
            GameEntity enemyBaseTarget = FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == focusEnemy, origin);
            _strategicCenter = enemyBaseTarget == null ? home :
                (home + enemyBaseTarget.GetFacade<IEntityTransformFacade>().Transform.position) * 0.5f;
            float now = Time.time;
            _teamPicture.EnsureRefreshed(now);
            _fleetTarget = FindClosestKnownShip(origin, now);
            GameEntity enemyFleetTarget = _fleetTarget;
            int focusTeamShipCount = CountKnownShips(focusEnemy, now);
            _hostileEconomyPressure = GetHostileEconomyPressure(focusEnemy);
            int ownedCapturableZoneCount = _reinforcementZonesSystem.GetOwnedCapturableZoneCount(self);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (FORCE_BUILDS_MARKER.Auto())
            {
#endif
            _forceBuilder.Build(_ownForce, entity => entity.Owner == self &&
                (!_groupScoped || _ownShipIds.Contains(entity.Id)));
            if (ships == null)
                _forceBuilder.Build(_ownTeamForce, entity => _playerRoster.IsAllied(self, entity.Owner));
            _forceBuilder.BuildKnown(_focusTeamForce, _intel, now,
                sighting => focusEnemy != PlayerId.None && _playerRoster.IsAllied(focusEnemy, sighting.Owner));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            GameEntity defenseTarget;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BUILD_BASE_THREAT_MARKER.Auto())
            {
#endif
            defenseTarget = BuildBaseThreat(ownBase);
            _forceBuilder.BuildKnown(_baseThreatForce, _intel, now, sighting => _stationThreats.Contains(sighting.EntityId));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (FORCE_BUILDS_MARKER.Auto())
            {
#endif
            BuildHostilesNear(_localThreatForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            BuildOwnNear(_localOwnForce, origin, LOCAL_ENGAGEMENT_RADIUS);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            _localThreat = FindLocalThreat(origin, now);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (FIND_RAID_TARGET_MARKER.Auto())
            {
#endif
            _raidTarget = FindRaidTarget(origin, now);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            if (!_groupScoped) UpdateRecovery(origin, ownBase, now);
            ForceComposition strikeForce = _ownForce;
            if (ships != null && preferStrategicObjectives)
            {
                // Allied main forces aimed at the same enemy attack together once their summed strength passes.
                _teamPicture.Cooperation.PublishStrike(self, _ownForce, focusEnemy, origin,
                    !_stationPressure.HasPressure, now);
                _combinedStrikeForce.CopyFrom(_ownForce);
                _teamPicture.Cooperation.AddAlliedStrikeStrength(_combinedStrikeForce, self, focusEnemy, now);
                strikeForce = _combinedStrikeForce;
            }
            // Zones are rated for the force that would go: the main force counts allied strike strength.
            _assessedForce = strikeForce;
            _home = home;
            int teamHeldZones = CountTeamHeldZones(self, out bool hasZoneMajority);
            _behindOnZones = IsBehindOnZones(teamHeldZones);
            EnemyCaptureObjective captureObjective;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (SELECT_CAPTURE_OBJECTIVE_MARKER.Auto())
            {
#endif
            captureObjective = SelectCaptureObjective(origin, now, unbuiltMinesOnly);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            // An outmatched group away from held ground pulls back to its gathering point instead of dying in place.
            Vector3? withdrawPoint = FindWithdrawPoint(origin, home);
            Vector3? retreatAnchor = withdrawPoint ?? FindRetreatAnchor(origin, ownBase);
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            bool securedCentre = centre != null && !centre.IsContested && _playerRoster.IsAllied(self, centre.Owner);
            // Never guard a zone the team does not hold; a group too weak for every zone gathers at its staging point.
            Vector3? stagingPoint = _assembleArea >= 0 ? GetStagingPoint(_assembleArea, home) : (Vector3?)null;
            Vector3? guardPosition = stagingPoint ?? (AssignedZoneId != null ? null :
                (preferStrategicObjectives && securedCentre ? centre.Center : (Vector3?)null) ?? FindNearbyHeldZone(origin));
            // The reserve always gathers at one point: the held centre, else the staging point of the next zone, else home.
            if (IsReserve) guardPosition = securedCentre ? centre.Center : ReserveStagingPoint ?? GetIdleGatherPoint(home);
            string recoveryReason = withdrawPoint.HasValue ? "Outmatched away from held ground; pull back to the gathering point." :
                retreatAnchor.HasValue
                ? "Validated a nearby allied retreat anchor."
                : "No safe local retreat anchor.";
            float baseThreatRatio = _baseThreatForce.IsEmpty
                ? 0f
                : 1f / CombatMatchup.Advantage(_ownForce, _baseThreatForce);

            float localRaw = CombatMatchup.Advantage(_localOwnForce, _localThreatForce);
            float visibleAdvantage = CombatMatchup.Advantage(ships == null ? _ownTeamForce : strikeForce, _focusTeamForce);
            float unseenInvestment = EstimateUnseenFleetInvestment(focusEnemy, now, fleetInvestment);
            float fleetRaw = visibleAdvantage / _hostileEconomyPressure;
            if (unseenInvestment > 0f && fleetInvestment > 0f)
            {
                float unseenRatio = unseenInvestment / fleetInvestment;
                fleetRaw = 1f / (1f / Mathf.Max(0.01f, fleetRaw) + unseenRatio);
                fleetRaw = Mathf.Max(fleetRaw, visibleAdvantage * MAX_UNSEEN_INVESTMENT_SHARE);
            }

            bool hasDecisiveSurplus = _faction.HasAssaultSurplus && fleetRaw >= 2f &&
                assaultInvestment >= 2f * unseenInvestment;
            bool canAssault = preferStrategicObjectives && (centre == null || securedCentre) &&
                !_stationPressure.HasPressure && ownBase != null && _productionAccess.HasHostBase &&
                _assaultCapitals.Count >= EnemyFactionModel.MINIMUM_ASSAULT_CAPITALS &&
                _faction.HasAssaultReinforcementPlan && fleetRaw >= 1f && (hasZoneMajority || hasDecisiveSurplus);
            bool canHunt = enemyFleetTarget != null && (!securedCentre || canAssault) &&
                !IsNearHostileBase(enemyFleetTarget.GetFacade<IEntityTransformFacade>().Transform.position);
            _advantageSmoother.Sample(localRaw, fleetRaw, now,
                !_localThreatForce.IsEmpty, !_focusTeamForce.IsEmpty || unseenInvestment > 0f);
            if (_localThreat != null && _advantageSmoother.Local <= EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage)
                _localThreat = FindLastStandTarget(origin, now) ?? _localThreat;
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                victoryCondition: _gameModel.VictoryCondition,
                difficulty: _owner.Difficulty,
                ownShipCount: ownShips.Count,
                enemyShipCount: focusTeamShipCount,
                hasCaptureTarget: captureObjective != null,
                hasEnemyBaseTarget: enemyBaseTarget != null,
                hasOwnBase: ownBase != null,
                ownedCapturableZoneCount: ownedCapturableZoneCount,
                fleetAdvantage: _advantageSmoother.Fleet,
                localRaw: localRaw, fleetRaw: fleetRaw,
                baseThreatRatio: baseThreatRatio,
                hasThreatenedSite: captureObjective != null && captureObjective.IsThreatened,
                localAdvantage: _advantageSmoother.Local,
                hasLocalThreat: _localThreat != null,
                hasRaidTarget: _raidTarget != null,
                isRegrouped: IsRegrouped(ownShips),
                hasStationPressure: _stationPressure.HasPressure,
                hasSafeRetreat: retreatAnchor.HasValue,
                hasEconomyRaidTarget: _raidTarget != null && _raidTarget.TryGetFacade(out IMiningFacilityFacade _),
                recoveryMode: _recovery.IsActive,
                isStationCritical: _stationPressure.IsStationCritical,
                hasGuardSite: guardPosition.HasValue,
                isCentreGuardGoal: preferStrategicObjectives && centre != null && !canAssault,
                canAssaultBase: canAssault,
                canHuntFleet: canHunt && captureObjective == null,
                hasSecuredCentre: preferStrategicObjectives && securedCentre,
                canDisengage: withdrawPoint.HasValue,
                raidWhenBehind: preferStrategicObjectives && _behindOnZones && captureObjective == null);
            _receivers.Clear();
            _shipForces.Clear();
            _targetForces.Clear();
            _usedForces = 0;
            foreach (IShipEntity ship in ownShips)
            {
                if (!_entityLocator.TryGetEntity(ship.EntityId, out GameEntity entity)) continue;
                _receivers.Add(ship, entity);
                ForceComposition force = NextForce();
                force.Clear();
                _forceBuilder.AddUnit(force, entity);
                AddEscortingAirWing(force, entity);
                _shipForces.Add(ship, force);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (ADD_TARGET_FORCE_MARKER.Auto())
            {
#endif
            AddTargetForce(enemyFleetTarget, now);
            AddTargetForce(enemyBaseTarget, now);
            AddTargetForce(_raidTarget, now);
            AddTargetForce(_localThreat, now);
            AddTargetForce(defenseTarget, now);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            GameEntity blockingFacility = null;
            if (captureObjective?.BlockingFacilityId is long facilityId &&
                _entityLocator.TryGetEntity(facilityId, out GameEntity facility) && !facility.HealthModel.IsDestroyed)
                blockingFacility = facility;
            return new EnemyStrategicContext(
                snapshot: snapshot,
                ships: ownShips,
                captureTarget: captureObjective != null ? captureObjective.Position : origin,
                guardPosition: guardPosition,
                assaultCapitals: _assaultCapitals,
                enemyFleetTarget: enemyFleetTarget,
                enemyBaseTarget: enemyBaseTarget,
                ownBase: ownBase,
                receivers: _receivers,
                shipForces: _shipForces,
                targetForces: _targetForces,
                localThreatTarget: _localThreat,
                raidTarget: _raidTarget,
                captureObjective: captureObjective,
                captureBlockingFacility: blockingFacility,
                defenseTarget: defenseTarget,
                stationPressureReason: !_stationPressure.HasPressure ? "No recent station pressure." :
                    defenseTarget == null ? "Station losing durability; attacker not visible" :
                    "Visible hostile can reach the station footprint.",
                retreatAnchor: retreatAnchor,
                recoveryReason: recoveryReason + " " + _captureReason,
                stationPressure: _stationPressure,
                isDefensePointClear: (point, radius) => _navigation.IsPositionClear(point, radius, includeReservations: false),
                visibleAdvantage: visibleAdvantage,
                unseenInvestment: unseenInvestment,
                economyPressure: _hostileEconomyPressure,
                heldShips: _faction.HeldShips,
                teamHeldZones: teamHeldZones);
        }

        /// <summary>
        /// A carrier's strike value includes the launched squadrons still guarding it, at their current strength:
        /// guarding bombers attack whatever the carrier is ordered to attack.
        /// </summary>
        private void AddEscortingAirWing(ForceComposition force, GameEntity carrier)
        {
            if (!carrier.TryGetFacade(out IHangarAirWingFacade airWing)) return;
            for (int i = 0; i < airWing.LaunchedCount; i++)
            {
                if (!_entityLocator.TryGetEntity(airWing.GetLaunched(i).EntityId, out GameEntity squadron) ||
                    squadron.HealthModel.IsDestroyed || !squadron.HealthModel.HasUnits ||
                    squadron.GetFacade<IUnitOrderObserverFacade>().CurrentOrder != UnitOrderType.Guard) continue;
                _forceBuilder.AddUnit(force, squadron);
            }
        }

        /// <summary>
        /// The closest visible hostile within <see cref="LOCAL_ENGAGEMENT_RADIUS"/>; ships win over squadrons.
        /// The previous pick stays while visible in range and no ship replaces a squadron pick.
        /// </summary>
        private GameEntity FindLocalThreat(Vector3 origin, float now)
        {
            GameEntity closest = null;
            bool closestIsShip = false;
            bool keepsPrevious = false;
            bool previousIsShip = false;
            float closestDistance = float.MaxValue;
            float radiusSquared = LOCAL_ENGAGEMENT_RADIUS * LOCAL_ENGAGEMENT_RADIUS;
            bool insideHostileBase = IsNearHostileBase(origin);
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                Vector3 position = ToVector(sighting.Position);
                if (!insideHostileBase && IsNearHostileBase(position)) continue;
                Vector3 offset = position - origin;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                float score = GetTargetScore(origin, ToVector(sighting.Position));
                bool isShip = sighting.UnitTypeId.IsShip;
                if (!_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits || entity.IsCloaked() ||
                    !_vision.IsVisible(_owner.Id, entity))
                {
                    continue;
                }

                if (ReferenceEquals(entity, _localThreat) && distance <= radiusSquared &&
                    _intel.GetConfidence(sighting, now) > 0f)
                {
                    keepsPrevious = GetIonCost(ToVector(sighting.Position)) == 0f;
                    previousIsShip = isShip;
                }

                if (distance > radiusSquared ||
                    closestIsShip && !isShip ||
                    isShip == closestIsShip && score >= closestDistance ||
                    _intel.GetConfidence(sighting, now) <= 0f)
                {
                    continue;
                }

                closest = entity;
                closestIsShip = isShip;
                closestDistance = score;
            }

            return keepsPrevious && (previousIsShip || !closestIsShip) ? _localThreat : closest;
        }

        private GameEntity FindLastStandTarget(Vector3 origin, float now)
        {
            GameEntity weakest = null;
            float durability = float.PositiveInfinity;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!sighting.UnitTypeId.IsShip || !_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                    _intel.GetConfidence(sighting, now) <= 0f ||
                    PlanarDistance(origin, ToVector(sighting.Position)) > LOCAL_ENGAGEMENT_RADIUS ||
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.HealthModel.IsDestroyed || entity.IsCloaked() || !_vision.IsVisible(_owner.Id, entity)) continue;
                IHealthModelObserver health = entity.HealthModel;
                float remaining = health.Hull + health.Shields;
                if (remaining >= durability ||
                    CombatMatchup.TimeToDestroyTarget(_localOwnForce, health.ShipClass, health.Hull, health.Shields,
                        null) >= CombatMatchup.MAX_TIME) continue;
                durability = remaining;
                weakest = entity;
            }
            if (weakest != null) return weakest;

            GameEntity mine = null;
            float distance = LOCAL_ENGAGEMENT_RADIUS;
            foreach (AiWorldSnapshot.Entry entry in _worldSnapshot.RaidStructures)
            {
                if (!entry.IsMiningFacility || !_playerRoster.IsHostile(_owner.Id, entry.Owner) ||
                    entry.Entity.HealthModel.IsDestroyed || !_vision.IsVisible(_owner.Id, entry.Entity)) continue;
                float candidate = PlanarDistance(origin, entry.Position);
                if (candidate > distance) continue;
                distance = candidate;
                mine = entry.Entity;
            }
            return mine;
        }

        private ForceComposition NextForce()
        {
            if (_usedForces == _forcePool.Count) _forcePool.Add(new ForceComposition());
            return _forcePool[_usedForces++];
        }

        private void AddTargetForce(GameEntity target, float now)
        {
            if (target == null || _targetForces.ContainsKey(target)) return;
            Vector3 center = target.GetFacade<IEntityTransformFacade>().Transform.position;
            ForceComposition force = NextForce();
            _forceBuilder.BuildKnown(force, _intel, now, sighting =>
                sighting.EntityId != target.Id && _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                PlanarDistance(center, ToVector(sighting.Position)) <= LOCAL_ENGAGEMENT_RADIUS);
            if (target.IsUnit()) _forceBuilder.AddUnit(force, target);
            _targetForces.Add(target, force);
        }

        /// <summary>
        /// The closest visible hostile structure (not a base), with no known hostile ship within
        /// <see cref="LOCAL_ENGAGEMENT_RADIUS"/> of it.
        /// </summary>
        private GameEntity FindRaidTarget(Vector3 origin, float now)
        {
            _teamPicture.EnsureRefreshed(now);
            GameEntity closest = null;
            bool closestIsMine = false;
            bool keepsPrevious = false;
            float closestDistance = float.MaxValue;
            IReadOnlyList<AiWorldSnapshot.Entry> raidStructures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < raidStructures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = raidStructures[i];
                if (!_playerRoster.IsHostile(_owner.Id, entry.Owner) || entry.Entity.HealthModel.IsDestroyed)
                {
                    continue;
                }

                Vector3 position = entry.Position;
                bool isMine = entry.IsMiningFacility;
                if (!_vision.IsVisible(_owner.Id, entry.Entity))
                {
                    if (!isMine || !_intel.TryGetMine(entry.Id, out HostileSighting known) ||
                        known.Owner != entry.Owner) continue;
                    position = ToVector(known.Position);
                }
                if (IsNearHostileBase(position) || IsDefended(entry.Id, position, now)) continue;
                if (ReferenceEquals(entry.Entity, _raidTarget) && GetIonCost(position) == 0f)
                    keepsPrevious = true;
                float distance = GetTargetScore(origin, position);
                // A target the hostile fleet reaches before this one is a race the raid loses.
                if (HostileReachesFirst(origin, position, now)) distance *= RAID_CONTESTED_SCORE_FACTOR;
                if (closest != null && (closestIsMine && !isMine ||
                    closestIsMine == isMine && distance >= closestDistance))
                {
                    continue;
                }

                closest = entry.Entity;
                closestIsMine = isMine;
                closestDistance = distance;
            }

            // Preserve an existing raid unless a mining target becomes available.
            return keepsPrevious && (!closestIsMine || _raidTarget.TryGetFacade(out IMiningFacilityFacade _))
                ? _raidTarget : closest;
        }

        private bool HostileReachesFirst(Vector3 origin, Vector3 position, float now)
        {
            float ownDistance = PlanarDistance(origin, position);
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner) || _intel.GetConfidence(sighting, now) <= 0f)
                    continue;
                float distance = PlanarDistance(position, ToVector(sighting.Position));
                if (distance <= RAID_CONTEST_RADIUS && distance < ownDistance) return true;
            }
            return false;
        }

        /// <summary>
        /// Known hostile units near the structure plus the armed structures covering it are more than this group
        /// can attack.
        /// </summary>
        private bool IsDefended(long entityId, Vector3 position, float now)
        {
            if (_teamPicture.TryGetStructureThreat(entityId, out ForceComposition cachedThreat))
                _objectiveThreat.CopyFrom(cachedThreat);
            else
            {
                _teamPicture.EnsureRefreshed(now);
                _nearbyCenter = position;
                _teamPicture.BuildKnownFromConfident(_objectiveThreat, _includeDefender);
            }
            _zoneObjectives.AddStaticDefenders(_objectiveThreat, position);
            return !_objectiveThreat.IsEmpty &&
                CombatMatchup.Advantage(_ownForce, _objectiveThreat) <
                EnemyAiDifficultyProfile.Get(_owner.Difficulty).RequiredAttackRatio;
        }

        private bool IncludeDefender(HostileSighting sighting)
        {
            Vector3 offset = ToVector(sighting.Position) - _nearbyCenter;
            offset.y = 0f;
            return _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                offset.sqrMagnitude <= LOCAL_ENGAGEMENT_RADIUS * LOCAL_ENGAGEMENT_RADIUS;
        }

        /// <summary>
        /// The team's shared focus while it keeps a living station; otherwise the hostile player whose living station
        /// is closest to <paramref name="home"/>; once every hostile station is gone, the owner of the closest hostile ship.
        /// </summary>
        private PlayerId FindFocusEnemy(Vector3 home)
        {
            PlayerId self = _owner.Id;
            GameEntity closestStation = FindClosestEntity(EntityRoles.IsPlayerBase,
                owner => _playerRoster.IsHostile(self, owner),
                home);
            PlayerId focus;
            if (closestStation == null)
            {
                GameEntity closestShip = FindClosestKnownShip(home, Time.time);
                focus = closestShip != null ? closestShip.Owner : PlayerId.None;
            }
            else
            {
                // Allies keep the team's focus while that enemy still has a living station.
                PlayerId shared = _teamPicture.Cooperation.FocusEnemy;
                if (!shared.IsNone && shared != closestStation.Owner && _playerRoster.IsHostile(self, shared) &&
                    FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == shared, home) != null)
                    return shared;
                focus = closestStation.Owner;
            }

            _teamPicture.Cooperation.FocusEnemy = focus;
            return focus;
        }

        /// <summary>Retains a visible, safe fleet target; otherwise chooses the nearest known ship.</summary>
        private GameEntity FindClosestKnownShip(Vector3 origin, float now)
        {
            _teamPicture.EnsureRefreshed(now);
            GameEntity closest = null;
            bool keepsPrevious = false;
            float closestDistance = float.MaxValue;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!sighting.UnitTypeId.IsShip ||
                    _intel.GetConfidence(sighting, now) <= 0f ||
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits ||
                    entity.IsCloaked() ||
                    !_vision.IsVisible(_owner.Id, entity))
                {
                    continue;
                }

                if (ReferenceEquals(entity, _fleetTarget) && GetIonCost(ToVector(sighting.Position)) == 0f)
                    keepsPrevious = true;
                float distance = GetTargetScore(origin, ToVector(sighting.Position));
                if (distance < closestDistance)
                {
                    closest = entity;
                    closestDistance = distance;
                }
            }

            return keepsPrevious ? _fleetTarget : closest;
        }

        private int CountKnownShips(PlayerId focusEnemy, float now)
        {
            int count = 0;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (sighting.UnitTypeId.IsShip &&
                    focusEnemy != PlayerId.None &&
                    _playerRoster.IsAllied(focusEnemy, sighting.Owner) &&
                    _intel.GetConfidence(sighting, now) > 0f)
                {
                    count++;
                }
            }

            return count;
        }

        private float GetTargetScore(Vector3 origin, Vector3 position)
        {
            return (position - origin).sqrMagnitude * (1f + _ionRiskWeight * GetIonCost(position));
        }

        private float GetIonCost(Vector3 position)
        {
            float cost = 0f;
            IReadOnlyList<IHazardSource> hazards = _hazards.Sources;
            for (int i = 0; i < hazards.Count; i++)
            {
                IHazardSource hazard = hazards[i];
                if (hazard.Data.DisablesShields && hazard.Contains(position, 0f, false))
                    cost += hazard.Data.PathCost;
            }
            return cost;
        }

        private void CollectCaptureObjectives()
        {
            _captureCandidates.Clear();
            IReadOnlyList<EnemyCaptureObjective> cachedZones = _teamPicture.BaseCaptureObjectives;
            for (int i = 0; i < cachedZones.Count; i++)
            {
                _captureCandidates.Add(cachedZones[i]);
            }
            foreach (IEnemyCaptureObjectiveSource source in _captureSources)
                if (!(source is AllyAiMarkerSource)) source.AddStrategicObjectives(_captureCandidates, _intel);
            if (AlliedMarkers != null) AlliedMarkers.AddStrategicObjectives(_captureCandidates, _intel);
        }

        private EnemyCaptureObjective SelectCaptureObjective(Vector3 origin, float now, bool unbuiltMinesOnly)
        {
            _teamPicture.EnsureRefreshed(now);
            CaptureReservedByOtherGroup = false;
            _assembleArea = -1;
            _selectedArea = -1;
            if (IsReserve)
            {
                // The allocator sends forces from the reserve; the reserve itself only gathers.
                _captureObjective = null;
                _captureIsExecuting = false;
                _captureReason = "Reserve: zones are assigned by the allocator.";
                return null;
            }
            _captureReason = "Ranking zone objectives.";
            CollectCaptureObjectives();

            EnemyCaptureObjective best = null;
            EnemyCaptureObjective retained = null;
            float bestScore = float.NegativeInfinity;
            float retainedScore = 0f;
            string invalidReason = "ownedOrRemoved";
            _rankedCaptures.Clear();
            _zoneCandidates.Clear();
            _zonePriorities.Clear();
            for (int i = 0; i < _captureCandidates.Count; i++)
            {
                EnemyCaptureObjective candidate = _captureCandidates[i];
                if (unbuiltMinesOnly && (!candidate.IsMine || candidate.BlockingFacilityId.HasValue)) continue;
                if (IsNearHostileBase(candidate.Position) && !candidate.IsThreatened)
                {
                    CountCaptureFilter("nearHostileBase");
                    continue;
                }
                bool isCurrent = IsCurrentCapture(candidate);
                if (IsCaptureReserved(candidate.Id))
                {
                    CaptureReservedByOtherGroup = true;
                    if (isCurrent) invalidReason = "reserved";
                    CountCaptureFilter("reservedByOtherGroup");
                    continue;
                }
                if (candidate.Id == _rejectedCaptureId && now < _captureRetryAt)
                {
                    if (isCurrent) invalidReason = "stalled";
                    CountCaptureFilter("stalledRetry");
                    continue;
                }
                float priority = -GetCaptureScore(origin, candidate);
                bool hostileRelay = candidate.Kind == EnemyCaptureObjectiveType.ReinforcementZone &&
                    !candidate.Owner.IsNone && _playerRoster.IsHostile(_owner.Id, candidate.Owner);
                _rankedCaptures.Add(new RankedCapture(candidate,
                    hostileRelay ? priority + HOSTILE_RELAY_FIRST_BONUS : priority, i));
                _zoneCandidates.Add(candidate);
                _zonePriorities.Add(priority);
            }
            float commit = EnemyZoneEvaluator.COMMIT_ADVANTAGE;
            _zoneObjectives.Collect(_zoneCandidates, _zonePriorities, origin, GetGroupSpeed(), _isNearHostileBase);
            _selectedArea = _zoneEvaluator.Select(_zoneObjectives.Objectives, _assessedForce, commit, _behindOnZones,
                _zoneAssessments);
            // A group the allocator sent takes sites only in its own zone.
            if (AssignedZoneId != null) _selectedArea = FindZoneArea(AssignedZoneId);
            RecordZoneEvaluations();
            int selectedTier = _selectedArea < 0 ? -1 : EnemyZoneEvaluator.GetTier(
                _zoneObjectives.Objectives[_selectedArea], _zoneAssessments[_selectedArea].Verdict, _behindOnZones);
            _rankedCaptures.Sort(RANK_CAPTURES);
            foreach (RankedCapture ranked in _rankedCaptures)
            {
                EnemyCaptureObjective candidate = ranked.Candidate;
                bool isCurrent = IsCurrentCapture(candidate);
                if (best != null && !isCurrent) continue;
                int area = _zoneObjectives.GetArea(candidate);
                if (AssignedZoneId != null && area != _selectedArea)
                {
                    CountCaptureFilter("otherZone");
                    continue;
                }
                EnemyZoneAssessment zone = _zoneAssessments[area];
                bool centre = IsCentreObjective(candidate.Position);
                // A site is taken only in a Go zone of the chosen tier (the chosen zone first by rank, the next when
                // its sites are unreachable), as a threatened own site or as the kept objective. The kept objective
                // survives a short dip and a margin below the commit line.
                // A group the allocator sent commits to its zone; its own local fight decides when it withdraws.
                bool viable = AssignedZoneId != null || zone.Verdict == EnemyZoneVerdict.Go && (candidate.IsThreatened || isCurrent ||
                              EnemyZoneEvaluator.GetTier(_zoneObjectives.Objectives[area], zone.Verdict,
                                  _behindOnZones) == selectedTier) ||
                              isCurrent && zone.Verdict == EnemyZoneVerdict.Assemble &&
                              zone.Advantage >= commit * EnemyZoneEvaluator.RETAIN_SCALE;
                if (!viable && isCurrent && zone.Verdict == EnemyZoneVerdict.Assemble)
                {
                    if (_captureUnsafeSince < 0f) _captureUnsafeSince = now;
                    if (now - _captureUnsafeSince < UNSAFE_GRACE_SECONDS)
                    {
                        viable = true;
                        AppendCaptureReason(FormattableString.Invariant(
                            $"Kept {candidate.Id} through unsafe grace: zone advantage {zone.Advantage:F2}."));
                    }
                    else invalidReason = centre ? "blockedApproach" : "unsafe";
                }
                else if (viable && isCurrent) _captureUnsafeSince = -1f;
                if (!viable)
                {
                    CountCaptureFilter(zone.Verdict == EnemyZoneVerdict.Go ? "otherZone" : "zone" + zone.Verdict);
                    continue;
                }
                if (!IsDestinationReachable(origin, candidate.Position, candidate.Radius))
                {
                    if (isCurrent) invalidReason = "unreachable";
                    AppendCaptureReason($"Rejected {candidate.Id}: {_objectiveSafetyReason}");
                    CountCaptureFilter(GetSafetyReasonKey(_objectiveSafetyReason));
                    continue;
                }
                if (isCurrent)
                {
                    float distance = CaptureDistance(candidate);
                    if (_captureIsExecuting) _captureStallSeconds += Mathf.Max(0f, now - _captureEvaluatedAt);
                    if (distance + 1f < _captureDistance || Mathf.Abs(candidate.Progress - _captureProgress) > 0.001f)
                    {
                        _captureDistance = distance;
                        _captureProgress = candidate.Progress;
                        _captureStallSeconds = 0f;
                    }
                    if (_captureStallSeconds >= CAPTURE_STALL_SECONDS)
                    {
                        _rejectedCaptureId = candidate.Id;
                        _captureRetryAt = now + CAPTURE_STALL_SECONDS;
                        invalidReason = "stalled";
                        continue;
                    }
                }
                float score = ranked.Score;
                if (best == null)
                {
                    best = candidate;
                    bestScore = score;
                }
                if (isCurrent) { retained = candidate; retainedScore = score; }
            }
            bool defensePreemptsCapture = best != null && best.IsThreatened &&
                retained != null && !retained.IsThreatened;
            if (defensePreemptsCapture) invalidReason = "threatenedSite";
            if (retained != null && !defensePreemptsCapture && IsCentreObjective(retained.Position))
            {
                best = retained;
                bestScore = retainedScore;
            }
            string selected = _captureCommitment.Select(best == null ? null : best.Id, bestScore,
                _captureObjective == null ? null : _captureObjective.Id, retainedScore,
                retained != null && !defensePreemptsCapture, now, invalidReason);
            EnemyCaptureObjective next = retained != null && retained.Id == selected ? retained : best;
            if (_captureCommitment.Changed)
            {
                RecordStabilityEvent("objectiveSwitch", FormattableString.Invariant(
                    $"domain=capture; fromId={_captureCommitment.LastFromId ?? "none"}; toId={selected ?? "none"}; fromScore={retainedScore:F3}; toScore={_captureCommitment.LastToScore:F3}; margin={EnemyTargetCommitment.SWITCH_MARGIN:F2}; heldSeconds={_captureCommitment.LastHeldSeconds:F1}; cause={_captureCommitment.LastCause}"));
                _captureStallSeconds = 0f;
                _captureUnsafeSince = -1f;
                _captureIsExecuting = false;
                _captureDistance = next == null ? 0f : CaptureDistance(next);
                _captureProgress = next == null ? 0f : next.Progress;
            }
            _captureEvaluatedAt = now;
            _captureObjective = next;
            CaptureReservedByOtherGroup &= next == null;
            if (next == null && _selectedArea >= 0 &&
                _zoneAssessments[_selectedArea].Verdict == EnemyZoneVerdict.Assemble)
            {
                // Too weak for every zone: gather for the cheapest one instead of standing idle.
                _assembleArea = _selectedArea;
            }
            _captureReason += next != null ? $" Selected {next.Id}." : _assembleArea >= 0
                ? FormattableString.Invariant(
                    $" Assemble for {_zoneObjectives.Objectives[_assembleArea].Id}: needs {_zoneAssessments[_assembleArea].RequiredScale:F2}x this force.")
                : " NoViableObjective: no zone to take.";
            RecordCaptureBlocked(next == null && _assembleArea < 0, now);
            return next;
        }

        private int FindZoneArea(string zoneId)
        {
            IReadOnlyList<EnemyZoneObjective> zones = _zoneObjectives.Objectives;
            for (int i = 0; i < zones.Count; i++)
                if (zones[i].Id == zoneId) return i;
            return -1;
        }

        private float GetGroupSpeed()
        {
            float speed = float.PositiveInfinity;
            foreach (IShipEntity ship in _ownShips)
                if (ship.NavigationSpeed > 0f) speed = Mathf.Min(speed, ship.NavigationSpeed);
            return float.IsPositiveInfinity(speed) ? 1f : speed;
        }

        /// <summary>One <c>zoneEval</c> row per zone whenever its verdict for this group changes.</summary>
        private void RecordZoneEvaluations()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            IReadOnlyList<EnemyZoneObjective> zones = _zoneObjectives.Objectives;
            for (int i = 0; i < zones.Count; i++)
            {
                EnemyZoneObjective zone = zones[i];
                EnemyZoneAssessment assessment = _zoneAssessments[i];
                if (_reportedVerdicts.TryGetValue(zone.Id, out EnemyZoneVerdict last) && last == assessment.Verdict)
                    continue;
                _reportedVerdicts[zone.Id] = assessment.Verdict;
                Vector3 staging = assessment.Verdict == EnemyZoneVerdict.Assemble
                    ? GetStagingPoint(i, _home) : _zoneObjectives.GetCentre(i);
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "zoneEval", FormattableString.Invariant(
                    $"zone={zone.Id};verdict={assessment.Verdict};selected={i == _selectedArea};required={assessment.RequiredScale:F2};advantage={assessment.Advantage:F2};assigned={_ownShips.Count};defenders={zone.Defenders.UnitCount};statics={zone.StaticDefenders};eta={zone.TravelSeconds:F1};score={assessment.Score:F0};priority={zone.Priority:F0};staging=({staging.x:F0} {staging.z:F0});mainForce={_preferStrategicObjectives};assignedZone={AssignedZoneId ?? "none"};behind={_behindOnZones};reason={assessment.Reason};battleTime={BattleClock.CurrentElapsed:F1}"));
            }
#endif
        }

        private void CountCaptureFilter(string reason)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _captureFilterCounts.TryGetValue(reason, out int count);
            _captureFilterCounts[reason] = count + 1;
#endif
        }

        // Safety reasons embed the measured numbers; the text before the first digit names the rule.
        private static string GetSafetyReasonKey(string reason)
        {
            int end = 0;
            while (end < reason.Length && !char.IsDigit(reason[end])) end++;
            return reason.Substring(0, end).Trim();
        }

        /// <summary>
        /// Every rule that removed a capture candidate from this group, with counts, when no objective is left.
        /// The decision reason is cut at 512 characters and omits the silent filters.
        /// </summary>
        private void RecordCaptureBlocked(bool blocked, float now)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (blocked && _ownShips.Count > 0 && now >= _nextCaptureBlockedReport)
            {
                _nextCaptureBlockedReport = now + CAPTURE_BLOCKED_REPORT_INTERVAL;
                var text = new System.Text.StringBuilder();
                text.Append(FormattableString.Invariant(
                    $"ships={_ownShips.Count};mainForce={_preferStrategicObjectives};assignedZone={AssignedZoneId ?? "none"};candidates={_captureCandidates.Count};ranked={_rankedCaptures.Count}"));
                foreach (KeyValuePair<string, int> pair in _captureFilterCounts)
                    text.Append(';').Append(pair.Key).Append('=').Append(pair.Value);
                EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "captureBlocked", text.ToString());
            }
            _captureFilterCounts.Clear();
#endif
        }

        private bool IsCurrentCapture(EnemyCaptureObjective candidate) =>
            _captureObjective != null && candidate.Id == _captureObjective.Id &&
            candidate.Owner == _captureObjective.Owner;

        /// <summary>Threatened first, then higher score, then original order: the order the old linear scan preferred.</summary>
        private static int CompareRankedCaptures(RankedCapture left, RankedCapture right)
        {
            if (left.Candidate.IsThreatened != right.Candidate.IsThreatened)
                return left.Candidate.IsThreatened ? -1 : 1;
            int score = right.Score.CompareTo(left.Score);
            return score != 0 ? score : left.Index.CompareTo(right.Index);
        }

        private readonly struct RankedCapture
        {
            public readonly EnemyCaptureObjective Candidate;
            public readonly float Score;
            public readonly int Index;

            public RankedCapture(EnemyCaptureObjective candidate, float score, int index)
            {
                Candidate = candidate;
                Score = score;
                Index = index;
            }
        }

        public void RecordStabilityEvent(string kind, string details) =>
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, kind, details);

        public void ReportCaptureExecution(EnemyCaptureObjective objective, float now)
        {
            _captureIsExecuting = objective != null && _captureObjective != null &&
                objective.Id == _captureObjective.Id && objective.Kind == _captureObjective.Kind &&
                objective.Owner == _captureObjective.Owner;
            _captureEvaluatedAt = now;
        }

        private void AppendCaptureReason(string reason)
        {
            if (_captureReason.Length < 512) _captureReason += " " + reason;
        }

        private float GetCaptureScore(Vector3 origin, EnemyCaptureObjective candidate)
        {
            // Value is measured in equivalent travel distance. Relays open deployment lanes; mines fund production.
            float value = candidate.Kind == EnemyCaptureObjectiveType.ReinforcementZone
                ? Mathf.Max(0f, 400f - PlanarDistance(_strategicCenter, candidate.Position) * 0.25f)
                : candidate.IsMine ? 300f : 0f;
            if (IsLowPriorityAllyMine(candidate))
                value -= ALLY_BASE_MINE_PENALTY;
            return GetTravelCost(origin, candidate.Position) - value - candidate.PriorityBonus;
        }

        private bool IsCentreObjective(Vector3 position)
        {
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            return centre != null && EnemyShipTaskGroupPlanner.IsInZone(centre, position);
        }

        private float GetTravelCost(Vector3 origin, Vector3 position)
        {
            return PlanarDistance(origin, position) * (1f + _ionRiskWeight * GetIonCost(position));
        }

        private float CaptureDistance(EnemyCaptureObjective objective)
        {
            float distance = float.MaxValue;
            foreach (IShipEntity ship in _ownShips)
                distance = Mathf.Min(distance, Mathf.Max(0f, PlanarDistance(ship.WorldPosition, objective.Position) - objective.Radius));
            return distance;
        }

        private bool IsObjectiveSafe(Vector3 origin, Vector3 destination, float radius, bool escaping,
            float safetyScale = 1f, string objectiveId = null)
        {
            _objectiveSafetyReason = "";
            if (_ownShips.Count == 0)
            {
                _objectiveSafetyReason = "no ships are available.";
                return false;
            }
            float clearance = escaping ? 0f : float.MaxValue;
            bool alreadyInside = false;
            foreach (IShipEntity ship in _ownShips)
            {
                clearance = escaping ? Mathf.Max(clearance, ship.NavigationRadius) :
                    Mathf.Min(clearance, ship.NavigationRadius);
                float distance = PlanarDistance(ship.WorldPosition, destination);
                if (escaping && distance > EnemyRecoveryAnchor.LOCAL_ANCHOR_RANGE)
                {
                    _objectiveSafetyReason = "retreat destination is too far from a ship in this group.";
                    return false;
                }
                alreadyInside |= distance <= radius;
            }
            // Strategic areas ignore transient reservations (including this fleet's own positions).
            // Tactical formation placement still enforces ship clearance on the issued orders.
            if (!escaping && !alreadyInside &&
                _navigation.IsPositionClear(destination, clearance, includeReservations: false) &&
                !_navigation.IsPositionClear(destination, clearance, includeReservations: true))
                AppendCaptureReason("transientReservation: ship reservations, including own fleet, ignored for strategic fit.");
            if (!alreadyInside && !_navigation.IsPositionClear(destination, clearance, includeReservations: false))
            {
                bool hasClearPoint = false;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f;
                    Vector3 point = destination + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * 0.5f;
                    hasClearPoint |= _navigation.IsPositionClear(point, clearance, includeReservations: false);
                }
                if (!hasClearPoint)
                {
                    _objectiveSafetyReason = "obstacle: no clear point fits the capturing ships.";
                    return false;
                }
            }
            if (GetIonCost(destination) > GetIonCost(origin))
            {
                _objectiveSafetyReason = "destination ion cost exceeds the current position.";
                return false;
            }
            if (!escaping && objectiveId != null && PlanarDistance(origin, destination) <= Mathf.Epsilon)
            {
                ForceComposition cachedThreat = _teamPicture.GetOrBuildObjectiveThreat(objectiveId, destination, radius);
                return IsThreatSafe(cachedThreat, safetyScale, escaping);
            }
            return IsRouteSafe(origin, destination, radius, escaping, safetyScale);
        }

        /// <summary>
        /// Hard reachability only: a clear point in the objective fits the group and the destination is not deeper in
        /// an ion field than the group already is. Hostile strength is the zone verdict's job.
        /// </summary>
        private bool IsDestinationReachable(Vector3 origin, Vector3 destination, float radius)
        {
            _objectiveSafetyReason = "";
            if (_ownShips.Count == 0)
            {
                _objectiveSafetyReason = "no ships are available.";
                return false;
            }
            float clearance = float.MaxValue;
            bool alreadyInside = false;
            foreach (IShipEntity ship in _ownShips)
            {
                clearance = Mathf.Min(clearance, ship.NavigationRadius);
                alreadyInside |= PlanarDistance(ship.WorldPosition, destination) <= radius;
            }
            if (!alreadyInside && !_navigation.IsPositionClear(destination, clearance, includeReservations: false))
            {
                bool hasClearPoint = false;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f;
                    Vector3 point = destination + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * 0.5f;
                    hasClearPoint |= _navigation.IsPositionClear(point, clearance, includeReservations: false);
                }
                if (!hasClearPoint)
                {
                    _objectiveSafetyReason = "obstacle: no clear point fits the capturing ships.";
                    return false;
                }
            }
            if (GetIonCost(destination) > GetIonCost(origin))
            {
                _objectiveSafetyReason = "destination ion cost exceeds the current position.";
                return false;
            }
            return true;
        }

        /// <summary>Known hostiles at the origin, along the route or near the destination are beatable.</summary>
        private bool IsRouteSafe(Vector3 origin, Vector3 destination, float radius, bool escaping, float safetyScale = 1f)
        {
            _routeOrigin = origin;
            _routeDestination = destination;
            _routeSegment = destination - origin;
            _routeSegment.y = 0f;
            _routeDestinationRadius = LOCAL_ENGAGEMENT_RADIUS + radius;
            _routeEscaping = escaping;
            if (IsInsideStaticDefense(origin, destination, radius))
            {
                _objectiveSafetyReason = "hostile: the route or destination is inside a visible static defense range.";
                return false;
            }
            _teamPicture.BuildKnownFromConfident(_objectiveThreat, _includeRouteThreat);
            if (escaping && !_objectiveThreat.IsEmpty)
            {
                _objectiveSafetyReason = "hostile: the retreat route crosses a known force.";
                return false;
            }
            return IsThreatSafe(_objectiveThreat, safetyScale, escaping);
        }

        /// <summary>Armed hostile structures (satellites, defend platforms) are not units, so intel never lists them.</summary>
        private bool IsInsideStaticDefense(Vector3 origin, Vector3 destination, float radius)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            Vector3 segment = destination - origin;
            segment.y = 0f;
            float lengthSquared = segment.sqrMagnitude;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!EnemyZoneObjectiveCollector.IsArmedHostileStructure(entry, _playerRoster, _vision, _owner.Id))
                    continue;
                Vector3 offset = entry.Position - origin;
                offset.y = 0f;
                float t = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / lengthSquared) : 0f;
                if (PlanarDistance(entry.Position, origin + segment * t) <=
                    EnemyZoneObjectiveCollector.STATIC_DEFENSE_REACH + radius) return true;
            }
            return false;
        }

        private bool IsThreatSafe(ForceComposition threat, float safetyScale, bool escaping)
        {
            if (threat.IsEmpty) return true;
            float advantage = CombatMatchup.Advantage(_groupScoped ? _ownForce : _localOwnForce, threat);
            float threshold = EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage * safetyScale;
            if (advantage > threshold) return true;
            _objectiveSafetyReason = $"hostile: known route/local force advantage {advantage:F2} <= safety threshold {threshold:F2}.";
            return false;
        }

        private bool IncludeRouteThreat(HostileSighting sighting)
        {
            if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner)) return false;
            Vector3 position = ToVector(sighting.Position);
            Vector3 offset = position - _routeOrigin;
            offset.y = 0f;
            float lengthSquared = _routeSegment.sqrMagnitude;
            float t = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(offset, _routeSegment) / lengthSquared) : 0f;
            // Hostiles behind a withdrawing fleet can be left behind; hostiles on its approach cannot.
            if (_routeEscaping && Vector3.Dot(offset, _routeSegment) < 0f) return false;
            bool nearDestination = PlanarDistance(position, _routeDestination) <= _routeDestinationRadius;
            bool alongRoute = PlanarDistance(position, _routeOrigin + _routeSegment * t) <= LOCAL_ENGAGEMENT_RADIUS;
            return nearDestination || alongRoute;
        }

        private Vector3? FindRetreatAnchor(Vector3 origin, GameEntity ownBase)
        {
            if (_ownShips.Count == 0) return null;
            return _recovery.FindRetreatAnchor(origin, ownBase != null,
                (position, radius) => IsObjectiveSafe(origin, position, radius, true));
        }

        private float GetHostileEconomyPressure(PlayerId enemy)
        {
            _hostileIncome = 0f;
            if (enemy.IsNone) return 1f;

            // Base income provides a floor before the first mines are scouted.
            const float BASE_INCOME_ESTIMATE = 15f;
            const float UNDEVELOPED_MINE_VALUE = 2f;
            float alliedIncome = BASE_INCOME_ESTIMATE;
            float hostileIncome = BASE_INCOME_ESTIMATE;
            foreach (AiWorldSnapshot.Entry structure in _worldSnapshot.RaidStructures)
                if (structure.IsMiningFacility && _playerRoster.IsAllied(_owner.Id, structure.Owner) &&
                    !structure.Entity.HealthModel.IsDestroyed)
                    alliedIncome += structure.Entity.GetFacade<IMiningFacilityFacade>().GrossIncome;
            foreach (HostileSighting mine in _intel.Mines)
                if (_playerRoster.IsAllied(enemy, mine.Owner)) hostileIncome += mine.Income;
            foreach (IEnemyCaptureObjectiveSource source in _captureSources)
            {
                if (source is not EnemySiteObjectives sites) continue;
                var claims = sites.CountKnownMineClaims(enemy, _vision);
                alliedIncome += claims.Allied * UNDEVELOPED_MINE_VALUE;
                hostileIncome += claims.Hostile * UNDEVELOPED_MINE_VALUE;
            }

            // Each allied AI faces its share of the enemy team's unseen fleet; the pressure ratio stays team-wide.
            _hostileIncome = hostileIncome / Mathf.Max(1, CountLivingAlliedPlayers());
            return Mathf.Clamp(hostileIncome / alliedIncome, 1f, 2f);
        }

        private float EstimateUnseenFleetInvestment(PlayerId enemy, float now, float ownFleetInvestment)
        {
            if (enemy.IsNone || BattleClock.CurrentElapsed <= 0f) return 0f;
            bool hasSeenHostileShip = false;
            float lastSeen = now - BattleClock.CurrentElapsed;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (sighting.UnitTypeId.IsShip && _playerRoster.IsAllied(enemy, sighting.Owner))
                {
                    hasSeenHostileShip = true;
                    lastSeen = Mathf.Max(lastSeen, sighting.SeenAt);
                }
            }
            if (!hasSeenHostileShip && BattleClock.CurrentElapsed < OPENING_UNSEEN_GRACE_SECONDS)
                return 0f;
            // Mine sightings carry effective income, including the last observed production modifiers.
            float unseen = _hostileIncome * Mathf.Clamp(now - lastSeen, 0f, UNSEEN_FLEET_WINDOW) *
                           HOSTILE_INCOME_TO_FLEET_SHARE +
                           (_focusTeamForce.IsEmpty ? UNSCOUTED_FLEET_INVESTMENT : 0f);
            if (ownFleetInvestment > 0f)
                unseen = Mathf.Min(unseen, ownFleetInvestment * MAX_UNSEEN_INVESTMENT_SHARE);
            return unseen;
        }

        /// <summary>
        /// Uncontested capturable zones held by this AI's team (allies included). Two teams: a majority means more
        /// than half. Three or more: more than every enemy team and at least an even share.
        /// </summary>
        private int CountTeamHeldZones(PlayerId self, out bool hasZoneMajority)
        {
            _enemyTeamZones.Clear();
            _rosterTeams.Clear();
            foreach (PlayerSlot player in _playerRoster.Players) _rosterTeams.Add(player.Team);
            int capturableZones = 0;
            int teamHeld = 0;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (!zone.IsCapturable) continue;
                capturableZones++;
                if (zone.IsContested || zone.Owner.IsNone) continue;
                if (_playerRoster.IsAllied(self, zone.Owner))
                {
                    teamHeld++;
                    continue;
                }
                TeamId team = _playerRoster.Get(zone.Owner).Team;
                _enemyTeamZones.TryGetValue(team, out int held);
                _enemyTeamZones[team] = held + 1;
            }
            if (_rosterTeams.Count <= 2)
            {
                hasZoneMajority = 2 * teamHeld > capturableZones;
                return teamHeld;
            }
            int strongestEnemy = 0;
            foreach (int held in _enemyTeamZones.Values) strongestEnemy = Mathf.Max(strongestEnemy, held);
            hasZoneMajority = teamHeld > strongestEnemy && teamHeld * _rosterTeams.Count >= capturableZones;
            return teamHeld;
        }

        /// <summary>Some enemy team holds more uncontested zones than this team; reads the last zone count.</summary>
        private bool IsBehindOnZones(int teamHeld)
        {
            foreach (int held in _enemyTeamZones.Values)
                if (held > teamHeld) return true;
            return false;
        }

        /// <summary>
        /// Where an outmatched group pulls back to: the nearest uncontested team-held zone with no known hostile near
        /// it, otherwise home. Null (fight in place) while defending the station or a held zone, with no local fight,
        /// when already at the point, or when a force the group cannot beat stands ahead on the way back (cut off).
        /// </summary>
        private Vector3? FindWithdrawPoint(Vector3 origin, Vector3 home)
        {
            if (_ownShips.Count == 0 || _localThreatForce.IsEmpty || _stationPressure.HasPressure) return null;
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (_playerRoster.IsAllied(_owner.Id, station.Owner) &&
                    PlanarDistance(origin, station.Position) <= LOCAL_ENGAGEMENT_RADIUS) return null;
            Vector3 best = home;
            float bestDistance = PlanarDistance(origin, home);
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (zone.IsContested || zone.Owner.IsNone || !_playerRoster.IsAllied(_owner.Id, zone.Owner)) continue;
                if (EnemyShipTaskGroupPlanner.IsInZone(zone, origin)) return null;
                if (_zoneObjectives.HasHostileNear(zone.Center, zone.Radius)) continue;
                float distance = PlanarDistance(origin, zone.Center);
                if (distance >= bestDistance) continue;
                best = ToVector(ToPoint(zone.Center));
                bestDistance = distance;
            }
            if (bestDistance <= FleetCohesion.RADIUS || IsCutOff(origin, best)) return null;
            return best;
        }

        /// <summary>
        /// A known force the group cannot beat stands ahead on the way to <paramref name="destination"/>: past the
        /// current fight and within weapon reach of the route. Hostiles beside or behind the group do not cut it off.
        /// </summary>
        private bool IsCutOff(Vector3 origin, Vector3 destination)
        {
            _routeOrigin = origin;
            _routeSegment = destination - origin;
            _routeSegment.y = 0f;
            _teamPicture.BuildKnownFromConfident(_objectiveThreat, _includeWithdrawBlocker);
            return !_objectiveThreat.IsEmpty && CombatMatchup.Advantage(_ownForce, _objectiveThreat) <=
                EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage;
        }

        private bool IncludeWithdrawBlocker(HostileSighting sighting)
        {
            if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner)) return false;
            Vector3 position = ToVector(sighting.Position);
            Vector3 offset = position - _routeOrigin;
            offset.y = 0f;
            float length = _routeSegment.magnitude;
            float along = Vector3.Dot(offset, _routeSegment) / length;
            if (along < WITHDRAW_BLOCK_REACH) return false;
            return PlanarDistance(position, _routeOrigin + _routeSegment * Mathf.Clamp01(along / length)) <=
                WITHDRAW_BLOCK_REACH;
        }

        // Income is shared with allies, so a free mine beside an ally's station is taken after other free mines.
        private bool IsLowPriorityAllyMine(EnemyCaptureObjective candidate)
        {
            if (!candidate.IsMine || candidate.IsThreatened || candidate.BlockingFacilityId.HasValue) return false;
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (station.Owner != _owner.Id && _playerRoster.IsAllied(_owner.Id, station.Owner) &&
                    PlanarDistance(candidate.Position, station.Position) <= LOCAL_ENGAGEMENT_RADIUS)
                    return true;
            return false;
        }

        private int CountLivingAlliedPlayers()
        {
            _livingAllies.Clear();
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (_playerRoster.IsAllied(_owner.Id, station.Owner) && !station.Entity.HealthModel.IsDestroyed)
                    _livingAllies.Add(station.Owner);
            return _livingAllies.Count;
        }

        private bool IsNearHostileBase(Vector3 position)
        {
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (_playerRoster.IsHostile(_owner.Id, station.Owner) &&
                    PlanarDistance(position, station.Position) <= BASE_EXCLUSION_MARGIN)
                    return true;
            return false;
        }

        private void UpdateRecovery(Vector3 origin, GameEntity ownBase, float now)
        {
            bool hasOwnStation = ownBase != null && ownBase.Owner == _owner.Id;
            bool inLocalFight = !_localThreatForce.IsEmpty;
            float fleetAdvantage = CombatMatchup.Advantage(_ownTeamForce, _focusTeamForce) /
                GetHostileEconomyPressure(FindFocusEnemy(origin));
            float localAdvantage = CombatMatchup.Advantage(_localOwnForce, _localThreatForce);
            if (_ownShips.Count == 0 && ownBase != null)
                origin = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
            bool weakInLocalFight = inLocalFight &&
                localAdvantage < EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage;
            _recovery.Update(origin, fleetAdvantage, weakInLocalFight, hasOwnStation, now,
                (position, radius) => _ownShips.Count == 0 || IsObjectiveSafe(origin, position, radius, true),
                !_focusTeamForce.IsEmpty);
        }

        private Vector3? FindNearbyHeldZone(Vector3 origin)
        {
            Vector3? closest = null;
            float distance = LOCAL_ENGAGEMENT_RADIUS;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (zone.IsContested || !_playerRoster.IsAllied(_owner.Id, zone.Owner)) continue;
                Vector3 position = ToVector(ToPoint(zone.Center));
                float candidate = PlanarDistance(origin, position);
                if (candidate > distance) continue;
                closest = position;
                distance = candidate;
            }
            return closest;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.magnitude;
        }

        private List<IShipEntity> GetShips(PlayerId owner)
        {
            _ownShips.Clear();
            foreach (IShipEntity ship in _shipService.Ships)
            {
                // Ships join the service before their entity is registered.
                if (ship.Owner == owner &&
                    _entityLocator.TryGetEntity(ship.EntityId, out GameEntity entity) &&
                    !entity.HealthModel.IsDestroyed)
                {
                    _ownShips.Add(ship);
                }
            }
            return _ownShips;
        }

        private bool IsRegrouped(IReadOnlyList<IShipEntity> ships)
        {
            _formationPositions.Clear();
            for (int i = 0; i < ships.Count; i++) _formationPositions.Add(ToPoint(ships[i].WorldPosition));
            return FleetCohesion.IsTogether(_formationPositions, FleetCohesion.RADIUS);
        }

        private static FormationPoint CalculateFleetCenter(IReadOnlyList<IShipEntity> ships)
        {
            if (ships.Count == 0) return new FormationPoint(0f, 0f);
            float x = 0f;
            float z = 0f;
            for (int i = 0; i < ships.Count; i++)
            {
                FormationPoint position = ToPoint(ships[i].WorldPosition);
                x += position.X;
                z += position.Z;
            }
            return new FormationPoint(x / ships.Count, z / ships.Count);
        }

        /// <summary>
        /// Visible or damage-revealed hostiles whose attack reach covers the station footprint. The defence target is
        /// a ship attacking the station, else the nearest threatening ship; a wing only while no ship threatens. The
        /// previous target is kept while it remains a threat of the same rank, so defenders do not switch every pass.
        /// </summary>
        private GameEntity BuildBaseThreat(GameEntity ownBase)
        {
            _stationThreats.Clear();
            GameEntity target = null;
            GameEntity kept = null;
            int targetRank = int.MaxValue;
            int keptRank = int.MaxValue;
            float closest = float.MaxValue;
            if (ownBase != null)
            {
                float now = Time.time;
                bool recentlyDamaged = _stationPressure.WasDamagedRecently(ownBase.Id, now, DAMAGE_REVEAL_SECONDS);
                if (recentlyDamaged) RevealStationAttackers(ownBase, now);
                Vector3 center = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
                float footprint = ownBase.GetFacade<INavigationFootprintFacade>().NavigationRadius;
                foreach (HostileSighting sighting in _intel.Sightings)
                {
                    if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                        _intel.GetConfidence(sighting, now) <= 0f ||
                        !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity hostile) ||
                        hostile.HealthModel.IsDestroyed || !hostile.HealthModel.HasUnits || hostile.IsCloaked() ||
                        !hostile.TryGetFacade(out IUnitCombatObserverFacade combat)) continue;
                    bool attackingBase = combat.IsAttacking(ownBase);
                    if (!_vision.IsVisible(_owner.Id, hostile) && !(recentlyDamaged && attackingBase)) continue;
                    Vector3 position = hostile.GetFacade<IEntityTransformFacade>().Transform.position;
                    float hostileRadius = hostile.TryGetFacade(out INavigationFootprintFacade body) ? body.NavigationRadius : 0f;
                    float distance = PlanarDistance(position, center);
                    if (!CanThreatenStation(distance, footprint, hostileRadius, combat.WeaponRange)) continue;
                    // Hull-edge reach requires a commanded or weapon-tracked station attack. Other contacts must be
                    // close enough to engage from their centre or at their navigation contact distance.
                    if (!attackingBase && distance > Mathf.Max(combat.WeaponRange, footprint + hostileRadius)) continue;
                    _stationThreats.Add(hostile.Id);
                    int rank = sighting.UnitTypeId.IsShip ? attackingBase ? 0 : 1 : 2;
                    if (hostile.Id == _defenseTargetId)
                    {
                        kept = hostile;
                        keptRank = rank;
                    }
                    if (rank < targetRank || rank == targetRank && distance < closest)
                    {
                        closest = distance;
                        target = hostile;
                        targetRank = rank;
                    }
                }
            }
            // Ships rank 0 and 1 count as one class for keeping: a defender stays on the ship it fights.
            if (kept != null && (keptRank < 2 || targetRank == 2)) target = kept;
            _defenseTargetId = target == null ? 0 : target.Id;
            if (ownBase == null) _stationPressure.Reset();
            else
            {
                IHealthModelObserver health = ownBase.HealthModel;
                float maximum = ownBase.GetFacade<IStationDurabilityFacade>().MaximumDurability;
                _stationPressure.Update(ownBase.Id, health.Hull + health.Shields, maximum, Time.time,
                    target == null ? 0 : target.Id);
                if (target != null)
                {
                    Vector3 knownPosition = target.GetFacade<IEntityTransformFacade>().Transform.position;
                    _stationPressure.ObserveThreatPosition(knownPosition.x, knownPosition.y, knownPosition.z);
                }
            }
            return target;
        }

        /// <summary>
        /// Recent station damage proves its source exists: hostile units now attacking the station are reported to
        /// the team intel even when no scout or radar currently sees them.
        /// </summary>
        private void RevealStationAttackers(GameEntity ownBase, float now)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> units = _worldSnapshot.LivingUnits;
            for (int i = 0; i < units.Count; i++)
            {
                AiWorldSnapshot.Entry entry = units[i];
                if (!entry.HasUnitType || !_playerRoster.IsHostile(_owner.Id, entry.Owner) ||
                    entry.Entity.HealthModel.IsDestroyed || entry.Entity.IsCloaked() ||
                    !entry.Entity.TryGetFacade(out IUnitCombatObserverFacade combat) ||
                    !combat.IsAttacking(ownBase)) continue;
                _intel.Report(new HostileSighting(entry.Id, entry.UnitTypeId, entry.Owner, ToPoint(entry.Position),
                    entry.Hull, entry.Shields, entry.OffenseScale, now));
            }
        }

        private long _defenseTargetId;
        private const float STATION_DAMAGED_FRACTION = 0.98f;
        private const float RAID_CONTEST_RADIUS = 2500f;
        private const float RAID_CONTESTED_SCORE_FACTOR = 4f;

        private GameEntity FindOwnBase(Vector3 origin)
        {
            PlayerId productionHost = _productionAccess.ProductionHost;
            // A station already under pressure stays tracked until it is quiet or lost; the tracker resets per station.
            GameEntity tracked = null;
            GameEntity mostDamaged = null;
            float lowestFraction = STATION_DAMAGED_FRACTION;
            IReadOnlyList<AiWorldSnapshot.Entry> all = _worldSnapshot.AllLiving;
            for (int i = 0; i < all.Count; i++)
            {
                AiWorldSnapshot.Entry entry = all[i];
                if (!entry.HasUnits || entry.Entity.HealthModel.IsDestroyed || entry.Owner != productionHost ||
                    !IsPrimaryStation(entry.Entity)) continue;
                if (_stationPressure.HasPressure && entry.Id == _stationPressure.StationId) tracked = entry.Entity;
                float maximum = entry.Entity.GetFacade<IStationDurabilityFacade>().MaximumDurability;
                float fraction = maximum > 0f
                    ? (entry.Entity.HealthModel.Hull + entry.Entity.HealthModel.Shields) / maximum : 1f;
                if (fraction >= lowestFraction) continue;
                lowestFraction = fraction;
                mostDamaged = entry.Entity;
            }
            if (tracked != null) return tracked;
            return mostDamaged ?? FindClosestEntity(IsPrimaryStation, owner => owner == productionHost, origin);
        }

        private static bool IsPrimaryStation(GameEntity entity) =>
            entity.IsPlayerBase() && !entity.TryGetFacade(out ISecondStationFacade _);

        public static bool CanThreatenStation(float distance, float stationRadius, float hostileRadius, float weaponRange)
        {
            return weaponRange > 0f && distance <= stationRadius + hostileRadius + weaponRange;
        }

        /// <summary>Own ships and squadrons within <paramref name="radius"/> of <paramref name="center"/> (ground plane).</summary>
        private void BuildOwnNear(ForceComposition force, Vector3 center, float radius)
        {
            _nearbyCenter = center;
            _nearbyRadiusSquared = radius * radius;
            _forceBuilder.Build(force, _includeNearbyOwn);
        }

        private bool IncludeNearbyOwn(GameEntity entity)
        {
            if (!_playerRoster.IsAllied(_owner.Id, entity.Owner)) return false;
            Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - _nearbyCenter;
            offset.y = 0f;
            return offset.sqrMagnitude <= _nearbyRadiusSquared;
        }

        /// <summary>Hostile ships and squadrons within <paramref name="radius"/> of <paramref name="center"/> (ground plane).</summary>
        private void BuildHostilesNear(ForceComposition force, Vector3 center, float radius)
        {
            float radiusSquared = radius * radius;
            _teamPicture.BuildKnownFromConfident(force, sighting =>
            {
                Vector3 offset = ToVector(sighting.Position) - center;
                offset.y = 0f;
                return offset.sqrMagnitude <= radiusSquared;
            });
        }

        private GameEntity FindClosestEntity(Predicate<GameEntity> hasRole, Predicate<PlayerId> includeOwner, Vector3 origin)
        {
            GameEntity closest = null;
            float closestDistance = float.MaxValue;
            IReadOnlyList<AiWorldSnapshot.Entry> all = _worldSnapshot.AllLiving;
            for (int i = 0; i < all.Count; i++)
            {
                AiWorldSnapshot.Entry entry = all[i];
                if (!entry.HasUnits || entry.Entity.HealthModel.IsDestroyed ||
                    !includeOwner(entry.Owner) || !hasRole(entry.Entity))
                {
                    continue;
                }

                float distance = (entry.Position - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = entry.Entity;
                    closestDistance = distance;
                }
            }
            return closest;
        }
    }
}
