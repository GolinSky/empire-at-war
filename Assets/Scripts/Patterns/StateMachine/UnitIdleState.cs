using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class UnitIdleState:BaseState
    {
        protected readonly IModel model;
        protected readonly IComponentHub componentHub;
        protected readonly IWeaponComponent weaponComponent;
        protected readonly IRadarModelObserver radarModelObserver;
        protected readonly IHealthModelObserver healthModelObserver;
        public new UnitStateMachine StateMachine { get; }
        
        public UnitIdleState(UnitStateMachine stateMachine) : base(stateMachine)
        {
            StateMachine = stateMachine;
            model = stateMachine.Model;
            componentHub = stateMachine.ComponentHub;
            weaponComponent = stateMachine.WeaponComponent;
            radarModelObserver = model.GetModelObserver<IRadarModelObserver>();
            healthModelObserver = model.GetModelObserver<IHealthModelObserver>();
        }
        
        public override void Enter()
        {
            base.Enter();
            radarModelObserver.OnHitDetected += HandleEnemy;
        }

        public override void Exit()
        {
            base.Exit();
            radarModelObserver.OnHitDetected -= HandleEnemy;
        }
        
        private void HandleEnemy(RaycastHit[] raycastHit)
        {
            List<AttackData> healthComponents = new List<AttackData>();
            foreach (RaycastHit hit in raycastHit)
            {
                IHardPointsProvider unitsProvider = hit.collider.GetComponentInChildren<IHardPointsProvider>();
                if (unitsProvider != null && unitsProvider.HasUnits)
                {
                    healthComponents.Add(new AttackData(unitsProvider, componentHub.GetComponent(unitsProvider.ModelObserver), HardPointType.Any));
                }
            }

            if (healthComponents.Count != 0)
            {
                weaponComponent.AddTargets(healthComponents.ToArray());
            }
        }
    }
}