﻿using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Health;
using LightWeightFramework.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Patterns.StateMachine
{
    public class UnitIdleState:BaseState
    {
        protected readonly IModel _model;
        protected readonly IAttackComponent _attackComponent;
        protected readonly IRadarModelObserver _radarModelObserver;
        protected readonly IHealthModelObserver _healthModelObserver;
        public new UnitStateMachine StateMachine { get; }
        
        public UnitIdleState(UnitStateMachine stateMachine) : base(stateMachine)
        {
            StateMachine = stateMachine;
            _model = stateMachine.Model;
            _attackComponent = stateMachine.AttackComponent;
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
                _attackComponent.AddTarget(attackData, AttackType.Base);
            }
        }
    }
}