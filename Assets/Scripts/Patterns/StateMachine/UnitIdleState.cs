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
        protected readonly IModel _model;
        protected readonly IComponentHub _componentHub;
        protected readonly IWeaponComponent _weaponComponent;
        protected readonly IRadarModelObserver _radarModelObserver;
        protected readonly IHealthModelObserver _healthModelObserver;
        public new UnitStateMachine StateMachine { get; }
        
        public UnitIdleState(UnitStateMachine stateMachine) : base(stateMachine)
        {
            StateMachine = stateMachine;
            _model = stateMachine.Model;
            _componentHub = stateMachine.ComponentHub;
            _weaponComponent = stateMachine.WeaponComponent;
            _radarModelObserver = _model.GetModelObserver<IRadarModelObserver>();
            _healthModelObserver = _model.GetModelObserver<IHealthModelObserver>();
        }
        
        public override void Enter()
        {
            base.Enter();
            _radarModelObserver.OnHitDetected += HandleEnemy;
        }

        public override void Exit()
        {
            base.Exit();
            _radarModelObserver.OnHitDetected -= HandleEnemy;
        }
        
        private void HandleEnemy(RaycastHit[] raycastHit)
        {
            List<AttackData> healthComponents = new List<AttackData>();
            foreach (RaycastHit hit in raycastHit)
            {
                IHardPointsProvider unitsProvider = hit.collider.GetComponentInChildren<IHardPointsProvider>();
                if (unitsProvider != null && unitsProvider.HasUnits)
                {
                    healthComponents.Add(new AttackData(unitsProvider, _componentHub.GetComponent(unitsProvider.ModelObserver), HardPointType.Any));
                }
            }

            if (healthComponents.Count != 0)
            {
                _weaponComponent.AddTargets(healthComponents.ToArray());
            }
        }
    }
}