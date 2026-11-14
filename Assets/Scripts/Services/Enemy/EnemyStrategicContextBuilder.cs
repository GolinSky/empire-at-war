using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
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

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyStrategicContext
    {
        public EnemyStrategicSnapshot Snapshot { get; }
        public float StateHeldSeconds { get; internal set; }
        public IReadOnlyList<IShipEntity> Ships { get; }
        public Vector3 CaptureTarget { get; }
        public GameEntity EnemyFleetTarget { get; }
        public GameEntity EnemyBaseTarget { get; }
        public GameEntity OwnBase { get; }
        /// <summary>Closest known hostile near the fleet, ships before squadrons; null when none.</summary>
        public GameEntity LocalThreatTarget { get; }
        /// <summary>Closest known undefended hostile structure; null when none.</summary>
        public GameEntity RaidTarget { get; }
        public EnemyCaptureObjective CaptureObjective { get; }
        public GameEntity DefenseTarget { get; }
        public string StationPressureReason { get; }
        public EnemyStationPressureTracker StationPressure { get; }
        public Func<Vector3, float, bool> IsDefensePointClear { get; }
        public Vector3? OrderDestination { get; set; }
        public string DefensePointReason { get; set; } = "";
        public Vector3? RetreatAnchor { get; }
        public string RecoveryReason { get; }
        public IReadOnlyDictionary<IShipEntity, GameEntity> Receivers { get; }
        public IReadOnlyDictionary<IShipEntity, ForceComposition> ShipForces { get; }
        public IReadOnlyDictionary<GameEntity, ForceComposition> TargetForces { get; }

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
            Func<Vector3, float, bool> isDefensePointClear = null)
        {
            Snapshot = snapshot;
            Ships = ships;
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
            DefenseTarget = defenseTarget;
            StationPressureReason = stationPressureReason;
            RetreatAnchor = retreatAnchor;
            RecoveryReason = recoveryReason;
            StationPressure = stationPressure;
            IsDefensePointClear = isDefensePointClear;
        }
    }

    public sealed class EnemyStrategicContextBuilder
    {
        private readonly EnemyStationPressureTracker _stationPressure;
        private readonly EnemyRecoveryAnchor _recovery;
        // Task groups share the commander's fleet-wide decision to leave the forward station.
        private readonly EnemyStrategicContextBuilder _stationAssessment;
        private GameEntity _withdrawingFromStation;
        private bool _preferStrategicObjectives;
        private Vector3 _strategicCenter;

        private const float CAPTURE_STALL_SECONDS = 20f;

        /// <summary>Scale on the retreat threshold the current capture objective must stay above to be kept.</summary>
        private const float RETAIN_SAFETY_SCALE = 0.85f;
        /// <summary>A hostile-strength dip must last this long before the current capture objective is dropped.</summary>
        private const float UNSAFE_GRACE_SECONDS = 3f;

        /// <summary>Hostiles this close to the fleet center can engage it soon: twice the common ship weapon range (500).</summary>
        private const float LOCAL_ENGAGEMENT_RADIUS = 1000f;

        private readonly IShipService _shipService;
        private ISet<string> _reservedCaptureObjectives;
        private bool _groupScoped;
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
        private readonly HashSet<long> _stationThreats = new HashSet<long>();
        private readonly List<EnemyCaptureObjective> _captureCandidates = new List<EnemyCaptureObjective>();
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
        public Vector3? ForwardStationFallback { get; private set; }
        public AllyAiMarkerSource AlliedMarkers { get; }
        private readonly List<IShipEntity> _ownShips = new List<IShipEntity>();
        private readonly List<FormationPoint> _formationPositions = new List<FormationPoint>();
        private readonly Dictionary<IShipEntity, GameEntity> _receivers = new Dictionary<IShipEntity, GameEntity>();
        private readonly Dictionary<IShipEntity, ForceComposition> _shipForces = new Dictionary<IShipEntity, ForceComposition>();
        private readonly Dictionary<GameEntity, ForceComposition> _targetForces = new Dictionary<GameEntity, ForceComposition>();
        private float _ionRiskWeight = 1f;
        // Targets stay picked while still valid: a new pick re-orders every committed ship, and each order makes
        // the ship's weapons re-sort their candidates.
        private GameEntity _localThreat;
        private GameEntity _raidTarget;
        private GameEntity _fleetTarget;

        private readonly PlayerSlot _owner;
        private readonly IProductionAccess _productionAccess;
        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _ownTeamForce = new ForceComposition();
        private readonly ForceComposition _focusTeamForce = new ForceComposition();
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
            EnemyRecoveryAnchor recovery)
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
            _productionAccess = productionAccess;
            _playerRoster = playerRoster;
            _zones = zones;
            _navigation = navigation;
            _stationPressure = stationPressure;
            _recovery = recovery;
        }

        /// <summary>The returned collections are borrowed until the next Build on this player's builder.</summary>
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
            _productionAccess = source._productionAccess;
            _stationPressure = source._stationPressure;
            _recovery = source._recovery;
            _stationAssessment = source;
        }

        public EnemyStrategicContextBuilder CreateGroupBuilder() => new EnemyStrategicContextBuilder(this);

        public bool IsMarkerApproachSafe(Vector3 point) =>
            IsObjectiveSafe(ToVector(CalculateFleetCenter(_ownShips)), point, AllyAiMarkerSource.REQUEST_RADIUS, false);

        public (bool HasPressure, bool GatherReinforcements) AssessStation(IReadOnlyList<IShipEntity> ships)
        {
            Vector3 origin = ToVector(CalculateFleetCenter(ships));
            GameEntity ownBase = _withdrawingFromStation != null && !_withdrawingFromStation.HealthModel.IsDestroyed
                ? _withdrawingFromStation
                : FindOwnBase(origin);
            BuildBaseThreat(ownBase);
            _groupScoped = false;
            _ownShips.Clear();
            _ownShips.AddRange(ships);
            _forceBuilder.Build(_ownTeamForce, entity => _playerRoster.IsAllied(_owner.Id, entity.Owner));
            _forceBuilder.BuildKnown(_focusTeamForce, _intel, Time.time,
                sighting => _playerRoster.IsHostile(_owner.Id, sighting.Owner));
            BuildOwnNear(_localOwnForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            ForwardStationFallback = FindForwardStationFallback(ownBase, origin);
            _withdrawingFromStation = ForwardStationFallback.HasValue ? ownBase : null;
            _recovery.Update(origin, CombatMatchup.Advantage(_ownTeamForce, _focusTeamForce),
                FindLocalThreat(origin, Time.time) != null, HasOwnStation(), Time.time,
                (position, radius) => ships.Count == 0 || IsObjectiveSafe(origin, position, radius, true), ships.Count > 0, !_focusTeamForce.IsEmpty);
            return (_stationPressure.HasPressure, ShouldGatherReinforcements(ownBase));
        }

        public EnemyStrategicContext Build(IReadOnlyList<IShipEntity> ships = null,
            ISet<string> reservedCaptureObjectives = null, bool preferStrategicObjectives = false)
        {
            _preferStrategicObjectives = preferStrategicObjectives;
            PlayerId self = _owner.Id;
            _groupScoped = ships != null;
            List<IShipEntity> ownShips;
            if (ships == null) ownShips = GetShips(self);
            else
            {
                _ownShips.Clear();
                _ownShips.AddRange(ships);
                ownShips = _ownShips;
            }
            _reservedCaptureObjectives = reservedCaptureObjectives;
            _ionRiskWeight = 1f;
            foreach (IShipEntity ship in ownShips)
            {
                IHealthModelObserver health = _entityLocator.GetEntity(ship.EntityId).HealthModel;
                float durability = health.Hull + health.Shields;
                if (durability <= 0f) continue;
                float capitalWeight = health.ShipClass == ShipClass.Capital || health.ShipClass == ShipClass.HeavyCapital ? 2f : 1f;
                _ionRiskWeight = Mathf.Max(_ionRiskWeight, 1f + capitalWeight * health.Shields / durability);
            }
            Vector3 origin = ToVector(CalculateFleetCenter(ownShips));
            EnemyStrategicContextBuilder stationAssessment = _stationAssessment ?? this;
            Vector3? forwardFallback = stationAssessment.ForwardStationFallback;
            GameEntity ownBase = stationAssessment._withdrawingFromStation ?? FindOwnBase(origin);
            Vector3 home = ownBase != null ? ownBase.GetFacade<IEntityTransformFacade>().Transform.position : origin;
            PlayerId focusEnemy = FindFocusEnemy(home);
            GameEntity enemyBaseTarget = FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == focusEnemy, origin);
            _strategicCenter = enemyBaseTarget == null ? home :
                (home + enemyBaseTarget.GetFacade<IEntityTransformFacade>().Transform.position) * 0.5f;
            float now = Time.time;
            _fleetTarget = FindClosestKnownShip(origin, now);
            GameEntity enemyFleetTarget = _fleetTarget;
            int focusTeamShipCount = CountKnownShips(focusEnemy, now);
            int ownedCapturableZoneCount = _reinforcementZonesSystem.GetOwnedCapturableZoneCount(self);
            _forceBuilder.Build(_ownForce, entity => entity.Owner == self &&
                (ships == null || ownShips.Exists(ship => ship.EntityId == entity.Id)));
            if (ships == null)
                _forceBuilder.Build(_ownTeamForce, entity => _playerRoster.IsAllied(self, entity.Owner));
            _forceBuilder.BuildKnown(_focusTeamForce, _intel, now,
                sighting => focusEnemy != PlayerId.None && _playerRoster.IsAllied(focusEnemy, sighting.Owner));
            GameEntity defenseTarget = BuildBaseThreat(ownBase);
            _forceBuilder.BuildKnown(_baseThreatForce, _intel, now, sighting => _stationThreats.Contains(sighting.EntityId));
            BuildHostilesNear(_localThreatForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            BuildOwnNear(_localOwnForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            _localThreat = FindLocalThreat(origin, now);
            _raidTarget = FindRaidTarget(origin, now);
            if (!_groupScoped)
                _recovery.Update(origin, CombatMatchup.Advantage(_ownTeamForce, _focusTeamForce),
                    _localThreat != null, HasOwnStation(), now,
                    (position, radius) => ownShips.Count == 0 || IsObjectiveSafe(origin, position, radius, true), ownShips.Count > 0, !_focusTeamForce.IsEmpty);
            EnemyCaptureObjective captureObjective = SelectCaptureObjective(origin, now);
            Vector3? retreatAnchor = forwardFallback ?? FindRetreatAnchor(origin, ownBase);
            string recoveryReason = retreatAnchor.HasValue
                ? "Validated a safe home or allied relay anchor."
                : "No safe home or allied relay; hold/rebuild or take a viable local objective.";
            if (forwardFallback.HasValue)
                recoveryReason = "Withdrawing from an outmatched forward station to the rear of the primary station.";
            float baseThreatRatio = _baseThreatForce.IsEmpty
                ? 0f
                : 1f / CombatMatchup.Advantage(_ownForce, _baseThreatForce);

            float localRaw = CombatMatchup.Advantage(_localOwnForce, _localThreatForce);
            float fleetRaw = CombatMatchup.Advantage(ships == null ? _ownTeamForce : _ownForce, _focusTeamForce);
            _advantageSmoother.Sample(localRaw, fleetRaw, now,
                !_localThreatForce.IsEmpty, !_focusTeamForce.IsEmpty);
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
                recoveryCapturePending: captureObjective != null && _recovery.PendingCaptureObjective != null &&
                    captureObjective.Id == _recovery.PendingCaptureObjective.Id,
                withdrawFromForwardStation: forwardFallback.HasValue);
            _receivers.Clear();
            _shipForces.Clear();
            foreach (IShipEntity ship in ownShips)
            {
                GameEntity entity = _entityLocator.GetEntity(ship.EntityId);
                _receivers.Add(ship, entity);
                ForceComposition force = new ForceComposition();
                _forceBuilder.AddUnit(force, entity);
                AddEscortingAirWing(force, entity);
                _shipForces.Add(ship, force);
            }
            _targetForces.Clear();
            AddTargetForce(enemyFleetTarget, now);
            AddTargetForce(enemyBaseTarget, now);
            AddTargetForce(_raidTarget, now);
            AddTargetForce(_localThreat, now);
            AddTargetForce(defenseTarget, now);
            return new EnemyStrategicContext(
                snapshot: snapshot,
                ships: ownShips,
                captureTarget: captureObjective != null ? captureObjective.Position : origin,
                enemyFleetTarget: enemyFleetTarget,
                enemyBaseTarget: enemyBaseTarget,
                ownBase: ownBase,
                receivers: _receivers,
                shipForces: _shipForces,
                targetForces: _targetForces,
                localThreatTarget: _localThreat,
                raidTarget: _raidTarget,
                captureObjective: captureObjective,
                defenseTarget: defenseTarget,
                stationPressureReason: !_stationPressure.HasPressure ? "No recent station pressure." :
                    defenseTarget == null ? "Station losing durability; attacker not visible" :
                    "Visible hostile can reach the station footprint.",
                retreatAnchor: retreatAnchor,
                recoveryReason: recoveryReason + " " + _captureReason,
                stationPressure: _stationPressure,
                isDefensePointClear: (point, radius) => _navigation.IsPositionClear(point, radius, includeReservations: false));
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
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                Vector3 offset = ToVector(sighting.Position) - origin;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                float score = GetTargetScore(origin, ToVector(sighting.Position));
                bool isShip = sighting.UnitTypeId.IsShip;
                if (!_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits || entity.IsCloaked() ||
                    !_vision.IsVisible(_owner.Id, entity.GetFacade<IEntityTransformFacade>().Transform.position))
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

        /// <summary>
        /// The closest visible hostile structure (not a base), with no known hostile ship within
        /// <see cref="LOCAL_ENGAGEMENT_RADIUS"/> of it.
        /// </summary>
        private void AddTargetForce(GameEntity target, float now)
        {
            if (target == null || _targetForces.ContainsKey(target)) return;
            Vector3 center = target.GetFacade<IEntityTransformFacade>().Transform.position;
            ForceComposition force = new ForceComposition();
            _forceBuilder.BuildKnown(force, _intel, now, sighting =>
                sighting.EntityId != target.Id && _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                PlanarDistance(center, ToVector(sighting.Position)) <= LOCAL_ENGAGEMENT_RADIUS);
            if (target.IsUnit()) _forceBuilder.AddUnit(force, target);
            _targetForces.Add(target, force);
        }

        private GameEntity FindRaidTarget(Vector3 origin, float now)
        {
            GameEntity closest = null;
            bool closestIsMine = false;
            bool keepsPrevious = false;
            float closestDistance = float.MaxValue;
            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (!_playerRoster.IsHostile(_owner.Id, entity.Owner) ||
                    entity.IsUnit() ||
                    entity.IsPlayerBase() ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits)
                {
                    continue;
                }

                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                // A retained or new raid also needs a survivable position and route, not only a safe target.
                if (!_vision.IsVisible(_owner.Id, position) || IsDefended(position, now) ||
                    !IsRouteSafe(origin, position, 0f, false)) continue;
                bool isMine = entity.TryGetFacade(out IMiningFacilityFacade _);
                if (ReferenceEquals(entity, _raidTarget) && GetIonCost(position) == 0f)
                    keepsPrevious = true;
                float distance = GetTargetScore(origin, position);
                if (closest != null && (closestIsMine && !isMine ||
                    closestIsMine == isMine && distance >= closestDistance))
                {
                    continue;
                }

                closest = entity;
                closestIsMine = isMine;
                closestDistance = distance;
            }

            // Preserve an existing raid unless a mining target becomes available.
            return keepsPrevious && (!closestIsMine || _raidTarget.TryGetFacade(out IMiningFacilityFacade _))
                ? _raidTarget : closest;
        }

        private bool IsDefended(Vector3 position, float now)
        {
            float radiusSquared = LOCAL_ENGAGEMENT_RADIUS * LOCAL_ENGAGEMENT_RADIUS;
            _forceBuilder.BuildKnown(_objectiveThreat, _intel, now, sighting =>
            {
                Vector3 offset = ToVector(sighting.Position) - position;
                offset.y = 0f;
                return _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                    offset.sqrMagnitude <= radiusSquared;
            });
            return !_objectiveThreat.IsEmpty &&
                CombatMatchup.Advantage(_ownForce, _objectiveThreat) <
                EnemyAiDifficultyProfile.Get(_owner.Difficulty).RequiredAttackRatio;
        }

        /// <summary>
        /// The hostile player whose living station is closest to <paramref name="home"/>;
        /// once every hostile station is gone, the owner of the closest hostile ship.
        /// </summary>
        private PlayerId FindFocusEnemy(Vector3 home)
        {
            PlayerId self = _owner.Id;
            GameEntity closestStation = FindClosestEntity(EntityRoles.IsPlayerBase,
                owner => _playerRoster.IsHostile(self, owner),
                home);
            if (closestStation != null)
            {
                return closestStation.Owner;
            }

            GameEntity closestShip = FindClosestKnownShip(home, Time.time);
            return closestShip != null ? closestShip.Owner : PlayerId.None;
        }

        /// <summary>Retains a visible, safe fleet target; otherwise chooses the nearest known ship.</summary>
        private GameEntity FindClosestKnownShip(Vector3 origin, float now)
        {
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
                    !_vision.IsVisible(_owner.Id, entity.GetFacade<IEntityTransformFacade>().Transform.position))
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

        private EnemyCaptureObjective SelectCaptureObjective(Vector3 origin, float now)
        {
            CaptureReservedByOtherGroup = false;
            _captureReason = "Ranking viable capture objectives.";
            _captureCandidates.Clear();
            for (int i = 0; i < _zones.Zones.Count; i++)
            {
                ReinforcementZonePresenter zone = _zones.Zones[i];
                if (!zone.IsCapturable || _playerRoster.IsAllied(_owner.Id, zone.Owner)) continue;
                _captureCandidates.Add(new EnemyCaptureObjective($"zone:{i}", EnemyCaptureObjectiveType.ReinforcementZone,
                    zone.Owner, ToVector(ToPoint(zone.Center)), zone.Radius, zone.CaptureProgress));
            }
            foreach (IEnemyCaptureObjectiveSource source in _captureSources)
                if (!(source is AllyAiMarkerSource)) source.AddStrategicObjectives(_captureCandidates, _intel);
            if (AlliedMarkers != null) AlliedMarkers.AddStrategicObjectives(_captureCandidates, _intel);

            EnemyCaptureObjective best = null;
            EnemyCaptureObjective retained = null;
            float bestScore = float.NegativeInfinity;
            float retainedScore = 0f;
            string invalidReason = "ownedOrRemoved";
            foreach (EnemyCaptureObjective candidate in _captureCandidates)
            {
                bool isCurrent = _captureObjective != null && candidate.Id == _captureObjective.Id &&
                                 candidate.Owner == _captureObjective.Owner;
                if (_reservedCaptureObjectives != null && _reservedCaptureObjectives.Contains(candidate.Id))
                {
                    CaptureReservedByOtherGroup = true;
                    if (isCurrent) invalidReason = "reserved";
                    continue;
                }
                if (candidate.Id == _rejectedCaptureId && now < _captureRetryAt)
                {
                    if (isCurrent) invalidReason = "stalled";
                    continue;
                }
                if (!IsObjectiveSafe(origin, candidate.Position, candidate.Radius, false,
                        isCurrent ? RETAIN_SAFETY_SCALE : 1f))
                {
                    // Raw strength estimates dip for a tick as squadrons launch or die; only a sustained
                    // hostile advantage drops the current objective.
                    bool hostileDip = isCurrent && _objectiveSafetyReason.StartsWith("hostile", StringComparison.Ordinal);
                    if (hostileDip && _captureUnsafeSince < 0f) _captureUnsafeSince = now;
                    if (!hostileDip || now - _captureUnsafeSince >= UNSAFE_GRACE_SECONDS)
                    {
                        if (isCurrent) invalidReason = "unsafe";
                        AppendCaptureReason($"Rejected {candidate.Id}: {_objectiveSafetyReason}");
                        continue;
                    }
                    AppendCaptureReason($"Kept {candidate.Id} through unsafe grace: {_objectiveSafetyReason}");
                }
                else if (isCurrent) _captureUnsafeSince = -1f;
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
                float score = -GetCaptureScore(origin, candidate);
                if (best == null || candidate.IsThreatened && !best.IsThreatened ||
                    candidate.IsThreatened == best.IsThreatened && score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
                if (isCurrent) { retained = candidate; retainedScore = score; }
            }
            if (_rejectedCaptureId != null && now < _captureRetryAt)
                _recovery.RejectCaptureObjective(_rejectedCaptureId, _captureRetryAt);
            if (_recovery.IsActive && _recovery.PendingCaptureObjective != null)
            {
                EnemyCaptureObjective relay = _recovery.PendingCaptureObjective;
                if (!(relay.Id == _rejectedCaptureId && now < _captureRetryAt) &&
                    (_reservedCaptureObjectives == null || !_reservedCaptureObjectives.Contains(relay.Id)) &&
                    IsObjectiveSafe(origin, relay.Position, relay.Radius, false))
                {
                    best = relay;
                    bestScore = -GetCaptureScore(origin, relay);
                    if (retained == null || retained.Id != relay.Id) invalidReason = "recoveryRelay";
                    retained = retained != null && retained.Id == relay.Id ? retained : null;
                }
            }
            bool defensePreemptsCapture = best != null && best.IsThreatened &&
                retained != null && !retained.IsThreatened;
            if (defensePreemptsCapture) invalidReason = "threatenedSite";
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
            _captureReason += next == null ? " NoViableObjective: no safe capture destination." : $" Selected {next.Id}.";
            return next;
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
            if (!_preferStrategicObjectives) return GetTargetScore(origin, candidate.Position) - candidate.PriorityBonus;
            // The main fleet contests strategic relays; detachments continue collecting nearby facilities.
            // Every term is in linear map units: a squared travel term outgrew relay preference on large maps.
            float relayPenalty = candidate.Kind == EnemyCaptureObjectiveType.ReinforcementZone ? 0f : 2000f;
            return PlanarDistance(_strategicCenter, candidate.Position) * 2f +
                   GetTravelCost(origin, candidate.Position) + relayPenalty - candidate.PriorityBonus;
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
            float safetyScale = 1f)
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
                alreadyInside |= PlanarDistance(ship.WorldPosition, destination) <= radius;
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
            return IsRouteSafe(origin, destination, radius, escaping, safetyScale);
        }

        /// <summary>Known hostiles at the origin, along the route or near the destination are beatable.</summary>
        private bool IsRouteSafe(Vector3 origin, Vector3 destination, float radius, bool escaping, float safetyScale = 1f)
        {
            Vector3 segment = destination - origin;
            segment.y = 0f;
            _forceBuilder.BuildKnown(_objectiveThreat, _intel, Time.time, sighting =>
            {
                if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner)) return false;
                Vector3 position = ToVector(sighting.Position);
                Vector3 offset = position - origin;
                offset.y = 0f;
                float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0f;
                // An escape leaves the force at the origin behind wherever the anchor is; only a second force
                // ahead of it, along the route or at the anchor, can make the withdrawal unsafe.
                if (escaping && PlanarDistance(position, origin) <= LOCAL_ENGAGEMENT_RADIUS) return false;
                bool nearDestination = PlanarDistance(position, destination) <= LOCAL_ENGAGEMENT_RADIUS + radius;
                bool alongRoute = PlanarDistance(position, origin + segment * t) <= LOCAL_ENGAGEMENT_RADIUS;
                return nearDestination || alongRoute;
            });
            if (_objectiveThreat.IsEmpty) return true;
            float advantage = CombatMatchup.Advantage(_groupScoped ? _ownForce : _localOwnForce, _objectiveThreat);
            float threshold = EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage * safetyScale;
            if (advantage > threshold) return true;
            _objectiveSafetyReason = $"hostile: known route/local force advantage {advantage:F2} <= safety threshold {threshold:F2}.";
            return false;
        }

        private Vector3? FindRetreatAnchor(Vector3 origin, GameEntity ownBase)
        {
            if (_ownShips.Count == 0) return null;
            return _recovery.FindRetreatAnchor(origin, ownBase != null,
                (position, radius) => IsObjectiveSafe(origin, position, radius, true));
        }

        private bool HasOwnStation()
        {
            foreach (GameEntity entity in _entityLocator.Entities)
                if (entity.Owner == _owner.Id && entity.IsPlayerBase() && !entity.HealthModel.IsDestroyed) return true;
            return false;
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
                if (ship.Owner == owner && _entityLocator.TryGetEntity(ship.EntityId, out GameEntity _))
                    _ownShips.Add(ship);
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

        /// <summary>Currently visible hostiles whose effective attack reach covers the station footprint.</summary>
        private GameEntity BuildBaseThreat(GameEntity ownBase)
        {
            _stationThreats.Clear();
            GameEntity target = null;
            float closest = float.MaxValue;
            if (ownBase != null)
            {
                Vector3 center = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
                float footprint = ownBase.GetFacade<INavigationFootprintFacade>().NavigationRadius;
                foreach (HostileSighting sighting in _intel.Sightings)
                {
                    if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                        _intel.GetConfidence(sighting, Time.time) <= 0f ||
                        !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity hostile) ||
                        hostile.HealthModel.IsDestroyed || !hostile.HealthModel.HasUnits || hostile.IsCloaked() ||
                        !hostile.TryGetFacade(out IUnitCombatObserverFacade combat)) continue;
                    Vector3 position = hostile.GetFacade<IEntityTransformFacade>().Transform.position;
                    if (!_vision.IsVisible(_owner.Id, position)) continue;
                    float hostileRadius = hostile.TryGetFacade(out INavigationFootprintFacade body) ? body.NavigationRadius : 0f;
                    float distance = PlanarDistance(position, center);
                    if (!CanThreatenStation(distance, footprint, hostileRadius, combat.WeaponRange)) continue;
                    // Hull-edge reach requires a commanded or weapon-tracked station attack. Other contacts must be
                    // close enough to engage from their centre or at their navigation contact distance.
                    if (!combat.IsAttacking(ownBase) && distance > Mathf.Max(combat.WeaponRange, footprint + hostileRadius)) continue;
                    _stationThreats.Add(hostile.Id);
                    bool attackingBase = combat.IsAttacking(ownBase);
                    bool previousAttacksBase = target != null && target.GetFacade<IUnitCombatObserverFacade>().IsAttacking(ownBase);
                    if (target == null || attackingBase && !previousAttacksBase ||
                        attackingBase == previousAttacksBase && distance < closest)
                    {
                        closest = distance;
                        target = hostile;
                    }
                }
            }
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

        private GameEntity FindOwnBase(Vector3 origin)
        {
            PlayerId productionHost = _productionAccess.ProductionHost;
            return FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == productionHost, origin);
        }

        private Vector3? FindForwardStationFallback(GameEntity station, Vector3 origin)
        {
            if (!_stationPressure.HasPressure || station == null ||
                !station.TryGetFacade(out ISecondStationFacade _)) return null;

            Vector3 front = station.GetFacade<IEntityTransformFacade>().Transform.position;
            if (_withdrawingFromStation != station)
            {
                float radius = LOCAL_ENGAGEMENT_RADIUS + station.GetFacade<INavigationFootprintFacade>().NavigationRadius;
                BuildHostilesNear(_localThreatForce, front, radius);
                BuildOwnNear(_baseThreatForce, front, radius);
                float threshold = EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage;
                if (CombatMatchup.Advantage(_baseThreatForce, _localThreatForce) > threshold) return null;
            }

            GameEntity primary = FindClosestEntity(
                entity => entity.IsPlayerBase() && !entity.TryGetFacade(out ISecondStationFacade _),
                owner => owner == station.Owner, front);
            if (primary == null) return null;

            Vector3 rear = primary.GetFacade<IEntityTransformFacade>().Transform.position;
            Vector3 away = rear - front;
            away.y = 0f;
            if (away.sqrMagnitude == 0f) return null;
            float clearance = 0f;
            foreach (IShipEntity ship in _ownShips) clearance = Mathf.Max(clearance, ship.NavigationRadius);
            Vector3 rally = _withdrawingFromStation == station && ForwardStationFallback is Vector3 previous
                ? previous
                : rear + away.normalized *
                    (primary.GetFacade<INavigationFootprintFacade>().NavigationRadius + 2f * clearance);
            if (IsSafeFallback(rally)) return rally;
            if (_reinforcementZonesSystem.TryGetDefaultZoneCenter(primary.Owner, out Vector3 home) &&
                PlanarDistance(home, front) > PlanarDistance(rear, front) && IsSafeFallback(home)) return home;
            return null;

            bool IsSafeFallback(Vector3 point) => _recovery.IsPlacementSafe(point, clearance) &&
                (_ownShips.Count == 0
                    ? _navigation.IsPositionClear(point, clearance, includeReservations: false)
                    : IsObjectiveSafe(origin, point, 0f, true));
        }

        private bool ShouldGatherReinforcements(GameEntity ownBase)
        {
            if (ownBase == null || _stationPressure.HasPressure || Time.time - _stationPressure.LastDamageTime < 5f)
                return false;

            Vector3 home = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
            float radius = LOCAL_ENGAGEMENT_RADIUS + ownBase.GetFacade<INavigationFootprintFacade>().NavigationRadius;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (sighting.UnitTypeId.IsShip && _playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                    _intel.GetConfidence(sighting, Time.time) > 0f &&
                    PlanarDistance(home, ToVector(sighting.Position)) <= radius)
                    return true;
            }
            return false;
        }

        public static bool CanThreatenStation(float distance, float stationRadius, float hostileRadius, float weaponRange)
        {
            return weaponRange > 0f && distance <= stationRadius + hostileRadius + weaponRange;
        }

        /// <summary>Own ships and squadrons within <paramref name="radius"/> of <paramref name="center"/> (ground plane).</summary>
        private void BuildOwnNear(ForceComposition force, Vector3 center, float radius)
        {
            PlayerId self = _owner.Id;
            float radiusSquared = radius * radius;
            _forceBuilder.Build(force, entity =>
            {
                if (entity.Owner != self) return false;
                if (_groupScoped && !_ownShips.Exists(ship => ship.EntityId == entity.Id)) return false;
                Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - center;
                offset.y = 0f;
                return offset.sqrMagnitude <= radiusSquared;
            });
        }

        /// <summary>Hostile ships and squadrons within <paramref name="radius"/> of <paramref name="center"/> (ground plane).</summary>
        private void BuildHostilesNear(ForceComposition force, Vector3 center, float radius)
        {
            float radiusSquared = radius * radius;
            _forceBuilder.BuildKnown(force, _intel, Time.time, sighting =>
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
            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (!includeOwner(entity.Owner) ||
                    !hasRole(entity) ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits)
                {
                    continue;
                }

                float distance = (entity.GetFacade<IEntityTransformFacade>().Transform.position - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = entity;
                    closestDistance = distance;
                }
            }

            return closest;
        }
    }
}
