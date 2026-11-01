using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
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
using EmpireAtWar.Services.Ships;

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
        bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome);
    }

    public sealed class EnemyUnitCommander :
        IInitializable,
        ITickable,
        ILateDisposable,
        IEnemyAiDebugInfo,
        IEnemyAiStateProvider
    {
        private readonly IShipService _shipService;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;
        private readonly IUnitOrderService _unitOrderService;

        private readonly PlayerSlot _owner;
        private readonly EnemyShipTaskGroupPlanner _taskGroups;
        private readonly EnemyStrategicContextBuilder _contextBuilder;
        private readonly List<IShipEntity> _ships = new List<IShipEntity>();
        private readonly HashSet<string> _reservedCaptureObjectives = new HashSet<string>();
        private readonly Dictionary<IShipEntity, Vector3> _zoneExitTargets =
            new Dictionary<IShipEntity, Vector3>();

        private float _decisionTimer;

        private bool _hasDecision;
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

        public bool TryGetReinforcementRally(out Vector3 rally, out bool defendHome)
        {
            rally = default;
            defendHome = false;
            if (!_hasDecision) return false;
            foreach (EnemyShipTaskGroup group in _taskGroups.Groups)
            {
                if (group.Context == null || !group.Context.Snapshot.HasStationPressure) continue;
                // Cached decisions can outlive entities while world-change reevaluation is deferred.
                if (group.Context.OwnBase == null ||
                    !_entityLocator.TryGetEntity(group.Context.OwnBase.Id, out GameEntity ownBase)) continue;
                defendHome = true;
                rally = ownBase.GetFacade<EmpireAtWar.Entities.BaseEntity.EntityFacades.IEntityTransformFacade>()
                    .Transform.position;
                return true;
            }
            // Deploy with the assembled fleet, not at an isolated relay beside its distant objective.
            foreach (EnemyShipTaskGroup group in _taskGroups.Groups)
            {
                if (group.IsCaptureDetachment || group.Ships.Count == 0) continue;
                int shipCount = 0;
                foreach (IShipEntity ship in group.Ships)
                {
                    if (!_entityLocator.TryGetEntity(ship.EntityId, out GameEntity _)) continue;
                    rally += ship.WorldPosition;
                    shipCount++;
                }
                if (shipCount == 0) continue;
                rally /= shipCount;
                return true;
            }
            return false;
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
            PlayerSlot owner)
        {
            _owner = owner;
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _entityLocator = entityLocator;
            _gameModel = gameModel;
            _contextBuilder = contextBuilder;
            _taskGroups = new EnemyShipTaskGroupPlanner(contextBuilder, decisionModel, taskForceExecutor, unitOrderService);
            _unitOrderService = unitOrderService;
        }

        public void Initialize()
        {
            _shipService.ShipAdded += HandleShipChanged;
            _shipService.ShipRemoved += HandleShipRemoved;
            _reinforcementZonesSystem.OwnershipChanged += HandleWorldChanged;
            _entityLocator.EntityAdded += HandleEntityChanged;
            _entityLocator.EntityRemoved += HandleEntityChanged;
            EvaluateAndExecute();
            _decisionTimer += EnemyAiStagger.Offset(_owner.Id, _decisionTimer);
        }

        public void LateDispose()
        {
            _shipService.ShipAdded -= HandleShipChanged;
            _shipService.ShipRemoved -= HandleShipRemoved;
            _zoneExitTargets.Clear();
            _reinforcementZonesSystem.OwnershipChanged -= HandleWorldChanged;
            _entityLocator.EntityAdded -= HandleEntityChanged;
            _entityLocator.EntityRemoved -= HandleEntityChanged;
        }

        public void Tick()
        {
            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer <= 0f)
            {
                EvaluateAndExecute();
            }
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
            // Removal events also arrive during teardown, after zone views may be destroyed.
            // Defer world queries to a later gameplay tick, on a frame of this player's own.
            _decisionTimer = Mathf.Min(_decisionTimer, EnemyAiStagger.WorldChangeDelay(_owner.Id));
        }

        private void EvaluateAndExecute()
        {
            _decisionTimer = EnemyAiDifficultyProfile.Get(_owner.Difficulty).DecisionInterval;
            _ships.Clear();
            foreach (IShipEntity ship in _shipService.Ships)
                if (ship.Owner == _owner.Id && _entityLocator.TryGetEntity(ship.EntityId, out GameEntity _))
                    _ships.Add(ship);
            bool stationPressure = _contextBuilder.Build(_ships).Snapshot.HasStationPressure;
            _taskGroups.Reconcile(_ships, stationPressure);
            _reservedCaptureObjectives.Clear();
            EnemyShipTaskGroup primary = null;
            int bodyCount = 0;
            int rallyingCount = 0;
            string groupSummary = "";
            foreach (EnemyShipTaskGroup group in _taskGroups.Groups)
            {
                group.Evaluate(_reservedCaptureObjectives, Time.time);
                if (group.Decision.State == EnemyStrategicState.RebuildFleet ||
                    group.Decision.State == EnemyStrategicState.Hold &&
                    (!group.IsMainForce || group.Context.Snapshot.IsRegrouped))
                    MoveShipsOutOfDefaultZone(group.Ships);
                else
                    foreach (IShipEntity ship in group.Ships) _zoneExitTargets.Remove(ship);
                bodyCount += group.Executor.BodyCount;
                rallyingCount += group.Executor.RallyingCount;
                if (primary == null || primary.IsCaptureDetachment && !group.IsCaptureDetachment ||
                    primary.IsCaptureDetachment == group.IsCaptureDetachment && group.Ships.Count > primary.Ships.Count)
                    primary = group;
                groupSummary += $" {(group.IsCaptureDetachment ? "Capture" : group.IsMainForce ? "Main" : "Combat")} group: " +
                    $"{group.Ships.Count} ships, {group.Decision.State}.";
            }
            _lastContext = primary.Context;
            EnemyStrategicSnapshot snapshot = primary.Context.Snapshot;
            LastSnapshot = new EnemyStrategicSnapshot(snapshot.VictoryCondition, snapshot.Difficulty,
                _ships.Count, snapshot.EnemyShipCount, snapshot.OwnedCapturableZoneCount,
                snapshot.FleetAdvantage, snapshot.BaseThreatRatio, snapshot.HasCaptureTarget,
                snapshot.HasEnemyBaseTarget, snapshot.HasOwnBase, snapshot.HasThreatenedSite,
                snapshot.LocalAdvantage, snapshot.HasLocalThreat, snapshot.HasRaidTarget,
                snapshot.IsRegrouped, snapshot.HasStationPressure, snapshot.HasSafeRetreat,
                snapshot.HasEconomyRaidTarget);
            _recoveryStartedAt = primary.RecoveryStartedAt;
            _actionReason = primary.Executor.ActionReason + groupSummary;
            PublishDecision(primary.Decision, bodyCount, rallyingCount);
            _taskGroups.TryCreateCaptureGroup(_ships, _reservedCaptureObjectives, stationPressure);
        }

        private void MoveShipsOutOfDefaultZone(IReadOnlyList<IShipEntity> ships)
        {
            List<IShipEntity> unassignedShips = new List<IShipEntity>();
            List<FormationPoint> positions = new List<FormationPoint>();
            List<float> radii = new List<float>();
            float maximumRadius = 0f;
            foreach (IShipEntity ship in ships)
            {
                if (_zoneExitTargets.TryGetValue(ship, out Vector3 target))
                {
                    if (_reinforcementZonesSystem.TryGetDefaultZoneExitPosition(
                            _owner.Id, ship.WorldPosition, ship.NavigationRadius, out _))
                    {
                        _unitOrderService.IssueMove(new[] { _entityLocator.GetEntity(ship.EntityId) },
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
                        _unitOrderService.IssueStop(new[] { _entityLocator.GetEntity(ship.EntityId) });
                    continue;
                }

                unassignedShips.Add(ship);
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
                _unitOrderService.IssueMove(new[] {
                    _entityLocator.GetEntity(unassignedShips[i].EntityId) },
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
                $"[EnemyAI] Player={_owner.Id}:{_owner.Faction}, Difficulty={_owner.Difficulty}, " +
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
