using System.Linq;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Ship.StateMachine;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Patterns.StateMachine;
using UnityEngine;
using Zenject;
using PatrolState = EmpireAtWar.Entities.Ship.StateMachine.PatrolState;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public class ShipAIBrain : ITickable
    {
        private readonly StateMachine1 _stateMachine;
        private readonly IHealthModelObserver _healthModel;
        private readonly IRadarComponent _radarComponent;
        private readonly AttackTargetState _attackTargetState;
        private readonly PatrolState _patrolState;
        private readonly FleeState _fleeState;
        private readonly IWeaponComponent _weaponComponent;
        private readonly PlayerType _playerType;

        private float _decisionTimer = 0f;
        private const float DECISION_INTERVAL = 1f;
        private bool _isEnabled = true;

        public ShipAIBrain(
            StateMachine1 stateMachine,
           IHealthComponent healthComponent,
            IRadarComponent radarComponent,
            AttackTargetState attackTargetState,
            PatrolState patrolState,
            FleeState fleeState,
            IWeaponComponent weaponComponent,
            PlayerType playerType)
        {
            _stateMachine = stateMachine;
            _healthModel = healthComponent.HealthModelObserver;
            _radarComponent = radarComponent;
            _attackTargetState = attackTargetState;
            _patrolState = patrolState;
            _fleeState = fleeState;
            _weaponComponent = weaponComponent;
            _playerType = playerType;
        }

        public void Enable(bool isEnabled)
        {
            _isEnabled = isEnabled;
        }

        public void Tick()
        {
            _stateMachine.Update();

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
                var targetHealth = _radarComponent.Enemies[i].Model.GetModelObserver<IHealthModelObserver>();
                if (targetHealth.IsDestroyed)
                {
                    _radarComponent.Enemies.RemoveAt(i);
                }
            }

            // 2. Enemy detection
            var enemies = _radarComponent.Enemies.Where(e => !e.Model.GetModelObserver<IHealthModelObserver>().IsDestroyed).ToList();

            if (enemies.Count > 2) // "a lot of enemies - run"
            {
                SetState(_fleeState);
                return;
            }

            if (enemies.Count > 0)
            {
                // "if one enemy or weak - attack and moving to enemy target if its runs away"
                var target = enemies.First();
                if (!_attackTargetState.IsTheSameTarget(target))
                {
                    _attackTargetState.SetData(target);
                }

                SetState(_attackTargetState);
                return;
            }

            // If no threats and no targets, Patrol
            SetState(_patrolState);
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
