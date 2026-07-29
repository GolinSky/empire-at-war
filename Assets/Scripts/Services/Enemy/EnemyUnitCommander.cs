using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;

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

        private float _decisionTimer;
        private bool _hasDecision;

        public EnemyUnitCommander(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel,
            EnemyStrategicDecisionModel decisionModel,
            EnemyStrategicContextBuilder contextBuilder,
            EnemyTaskForceExecutor taskForceExecutor)
        {
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _reinforcementZonesSystem = reinforcementZonesSystem ??
                throw new ArgumentNullException(nameof(reinforcementZonesSystem));
            _entityLocator = entityLocator ?? throw new ArgumentNullException(nameof(entityLocator));
            _gameModel = gameModel ?? throw new ArgumentNullException(nameof(gameModel));
            _decisionModel = decisionModel ?? throw new ArgumentNullException(nameof(decisionModel));
            _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
            _taskForceExecutor = taskForceExecutor ??
                throw new ArgumentNullException(nameof(taskForceExecutor));
        }

        public event Action<EnemyStrategicDecision> DecisionChanged;

        public string Id => nameof(EnemyUnitCommander);
        public EnemyStrategicDecision LastDecision { get; private set; }
        public EnemyStrategicSnapshot LastSnapshot { get; private set; }
        public EnemyStrategicState CurrentState =>
            _hasDecision ? LastDecision.State : EnemyStrategicState.RebuildFleet;

        public void Initialize()
        {
            _shipService.ShipAdded += HandleShipChanged;
            _shipService.ShipRemoved += HandleShipChanged;
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
            _shipService.ShipRemoved -= HandleShipChanged;
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
            _taskForceExecutor.Execute(decision, context);
            PublishDecision(decision);
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
                $"EnemyShips={LastSnapshot.EnemyShipCount}, Reason={decision.Reason}");
            DecisionChanged?.Invoke(decision);
        }
    }
}
