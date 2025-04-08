using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipIdleState:UnitIdleState
    {
        private const float MOVE_AROUND_DURATION = 10f;
        private readonly ITimer _moveAroundTimer;

        protected readonly IModel _model;
        protected readonly IComponentHub _componentHub;
        protected readonly IWeaponComponent _weaponComponent;
        protected readonly IShipMoveComponent _shipMoveComponent;
        protected readonly IHealthModelObserver _healthModelObserver;
        public new ShipStateMachine StateMachine { get; }
        
        
        public ShipIdleState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            _model = stateMachine.Model;
            _componentHub = stateMachine.ComponentHub;
            _weaponComponent = stateMachine.WeaponComponent;
            _shipMoveComponent = stateMachine.ShipMoveComponent;
            _healthModelObserver = _model.GetModelObserver<IHealthModelObserver>();
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