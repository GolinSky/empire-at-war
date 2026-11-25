using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Utils;
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
using EmpireAtWar.Services.Repair;
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
        /// <summary>The group was sent to one zone by the allocator, which already sized it: it commits every ship.</summary>
        public bool IsZoneForce { get; set; }
        /// <summary>
        /// What an assault attacks now (S1a, S1b): a hostile ship fighting near the fleet (on the way or at the base),
        /// else a known hostile ship at the base, else a visible armed platform there, else the station itself.
        /// </summary>
        public GameEntity AssaultTarget { get; set; }
        /// <summary><c>canAssault</c> parts of this build (W43 telemetry).</summary>
        public string AssaultGate { get; set; } = "";
        /// <summary>Where the local fight was rated: <c>body</c>, <c>all</c> or <c>death</c> (W43 telemetry).</summary>
        public string LocalOrigin { get; set; } = "all";

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
        /// <summary>W48: under a last stand the reserve gathers and guards here, before any other gather point.</summary>
        public Vector3? LastStandPoint { get; set; }
        /// <summary>
        /// W47: where the planner keeps this zone force once its zone has no site left to take (garrison, dwell, whole
        /// force while hostiles approach): the zone centre, or the hold-back point while <see cref="ZoneHoldsBack"/>.
        /// An own satellite in the zone is guarded first unless the force holds back.
        /// </summary>
        public Vector3? ZoneGuardPoint { get; set; }
        /// <summary>W47 HoldBackAndCounter: the zone is owned and quiet; its force waits at <see cref="ZoneGuardPoint"/>.</summary>
        public bool ZoneHoldsBack { get; set; }
        /// <summary>
        /// Ships the group's executor committed with its body on the last order. The local fight is rated around
        /// them, so idle or straggling group ships do not pull the rating away from the fight (W32).
        /// </summary>
        public IReadOnlyList<IShipEntity> CommittedBody { get; set; }
        /// <summary>The group's last decision was <see cref="EnemyStrategicState.AssaultBase"/>.</summary>
        public bool AssaultUnderWay { get; set; }
        /// <summary><c>canAssault</c> parts of the last build, for the decision row (W43).</summary>
        public string AssaultGate { get; private set; } = "";
        /// <summary>Fleet advantage that keeps an assault under way when its opening conditions lapse (W33).</summary>
        public const float ASSAULT_KEEP_ADVANTAGE = 2f;
        /// <summary>Where the last local rating was centred: <c>body</c>, <c>all</c> or <c>death</c> (telemetry).</summary>
        public string LocalOriginSource { get; private set; } = "all";
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
        /// Inside a hostile zone the relay is taken before its pads: about one zone hop of travel.
        /// </summary>
        private const float HOSTILE_RELAY_FIRST_BONUS = 1000f;
        /// <summary>Ships route this far outside twice the radius of a hostile-held centre: one common weapon range.</summary>
        private const float CENTRE_ROUTE_MARGIN = 500f;
        /// <summary>Known hostile ships this close to the line from the pool to a zone count as its defenders.</summary>
        public const float ROUTE_DEFENDER_REACH = 800f;
        private readonly ForceComposition _routeDefenders = new ForceComposition();
        private readonly Predicate<HostileSighting> _includeRouteDefender;
        private Vector3 _routeDefenderOrigin;
        private Vector3 _routeDefenderEnd;
        private float _routeDefenderSkip;
        /// <summary>Hostile satellites come before the relay: order inside a hostile zone is satellites → relay → pads.</summary>
        private const float HOSTILE_SATELLITE_FIRST_BONUS = 2000f;
        /// <summary>A hostile repair satellite that heals other hostile structures falls before them.</summary>
        private const float HOSTILE_REPAIRER_FIRST_BONUS = 3000f;
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
        // W47: per-player zone memory and the hold/plan inputs of the pool zones; shared by every group builder.
        private readonly EnemyZoneCaution _zoneCaution;
        private readonly EnemyZoneHoldAssessor _zoneHold;
        private readonly ForceComposition _zoneOwnFleet = new ForceComposition();
        private readonly Predicate<GameEntity> _includeOwnUnit;
        private readonly Func<Vector3, float> _rateZoneReach;
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
        private readonly List<AiWorldSnapshot.Entry> _teamAssets = new List<AiWorldSnapshot.Entry>();
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

        /// <summary>A known hostile ship this close to a relay makes it no landing point for a capital (W38).</summary>
        public const float CAPITAL_LANDING_SAFE_RADIUS = 1000f;

        /// <summary>
        /// The allied relay nearest <paramref name="rally"/> (the one a rally landing picks unless a station is nearer)
        /// takes a capital only when it is held by the team, uncontested and no known hostile ship stands within
        /// <see cref="CAPITAL_LANDING_SAFE_RADIUS"/> of it. Executor 1 landed alone at a relay being lost (W38).
        /// </summary>
        public bool IsCapitalRallySafe(Vector3 rally)
        {
            float nearest = float.MaxValue;
            ReinforcementZonePresenter relay = null;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (zone.Owner.IsNone || !_playerRoster.IsAllied(_owner.Id, zone.Owner)) continue;
                float distance = PlanarDistance(zone.Center, rally);
                if (distance >= nearest) continue;
                nearest = distance;
                relay = zone;
            }
            if (relay == null) return true;
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (_playerRoster.IsAllied(_owner.Id, station.Owner) && PlanarDistance(station.Position, rally) <= nearest)
                    return true;
            if (relay.IsContested) return false;
            _teamPicture.EnsureRefreshed(Time.time);
            foreach (HostileSighting sighting in _teamPicture.ConfidentSightings)
                if (sighting.UnitTypeId.IsShip &&
                    PlanarDistance(ToVector(sighting.Position), relay.Center) <= CAPITAL_LANDING_SAFE_RADIUS)
                    return false;
            return true;
        }

        public bool TryGetDefendedStation(out Vector3 position)
        {
            position = default;
            if (!_stationPressure.HasPressure ||
                !_entityLocator.TryGetEntity(_stationPressure.StationId, out GameEntity station) ||
                station.HealthModel.IsDestroyed) return false;
            position = station.GetFacade<IEntityTransformFacade>().Transform.position;
            return true;
        }

        /// <summary>Remembered threats around the defended station; the station attacker sets the rear direction
        /// when no threat is near the station itself.</summary>
        public void FillDefenseDeploymentThreats(PlacementThreatSnapshot threats)
        {
            _recovery.FillPlacementThreats(threats);
            if (_stationPressure.HasThreatPosition)
                threats.SetApproach(_stationPressure.ThreatX, _stationPressure.ThreatZ);
        }

        public AllyAiMarkerSource AlliedMarkers { get; }
        public TeamAiCooperation TeamCooperation => _teamPicture.Cooperation;
        private TacticalMarker _allyDefenseMarker;
        private readonly List<IShipEntity> _ownShips = new List<IShipEntity>();
        private readonly HashSet<long> _ownShipIds = new HashSet<long>();
        private readonly List<FormationPoint> _formationPositions = new List<FormationPoint>();
        private readonly List<float> _formationRadii = new List<float>();
        private readonly Dictionary<IShipEntity, GameEntity> _receivers = new Dictionary<IShipEntity, GameEntity>();
        private readonly Dictionary<IShipEntity, ForceComposition> _shipForces = new Dictionary<IShipEntity, ForceComposition>();
        private readonly Dictionary<GameEntity, ForceComposition> _targetForces = new Dictionary<GameEntity, ForceComposition>();
        private readonly List<ForceComposition> _forcePool = new List<ForceComposition>();
        private int _usedForces;
        private float _ionRiskWeight = 1f;
        // Summed price of _ownForce at the last Build: the reference that turns credits into strength (W63).
        private float _ownForceValue;
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
        private readonly ForceComposition _stationDefenders = new ForceComposition();
        private readonly ForceComposition _fleetForce = new ForceComposition();
        private readonly ForceComposition _outmatchingArmada = new ForceComposition();
        private Predicate<HostileSighting> _includeStationThreat;
        private TacticalMarker _armadaMarker;
        private string _lastArmadaResponse;
        private readonly ForceComposition _localThreatForce = new ForceComposition();
        private readonly ForceComposition _localOwnForce = new ForceComposition();
        private readonly ForceComposition _deathThreatForce = new ForceComposition();
        private readonly ForceComposition _deathOwnForce = new ForceComposition();
        /// <summary>Last known position of each group ship, to find where a group ship died (W32).</summary>
        private readonly Dictionary<long, Vector3> _lastShipPositions = new Dictionary<long, Vector3>();
        private readonly List<long> _goneShipIds = new List<long>();
        private readonly HashSet<IShipEntity> _committedBodySet = new HashSet<IShipEntity>();
        private Vector3 _lastDeathPosition;
        private float _lastDeathTime = float.NegativeInfinity;
        /// <summary>A group ship death re-rates the local fight at its position for this long (W32).</summary>
        private const float DEATH_RERATE_SECONDS = 10f;
        /// <summary>Death this close to the body centre is already inside its local rating.</summary>
        private const float DEATH_RERATE_MIN_DISTANCE = 200f;

        // The player's main builder; group builders share its late-game flags, trade ledgers and reinforcement budget.
        private readonly EnemyStrategicContextBuilder _root;
        private bool _lateGameSituation;
        private bool _teamStationPressure;
        private bool _endGameSituation;
        private bool _pressSituation;
        private int _fleetAssaultCapitals;
        private EnemyReinforcementBudget _ownReinforcement = EnemyReinforcementBudget.IncomeOnly;
        private readonly Dictionary<TeamId, EnemyTradeLedger> _tradeLedgers = new Dictionary<TeamId, EnemyTradeLedger>();
        private readonly EnemyFightRecorder _fight;
        private readonly Func<long, bool> _isHostileGone;
        private readonly Dictionary<long, UnitTypeId> _groupShipTypes = new Dictionary<long, UnitTypeId>();
        // W63 reach rating: what can be at the rated area within EnemyFleetRating.REACH_WINDOW_SECONDS.
        private readonly ForceComposition _reachOwnForce = new ForceComposition();
        private readonly ForceComposition _reachHostileForce = new ForceComposition();
        private readonly ForceComposition _ratedOwnForce = new ForceComposition();
        private readonly ForceComposition _ratedHostileForce = new ForceComposition();
        private readonly Predicate<GameEntity> _includeReachableOwn;
        private readonly Predicate<HostileSighting> _includeReachableHostile;
        private readonly HashSet<long> _reachExcluded = new HashSet<long>();
        private Vector3 _reachArea;
        private bool _reachUnlimited;
        private float _ownIncome;
        private float _ownCredits;
        private float _hostileIncomeCredits;
        // W49: the gathering point a retreating group is heading for, kept until it is reached.
        private Vector3? _retreatPoint;

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
            _includeRouteDefender = IncludeRouteDefender;
            _zoneObjectives = new EnemyZoneObjectiveCollector(zones, playerRoster, worldSnapshot, vision, _teamPicture,
                forceBuilder, _intel, owner.Id);
            _zoneCaution = new EnemyZoneCaution();
            _zoneHold = new EnemyZoneHoldAssessor(zones, playerRoster, worldSnapshot, _teamPicture, forceBuilder, owner.Id,
                _zoneCaution);
            _includeOwnUnit = IncludeOwnUnit;
            _rateZoneReach = RateZoneReach;
            _assessedForce = _ownForce;
            _root = this;
            _fight = new EnemyFightRecorder(GetTradeLedger);
            _isHostileGone = IsHostileGone;
            _includeReachableOwn = IncludeReachableOwn;
            _includeReachableHostile = IncludeReachableHostile;
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
            _includeRouteDefender = IncludeRouteDefender;
            _zoneObjectives = new EnemyZoneObjectiveCollector(_zones, _playerRoster, _worldSnapshot, _vision, _teamPicture,
                _forceBuilder, _intel, _owner.Id);
            _zoneCaution = source._zoneCaution;
            _zoneHold = source._zoneHold;
            _includeOwnUnit = IncludeOwnUnit;
            _rateZoneReach = RateZoneReach;
            _assessedForce = _ownForce;
            _root = source._root;
            _fight = new EnemyFightRecorder(GetTradeLedger);
            _isHostileGone = IsHostileGone;
            _includeReachableOwn = IncludeReachableOwn;
            _includeReachableHostile = IncludeReachableHostile;
        }

        /// <summary>
        /// Bank, free-capacity credits and build delay of this AI (W63 addendum), set by its controller every cycle.
        /// Until set, only income counts as own reinforcement. Shared by every group builder of the player.
        /// </summary>
        public EnemyReinforcementBudget OwnReinforcement
        {
            get => _root._ownReinforcement;
            set => _root._ownReinforcement = value;
        }

        /// <summary>
        /// This player's loss-exchange ledger against <paramref name="hostileTeam"/> (W67), shared by its groups.
        /// <see cref="EnemyTradeLedger.ExpectedGain"/> rates an attack; <see cref="EnemyTradeLedger.GetCorrection"/> the trades.
        /// </summary>
        public EnemyTradeLedger GetTradeLedger(TeamId hostileTeam)
        {
            Dictionary<TeamId, EnemyTradeLedger> ledgers = _root._tradeLedgers;
            if (!ledgers.TryGetValue(hostileTeam, out EnemyTradeLedger ledger))
            {
                ledger = new EnemyTradeLedger();
                ledgers.Add(hostileTeam, ledger);
            }
            return ledger;
        }

        private float GetTradeCorrection(PlayerId hostile, float now) =>
            hostile.IsNone ? 1f : GetTradeLedger(_playerRoster.Get(hostile).Team).GetCorrection(now);

        public EnemyStrategicContextBuilder CreateGroupBuilder() => new EnemyStrategicContextBuilder(this);

        /// <summary>
        /// Rates every zone for the whole ship pool as seen from <paramref name="home"/>: the allocator's input.
        /// Zones an allied AI reserved are left out. Borrowed until the next call on this builder. Priorities carry no
        /// travel from home: the allocator charges the travel of its nearest free ship (W49). Each zone gets its hold
        /// inputs and plan (W47, <see cref="EnemyZoneHoldAssessor"/>).
        /// </summary>
        /// <param name="origin">Where the pool stands; ships on the way from it to a zone defend that zone.</param>
        public IReadOnlyList<EnemyZoneObjective> CollectPoolZones(Vector3 home, float speed, Vector3? origin = null)
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
                _zonePriorities.Add(PlanarDistance(home, candidate.Position) - GetCaptureScore(home, candidate));
            }
            AddKeptZoneCandidates(home);
            _zoneObjectives.Collect(_zoneCandidates, _zonePriorities, home, speed, _isNearHostileBase);
            AddRouteDefenders(origin ?? home);
            ApplyZonePlans(focusEnemy, speed, Time.time);
            return _zoneObjectives.Objectives;
        }

        /// <summary>
        /// W47: hold inputs and plan of every pool zone against the whole own fleet, with the reinforcement credits of
        /// both sides and the trade correction against the focus enemy.
        /// </summary>
        private void ApplyZonePlans(PlayerId focusEnemy, float speed, float now)
        {
            _forceBuilder.Build(_zoneOwnFleet, _includeOwnUnit);
            GetHostileEconomyPressure(focusEnemy);
            UpdateReinforcementCredits();
            _reachExcluded.Clear();
            foreach (AiWorldSnapshot.Entry entry in _worldSnapshot.GetOwnedUnits(_owner.Id)) _reachExcluded.Add(entry.Id);
            _zoneHold.Apply(_zoneObjectives, now, speed, _zoneOwnFleet, OwnReinforcement, _hostileIncome,
                GetTradeCorrection(focusEnemy, now), _rateZoneReach);
        }

        private bool IncludeOwnUnit(GameEntity entity) => entity.Owner == _owner.Id;

        /// <summary>
        /// W47: a zone the team has taken stays a pool zone while this player's force sent to it is still there (its
        /// expansion reservation of the last cycle), even with no site left to capture: the allocator then decides who
        /// stays (dwell, garrison) instead of every ship leaving the moment the relay turns.
        /// </summary>
        private void AddKeptZoneCandidates(Vector3 home)
        {
            IReadOnlyList<ReinforcementZonePresenter> zones = _zones.Zones;
            for (int i = 0; i < zones.Count; i++)
            {
                ReinforcementZonePresenter zone = zones[i];
                if (!zone.IsCapturable || zone.Owner.IsNone || !_playerRoster.IsAllied(_owner.Id, zone.Owner) ||
                    !_teamPicture.Cooperation.IsReservedBy(_owner.Id, GetExpansionReservationId(zone)) ||
                    HasZoneCandidate(zone)) continue;
                var kept = new EnemyCaptureObjective(_teamPicture.GetZoneId(i), EnemyCaptureObjectiveType.ReinforcementZone,
                    zone.Owner, zone.Center, zone.Radius, 1f);
                _zoneCandidates.Add(kept);
                _zonePriorities.Add(PlanarDistance(home, kept.Position) - GetCaptureScore(home, kept));
            }
        }

        private bool HasZoneCandidate(ReinforcementZonePresenter zone)
        {
            foreach (EnemyCaptureObjective candidate in _zoneCandidates)
                if (EnemyShipTaskGroupPlanner.IsInZone(zone, candidate.Position)) return true;
            return false;
        }

        private float RateZoneReach(Vector3 area) => RateReachAt(_zoneOwnFleet, _zoneOwnFleet.Value, area, false);

        /// <summary>
        /// Known hostile ships within <see cref="ROUTE_DEFENDER_REACH"/> of the straight line from the pool to a zone
        /// count as that zone's defenders: a force sent past them fights them first. Held zones are left as they are.
        /// </summary>
        private void AddRouteDefenders(Vector3 origin)
        {
            IReadOnlyList<EnemyZoneObjective> zones = _zoneObjectives.Objectives;
            _routeDefenderOrigin = origin;
            for (int area = 0; area < zones.Count; area++)
            {
                if (zones[area].IsDenied || zones[area].IsHeld) continue;
                _routeDefenderEnd = _zoneObjectives.GetCentre(area);
                ReinforcementZonePresenter zone = _zoneObjectives.GetZone(area);
                // Ships near the zone are already its defenders.
                _routeDefenderSkip = EnemyZoneObjectiveCollector.DEFENDER_REACH + (zone != null ? 2f * zone.Radius : 0f);
                _teamPicture.BuildKnownFromConfident(_routeDefenders, _includeRouteDefender);
                if (!_routeDefenders.IsEmpty) zones[area].Defenders.Add(_routeDefenders);
            }
        }

        private bool IncludeRouteDefender(HostileSighting sighting)
        {
            if (!sighting.UnitTypeId.IsShip || !_playerRoster.IsHostile(_owner.Id, sighting.Owner)) return false;
            Vector3 position = ToVector(sighting.Position);
            if (PlanarDistance(position, _routeDefenderEnd) <= _routeDefenderSkip) return false;
            Vector3 segment = _routeDefenderEnd - _routeDefenderOrigin;
            segment.y = 0f;
            Vector3 offset = position - _routeDefenderOrigin;
            offset.y = 0f;
            float lengthSquared = segment.sqrMagnitude;
            float t = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / lengthSquared) : 0f;
            return PlanarDistance(position, _routeDefenderOrigin + segment * t) <= ROUTE_DEFENDER_REACH;
        }

        /// <summary>The reinforcement zone of a pool zone; null for a lone site.</summary>
        public ReinforcementZonePresenter GetPoolZone(int area) => _zoneObjectives.GetZone(area);

        public Vector3 GetPoolZoneCentre(int area) => _zoneObjectives.GetCentre(area);

        public Vector3 GetPoolStagingPoint(int area, Vector3 home) => GetStagingPoint(area, home);

        /// <summary>
        /// Where the ships of a pool zone with <see cref="EnemyZonePlan.HoldBackAndCounter"/> wait: just outside the zone
        /// edge toward its staging point, ready to hit the retake.
        /// </summary>
        public Vector3 GetPoolHoldBackPoint(int area, Vector3 home) => _zoneObjectives.GetHoldBackPoint(area, home);

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
        /// <summary>
        /// A capture move whose straight line passes a hostile-held centre goes first to a waypoint beside it:
        /// <see cref="CENTRE_ROUTE_MARGIN"/> beyond twice the centre radius, on the side the group comes from. A
        /// target inside the centre, or a group already inside its keep-out circle, keeps the direct line.
        /// </summary>
        private Vector3 RouteAroundHostileCentre(Vector3 origin, Vector3 target)
        {
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            if (centre == null || centre.Owner.IsNone || !_playerRoster.IsHostile(_owner.Id, centre.Owner) ||
                EnemyShipTaskGroupPlanner.IsInZone(centre, target)) return target;
            Vector3 middle = ToVector(ToPoint(centre.Center));
            float keepOut = 2f * centre.Radius + CENTRE_ROUTE_MARGIN;
            if (PlanarGeometry.Distance(origin, middle) <= keepOut) return target;
            Vector3 line = target - origin;
            line.y = 0f;
            float length = line.magnitude;
            if (length < 1f) return target;
            Vector3 direction = line / length;
            Vector3 toMiddle = middle - origin;
            toMiddle.y = 0f;
            float along = Vector3.Dot(toMiddle, direction);
            if (along <= 0f || along >= length) return target;
            Vector3 closest = origin + direction * along;
            Vector3 side = closest - middle;
            side.y = 0f;
            if (side.magnitude >= keepOut) return target;
            Vector3 normal = side.sqrMagnitude > 1f ? side.normalized : new Vector3(-direction.z, 0f, direction.x);
            return middle + normal * keepOut;
        }

        /// <summary>Living stations this AI owns.</summary>
        public int CountOwnStations()
        {
            int count = 0;
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
                if (station.Owner == _owner.Id) count++;
            return count;
        }

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
        /// A Defend request for an allied AI station under pressure when this player's main force is free (no station
        /// pressure of its own) and every answering allied main force together with the station's defenders beats the
        /// attackers (<see cref="TeamAiCooperation.TryGetDefenseAssignment"/>); recovery does not block it. Null otherwise.
        /// </summary>
        /// <remarks>
        /// S5: <paramref name="attacker"/> is the known hostile ship nearest the station (the answer attacks it); null
        /// attack-moves to the station. An answer with no known attacker and no hostile near the group for
        /// <see cref="ALLY_DEFENSE_IDLE_SECONDS"/> is released for <see cref="ALLY_DEFENSE_RELEASE_SECONDS"/>, so the
        /// normal decision (an assault as relief) runs.
        /// </remarks>
        public TacticalMarker SelectAllyDefenseRequest(out GameEntity attacker)
        {
            attacker = null;
            float now = Time.time;
            if (_stationPressure.HasPressure || now < _allyDefenseReleasedUntil ||
                !_teamPicture.Cooperation.TryGetDefenseAssignment(_owner.Id, now,
                    EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage, out PlayerId requester,
                    out Vector3 station))
            {
                _allyDefenseIdleSince = float.NaN;
                return null;
            }
            long id = -100L - requester.Index;
            if (_allyDefenseMarker == null || _allyDefenseMarker.Id != id ||
                (_allyDefenseMarker.Point - station).sqrMagnitude > 1f)
                _allyDefenseMarker = new TacticalMarker(id, TacticalMarkerType.Defend, requester, _owner.Team, station,
                    -1, now, now + TeamAiCooperation.STALE_SECONDS);
            attacker = FindStationAttacker(station, now, out float attackerDistance);
            bool idle = attacker == null && _localThreatForce.IsEmpty;
            if (!idle) _allyDefenseIdleSince = float.NaN;
            else if (float.IsNaN(_allyDefenseIdleSince)) _allyDefenseIdleSince = now;
            float idleSeconds = float.IsNaN(_allyDefenseIdleSince) ? 0f : now - _allyDefenseIdleSince;
            bool release = idleSeconds >= ALLY_DEFENSE_IDLE_SECONDS;
            RecordAllyDefense(requester, attacker, attackerDistance, idleSeconds, release);
            if (!release) return _allyDefenseMarker;
            _allyDefenseReleasedUntil = now + ALLY_DEFENSE_RELEASE_SECONDS;
            _allyDefenseIdleSince = float.NaN;
            attacker = null;
            return null;
        }

        /// <summary>S5: an ally-defence answer with nothing to fight this long is released.</summary>
        public const float ALLY_DEFENSE_IDLE_SECONDS = 30f;
        /// <summary>S5: a released ally-defence answer is not taken again for this long.</summary>
        public const float ALLY_DEFENSE_RELEASE_SECONDS = TeamAiCooperation.ZONE_STRIKE_SECONDS;
        private float _allyDefenseIdleSince = float.NaN;
        private float _allyDefenseReleasedUntil = float.NegativeInfinity;
        private string _lastAllyDefense;

        /// <summary>The known, visible hostile ship within <see cref="LOCAL_ENGAGEMENT_RADIUS"/> of the station nearest it.</summary>
        private GameEntity FindStationAttacker(Vector3 station, float now, out float distance)
        {
            GameEntity nearest = null;
            distance = float.PositiveInfinity;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!sighting.UnitTypeId.IsShip || !_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                    _intel.GetConfidence(sighting, now) <= 0f) continue;
                float candidate = PlanarDistance(station, ToVector(sighting.Position));
                if (candidate > LOCAL_ENGAGEMENT_RADIUS || candidate >= distance ||
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.HealthModel.IsDestroyed || entity.IsCloaked() || !_vision.IsVisible(_owner.Id, entity)) continue;
                nearest = entity;
                distance = candidate;
            }
            return nearest;
        }

        // S7: one allyDefense row per change of requester, target or release; idle seconds drift without a new row.
        private void RecordAllyDefense(PlayerId requester, GameEntity attacker, float distance, float idleSeconds,
            bool release)
        {
            string key = FormattableString.Invariant(
                $"requester={requester};attacker={(attacker == null ? 0 : attacker.Id)};released={release}");
            if (key == _lastAllyDefense) return;
            _lastAllyDefense = key;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "allyDefense", FormattableString.Invariant(
                $"responder={_owner.Id};{key};attackersKnown={attacker != null};nearestAttacker={(attacker == null ? -1f : distance):F0};idleSeconds={idleSeconds:F0}"));
        }

        /// <summary>
        /// A team station is under pressure, or a hostile armada threatens a team station or held zone. Zone forces then
        /// keep only what they need and the rest stays with the main force. Set by <see cref="AssessStation"/>.
        /// </summary>
        public bool LateGameSituation
        {
            get => _root._lateGameSituation;
            private set => _root._lateGameSituation = value;
        }

        /// <summary>A station of this AI or of an allied AI is under pressure. Set by <see cref="AssessStation"/>.</summary>
        public bool TeamStationPressure
        {
            get => _root._teamStationPressure;
            private set => _root._teamStationPressure = value;
        }

        /// <summary>
        /// S4 end game, by situation: the station is at its top level and the unit cap is full or an assault surplus is
        /// banked. The centre no longer holds the fleet; zone forces return to the main force. Set by <see cref="AssessStation"/>.
        /// </summary>
        public bool EndGameSituation
        {
            get => _root._endGameSituation;
            private set => _root._endGameSituation = value;
        }

        /// <summary>
        /// S3: the whole fleet rates at least the attack ratio at the focused hostile station and the unit cap is full
        /// or an assault surplus is banked: zone forces keep only what their zone needs, the rest gathers to strike.
        /// Set by <see cref="AssessStation"/>.
        /// </summary>
        public bool PressSituation
        {
            get => _root._pressSituation;
            private set => _root._pressSituation = value;
        }

        /// <summary>S3: assault capitals over the whole fleet (every group). Set by <see cref="AssessStation"/>.</summary>
        public int FleetAssaultCapitals
        {
            get => _root._fleetAssaultCapitals;
            private set => _root._fleetAssaultCapitals = value;
        }

        /// <summary>
        /// Copies the armada that threatens a team asset and outmatches the whole own fleet with the available allied
        /// main forces, while no team station is under pressure; false when there is none. Production answers it.
        /// </summary>
        public bool TryCopyOutmatchingArmada(ForceComposition armada)
        {
            if (_outmatchingArmada.IsEmpty) return false;
            armada.CopyFrom(_outmatchingArmada);
            return true;
        }

        /// <summary>
        /// The main force's answer to the most dangerous armada that threatens a team station or held zone (W53), rated
        /// like the fleet (W63): what each side can bring to the asset within the reach window, plus reinforcements.
        /// An Attack toward the armada only when the team's ships alone beat it by the attack ratio; else a Defend at
        /// the asset, where its platforms and stations fight too. Null under own station pressure, while hunting,
        /// assaulting, defending or retreating, or with no such armada; a null answer resets the response hold (W49).
        /// </summary>
        public TacticalMarker SelectArmadaResponse(EnemyStrategicState planned, out string reason)
        {
            reason = null;
            float now = Time.time;
            HostileArmada armada = null;
            float advantage = float.PositiveInfinity;
            if (!_stationPressure.HasPressure && _ownShips.Count > 0 && planned is not (EnemyStrategicState.HuntFleet or
                    EnemyStrategicState.AssaultBase or EnemyStrategicState.DefendBase or EnemyStrategicState.RetreatValue))
                foreach (HostileArmada candidate in _teamPicture.Armadas)
                {
                    if (!candidate.ThreatensAsset ||
                        !HostileArmadaTracker.IsArmada(candidate, CombatMatchup.Advantage(_ownForce, candidate.Strength)))
                        continue;
                    float value = RateAgainstArmada(_ownForce, _ownForceValue, candidate, true);
                    if (value >= advantage) continue;
                    armada = candidate;
                    advantage = value;
                }
            if (armada == null)
            {
                _armadaMarker = null;
                _armadaResponseSince = float.NegativeInfinity;
                RecordArmadaResponse("response=none");
                return null;
            }
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(_owner.Difficulty);
            // The platforms at the asset fight only where they stand: away from them the ships must win alone.
            float mobile = RateAgainstArmada(_ownForce, _ownForceValue, armada, false);
            bool intercept = mobile >= profile.RequiredAttackRatio;
            // A response is kept for ARMADA_RESPONSE_HOLD_SECONDS: gather and intercept flipped every 3–40 s (W37).
            if (_armadaMarker != null && now - _armadaResponseSince < ARMADA_RESPONSE_HOLD_SECONDS)
                intercept = _armadaMarker.Type == TacticalMarkerType.Attack;
            TacticalMarkerType type = intercept ? TacticalMarkerType.Attack : TacticalMarkerType.Defend;
            if (_armadaMarker == null || _armadaMarker.Type != type) _armadaResponseSince = now;
            Vector3 point = intercept ? armada.Centre : armada.AssetPosition;
            // Each new point re-orders every ship: keep it while the armada moves less than one weapon range.
            if (_armadaMarker == null || _armadaMarker.Type != type ||
                PlanarDistance(_armadaMarker.Point, point) > ARMADA_REPOINT_DISTANCE)
                _armadaMarker = new TacticalMarker(ARMADA_MARKER_ID, type, _owner.Id, _owner.Team, point, -1, now,
                    now + TeamAiCooperation.STALE_SECONDS);
            bool holds = !intercept && advantage >= profile.RetreatAdvantage;
            reason = intercept
                ? "Intercept: the team's ships beat the armada that threatens its " + armada.AssetKind + "."
                : holds
                    ? "Hold: the team beats the armada that threatens its " + armada.AssetKind + " only under its platforms."
                    : "Gather: an armada stronger than the team threatens its " + armada.AssetKind + "; hold under its platforms.";
            RecordArmadaResponse(FormattableString.Invariant(
                $"response={(intercept ? "intercept" : holds ? "hold" : "gather")};asset={armada.AssetKind};assetAt=({armada.AssetPosition.x:F0} {armada.AssetPosition.z:F0});armadaShips={armada.Ships};advantage={advantage:F2};mobile={mobile:F2}"));
            return _armadaMarker;
        }

        private const long ARMADA_MARKER_ID = -200L;
        /// <summary>Shortest time an intercept or gather answer stands before it may flip (W37).</summary>
        private const float ARMADA_RESPONSE_HOLD_SECONDS = 15f;
        private float _armadaResponseSince = float.NegativeInfinity;
        /// <summary>The intercept point follows the armada only once it moved this far: one common weapon range.</summary>
        private const float ARMADA_REPOINT_DISTANCE = 500f;

        private float RateAgainstArmada(ForceComposition own, float ownValue, HostileArmada armada, bool atAsset) =>
            RateReachAt(own, ownValue, armada.AssetPosition, atAsset);

        /// <summary>
        /// W63 rating at <paramref name="area"/> on the fleet-advantage scale: <paramref name="own"/> (worth
        /// <paramref name="ownValue"/> credits) plus the team's other units that can be within
        /// <see cref="LOCAL_ENGAGEMENT_RADIUS"/> of the area in <see cref="EnemyFleetRating.REACH_WINDOW_SECONDS"/>, against
        /// the known hostile units that can, both sides with the reinforcement credits of the last
        /// <see cref="Build"/>. With <paramref name="withStructures"/> the team's armed platforms and stations at the area
        /// fight too (a fight held there). Call it on a group builder after its Build: the group's own ships are the ones
        /// left out of the team's units.
        /// </summary>
        public float RateReachAt(ForceComposition own, float ownValue, Vector3 area, bool withStructures)
        {
            CollectReach(own, area, false);
            if (withStructures)
            {
                EnemyZoneObjectiveCollector.AddAlliedArmedStructures(_reachOwnForce, _worldSnapshot, _playerRoster,
                    _forceBuilder, _owner.Id, area, LOCAL_ENGAGEMENT_RADIUS);
                AddTeamStations(_reachOwnForce, area, LOCAL_ENGAGEMENT_RADIUS);
            }
            return EnemyFleetRating.Rate(_reachOwnForce, _reachHostileForce, own, ownValue, _ownCredits,
                _hostileIncomeCredits, _ratedOwnForce, _ratedHostileForce);
        }

        /// <summary>W63 reach rating of this group at a hostile target, with the loss-exchange correction (W67).</summary>
        private float RateAtTarget(GameEntity target, float tradeCorrection) => ClampAdvantage(tradeCorrection *
            RateReachAt(_ownForce, _ownForceValue, target.GetFacade<IEntityTransformFacade>().Transform.position, false));

        /// <summary>W47 AllyCombined: asks the team for a combined strike on a pool zone (see <see cref="TeamAiCooperation.RequestZoneStrike"/>).</summary>
        public void RequestAllyZoneStrike(string zoneId, Vector3 centre, Vector3 staging, ForceComposition needed,
            bool committed) =>
            _teamPicture.Cooperation.RequestZoneStrike(_owner.Id, zoneId, centre, staging, needed, committed, Time.time);

        /// <summary>W47 AllyCombined: no zone of this player waits for an ally any more.</summary>
        public void ClearAllyZoneStrike() => _teamPicture.Cooperation.ClearZoneStrike(_owner.Id);

        private const long ZONE_STRIKE_MARKER_ID = -300L;
        private TacticalMarker _zoneStrikeMarker;

        /// <summary>
        /// W47 AllyCombined, answering side: a Defend marker for the main force at an allied player's combined zone
        /// strike (its staging point, then the zone once the ally attacks). Null in the opening, under own or team
        /// station pressure (the answer is released), while the main force defends or retreats, or with no request.
        /// </summary>
        public TacticalMarker SelectAllyZoneStrike(EnemyStrategicState planned, out string reason)
        {
            reason = null;
            float now = Time.time;
            if (_stationPressure.HasPressure || TeamStationPressure)
            {
                _teamPicture.Cooperation.ReleaseZoneStrike(_owner.Id);
                return null;
            }
            if (!HoldsOuterZone() || planned is EnemyStrategicState.DefendBase or EnemyStrategicState.RetreatValue ||
                !_teamPicture.Cooperation.TryGetZoneStrikeAssignment(_owner.Id, now, out PlayerId requester,
                    out Vector3 point)) return null;
            long id = ZONE_STRIKE_MARKER_ID - requester.Index;
            if (_zoneStrikeMarker == null || _zoneStrikeMarker.Id != id || (_zoneStrikeMarker.Point - point).sqrMagnitude > 1f)
                _zoneStrikeMarker = new TacticalMarker(id, TacticalMarkerType.Defend, requester, _owner.Team, point, -1, now,
                    now + TeamAiCooperation.STALE_SECONDS);
            reason = "Joining an allied combined zone strike.";
            return _zoneStrikeMarker;
        }

        /// <summary>W47 AllyCombined: ships of an allied player (not this one) can reach the area in the reach window.</summary>
        public bool CanAlliesReach(Vector3 area)
        {
            _alliedReach.Clear();
            _forceBuilder.AddReachable(_alliedReach, _includeAlliedUnit ??= IncludeAlliedUnit, area, LOCAL_ENGAGEMENT_RADIUS,
                EnemyFleetRating.REACH_WINDOW_SECONDS);
            return !_alliedReach.IsEmpty;
        }

        private readonly ForceComposition _alliedReach = new ForceComposition();
        private Predicate<GameEntity> _includeAlliedUnit;

        private bool IncludeAlliedUnit(GameEntity entity) =>
            entity.Owner != _owner.Id && _playerRoster.IsAllied(_owner.Id, entity.Owner);

        /// <summary>W48: a known hostile armada threatens a team station.</summary>
        public bool IsArmadaThreateningTeamStation()
        {
            foreach (HostileArmada armada in _teamPicture.Armadas)
                if (armada.AssetKind == ArmadaAssetKind.Station) return true;
            return false;
        }

        /// <summary>Team platforms this close to a station make it a last-stand station (W48).</summary>
        private const float LAST_STAND_PLATFORM_REACH = 2000f;
        /// <summary>The last-stand gather point keeps this gap from the station hull, toward the map centre.</summary>
        private const float LAST_STAND_HULL_GAP = 300f;

        /// <summary>
        /// W48: where a last stand gathers: beside the living team station with the most team platforms within
        /// <see cref="LAST_STAND_PLATFORM_REACH"/> (ties: the nearest to <paramref name="origin"/>), on its side toward the
        /// map centre and clear of its hull. False when the team has no living station.
        /// </summary>
        public bool TryGetLastStandPoint(Vector3 origin, out Vector3 point)
        {
            point = default;
            AiWorldSnapshot.Entry best = default;
            int bestPlatforms = -1;
            float bestDistance = float.PositiveInfinity;
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _worldSnapshot.Bases;
            for (int i = 0; i < bases.Count; i++)
            {
                AiWorldSnapshot.Entry station = bases[i];
                if (!_playerRoster.IsAllied(_owner.Id, station.Owner) || station.Entity.HealthModel.IsDestroyed) continue;
                int platforms = CountTeamPlatforms(station.Position);
                float distance = PlanarDistance(station.Position, origin);
                if (platforms < bestPlatforms || platforms == bestPlatforms && distance >= bestDistance) continue;
                best = station;
                bestPlatforms = platforms;
                bestDistance = distance;
            }
            if (bestPlatforms < 0) return false;
            Vector3 toCentre = (CentrePosition ?? origin) - best.Position;
            toCentre.y = 0f;
            float clearance = best.Entity.GetFacade<INavigationFootprintFacade>().NavigationRadius + LAST_STAND_HULL_GAP;
            point = toCentre.sqrMagnitude < 1f ? best.Position : best.Position + toCentre.normalized * clearance;
            return true;
        }

        private int CountTeamPlatforms(Vector3 station)
        {
            int count = 0;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (_playerRoster.IsAllied(_owner.Id, entry.Owner) && !entry.Entity.HealthModel.IsDestroyed &&
                    entry.Entity.TryGetFacade(out IStructureKindFacade structure) &&
                    structure.Kind == StructureKind.DefendPlatform &&
                    PlanarDistance(entry.Position, station) <= LAST_STAND_PLATFORM_REACH) count++;
            }
            return count;
        }

        /// <summary>W68: the team sees some part of the pool zone now, so its defenders are fresh.</summary>
        public bool IsPoolZoneVisible(int area) =>
            _vision.IsAreaVisible(_owner.Id, _zoneObjectives.GetCentre(area), _zoneObjectives.GetRadius(area));

        // One armadaResponse row per change of response or asset; ship count and advantage drift without a new row.
        private void RecordArmadaResponse(string details)
        {
            int cut = details.IndexOf(";armadaShips=", StringComparison.Ordinal);
            string key = cut < 0 ? details : details.Substring(0, cut);
            if (key == _lastArmadaResponse) return;
            bool first = _lastArmadaResponse == null;
            _lastArmadaResponse = key;
            if (first && cut < 0) return;
            EnemyAiTelemetry.RecordEvent(_owner.Id, _owner.Faction, "armadaResponse", details);
        }

        /// <summary>
        /// Late-game situation for this cycle: team station pressure, or an armada threatening a team asset. The
        /// armada the whole own fleet and the team cannot hold at the asset is kept for production.
        /// </summary>
        private void UpdateArmadaSituation(IReadOnlyList<IShipEntity> ships, Vector3 origin, float now)
        {
            bool teamPressure = _stationPressure.HasPressure || _teamPicture.Cooperation.HasDefenseRequest(now);
            TeamStationPressure = teamPressure;
            _fleetForce.Clear();
            float fleetValue = 0f;
            int fleetCapitals = 0;
            _reachExcluded.Clear();
            foreach (IShipEntity ship in ships)
            {
                if (!_entityLocator.TryGetEntity(ship.EntityId, out GameEntity entity) || entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits) continue;
                _forceBuilder.AddUnit(_fleetForce, entity);
                UnitTypeId unitType = entity.GetFacade<IUnitTypeFacade>().UnitTypeId;
                fleetValue += _forceBuilder.GetProfile(unitType).Price;
                _reachExcluded.Add(entity.Id);
                if (_faction.CurrentLevel >= 4 && unitType.IsShip &&
                    _faction.IsAssaultCapital(unitType.ShipType, entity.HealthModel.ShipClass)) fleetCapitals++;
            }
            FleetAssaultCapitals = fleetCapitals;
            PlayerId focusEnemy = FindFocusEnemy(origin);
            _hostileEconomyPressure = GetHostileEconomyPressure(focusEnemy);
            UpdateReinforcementCredits();
            // S3 / S4: production has nothing left to spend on but the fleet; whether the fleet goes is its strength.
            bool spendsOnFleet = _faction.IsAtUnitCap || _faction.HasAssaultSurplus;
            EndGameSituation = spendsOnFleet && _faction.IsAtTopLevel;
            GameEntity hostileBase = spendsOnFleet
                ? FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == focusEnemy, origin) : null;
            PressSituation = hostileBase != null && !_fleetForce.IsEmpty &&
                ClampAdvantage(GetTradeCorrection(focusEnemy, now) * RateReachAt(_fleetForce, fleetValue,
                    hostileBase.GetFacade<IEntityTransformFacade>().Transform.position, false)) >=
                EnemyAiDifficultyProfile.Get(_owner.Difficulty).RequiredAttackRatio;
            HostileArmada worst = null;
            float advantage = float.PositiveInfinity;
            foreach (HostileArmada armada in _teamPicture.Armadas)
            {
                if (!armada.ThreatensAsset ||
                    !HostileArmadaTracker.IsArmada(armada, CombatMatchup.Advantage(_fleetForce, armada.Strength)))
                    continue;
                float value = RateAgainstArmada(_fleetForce, fleetValue, armada, true);
                if (value >= advantage) continue;
                worst = armada;
                advantage = value;
            }
            LateGameSituation = teamPressure || worst != null;
            _outmatchingArmada.Clear();
            if (!teamPressure && worst != null &&
                advantage < EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage)
                _outmatchingArmada.CopyFrom(worst.Strength);
        }

        /// <summary>
        /// W63: fills <see cref="_reachOwnForce"/> with <paramref name="group"/> and the team's other units that reach
        /// <paramref name="area"/> (within <see cref="LOCAL_ENGAGEMENT_RADIUS"/>) in the reach window, and
        /// <see cref="_reachHostileForce"/> with the known hostile units that reach it. Stations and platforms never
        /// move: they are no fleet. <see cref="_reachExcluded"/> holds the group's own ids. Unlimited: everything counts.
        /// </summary>
        private void CollectReach(ForceComposition group, Vector3 area, bool unlimited)
        {
            _reachArea = area;
            _reachUnlimited = unlimited;
            _reachOwnForce.CopyFrom(group);
            _forceBuilder.AddReachable(_reachOwnForce, _includeReachableOwn, area,
                unlimited ? float.PositiveInfinity : LOCAL_ENGAGEMENT_RADIUS, EnemyFleetRating.REACH_WINDOW_SECONDS);
            _teamPicture.BuildKnownFromConfident(_reachHostileForce, _includeReachableHostile);
        }

        private bool IncludeReachableOwn(GameEntity entity) =>
            _playerRoster.IsAllied(_owner.Id, entity.Owner) && !_reachExcluded.Contains(entity.Id);

        private bool IncludeReachableHostile(HostileSighting sighting)
        {
            if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner)) return false;
            if (_reachUnlimited) return true;
            return EnemyFleetRating.CanReach(PlanarDistance(ToVector(sighting.Position), _reachArea),
                LOCAL_ENGAGEMENT_RADIUS, _forceBuilder.GetProfile(sighting.UnitTypeId).Speed,
                EnemyFleetRating.REACH_WINDOW_SECONDS);
        }

        /// <summary>
        /// Credits each side turns into ships within the reach window (W63 addendum): own bank and income limited by the
        /// free unit capacity; hostile income only (bank and capacity unknown), after the same build delay.
        /// </summary>
        private void UpdateReinforcementCredits()
        {
            EnemyReinforcementBudget budget = OwnReinforcement;
            _ownCredits = budget.CreditsWithin(_ownIncome, EnemyFleetRating.REACH_WINDOW_SECONDS);
            _hostileIncomeCredits = new EnemyReinforcementBudget(0f, float.PositiveInfinity, budget.ReadySeconds)
                .CreditsWithin(_hostileIncome, EnemyFleetRating.REACH_WINDOW_SECONDS);
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
            // Allies are asked from the first station pressure on: stations fall too fast to wait until critical.
            if (ownBase != null && _stationPressure.HasPressure)
            {
                Vector3 station = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
                _includeStationThreat ??= sighting => _stationThreats.Contains(sighting.EntityId);
                _forceBuilder.BuildKnown(_baseThreatForce, _intel, now, _includeStationThreat);
                BuildOwnNear(_stationDefenders, station, LOCAL_ENGAGEMENT_RADIUS);
                _teamPicture.Cooperation.RequestDefense(_owner.Id, station, now, _baseThreatForce, _stationDefenders);
            }
            else _teamPicture.Cooperation.ClearDefense(_owner.Id);
            UpdateArmadaSituation(ships, origin, now);
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
            // S6: a main force joins allied human ships fighting at a hostile station; own station pressure wins.
            GameEntity allyAssaultStation = preferStrategicObjectives && !_stationPressure.HasPressure
                ? FindAllyAssaultStation(origin) : null;
            PlayerId focusEnemy;
            if (allyAssaultStation == null) focusEnemy = FindFocusEnemy(home);
            else
            {
                focusEnemy = allyAssaultStation.Owner;
                _teamPicture.Cooperation.FocusEnemy = focusEnemy;
            }
            GameEntity enemyBaseTarget = allyAssaultStation ??
                FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == focusEnemy, origin);
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
            _ownForceValue = _forceBuilder.Build(_ownForce, entity => entity.Owner == self &&
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
            Vector3 localOrigin;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (FORCE_BUILDS_MARKER.Auto())
            {
#endif
            localOrigin = BuildLocalForces(ownShips, origin, now);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            _localThreat = FindLocalThreat(localOrigin, now);
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
            // W49: a group already retreating keeps the point it retreats to until it gets there.
            bool retreating = _groupScoped && !_stationPressure.HasPressure && IsRetreating(ownShips);
            if (retreating && _retreatPoint.HasValue) withdrawPoint = _retreatPoint;
            else _retreatPoint = withdrawPoint;
            bool reachedRetreatPoint = !retreating || !_retreatPoint.HasValue ||
                PlanarDistance(origin, _retreatPoint.Value) <= FleetCohesion.RADIUS;
            Vector3? retreatAnchor = withdrawPoint ?? FindRetreatAnchor(origin, ownBase);
            ReinforcementZonePresenter centre = EnemyShipTaskGroupPlanner.GetCentre(_zones.Zones);
            bool securedCentre = centre != null && !centre.IsContested && _playerRoster.IsAllied(self, centre.Owner);
            // Never guard a zone the team does not hold; a group too weak for every zone gathers at its staging point.
            Vector3? stagingPoint = _assembleArea >= 0 ? GetStagingPoint(_assembleArea, home) : (Vector3?)null;
            // A zone force whose zone has nothing left to take guards the zone's nearest own satellite.
            Vector3? guardPosition = stagingPoint ?? (AssignedZoneId != null
                ? captureObjective == null
                    ? ZoneHoldsBack ? ZoneGuardPoint : FindOwnSatelliteInAssignedZone(origin) ?? ZoneGuardPoint
                    : null
                : (preferStrategicObjectives && securedCentre ? centre.Center : (Vector3?)null) ?? FindNearbyHeldZone(origin));
            // The reserve always gathers at one point: the held centre, else the staging point of the next zone, else home.
            if (IsReserve) guardPosition = LastStandPoint ??
                (securedCentre ? centre.Center : ReserveStagingPoint ?? GetIdleGatherPoint(home));
            string recoveryReason = withdrawPoint.HasValue ? "Outmatched away from held ground; pull back to the gathering point." :
                retreatAnchor.HasValue
                ? "Validated a nearby allied retreat anchor."
                : "No safe local retreat anchor.";
            float baseThreatRatio = _baseThreatForce.IsEmpty
                ? 0f
                : 1f / CombatMatchup.Advantage(_ownForce, _baseThreatForce);

            float localRaw = CombatMatchup.Advantage(_localOwnForce, _localThreatForce);
            if (_groupScoped) RecordFight(ownShips, localOrigin, localRaw, now);
            // W63: the group against what can reach its area in the reach window, both sides with the credits they turn
            // into ships in that time (hostile: estimated income; the unseen estimate below is its unseen fleet). Income
            // now counts on both sides, so the old one-way economy-pressure divisor is gone.
            UpdateReinforcementCredits();
            _reachExcluded.Clear();
            if (_groupScoped) _reachExcluded.UnionWith(_ownShipIds);
            else
                foreach (AiWorldSnapshot.Entry entry in _worldSnapshot.GetOwnedUnits(self)) _reachExcluded.Add(entry.Id);
            CollectReach(_ownForce, origin, ships == null);
            float visibleAdvantage = EnemyFleetRating.Rate(_reachOwnForce, _reachHostileForce, _ownForce, _ownForceValue,
                _ownCredits, _hostileIncomeCredits, _ratedOwnForce, _ratedHostileForce);
            float unseenInvestment = EstimateUnseenFleetInvestment(focusEnemy, now, fleetInvestment);
            float fleetRaw = visibleAdvantage;
            if (unseenInvestment > 0f)
                fleetRaw = Mathf.Max(EnemyFleetRating.Rate(_reachOwnForce, _reachHostileForce, _ownForce,
                        _ownForceValue, _ownCredits, _hostileIncomeCredits + unseenInvestment, _ratedOwnForce,
                        _ratedHostileForce), visibleAdvantage * MAX_UNSEEN_INVESTMENT_SHARE);
            bool hasFleetThreat = !_reachHostileForce.IsEmpty || unseenInvestment > 0f;
            // W67: both ratings carry the loss exchange seen against this enemy so far.
            float tradeCorrection = GetTradeCorrection(focusEnemy, now);
            localRaw = ClampAdvantage(localRaw * tradeCorrection);
            fleetRaw = ClampAdvantage(fleetRaw * tradeCorrection);

            // W63: an assault or a hunt is rated where it fights: what each side can bring to the target in the reach
            // window, not what stands around the group now.
            float assaultRaw = enemyBaseTarget == null ? fleetRaw : RateAtTarget(enemyBaseTarget, tradeCorrection);
            bool hasDecisiveSurplus = _faction.HasAssaultSurplus && assaultRaw >= 2f &&
                assaultInvestment >= 2f * unseenInvestment;
            // An assault under way is not dropped for a hostile force the fleet still outmatches 2 : 1 (W33).
            bool keepsAssault = AssaultUnderWay && assaultRaw >= ASSAULT_KEEP_ADVANTAGE;
            // S4: the end game skips the centre and zone-majority terms; S6: so does joining an allied assault, which
            // also needs no capital strike group and no reinforcement wave (the allied ships are the strike).
            bool endGame = preferStrategicObjectives && EndGameSituation;
            bool allyAssault = allyAssaultStation != null;
            bool skipsCentre = endGame || allyAssault;
            // S3: the gate counts capitals over the whole fleet; the strike itself still needs them in this group.
            int gateCapitals = preferStrategicObjectives
                ? Math.Max(_assaultCapitals.Count, FleetAssaultCapitals) : _assaultCapitals.Count;
            bool assaultPlanned = assaultRaw >= 1f && (allyAssault || _faction.HasAssaultReinforcementPlan &&
                (hasZoneMajority || hasDecisiveSurplus || endGame));
            bool canAssault = preferStrategicObjectives && (centre == null || securedCentre || skipsCentre) &&
                !_stationPressure.HasPressure && ownBase != null && _productionAccess.HasHostBase &&
                (allyAssault || gateCapitals >= EnemyFactionModel.MINIMUM_ASSAULT_CAPITALS) &&
                (assaultPlanned || keepsAssault);
            AssaultGate = FormattableString.Invariant(
                $"capitals={_assaultCapitals.Count};fleetCapitals={FleetAssaultCapitals};reinforcementPlan={_faction.HasAssaultReinforcementPlan};zoneMajority={hasZoneMajority};decisiveSurplus={hasDecisiveSurplus};keep={keepsAssault};assaultRaw={assaultRaw:F2};objective={_gameModel.VictoryCondition};securedCentre={securedCentre};stationPressure={_stationPressure.HasPressure};hostBase={_productionAccess.HasHostBase};reserveShips={ownShips.Count};press={PressSituation};endGame={endGame};allyAssault={allyAssault};canAssault={canAssault}");
            bool canHunt = enemyFleetTarget != null && (!securedCentre || canAssault) &&
                !IsNearHostileBase(enemyFleetTarget.GetFacade<IEntityTransformFacade>().Transform.position) &&
                RateAtTarget(enemyFleetTarget, tradeCorrection) >=
                EnemyAiDifficultyProfile.Get(_owner.Difficulty).HuntAdvantage;
            float retreatLine = EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage;
            _advantageSmoother.Sample(localRaw, fleetRaw, now, !_localThreatForce.IsEmpty, hasFleetThreat, retreatLine);
            if (_localThreat != null && _advantageSmoother.Local <= retreatLine)
                _localThreat = FindLastStandTarget(origin, now) ?? _localThreat;
            GameEntity assaultTarget = enemyBaseTarget == null ? null : FindAssaultTarget(enemyBaseTarget, origin, now);
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
                isCentreGuardGoal: preferStrategicObjectives && centre != null && !canAssault && !skipsCentre,
                canAssaultBase: canAssault,
                canHuntFleet: canHunt && captureObjective == null,
                hasSecuredCentre: preferStrategicObjectives && securedCentre,
                canDisengage: withdrawPoint.HasValue,
                raidWhenBehind: preferStrategicObjectives && _behindOnZones && captureObjective == null,
                isLateGame: LateGameSituation,
                hasReachedRetreatPoint: reachedRetreatPoint,
                tradeCorrection: tradeCorrection,
                isEndGame: endGame,
                joinsAllyAssault: allyAssault);
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
            AddTargetForce(assaultTarget, now);
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
            // X2: a zone force with a capture objective has no guard site, but ships with no task still need a point to
            // hold: the zone's staging point, else home. The decision still sees no guard site.
            if (!guardPosition.HasValue && AssignedZoneId != null)
            {
                int assignedArea = FindZoneArea(AssignedZoneId);
                guardPosition = assignedArea >= 0 ? GetStagingPoint(assignedArea, home) : home;
            }
            return new EnemyStrategicContext(
                snapshot: snapshot,
                ships: ownShips,
                captureTarget: captureObjective != null ? RouteAroundHostileCentre(origin, captureObjective.Position) : origin,
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
                teamHeldZones: teamHeldZones)
            {
                IsZoneForce = AssignedZoneId != null,
                AssaultTarget = assaultTarget,
                AssaultGate = AssaultGate,
                LocalOrigin = LocalOriginSource
            };
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
            // W62: an assaulted station fights back with its own weapons; its hull is rated as the target itself.
            else if (target.IsPlayerBase()) _forceBuilder.AddStationWeapons(force, target);
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

        /// <summary>The facility is a working hostile repairer with another hostile armed structure inside its radius.</summary>
        private bool IsRepairerCoveringHostileStructure(long facilityId)
        {
            if (!_entityLocator.TryGetEntity(facilityId, out GameEntity facility) ||
                !facility.TryGetFacade(out IRepairSourceFacade repairer) || !repairer.CanRepair) return false;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (entry.Id != facilityId &&
                    EnemyZoneObjectiveCollector.IsArmedHostileStructure(entry, _playerRoster, _vision, _owner.Id) &&
                    PlanarDistance(entry.Position, repairer.Position) <= repairer.Profile.Radius) return true;
            }
            return false;
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
                bool hostileSatellite = candidate.BlockingFacilityId.HasValue && !candidate.IsMine;
                bool hostileRepairer = hostileSatellite && IsRepairerCoveringHostileStructure(candidate.BlockingFacilityId.Value);
                _rankedCaptures.Add(new RankedCapture(candidate,
                    hostileRepairer ? priority + HOSTILE_REPAIRER_FIRST_BONUS :
                    hostileSatellite ? priority + HOSTILE_SATELLITE_FIRST_BONUS :
                    hostileRelay ? priority + HOSTILE_RELAY_FIRST_BONUS : priority, i));
                _zoneCandidates.Add(candidate);
                _zonePriorities.Add(priority);
            }
            float commit = EnemyZoneEvaluator.COMMIT_ADVANTAGE;
            _zoneObjectives.Collect(_zoneCandidates, _zonePriorities, origin, GetGroupSpeed(), _isNearHostileBase);
            bool strongEconomy = HasStrongEconomy();
            _selectedArea = _zoneEvaluator.Select(_zoneObjectives.Objectives, _assessedForce, commit, _behindOnZones,
                _zoneAssessments, strongEconomy);
            // A group the allocator sent takes sites only in its own zone.
            if (AssignedZoneId != null) _selectedArea = FindZoneArea(AssignedZoneId);
            RecordZoneEvaluations();
            int selectedTier = _selectedArea < 0 ? -1 : EnemyZoneEvaluator.GetTier(
                _zoneObjectives.Objectives[_selectedArea], _zoneAssessments[_selectedArea].Verdict, _behindOnZones,
                strongEconomy);
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

        /// <summary>The own satellite or platform in the assigned zone nearest the group; null when none stands.</summary>
        private Vector3? FindOwnSatelliteInAssignedZone(Vector3 origin)
        {
            int area = FindZoneArea(AssignedZoneId);
            ReinforcementZonePresenter zone = area >= 0 ? _zoneObjectives.GetZone(area) : null;
            if (zone == null) return null;
            Vector3? best = null;
            float bestDistance = float.PositiveInfinity;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (entry.Owner != _owner.Id || entry.IsMiningFacility ||
                    !EnemyShipTaskGroupPlanner.IsInZone(zone, entry.Position)) continue;
                float distance = PlanarDistance(entry.Position, origin);
                if (distance >= bestDistance) continue;
                best = entry.Position;
                bestDistance = distance;
            }
            return best;
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

        /// <summary>
        /// Hostile over team income (clamped 1–2; telemetry only since W63). Sets the per-player income shares of both
        /// sides that the reach rating turns into reinforcements.
        /// </summary>
        private float GetHostileEconomyPressure(PlayerId enemy)
        {
            // Base income provides a floor before the first mines are scouted.
            const float BASE_INCOME_ESTIMATE = 15f;
            const float UNDEVELOPED_MINE_VALUE = 2f;
            float alliedIncome = BASE_INCOME_ESTIMATE;
            foreach (AiWorldSnapshot.Entry structure in _worldSnapshot.RaidStructures)
                if (structure.IsMiningFacility && _playerRoster.IsAllied(_owner.Id, structure.Owner) &&
                    !structure.Entity.HealthModel.IsDestroyed)
                    alliedIncome += structure.Entity.GetFacade<IMiningFacilityFacade>().GrossIncome;
            int livingAllies = Mathf.Max(1, CountLivingAlliedPlayers());
            _ownIncome = alliedIncome / livingAllies;
            _hostileIncome = 0f;
            if (enemy.IsNone) return 1f;

            float hostileIncome = BASE_INCOME_ESTIMATE;
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
            _ownIncome = alliedIncome / livingAllies;
            _hostileIncome = hostileIncome / livingAllies;
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

        /// <summary>S6: allied human ships at a hostile station that make it an allied assault.</summary>
        private const int ALLY_ASSAULT_MIN_SHIPS = 3;

        /// <summary>
        /// S6: the hostile station nearest <paramref name="origin"/> with at least <see cref="ALLY_ASSAULT_MIN_SHIPS"/>
        /// allied human ships inside <see cref="BASE_EXCLUSION_MARGIN"/> of it; null when no human pushes a station.
        /// </summary>
        private GameEntity FindAllyAssaultStation(Vector3 origin)
        {
            GameEntity best = null;
            float bestDistance = float.PositiveInfinity;
            IReadOnlyList<AiWorldSnapshot.Entry> units = _worldSnapshot.LivingUnits;
            foreach (AiWorldSnapshot.Entry station in _worldSnapshot.Bases)
            {
                if (!_playerRoster.IsHostile(_owner.Id, station.Owner) || station.Entity.HealthModel.IsDestroyed) continue;
                float distance = PlanarDistance(origin, station.Position);
                if (distance >= bestDistance) continue;
                int humanShips = 0;
                for (int i = 0; i < units.Count && humanShips < ALLY_ASSAULT_MIN_SHIPS; i++)
                {
                    AiWorldSnapshot.Entry unit = units[i];
                    if (unit.Owner == _owner.Id || !unit.UnitTypeId.IsShip ||
                        !_playerRoster.IsAllied(_owner.Id, unit.Owner) || _playerRoster.Get(unit.Owner).IsAi ||
                        PlanarDistance(unit.Position, station.Position) > BASE_EXCLUSION_MARGIN) continue;
                    humanShips++;
                }
                if (humanShips < ALLY_ASSAULT_MIN_SHIPS) continue;
                best = station.Entity;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>
        /// S1a / S1b assault target order: a hostile ship fighting near the fleet (on the way or at the base), else the
        /// known hostile ship inside <see cref="BASE_EXCLUSION_MARGIN"/> of <paramref name="station"/> nearest the fleet,
        /// else the visible armed platform there nearest the fleet, else the station.
        /// </summary>
        private GameEntity FindAssaultTarget(GameEntity station, Vector3 origin, float now)
        {
            if (_localThreat != null && _localThreat.TryGetFacade(out IUnitTypeFacade threatType) &&
                threatType.UnitTypeId.IsShip) return _localThreat;
            Vector3 stationPosition = station.GetFacade<IEntityTransformFacade>().Transform.position;
            GameEntity best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                Vector3 position = ToVector(sighting.Position);
                if (!sighting.UnitTypeId.IsShip || !_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                    PlanarDistance(position, stationPosition) > BASE_EXCLUSION_MARGIN ||
                    _intel.GetConfidence(sighting, now) <= 0f) continue;
                float distance = PlanarDistance(origin, position);
                if (distance >= bestDistance ||
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.HealthModel.IsDestroyed || entity.IsCloaked()) continue;
                best = entity;
                bestDistance = distance;
            }
            if (best != null) return best;
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry entry = structures[i];
                if (!_playerRoster.IsHostile(_owner.Id, entry.Owner) || entry.Entity.HealthModel.IsDestroyed ||
                    PlanarDistance(entry.Position, stationPosition) > BASE_EXCLUSION_MARGIN ||
                    !entry.Entity.TryGetFacade(out IStructureKindFacade structure) ||
                    structure.Kind != StructureKind.DefendPlatform || !_vision.IsVisible(_owner.Id, entry.Entity)) continue;
                float distance = PlanarDistance(origin, entry.Position);
                if (distance >= bestDistance) continue;
                best = entry.Entity;
                bestDistance = distance;
            }
            return best ?? station;
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

        // W55: hull gaps, so a hull wider than the cohesion radius (Viscount) can count as regrouped.
        private bool IsRegrouped(IReadOnlyList<IShipEntity> ships)
        {
            _formationPositions.Clear();
            _formationRadii.Clear();
            for (int i = 0; i < ships.Count; i++)
            {
                _formationPositions.Add(ToPoint(ships[i].WorldPosition));
                _formationRadii.Add(ships[i].NavigationRadius);
            }
            return FleetCohesion.IsTogether(_formationPositions, _formationRadii, FleetCohesion.RADIUS);
        }

        /// <summary>
        /// Rates the local fight around the committed body (fallback: every group ship). A group ship that died in the
        /// last <see cref="DEATH_RERATE_SECONDS"/> inside that fight (between <see cref="DEATH_RERATE_MIN_DISTANCE"/> and
        /// <see cref="LOCAL_ENGAGEMENT_RADIUS"/> from the centre) re-rates it at its last position; the worse rating wins,
        /// so a group losing ships never reads as having no local threat (W32). A straggler lost farther away is not the
        /// group's fight and leaves the rating alone (W61).
        /// </summary>
        private Vector3 BuildLocalForces(IReadOnlyList<IShipEntity> ownShips, Vector3 fleetCenter, float now)
        {
            Vector3 origin = fleetCenter;
            LocalOriginSource = "all";
            if (_groupScoped)
            {
                TrackGroupDeaths(ownShips, now);
                if (CommittedBody != null && CommittedBody.Count > 0)
                {
                    _committedBodySet.Clear();
                    for (int i = 0; i < CommittedBody.Count; i++) _committedBodySet.Add(CommittedBody[i]);
                    _formationPositions.Clear();
                    for (int i = 0; i < ownShips.Count; i++)
                        if (_committedBodySet.Contains(ownShips[i]))
                            _formationPositions.Add(ToPoint(ownShips[i].WorldPosition));
                    if (_formationPositions.Count > 0)
                    {
                        float x = 0f;
                        float z = 0f;
                        foreach (FormationPoint point in _formationPositions)
                        {
                            x += point.X;
                            z += point.Z;
                        }
                        origin = ToVector(new FormationPoint(x / _formationPositions.Count, z / _formationPositions.Count));
                        LocalOriginSource = "body";
                    }
                }
            }
            BuildHostilesNear(_localThreatForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            BuildOwnNear(_localOwnForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            float deathDistance = PlanarDistance(origin, _lastDeathPosition);
            if (!_groupScoped || now - _lastDeathTime > DEATH_RERATE_SECONDS ||
                deathDistance <= DEATH_RERATE_MIN_DISTANCE || deathDistance > LOCAL_ENGAGEMENT_RADIUS) return origin;
            BuildHostilesNear(_deathThreatForce, _lastDeathPosition, LOCAL_ENGAGEMENT_RADIUS);
            if (_deathThreatForce.IsEmpty) return origin;
            BuildOwnNear(_deathOwnForce, _lastDeathPosition, LOCAL_ENGAGEMENT_RADIUS);
            if (!_localThreatForce.IsEmpty && CombatMatchup.Advantage(_deathOwnForce, _deathThreatForce) >=
                CombatMatchup.Advantage(_localOwnForce, _localThreatForce)) return origin;
            _localThreatForce.CopyFrom(_deathThreatForce);
            _localOwnForce.CopyFrom(_deathOwnForce);
            LocalOriginSource = "death";
            return _lastDeathPosition;
        }

        /// <summary>
        /// Notes the group's ships that died since the last build: the latest death spot for the local re-rate, and
        /// each lost ship's price for the running fight's trade record (W67).
        /// </summary>
        private void TrackGroupDeaths(IReadOnlyList<IShipEntity> ownShips, float now)
        {
            _goneShipIds.Clear();
            foreach (long id in _lastShipPositions.Keys)
                if (!_ownShipIds.Contains(id)) _goneShipIds.Add(id);
            foreach (long id in _goneShipIds)
            {
                if (!_entityLocator.TryGetEntity(id, out GameEntity entity) || entity.HealthModel.IsDestroyed)
                {
                    _lastDeathPosition = _lastShipPositions[id];
                    _lastDeathTime = now;
                    _fight.RecordOwnLoss(_forceBuilder.GetProfile(_groupShipTypes[id]).Price, now);
                }
                _lastShipPositions.Remove(id);
                _groupShipTypes.Remove(id);
            }
            for (int i = 0; i < ownShips.Count; i++)
            {
                IShipEntity ship = ownShips[i];
                _lastShipPositions[ship.EntityId] = ship.WorldPosition;
                if (!_groupShipTypes.ContainsKey(ship.EntityId) &&
                    _entityLocator.TryGetEntity(ship.EntityId, out GameEntity entity))
                    _groupShipTypes.Add(ship.EntityId, entity.GetFacade<IUnitTypeFacade>().UnitTypeId);
            }
        }

        /// <summary>
        /// W67: feeds this group's fight into the trade ledgers. A fight starts when hostiles stand within
        /// <see cref="LOCAL_ENGAGEMENT_RADIUS"/> of <paramref name="origin"/>: the losses predicted at
        /// <paramref name="advantage"/> for the group's ships there and the hostile ships there go to the ledger of the
        /// local threat's team (else the first hostile team there). Hostile ships seen in the fight count once destroyed.
        /// </summary>
        private void RecordFight(IReadOnlyList<IShipEntity> ownShips, Vector3 origin, float advantage, float now)
        {
            float ownValue = 0f;
            for (int i = 0; i < ownShips.Count; i++)
                if (PlanarDistance(ownShips[i].WorldPosition, origin) <= LOCAL_ENGAGEMENT_RADIUS &&
                    _groupShipTypes.TryGetValue(ownShips[i].EntityId, out UnitTypeId type))
                    ownValue += _forceBuilder.GetProfile(type).Price;
            float hostileValue = 0f;
            PlayerId hostileOwner = PlayerId.None;
            IReadOnlyList<HostileSighting> sightings = _teamPicture.ConfidentSightings;
            for (int i = 0; i < sightings.Count; i++)
            {
                HostileSighting sighting = sightings[i];
                if (!sighting.UnitTypeId.IsShip || !_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                    PlanarDistance(ToVector(sighting.Position), origin) > LOCAL_ENGAGEMENT_RADIUS) continue;
                hostileValue += _forceBuilder.GetProfile(sighting.UnitTypeId).Price;
                if (hostileOwner.IsNone) hostileOwner = sighting.Owner;
            }
            if (_localThreat != null && _playerRoster.IsHostile(_owner.Id, _localThreat.Owner))
                hostileOwner = _localThreat.Owner;
            bool inFight = !hostileOwner.IsNone;
            _fight.Update(now, inFight, inFight ? _playerRoster.Get(hostileOwner).Team : default, advantage, ownValue,
                hostileValue, _isHostileGone);
            if (!_fight.IsActive) return;
            for (int i = 0; i < sightings.Count; i++)
            {
                HostileSighting sighting = sightings[i];
                if (!sighting.UnitTypeId.IsShip || !_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                    PlanarDistance(ToVector(sighting.Position), origin) > LOCAL_ENGAGEMENT_RADIUS) continue;
                _fight.TrackHostile(sighting.EntityId, _playerRoster.Get(sighting.Owner).Team,
                    _forceBuilder.GetProfile(sighting.UnitTypeId).Price);
            }
        }

        private bool IsHostileGone(long entityId) =>
            !_entityLocator.TryGetEntity(entityId, out GameEntity entity) || entity.HealthModel.IsDestroyed;

        private static bool IsRetreating(IReadOnlyList<IShipEntity> ships)
        {
            for (int i = 0; i < ships.Count; i++)
                if (ships[i].CurrentOrder == UnitOrderType.Retreat) return true;
            return false;
        }

        private static float ClampAdvantage(float advantage) =>
            Mathf.Clamp(advantage, CombatMatchup.MIN_ADVANTAGE, CombatMatchup.MAX_ADVANTAGE);

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
        /// Hostiles that threaten the team's other stations or the armed platforms around any team station count as
        /// well (W53); one of them is the defence target only while nothing threatens this station.
        /// </summary>
        private GameEntity BuildBaseThreat(GameEntity ownBase)
        {
            _stationThreats.Clear();
            GameEntity target = null;
            GameEntity kept = null;
            int targetRank = int.MaxValue;
            int keptRank = int.MaxValue;
            float closest = float.MaxValue;
            float now = Time.time;
            if (ownBase != null)
            {
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
            GameEntity stationTarget = target;
            GameEntity assetTarget = AddTeamAssetThreats(ownBase, now);
            target ??= assetTarget;
            _defenseTargetId = target == null ? 0 : target.Id;
            if (ownBase == null) _stationPressure.Reset();
            else
            {
                IHealthModelObserver health = ownBase.HealthModel;
                float maximum = ownBase.GetFacade<IStationDurabilityFacade>().MaximumDurability;
                _stationPressure.Update(ownBase.Id, health.Hull + health.Shields, maximum, now,
                    stationTarget == null ? 0 : stationTarget.Id);
                if (stationTarget != null)
                {
                    Vector3 knownPosition = stationTarget.GetFacade<IEntityTransformFacade>().Transform.position;
                    _stationPressure.ObserveThreatPosition(knownPosition.x, knownPosition.y, knownPosition.z);
                }
            }
            return target;
        }

        /// <summary>
        /// W53: adds to the base threat the visible hostiles that reach a team station other than
        /// <paramref name="ownBase"/> or an armed platform within <see cref="LOCAL_ENGAGEMENT_RADIUS"/> of any team
        /// station (same reach rule as the station). Returns the one to defend against: the previous pick while it still
        /// threatens with a ship, else a ship attacking an asset, then the nearest ship, then the nearest wing.
        /// </summary>
        private GameEntity AddTeamAssetThreats(GameEntity ownBase, float now)
        {
            _teamAssets.Clear();
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _worldSnapshot.Bases;
            for (int i = 0; i < bases.Count; i++)
            {
                AiWorldSnapshot.Entry station = bases[i];
                if (!_playerRoster.IsAllied(_owner.Id, station.Owner) || station.Entity.HealthModel.IsDestroyed ||
                    ReferenceEquals(station.Entity, ownBase)) continue;
                _teamAssets.Add(station);
            }
            IReadOnlyList<AiWorldSnapshot.Entry> structures = _worldSnapshot.RaidStructures;
            for (int i = 0; i < structures.Count; i++)
            {
                AiWorldSnapshot.Entry platform = structures[i];
                if (_playerRoster.IsAllied(_owner.Id, platform.Owner) && !platform.Entity.HealthModel.IsDestroyed &&
                    platform.Entity.TryGetFacade(out IStructureKindFacade structure) &&
                    structure.Kind == StructureKind.DefendPlatform && IsNearTeamStation(platform.Position))
                    _teamAssets.Add(platform);
            }
            if (_teamAssets.Count == 0) return null;
            GameEntity target = null;
            GameEntity kept = null;
            int targetRank = int.MaxValue;
            int keptRank = int.MaxValue;
            float closest = float.MaxValue;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner) || _intel.GetConfidence(sighting, now) <= 0f ||
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity hostile) ||
                    hostile.HealthModel.IsDestroyed || !hostile.HealthModel.HasUnits || hostile.IsCloaked() ||
                    !_vision.IsVisible(_owner.Id, hostile) ||
                    !hostile.TryGetFacade(out IUnitCombatObserverFacade combat)) continue;
                Vector3 position = hostile.GetFacade<IEntityTransformFacade>().Transform.position;
                float hostileRadius = hostile.TryGetFacade(out INavigationFootprintFacade body) ? body.NavigationRadius : 0f;
                bool threatens = false;
                bool attacking = false;
                float nearest = float.MaxValue;
                foreach (AiWorldSnapshot.Entry asset in _teamAssets)
                {
                    float assetRadius = asset.Entity.TryGetFacade(out INavigationFootprintFacade footprint)
                        ? footprint.NavigationRadius : 0f;
                    float distance = PlanarDistance(position, asset.Position);
                    bool attackingAsset = combat.IsAttacking(asset.Entity);
                    if (!CanThreatenStation(distance, assetRadius, hostileRadius, combat.WeaponRange) ||
                        !attackingAsset && distance > Mathf.Max(combat.WeaponRange, assetRadius + hostileRadius)) continue;
                    threatens = true;
                    attacking |= attackingAsset;
                    nearest = Mathf.Min(nearest, distance);
                }
                if (!threatens) continue;
                _stationThreats.Add(hostile.Id);
                int rank = sighting.UnitTypeId.IsShip ? attacking ? 0 : 1 : 2;
                if (hostile.Id == _defenseTargetId)
                {
                    kept = hostile;
                    keptRank = rank;
                }
                if (rank < targetRank || rank == targetRank && nearest < closest)
                {
                    closest = nearest;
                    target = hostile;
                    targetRank = rank;
                }
            }
            return kept != null && (keptRank < 2 || targetRank == 2) ? kept : target;
        }

        private bool IsNearTeamStation(Vector3 position)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _worldSnapshot.Bases;
            for (int i = 0; i < bases.Count; i++)
                if (_playerRoster.IsAllied(_owner.Id, bases[i].Owner) &&
                    PlanarDistance(position, bases[i].Position) <= LOCAL_ENGAGEMENT_RADIUS) return true;
            return false;
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

        /// <summary>
        /// Allied ships, squadrons, armed structures and stations within <paramref name="radius"/> of
        /// <paramref name="center"/> (ground plane), healed by the allied repairers covering the centre.
        /// </summary>
        private void BuildOwnNear(ForceComposition force, Vector3 center, float radius)
        {
            _nearbyCenter = center;
            _nearbyRadiusSquared = radius * radius;
            _forceBuilder.Build(force, _includeNearbyOwn);
            EnemyZoneObjectiveCollector.AddAlliedArmedStructures(force, _worldSnapshot, _playerRoster, _forceBuilder,
                _owner.Id, center, radius);
            AddTeamStations(force, center, radius);
            EnemyZoneObjectiveCollector.AddAlliedRepairSources(force, _worldSnapshot, _playerRoster, _owner.Id, center);
        }

        /// <summary>W62: the team's stations within <paramref name="radius"/> fight in a local fight there.</summary>
        private void AddTeamStations(ForceComposition force, Vector3 center, float radius)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _worldSnapshot.Bases;
            for (int i = 0; i < bases.Count; i++)
            {
                AiWorldSnapshot.Entry station = bases[i];
                if (_playerRoster.IsAllied(_owner.Id, station.Owner) && !station.Entity.HealthModel.IsDestroyed &&
                    PlanarDistance(station.Position, center) <= radius) _forceBuilder.AddStation(force, station.Entity);
            }
        }

        /// <summary>
        /// W62: known hostile stations within <paramref name="radius"/> fight in a local fight there. A station seen
        /// once stays known while it stands, like a platform.
        /// </summary>
        private void AddHostileStations(ForceComposition force, Vector3 center, float radius)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> bases = _worldSnapshot.Bases;
            for (int i = 0; i < bases.Count; i++)
            {
                AiWorldSnapshot.Entry station = bases[i];
                if (!_playerRoster.IsHostile(_owner.Id, station.Owner) || station.Entity.HealthModel.IsDestroyed ||
                    PlanarDistance(station.Position, center) > radius) continue;
                if (_vision.IsVisible(_owner.Id, station.Entity)) _intel.RememberStructure(station.Id);
                else if (!_intel.IsStructureKnown(station.Id)) continue;
                _forceBuilder.AddStation(force, station.Entity);
            }
        }

        private bool IncludeNearbyOwn(GameEntity entity)
        {
            if (!_playerRoster.IsAllied(_owner.Id, entity.Owner)) return false;
            Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - _nearbyCenter;
            offset.y = 0f;
            return offset.sqrMagnitude <= _nearbyRadiusSquared;
        }

        /// <summary>
        /// Known hostile ships, squadrons, armed structures and stations within <paramref name="radius"/> of
        /// <paramref name="center"/> (ground plane), healed by the known hostile repairers covering the centre. Without
        /// the structures a fight under hostile platforms reads as even.
        /// </summary>
        private void BuildHostilesNear(ForceComposition force, Vector3 center, float radius)
        {
            float radiusSquared = radius * radius;
            _teamPicture.BuildKnownFromConfident(force, sighting =>
            {
                Vector3 offset = ToVector(sighting.Position) - center;
                offset.y = 0f;
                return offset.sqrMagnitude <= radiusSquared;
            });
            EnemyZoneObjectiveCollector.AddArmedStructures(force, _worldSnapshot, _playerRoster, _vision, _forceBuilder,
                _intel, _owner.Id, center, radius);
            AddHostileStations(force, center, radius);
            EnemyZoneObjectiveCollector.AddHostileRepairSources(force, _worldSnapshot, _playerRoster, _vision, _intel,
                _owner.Id, center);
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
