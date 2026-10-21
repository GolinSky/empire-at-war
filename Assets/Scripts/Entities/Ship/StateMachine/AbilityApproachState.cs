using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Patterns.StateMachine;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    /// <summary>
    /// Flies toward an ability target until it is within the ability's range.
    /// </summary>
    public sealed class AbilityApproachState : IBaseState
    {
        private const float RETRY_INTERVAL = 0.5f;
        private const int MAX_STALLED_ATTEMPTS = 3;
        private const float STANDOFF_RANGE_FACTOR = 0.9f;

        private readonly IShipMovement _movement;
        private IEntity _target;
        private Transform _targetTransform;
        private Vector3 _lastAttemptPosition;
        private float _range;
        private float _retryTimer;
        private int _stalledAttempts;

        private Vector3 TargetPosition => _targetTransform.position;
        public bool IsInRange => PlanarGeometry.Distance(_movement.CurrentPosition, TargetPosition) <= _range;
        public bool IsComplete => _target.HealthModel.IsDestroyed || _target.IsCloaked() || IsInRange ||
                                  _stalledAttempts >= MAX_STALLED_ATTEMPTS;

        public AbilityApproachState(IShipMovement movement)
        {
            _movement = movement;
        }

        public void SetData(IEntity target, float range)
        {
            _target = target;
            _targetTransform = target.GetFacade<IEntityTransformFacade>().Transform;
            _range = range;
        }

        public void Enter()
        {
            _stalledAttempts = 0;
            _retryTimer = 0f;
            if (IsComplete) return;
            MoveToTarget();
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete) return;
            _retryTimer -= deltaTime;
            if (_movement.IsMoving || _retryTimer > 0f) return;
            if (PlanarGeometry.Distance(_movement.CurrentPosition, _lastAttemptPosition) < 0.1f)
                _stalledAttempts++;
            else
                _stalledAttempts = 0;
            if (!IsComplete) MoveToTarget();
        }

        public void Exit()
        {
        }

        private void MoveToTarget()
        {
            _lastAttemptPosition = _movement.CurrentPosition;
            _retryTimer = RETRY_INTERVAL;
            Vector3 fromTarget = _movement.CurrentPosition - TargetPosition;
            fromTarget.y = 0f;
            float distance = Mathf.Max(_range * STANDOFF_RANGE_FACTOR,
                ShipEngagement.ContactDistance(_movement, _target));
            _movement.MoveToPosition(TargetPosition + fromTarget.normalized * distance, preserveCourse: true);
        }
    }
}
