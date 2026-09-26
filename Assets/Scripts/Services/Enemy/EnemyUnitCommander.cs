using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyAiDebugInfo : IService
    {
        event Action<EnemyStrategicDecision> DecisionChanged;
        EnemyStrategicDecision LastDecision { get; }
        EnemyStrategicSnapshot LastSnapshot { get; }
    }

    public interface IEnemyAiStateProvider
    {
        EnemyStrategicState CurrentState { get; }
        int ActiveShipCount { get; }
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
        private readonly EnemyStrategicDecisionModel _decisionModel;
        private readonly EnemyStrategicContextBuilder _contextBuilder;
        private readonly EnemyTaskForceExecutor _taskForceExecutor;
        private readonly IUnitOrderService _orders;
        private readonly Dictionary<IShipEntity, Vector3> _zoneExitTargets =
            new Dictionary<IShipEntity, Vector3>();

        private float _decisionTimer;
        private bool _hasDecision;

        public EnemyUnitCommander(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel,
            EnemyStrategicDecisionModel decisionModel,
            EnemyStrategicContextBuilder contextBuilder,
            EnemyTaskForceExecutor taskForceExecutor,
            IUnitOrderService orders)
        {
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _entityLocator = entityLocator;
            _gameModel = gameModel;
            _decisionModel = decisionModel;
            _contextBuilder = contextBuilder;
            _taskForceExecutor = taskForceExecutor;
            _orders = orders;
        }

        public event Action<EnemyStrategicDecision> DecisionChanged;

        public string Id => nameof(EnemyUnitCommander);
        public EnemyStrategicDecision LastDecision { get; private set; }
        public EnemyStrategicSnapshot LastSnapshot { get; private set; }
        public EnemyStrategicState CurrentState =>
            _hasDecision ? LastDecision.State : EnemyStrategicState.RebuildFleet;
        public int ActiveShipCount => LastSnapshot.OwnShipCount;

        public void Initialize()
        {
            _shipService.ShipAdded += HandleShipChanged;
            _shipService.ShipRemoved += HandleShipRemoved;
            _reinforcementZonesSystem.OwnershipChanged += HandleWorldChanged;
            _entityLocator.EntityAdded += HandleEntityChanged;
            _entityLocator.EntityRemoved += HandleEntityChanged;
            EvaluateAndExecute();
        }

        public void Tick()
        {
            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer <= 0f)
            {
                EvaluateAndExecute();
            }
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

        private void HandleShipChanged(IShipEntity ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            EvaluateAndExecute();
        }

        private void HandleShipRemoved(IShipEntity ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            _zoneExitTargets.Remove(ship);
            EvaluateAndExecute();
        }

        private void HandleEntityChanged(GameEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            EvaluateAndExecute();
        }

        private void HandleWorldChanged()
        {
            EvaluateAndExecute();
        }

        private void EvaluateAndExecute()
        {
            _decisionTimer = EnemyAiDifficultyProfile
                .Get(_gameModel.EnemyDifficulty)
                .DecisionInterval;
            EnemyStrategicContext context = _contextBuilder.Build();
            LastSnapshot = context.Snapshot;
            EnemyStrategicDecision decision = _decisionModel.Evaluate(context.Snapshot);
            if (decision.State == EnemyStrategicState.RebuildFleet ||
                decision.State == EnemyStrategicState.Hold)
            {
                MoveShipsOutOfDefaultZone(context.Ships);
            }
            else
            {
                _zoneExitTargets.Clear();
                _taskForceExecutor.Execute(decision, context);
            }
            PublishDecision(decision);
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
                    _orders.IssueMove(new[] { _entityLocator.GetEntity(ship.EntityId) },
                        new[] { target });
                    continue;
                }

                if (!_reinforcementZonesSystem.TryGetDefaultZoneExitPosition(
                        PlayerType.Opponent,
                        ship.WorldPosition,
                        ship.NavigationRadius,
                        out _))
                {
                    _zoneExitTargets.Remove(ship);
                    if (ship.CurrentOrder != UnitOrderType.None)
                        _orders.IssueStop(new[] { _entityLocator.GetEntity(ship.EntityId) });
                    continue;
                }

                unassignedShips.Add(ship);
                positions.Add(new FormationPoint(ship.WorldPosition.x, ship.WorldPosition.z));
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
                    PlayerType.Opponent, unassignedShips[0].WorldPosition,
                    formationClearance, out Vector3 exitPosition))
            {
                throw new InvalidOperationException("An exiting fleet requires its default zone.");
            }

            List<FormationPoint> destinations = new List<FormationPoint>();
            FormationModel.CalculateCompactDestinations(positions, radii,
                new FormationPoint(exitPosition.x, exitPosition.z), destinations);
            for (int i = 0; i < unassignedShips.Count; i++)
            {
                Vector3 target = new Vector3(destinations[i].X, 0f, destinations[i].Z);
                _zoneExitTargets.Add(unassignedShips[i], target);
                _orders.IssueMove(new[] {
                    _entityLocator.GetEntity(unassignedShips[i].EntityId) },
                    new[] { target });
            }
        }

        private void PublishDecision(EnemyStrategicDecision decision)
        {
            bool hasChanged = !_hasDecision ||
                LastDecision.State != decision.State ||
                LastDecision.CommittedShipCount != decision.CommittedShipCount ||
                LastDecision.Reason != decision.Reason;
            LastDecision = decision;
            _hasDecision = true;
            if (!hasChanged)
            {
                return;
            }

            Debug.Log(
                $"[EnemyAI] Difficulty={_gameModel.EnemyDifficulty}, " +
                $"Objective={_gameModel.VictoryCondition}, State={decision.State}, " +
                $"Committed={decision.CommittedShipCount}/{LastSnapshot.OwnShipCount}, " +
                $"EnemyShips={LastSnapshot.EnemyShipCount}, " +
                $"BaseThreats={LastSnapshot.EnemyShipsNearOwnBase}, " +
                $"ControlledZones={LastSnapshot.OwnedCapturableZoneCount}, " +
                $"Reason={decision.Reason}");
            DecisionChanged?.Invoke(decision);
        }
    }
}
