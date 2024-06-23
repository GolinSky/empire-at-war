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
        private const float MoveAroundDuration = 10f;
        private readonly ITimer moveAroundTimer;

        protected readonly IModel model;
        protected readonly IComponentHub componentHub;
        protected readonly IWeaponComponent weaponComponent;
        protected readonly IShipMoveComponent shipMoveComponent;
        protected readonly IRadarModelObserver radarModelObserver;
        protected readonly IHealthModelObserver healthModelObserver;
        public new ShipStateMachine StateMachine { get; }
        
        
        public ShipIdleState(ShipStateMachine stateMachine) : base(stateMachine)
        {
            model = stateMachine.Model;
            componentHub = stateMachine.ComponentHub;
            weaponComponent = stateMachine.WeaponComponent;
            shipMoveComponent = stateMachine.ShipMoveComponent;
            radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
            healthModelObserver = model.GetModelObserver<IHealthModelObserver>();
            StateMachine = stateMachine;
            moveAroundTimer = TimerFactory.ConstructTimer(MoveAroundDuration);
        }

        public override void Enter()
        {
            base.Enter();
            healthModelObserver.OnValueChanged += HandleHealth;
        }

        public override void Exit()
        {
            base.Exit();
            healthModelObserver.OnValueChanged -= HandleHealth;
        }
        
        protected virtual void HandleHealth()
        {
            //add condition - if under attack
            float randomRange = Random.Range(healthModelObserver.ShieldDangerStateRange.Min,
                healthModelObserver.ShieldDangerStateRange.Max);
            if (moveAroundTimer.IsComplete && healthModelObserver.ShieldPercentage < randomRange)
            {
                moveAroundTimer.ChangeDelay(shipMoveComponent.MoveAround()); 
                moveAroundTimer.StartTimer();
            }
        }
    }
}