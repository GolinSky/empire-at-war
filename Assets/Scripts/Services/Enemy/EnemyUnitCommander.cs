using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Game;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyAiDebugInfo : IService
    {
        event Action<EnemyStrategicDecision> DecisionChanged;

        EnemyStrategicDecision LastDecision { get; }
        EnemyStrategicSnapshot LastSnapshot { get; }
        /// <summary>False until the first strategic decision fills <see cref="LastSnapshot"/>.</summary>
        bool HasDecision { get; }
    }

    public interface IEnemyAiStateProvider
    {
        EnemyStrategicState CurrentState { get; }
        int ActiveShipCount { get; }
        bool HoldFinishedReinforcements => false;
        bool IsStationCritical => false;
        bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome);
        void FillDefenseDeploymentThreats(PlacementThreatSnapshot threats) =>
            throw new InvalidOperationException("This AI does not provide station-defense deployment threats.");
    }

    public sealed class EnemyUnitCommander :
        IInitializable,
        ITickable,
        ILateDisposable,
        IObserver<BattleState>,
        IEnemyAiDebugInfo,
        IEnemyAiStateProvider
    {
        private const float MINIMUM_WORLD_CHANGE_DECISION_GAP = 0.5f;

        private readonly IShipService _shipService;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;
        private readonly INotifier<BattleState> _battleState;
        private readonly IUnitOrderService _unitOrderService;

        private readonly PlayerSlot _owner;
        private readonly EnemyShipTaskGroupPlanner _taskGroups;
        private readonly EnemyStrategicContextBuilder _contextBuilder;
        private readonly EnemyPatrolService _patrol;
        private readonly EnemyPatrolShipController _patrolShips;
        private readonly EnemyAiScheduler _scheduler;
        private readonly List<IShipEntity> _ships = new List<IShipEntity>();
        private readonly HashSet<string> _reservedCaptureObjectives = new HashSet<string>();
        private readonly Dictionary<IShipEntity, Vector3> _zoneExitTargets =
            new Dictionary<IShipEntity, Vector3>();

        private EnemyAiScheduler.WorkHandle _workHandle;
        private bool _cycleInProgress;
        private int _nextGroupIndex;
        private bool _cycleHasPressure;
        private IShipEntity _cyclePatrolShip;
        private EnemyShipTaskGroup _cyclePrimary;
        private int _cycleBodyCount;
        private int _cycleRallyingCount;
        private string _cycleGroupSummary = "";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker ASSESS_STATION_MARKER = new ProfilerMarker("EnemyAI.Ship.AssessStation");
        private static readonly ProfilerMarker RECONCILE_MARKER = new ProfilerMarker("EnemyAI.Ship.Reconcile");
        private static readonly ProfilerMarker BUILD_GROUP_MARKER = new ProfilerMarker("EnemyAI.Ship.Build");
        private static readonly ProfilerMarker EXECUTOR_MARKER = new ProfilerMarker("EnemyAI.Ship.Executor");
        private static readonly ProfilerMarker CAPTURE_PROBE_MARKER = new ProfilerMarker("EnemyAI.Ship.CaptureProbe");
        private static readonly ProfilerMarker TELEMETRY_MARKER = new ProfilerMarker("EnemyAI.Ship.Telemetry");
        private static readonly ProfilerMarker PATROL_SAMPLE_MARKER = new ProfilerMarker("EnemyAI.Ship.PatrolSample");
        private static readonly ProfilerMarker PATROL_SELECT_MARKER = new ProfilerMarker("EnemyAI.Ship.PatrolSelect");
        public EnemyDecisionDiagnostics DecisionDiagnostics { get; }
        public int WorldChangeNotifications { get; private set; }
#endif

        private bool _hasDecision;
        private bool _isRunning;
        private float _recoveryStartedAt = -1f;
        private EnemyStrategicContext _lastContext;
        private string _actionReason;

        public event Action<EnemyStrategicDecision> DecisionChanged;

        public string Id => nameof(EnemyUnitCommander);
        public EnemyStrategicDecision LastDecision { get; private set; }
        public EnemyStrategicSnapshot LastSnapshot { get; private set; }
        public bool HasDecision => _hasDecision;
        public EnemyStrategicState CurrentState =>
            _hasDecision ? LastDecision.State : EnemyStrategicState.RebuildFleet;
        public int ActiveShipCount => _ships.Count;
        public bool HoldFinishedReinforcements => true;
        public bool IsStationCritical => _contextBuilder.StationCritical;

        public void FillDefenseDeploymentThreats(PlacementThreatSnapshot threats) =>
            _contextBuilder.FillDefenseDeploymentThreats(threats);

        public bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome)
        {
            rally = default;
            defendHome = _contextBuilder.TryGetDefendedStation(out rally);
            if (defendHome) return true;
            if (!_hasDecision) return false;
            foreach (EnemyShipTaskGroup group in _taskGroups.Groups)
            {
                GameEntity station = group.Context?.OwnBase;
                if (station == null || !group.Context.Snapshot.HasStationPressure ||
                    !_entityLocator.TryGetEntity(station.Id, out GameEntity liveStation) ||
                    liveStation.HealthModel.IsDestroyed) continue;
                defendHome = true;
                rally = liveStation.GetFacade<IEntityTransformFacade>().Transform.position;
                return true;
            }
            foreach (EnemyShipTaskGroup group in _taskGroups.Groups)
            {
                if (!group.IsMainForce || group.Context == null) continue;
                if (TryGetTaskRally(group, out rally)) return true;
            }
            foreach (EnemyShipTaskGroup group in _taskGroups.Groups)
                if (TryGetTaskRally(group, out rally)) return true;
            return false;
        }

        private bool TryGetTaskRally(EnemyShipTaskGroup group, out Vector3 rally)
        {
            rally = default;
            EnemyStrategicContext context = group.Context;
            if (context == null) return false;
            if (group.Decision.State == EnemyStrategicState.CaptureZone && context.CaptureObjective != null)
            {
                rally = context.CaptureTarget;
                return true;
            }
            if (group.Decision.State == EnemyStrategicState.GuardSite && context.GuardPosition is Vector3 guard)
            {
                rally = guard;
                return true;
            }
            GameEntity target = group.Decision.State switch
            {
                EnemyStrategicState.RaidStructures => context.RaidTarget,
                EnemyStrategicState.EngageLocal => context.LocalThreatTarget,
                EnemyStrategicState.AssaultBase => context.EnemyBaseTarget,
                EnemyStrategicState.HuntFleet => context.EnemyFleetTarget,
                _ => null
            };
            if (target == null || !_entityLocator.TryGetEntity(target.Id, out GameEntity liveTarget) ||
                liveTarget.HealthModel.IsDestroyed) return false;
            rally = liveTarget.GetFacade<IEntityTransformFacade>().Transform.position;
            return true;
        }

        public EnemyUnitCommander(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel,
            IUnitOrderService unitOrderService,
            EnemyStrategicDecisionModel decisionModel,
            EnemyStrategicContextBuilder contextBuilder,
            EnemyTaskForceExecutor taskForceExecutor,
            PlayerSlot owner, EnemyPatrolService patrol, EnemyPatrolShipController patrolShips,
            INotifier<BattleState> battleState, EnemyAiScheduler scheduler)
        {
            _owner = owner;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DecisionDiagnostics = new EnemyDecisionDiagnostics("EnemyAI.Ship.Decision." + owner.Id);
#endif
            _patrol = patrol;
            _patrolShips = patrolShips;
            _scheduler = scheduler;
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _entityLocator = entityLocator;
            _gameModel = gameModel;
            _battleState = battleState;
            _contextBuilder = contextBuilder;
            _taskGroups = new EnemyShipTaskGroupPlanner(contextBuilder, decisionModel, taskForceExecutor, unitOrderService);
            _unitOrderService = unitOrderService;
        }

        public void Initialize()
        {
            // Player scopes initialize before the asynchronous map load finishes.
            float interval = EnemyAiDifficultyProfile.Get(_owner.Difficulty).DecisionInterval;
            float initialDelay = EnemyAiStagger.Offset(_owner.Id, interval);
            _workHandle = _scheduler.Register(
                _owner.Id,
                interval,
                initialDelay,
                RunScheduledUnit,
                () => _contextBuilder.StationCritical,
                () => _cycleInProgress);
            _shipService.ShipAdded += HandleShipChanged;
            _shipService.ShipRemoved += HandleShipRemoved;
            _reinforcementZonesSystem.OwnershipChanged += HandleWorldChanged;
            _entityLocator.EntityAdded += HandleEntityChanged;
            _entityLocator.EntityRemoved += HandleEntityChanged;
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _isRunning = false;
            _shipService.ShipAdded -= HandleShipChanged;
            _shipService.ShipRemoved -= HandleShipRemoved;
            _zoneExitTargets.Clear();
            _reinforcementZonesSystem.OwnershipChanged -= HandleWorldChanged;
            _entityLocator.EntityAdded -= HandleEntityChanged;
            _entityLocator.EntityRemoved -= HandleEntityChanged;
            _contextBuilder.TeamCooperation.RemovePlayer(_owner.Id);
            if (_workHandle != null)
            {
                _scheduler.Unregister(_workHandle);
                _workHandle = null;
            }
        }

        public void UpdateState(BattleState state)
        {
            _isRunning = state == BattleState.Running;
        }

        public void Tick()
        {
            if (!_isRunning) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DecisionDiagnostics.Tick(Time.deltaTime);
#endif
        }

        private void HandleShipChanged(IShipEntity ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            HandleWorldChanged();
        }

        private void HandleShipRemoved(IShipEntity ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            _ships.Remove(ship);
            if (_cyclePatrolShip == ship) _cyclePatrolShip = null;
            _zoneExitTargets.Remove(ship);
            HandleWorldChanged();
        }

        private void HandleEntityChanged(GameEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            HandleWorldChanged();
        }

        private void HandleWorldChanged()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_isRunning) WorldChangeNotifications++;
#endif
            _scheduler.MarkDirty(_workHandle, MINIMUM_WORLD_CHANGE_DECISION_GAP);
        }

        private void RunScheduledUnit()
        {
            if (!_cycleInProgress)
            {
                BeginDecisionCycle();
            }

            if (EvaluateNextGroup())
            {
                FinishDecisionCycle();
            }
        }

        private void BeginDecisionCycle()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DecisionDiagnostics.Record();
            using var marker = DecisionDiagnostics.Marker.Auto();
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (PATROL_SAMPLE_MARKER.Auto())
#endif
                _patrol.Sample(Time.time);
            _ships.Clear();
            foreach (IShipEntity ship in _shipService.Ships)
                if (ship.Owner == _owner.Id &&
                    _entityLocator.TryGetEntity(ship.EntityId, out GameEntity entity) &&
                    !entity.HealthModel.IsDestroyed)
                    _ships.Add(ship);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (ASSESS_STATION_MARKER.Auto())
#endif
                _cycleHasPressure = _contextBuilder.AssessStation(_ships);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (PATROL_SELECT_MARKER.Auto())
#endif
                _cyclePatrolShip = _patrolShips.Select(_ships,
                    _contextBuilder.StationCritical);
            if (_cyclePatrolShip != null) _ships.Remove(_cyclePatrolShip);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (RECONCILE_MARKER.Auto())
#endif
                _taskGroups.Reconcile(_ships, _cycleHasPressure);
            _reservedCaptureObjectives.Clear();
            _nextGroupIndex = 0;
            _cyclePrimary = null;
            _cycleBodyCount = 0;
            _cycleRallyingCount = 0;
            _cycleGroupSummary = "";
            _cycleInProgress = true;
        }

        private bool EvaluateNextGroup()
        {
            if (_nextGroupIndex >= _taskGroups.Groups.Count)
            {
                return true;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var marker = DecisionDiagnostics.Marker.Auto();
#endif
            EnemyShipTaskGroup group = _taskGroups.Groups[_nextGroupIndex++];
            PruneDeadShips(group.Ships);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BUILD_GROUP_MARKER.Auto())
#endif
                group.Evaluate(_reservedCaptureObjectives, Time.time);
            _contextBuilder.TeamCooperation.AddReservations(_owner.Id, _reservedCaptureObjectives);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (EXECUTOR_MARKER.Auto())
#endif
            {
                if (!group.HasMarkerRequest && (group.Decision.State == EnemyStrategicState.RebuildFleet ||
                    group.Decision.State == EnemyStrategicState.Hold &&
                    (!group.IsMainForce || group.Context.Snapshot.IsRegrouped)))
                    MoveShipsOutOfDefaultZone(group.Ships);
                else
                    foreach (IShipEntity ship in group.Ships) _zoneExitTargets.Remove(ship);
            }
            _cycleBodyCount += group.Executor.BodyCount;
            _cycleRallyingCount += group.Executor.RallyingCount;
            if (_cyclePrimary == null || _cyclePrimary.IsCaptureDetachment && !group.IsCaptureDetachment ||
                _cyclePrimary.IsCaptureDetachment == group.IsCaptureDetachment && group.Ships.Count > _cyclePrimary.Ships.Count)
                _cyclePrimary = group;
            _cycleGroupSummary += $" {(group.IsCaptureDetachment ? "Capture" : group.IsMainForce ? "Main" : "Combat")} group: " +
                $"{group.Ships.Count} ships, {group.Decision.State}.";

            return _nextGroupIndex >= _taskGroups.Groups.Count;
        }

        private void FinishDecisionCycle()
        {
            _cycleInProgress = false;
            if (_cyclePrimary == null)
            {
                _contextBuilder.TeamCooperation.PublishReservations(_owner.Id, _reservedCaptureObjectives);
                if (_cyclePatrolShip != null) _ships.Add(_cyclePatrolShip);
                return;
            }

            _lastContext = _cyclePrimary.Context;
            EnemyStrategicSnapshot snapshot = _cyclePrimary.Context.Snapshot;
            LastSnapshot = new EnemyStrategicSnapshot(snapshot.VictoryCondition, snapshot.Difficulty,
                _ships.Count + (_cyclePatrolShip == null ? 0 : 1), snapshot.EnemyShipCount, snapshot.OwnedCapturableZoneCount,
                snapshot.FleetAdvantage, snapshot.BaseThreatRatio, snapshot.HasCaptureTarget,
                snapshot.HasEnemyBaseTarget, snapshot.HasOwnBase, snapshot.HasThreatenedSite,
                snapshot.LocalAdvantage, snapshot.HasLocalThreat, snapshot.HasRaidTarget,
                snapshot.IsRegrouped, snapshot.HasStationPressure, snapshot.HasSafeRetreat,
                snapshot.HasEconomyRaidTarget, snapshot.LocalRaw, snapshot.FleetRaw,
                snapshot.RecoveryMode, snapshot.IsStationCritical, snapshot.HasGuardSite, snapshot.IsCentreGuardGoal);
            _recoveryStartedAt = _cyclePrimary.RecoveryStartedAt;
            _actionReason = _cyclePrimary.Executor.ActionReason + _cycleGroupSummary;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (TELEMETRY_MARKER.Auto())
#endif
            {
                PublishDecision(_cyclePrimary.Decision, _cycleBodyCount, _cycleRallyingCount);
                if (_contextBuilder.AlliedMarkers != null)
                    _contextBuilder.AlliedMarkers.Observe(_cyclePrimary.Context, _cyclePrimary.Decision);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (CAPTURE_PROBE_MARKER.Auto())
#endif
            {
                PruneDeadShips(_ships);
                _taskGroups.TryCreateCaptureGroup(_ships, _reservedCaptureObjectives, _cycleHasPressure);
            }
            _contextBuilder.TeamCooperation.PublishReservations(_owner.Id, _reservedCaptureObjectives);
            if (_cyclePatrolShip != null && _entityLocator.TryGetEntity(_cyclePatrolShip.EntityId, out GameEntity patrolEntity) &&
                !patrolEntity.HealthModel.IsDestroyed)
            {
                _ships.Add(_cyclePatrolShip);
            }
        }

        private void PruneDeadShips(List<IShipEntity> ships)
        {
            for (int i = ships.Count - 1; i >= 0; i--)
            {
                if (!_entityLocator.TryGetEntity(ships[i].EntityId, out GameEntity entity) ||
                    entity.HealthModel.IsDestroyed)
                {
                    ships.RemoveAt(i);
                }
            }
        }

        private void MoveShipsOutOfDefaultZone(IReadOnlyList<IShipEntity> ships)
        {
            List<IShipEntity> unassignedShips = new List<IShipEntity>();
            List<GameEntity> unassignedEntities = new List<GameEntity>();
            List<FormationPoint> positions = new List<FormationPoint>();
            List<float> radii = new List<float>();
            float maximumRadius = 0f;
            foreach (IShipEntity ship in ships)
            {
                if (!_entityLocator.TryGetEntity(ship.EntityId, out GameEntity shipEntity) ||
                    shipEntity.HealthModel.IsDestroyed)
                {
                    _zoneExitTargets.Remove(ship);
                    continue;
                }

                if (_zoneExitTargets.TryGetValue(ship, out Vector3 target))
                {
                    if (_reinforcementZonesSystem.TryGetDefaultZoneExitPosition(
                            _owner.Id, ship.WorldPosition, ship.NavigationRadius, out _))
                    {
                        _unitOrderService.IssueMove(new[] { shipEntity },
                            new[] { target });
                        continue;
                    }
                    _zoneExitTargets.Remove(ship);
                }

                if (!_reinforcementZonesSystem.TryGetDefaultZoneExitPosition(
                        _owner.Id,
                        ship.WorldPosition,
                        ship.NavigationRadius,
                        out _))
                {
                    _zoneExitTargets.Remove(ship);
                    if (ship.CurrentOrder != UnitOrderType.None)
                        _unitOrderService.IssueStop(new[] { shipEntity });
                    continue;
                }

                unassignedShips.Add(ship);
                unassignedEntities.Add(shipEntity);
                positions.Add(ToPoint(ship.WorldPosition));
                radii.Add(ship.NavigationRadius);
                maximumRadius = Mathf.Max(maximumRadius, ship.NavigationRadius);
            }

            if (unassignedShips.Count == 0)
            {
                return;
            }

            float formationClearance = maximumRadius *
                (2f * Mathf.Ceil(Mathf.Sqrt(unassignedShips.Count)) + 1f);
            if (!_reinforcementZonesSystem.TryGetDefaultZoneExitPosition(
                    _owner.Id, unassignedShips[0].WorldPosition,
                    formationClearance, out Vector3 exitPosition))
            {
                throw new InvalidOperationException("An exiting fleet requires its default zone.");
            }

            List<FormationPoint> destinations = new List<FormationPoint>();
            FormationModel.CalculateCompactDestinations(positions, radii,
                ToPoint(exitPosition), destinations);
            for (int i = 0; i < unassignedShips.Count; i++)
            {
                Vector3 target = ToVector(destinations[i]);
                _zoneExitTargets.Add(unassignedShips[i], target);
                _unitOrderService.IssueMove(new[] { unassignedEntities[i] },
                    new[] { target });
            }
        }

        private void PublishDecision(EnemyStrategicDecision decision, int bodyCount, int rallyingCount)
        {
            bool hasChanged = !_hasDecision ||
                LastDecision.State != decision.State ||
                LastDecision.CommittedShipCount != decision.CommittedShipCount ||
                LastDecision.Reason != decision.Reason;
            LastDecision = decision;
            _hasDecision = true;
            EnemyAiTelemetry.RecordDecision(_owner.Id, _owner.Faction, hasChanged, decision, LastSnapshot,
                bodyCount, rallyingCount, _lastContext,
                _actionReason,
                _recoveryStartedAt < 0f ? 0f : Time.time - _recoveryStartedAt);
            if (!hasChanged)
            {
                return;
            }

            Debug.Log(
                $"[EnemyAI] BattleTime={BattleClock.CurrentElapsed:F1}, Player={_owner.Id}:{_owner.Faction}, Difficulty={_owner.Difficulty}, " +
                $"Objective={_gameModel.VictoryCondition}, State={decision.State}, " +
                $"Committed={decision.CommittedShipCount}/{LastSnapshot.OwnShipCount}, " +
                $"EnemyShips={LastSnapshot.EnemyShipCount}, " +
                $"FleetAdvantage={LastSnapshot.FleetAdvantage:F2}, " +
                $"BaseThreat={LastSnapshot.BaseThreatRatio:F2}, LocalAdvantage={LastSnapshot.LocalAdvantage:F2}, " +
                $"ControlledZones={LastSnapshot.OwnedCapturableZoneCount}, " +
                $"Reason={decision.Reason}");
            DecisionChanged?.Invoke(decision);
        }
    }
}
