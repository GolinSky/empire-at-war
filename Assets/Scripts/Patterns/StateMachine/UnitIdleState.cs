using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.WeaponComponent;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Radar;
using EmpireAtWar.Services.ComponentHub;
using EmpireAtWar.ViewComponents.Health;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class UnitIdleState:BaseState
    {
        protected readonly IModel _model;
        protected readonly IWeaponComponent _weaponComponent;
        protected readonly IRadarModelObserver _radarModelObserver;
        protected readonly IHealthModelObserver _healthModelObserver;
        public new UnitStateMachine StateMachine { get; }
        
        public UnitIdleState(UnitStateMachine stateMachine) : base(stateMachine)
        {
            StateMachine = stateMachine;
            _model = stateMachine.Model;
            _weaponComponent = stateMachine.WeaponComponent;
            _radarModelObserver = _model.GetModelObserver<IRadarModelObserver>();
            _healthModelObserver = _model.GetModelObserver<IHealthModelObserver>();
        }
        
        public override void Enter()
        {
            base.Enter();
            _radarModelObserver.Enemies.ItemAdded += HandleNewEnemy;
        }

        public override void Exit()
        {
            base.Exit();
            _radarModelObserver.Enemies.ItemAdded += HandleNewEnemy;
        }
        
        private void HandleNewEnemy(ObservableList<IEntity> sender, ListChangedEventArgs<IEntity> e)
        {
            IEntity newEntity = e.item;

            var healthModel = newEntity.Model.GetModelObserver<IHealthModelObserver>();
            if (healthModel.HasUnits && newEntity.TryGetCommand(out IHealthCommand healthCommand))
            {
                AttackData attackData = new AttackData(healthModel, healthCommand, HardPointType.Any);
                _weaponComponent.AddTarget(attackData, AttackType.Base);
            }
        }
        
        private void HandleEnemy(RaycastHit[] raycastHit)
        {
            // List<AttackData> healthComponents = new List<AttackData>();
            // foreach (RaycastHit hit in raycastHit)
            // {
            //     IHardPointsProvider unitsProvider = hit.collider.GetComponentInChildren<IHardPointsProvider>();
            //     if (unitsProvider != null && unitsProvider.HasUnits)
            //     {
            //         healthComponents.Add(new AttackData(unitsProvider, _componentHub.GetComponent(unitsProvider.ModelObserver), HardPointType.Any));
            //     }
            // }
            //
            // if (healthComponents.Count != 0)
            // {
            //     _weaponComponent.AddTargets(healthComponents.ToArray());
            // }
        }
    }
}