using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipIdleState:UnitIdleState
    {
        private const float MOVE_AROUND_DURATION = 10f;
        private readonly ITimer _moveAroundTimer;

        protected readonly IAttackComponent _attackComponent;
        protected readonly IShipMoveComponent _shipMoveComponent;
        protected readonly IHealthModelObserver _healthModelObserver;
        public new ShipStateMachine StateMachine { get; }
        
        
        public ShipIdleState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            _attackComponent = stateMachine.AttackComponent;
            _shipMoveComponent = stateMachine.ShipMoveComponent;
            _healthModelObserver = stateMachine.HealthModel;
            StateMachine = stateMachine;
            _moveAroundTimer = TimerFactory.ConstructTimer(MOVE_AROUND_DURATION);
        }

        public override void Enter()
        {
            base.Enter();
            _healthModelObserver.OnValueChanged += HandleHealth;
        }

        public override void Exit()
        {
            base.Exit();
            _healthModelObserver.OnValueChanged -= HandleHealth;
        }
        
        protected virtual void HandleHealth()
        {
            //add condition - if under attack
            float randomRange = Random.Range(_healthModelObserver.ShieldDangerStateRange.Min,
                _healthModelObserver.ShieldDangerStateRange.Max);
            if (_moveAroundTimer.IsComplete && _healthModelObserver.ShieldPercentage < randomRange)
            {
                _moveAroundTimer.ChangeDelay(_shipMoveComponent.MoveAround()); 
                _moveAroundTimer.StartTimer();
            }
        }
    }
}
