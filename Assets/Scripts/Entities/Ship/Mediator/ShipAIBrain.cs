using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.Orders;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public class ShipAIBrain : ITickable
    {
        private readonly StateMachine1 _stateMachine;
        private readonly IHealthModelObserver _healthModel;
        private readonly IRadarComponent _radar;
        private readonly IShipMoveComponent _movement;
        private readonly FleeState _fleeState;
        private readonly ShipAiDecisionModel _decisionModel;
        private readonly IGameModelObserver _gameModel;
        private readonly ShipOrderModel _orders;
        private readonly LazyInject<EmpireAtWar.Ship.Ship> _ship;
        private float _decisionTimer;
        private bool _isEnabled;

        public bool IsFleeing => _stateMachine.CurrentState == _fleeState;

        public ShipAIBrain(StateMachine1 stateMachine,
            IHealthModelObserver healthModel, IRadarComponent radar,
            IShipMoveComponent movement, FleeState fleeState,
            ShipAiDecisionModel decisionModel, IGameModelObserver gameModel,
            ShipOrderModel orders, LazyInject<EmpireAtWar.Ship.Ship> ship)
        {
            _stateMachine = stateMachine;
            _healthModel = healthModel;
            _radar = radar;
            _movement = movement;
            _fleeState = fleeState;
            _decisionModel = decisionModel;
            _gameModel = gameModel;
            _orders = orders;
            _ship = ship;
        }

        public void Enable(bool isEnabled)
        {
            _isEnabled = isEnabled;
            if (isEnabled) _decisionTimer = 0f;
        }

        public void Tick()
        {
            if (!_isEnabled) return;
            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0f) return;
            _decisionTimer = EnemyAiDifficultyProfile.Get(
                _gameModel.EnemyDifficulty).DecisionInterval;

            bool hasTarget = _orders.Target != null;
            bool targetAvailable = hasTarget &&
                !_orders.Target.HealthModel.IsDestroyed &&
                _orders.Target.HealthModel.HasUnits;
            ShipAiDecision decision = _decisionModel.Evaluate(new ShipAiSnapshot(
                _healthModel.IsDestroyed, _healthModel.HasShields,
                _healthModel.ShieldPercentage, _radar.Enemies.Count,
                hasTarget, targetAvailable, _movement.IsMoving),
                _gameModel.EnemyDifficulty);
            if (decision == ShipAiDecision.Flee)
            {
                if (!IsFleeing) _stateMachine.SetState(_fleeState);
            }
            else if (IsFleeing)
            {
                _ship.Value.ResumeOrder();
            }
        }
    }
}
