using System.Linq;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public class ShipAIBrain : ITickable
    {
        private const float DECISION_INTERVAL = 1f;

        private readonly StateMachine1 _stateMachine;
        private readonly IHealthModelObserver _healthModel;
        private readonly IRadarComponent _radarComponent;
        private readonly AttackTargetState _attackTargetState;
        private readonly IdleState _idleState;
        private readonly FleeState _fleeState;

        private float _decisionTimer = 0f;
        private bool _isEnabled = false;
        private IEntity _assignedTarget;

        public ShipAIBrain(
            StateMachine1 stateMachine,
            IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            AttackTargetState attackTargetState,
            IdleState idleState,
            FleeState fleeState)
        {
            _stateMachine = stateMachine;
            _healthModel = healthComponent.HealthModelObserver;
            _radarComponent = radarComponent;
            _attackTargetState = attackTargetState;
            _idleState = idleState;
            _fleeState = fleeState;
        }

        public void Enable(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public void AssignAttackTarget(IEntity target)
        {
            _assignedTarget = target;
        }

        public void Tick()
        {
            if (!_isEnabled) return;

            _decisionTimer -= Time.deltaTime;
            if (_decisionTimer > 0) return;
            _decisionTimer = DECISION_INTERVAL;

            MakeDecision();
        }

        private void MakeDecision()
        {
            if (_healthModel.IsDestroyed) return;

            // 1. Check HP status
            if (_healthModel.HasShields && _healthModel.ShieldPercentage < 0.2f)
            {
                SetState(_fleeState);
                return;
            }

            // Clean up radar enemies
            for (int i = _radarComponent.Enemies.Count - 1; i >= 0; i--)
            {
                var radarEnemyHealth = _radarComponent.Enemies[i].Model.GetModelObserver<IHealthModelObserver>();
                if (radarEnemyHealth.IsDestroyed)
                {
                    _radarComponent.Enemies.RemoveAt(i);
                }
            }

            var enemies = _radarComponent.Enemies.Where(e => !e.Model.GetModelObserver<IHealthModelObserver>().IsDestroyed).ToList();

            if (enemies.Count > 2)
            {
                SetState(_fleeState);
                return;
            }

            if (_assignedTarget == null)
            {
                SetState(_idleState);
                return;
            }

            IHealthModelObserver targetHealth = _assignedTarget.Model.GetModelObserver<IHealthModelObserver>();
            if (targetHealth.IsDestroyed || !targetHealth.HasUnits)
            {
                _assignedTarget = null;
                SetState(_idleState);
                return;
            }

            if (!_attackTargetState.IsTheSameTarget(_assignedTarget))
            {
                _attackTargetState.SetData(_assignedTarget);
            }

            SetState(_attackTargetState);
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
