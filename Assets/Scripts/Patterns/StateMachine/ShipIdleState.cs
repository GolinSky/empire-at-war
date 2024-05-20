using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class ShipIdleState:BaseState
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
            radarModelObserver.OnHitDetected += HandleEnemy;
            healthModelObserver.OnValueChanged += HandleHealth;
        }

        public override void Exit()
        {
            base.Exit();
            radarModelObserver.OnHitDetected -= HandleEnemy;
            healthModelObserver.OnValueChanged -= HandleHealth;
        }
        
        private void HandleEnemy(RaycastHit[] raycastHit)
        {
            List<AttackData> healthComponents = new List<AttackData>();
            foreach (RaycastHit hit in raycastHit)
            {
                IShipUnitsProvider unitsProvider = hit.collider.GetComponentInChildren<IShipUnitsProvider>();
                if (unitsProvider != null && unitsProvider.HasUnits)
                {
                    healthComponents.Add(new AttackData(unitsProvider, componentHub.GetComponent(unitsProvider.ModelObserver), ShipUnitType.Any));
                }
            }

            if (healthComponents.Count != 0)
            {
                weaponComponent.AddTargets(healthComponents.ToArray());
            }
        }
        
        protected virtual void HandleHealth()
        {
            //add condition - if under attack
            if (moveAroundTimer.IsComplete && healthModelObserver.ShieldPercentage < 0.5f)
            {
                moveAroundTimer.ChangeDelay(shipMoveComponent.MoveAround()); 
                moveAroundTimer.StartTimer();
            }
        }
    }
}