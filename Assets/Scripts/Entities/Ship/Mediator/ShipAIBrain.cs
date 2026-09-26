using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    /// <summary>Decides whether an AI ship should flee. It never changes ship state itself.</summary>
    public class ShipAIBrain
    {
        private readonly IHealthModelObserver _healthModel;
        private readonly IRadarComponent _radar;
        private readonly IShipMovement _movement;
        private readonly ShipAiDecisionModel _decisionModel;
        private readonly IGameModelObserver _gameModel;
        private readonly UnitOrderModel _orders;
        private float _decisionTimer;
        private bool _isEnabled;

        public bool IsFleeing { get; private set; }

        public ShipAIBrain(IHealthModelObserver healthModel, IRadarComponent radar,
            IShipMovement movement, ShipAiDecisionModel decisionModel,
            IGameModelObserver gameModel, UnitOrderModel orders)
        {
            _healthModel = healthModel;
            _radar = radar;
            _movement = movement;
            _decisionModel = decisionModel;
            _gameModel = gameModel;
            _orders = orders;
        }

        public void Enable(bool isEnabled)
        {
            _isEnabled = isEnabled;
            if (isEnabled) _decisionTimer = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!_isEnabled) return;
            _decisionTimer -= deltaTime;
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
            IsFleeing = decision == ShipAiDecision.Flee;
        }
    }
}
