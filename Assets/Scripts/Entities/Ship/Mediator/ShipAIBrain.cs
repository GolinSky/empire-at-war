using System;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public class ShipAIBrain : ITickable
    {
        private readonly StateMachine1 _stateMachine;
        private readonly IHealthModelObserver _healthModel;
        private readonly IRadarComponent _radarComponent;
        private readonly IShipMoveComponent _shipMoveComponent;
        private readonly AttackTargetState _attackTargetState;
        private readonly IdleState _idleState;
        private readonly FleeState _fleeState;
        private readonly ShipAiDecisionModel _decisionModel;
        private readonly IGameModelObserver _gameModel;

        private float _decisionTimer = 0f;
        private bool _isEnabled = false;
        private IEntity _assignedTarget;
        private Vector3 _attackFormationOffset;

        public ShipAIBrain(
            StateMachine1 stateMachine,
            IHealthModelObserver healthModel,
            IRadarComponent radarComponent,
            IShipMoveComponent shipMoveComponent,
            AttackTargetState attackTargetState,
            IdleState idleState,
            FleeState fleeState,
            ShipAiDecisionModel decisionModel,
            IGameModelObserver gameModel)
        {
            _stateMachine = stateMachine;
            _healthModel = healthModel ?? throw new ArgumentNullException(nameof(healthModel));
            _radarComponent = radarComponent;
            _shipMoveComponent = shipMoveComponent;
            _attackTargetState = attackTargetState;
            _idleState = idleState;
            _fleeState = fleeState;
            _decisionModel = decisionModel;
            _gameModel = gameModel;
        }

        public void Enable(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public void AssignAttackTarget(IEntity target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (_assignedTarget != null && _assignedTarget.Id == target.Id)
            {
                return;
            }

            AssignAttackTarget(target, Vector3.zero);
        }

        public void AssignAttackTarget(
            IEntity target,
            Vector3 formationOffset)
        {
            _assignedTarget = target ??
                throw new ArgumentNullException(nameof(target));
            formationOffset.y = 0f;
            _attackFormationOffset = formationOffset;
        }

        public void Tick()
        {
            if (!_isEnabled) return;

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0) return;
            _decisionTimer = EnemyAiDifficultyProfile.Get(_gameModel.EnemyDifficulty).DecisionInterval;

            MakeDecision();
        }

        private void MakeDecision()
        {
            for (int i = _radarComponent.Enemies.Count - 1; i >= 0; i--)
            {
                var radarEnemyHealth = _radarComponent.Enemies[i].HealthModel;
                if (radarEnemyHealth.IsDestroyed)
                {
                    _radarComponent.Enemies.RemoveAt(i);
                }
            }

            bool hasAssignedTarget = _assignedTarget != null;
            bool isAssignedTargetAvailable = hasAssignedTarget &&
                !_assignedTarget.HealthModel.IsDestroyed &&
                _assignedTarget.HealthModel.HasUnits;
            ShipAiDecision decision = _decisionModel.Evaluate(
                new ShipAiSnapshot(
                    _healthModel.IsDestroyed,
                    _healthModel.HasShields,
                    _healthModel.ShieldPercentage,
                    _radarComponent.Enemies.Count,
                    hasAssignedTarget,
                    isAssignedTargetAvailable,
                    _shipMoveComponent.IsMoving),
                _gameModel.EnemyDifficulty);

            if (hasAssignedTarget && !isAssignedTargetAvailable)
            {
                _assignedTarget = null;
                _attackFormationOffset = Vector3.zero;
            }

            switch (decision)
            {
                case ShipAiDecision.Flee:
                    SetState(_fleeState);
                    return;
                case ShipAiDecision.Attack:
                    if (!_attackTargetState.IsTheSameTarget(
                            _assignedTarget,
                            _attackFormationOffset))
                    {
                        _attackTargetState.SetData(
                            _assignedTarget,
                            _attackFormationOffset);
                    }

                    SetState(_attackTargetState);
                    return;
                case ShipAiDecision.Navigate:
                    return;
                case ShipAiDecision.Idle:
                    SetState(_idleState);
                    return;
            }
        }

        private void SetState(IBaseState state)
        {
            if (_stateMachine.CurrentState != state)
            {
                _stateMachine.SetState(state);
            }
        }
    }
}
