using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.EnemyFaction.Models;
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
        private readonly UnitOrderModel _orders;

        private readonly EnemyAiDifficulty _difficulty;

        private float _decisionTimer;

        private bool _isEnabled;

        public bool IsFleeing { get; private set; }

        public ShipAIBrain(IHealthModelObserver healthModel, IRadarComponent radar,
            IShipMovement movement, IPlayerRoster roster,
            ShipAiDecisionModel decisionModel, UnitOrderModel orders, PlayerId owner)
        {
            _healthModel = healthModel;
            _radar = radar;
            _movement = movement;
            _decisionModel = decisionModel;
            _difficulty = roster.Get(owner).Difficulty;
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
                _difficulty).DecisionInterval;

            bool hasTarget = _orders.Target != null;
            bool targetAvailable = hasTarget &&
                !_orders.Target.HealthModel.IsDestroyed &&
                _orders.Target.HealthModel.HasUnits;
            ShipAiDecision decision = _decisionModel.Evaluate(new ShipAiSnapshot(
                isDestroyed: _healthModel.IsDestroyed, hasShields: _healthModel.HasShields,
                shieldPercentage: _healthModel.ShieldPercentage, nearbyEnemyCount: _radar.Enemies.Count,
                hasAssignedTarget: hasTarget, isAssignedTargetAvailable: targetAvailable, isMoving: _movement.IsMoving),
                _difficulty);
            IsFleeing = decision == ShipAiDecision.Flee;
        }
    }
}
