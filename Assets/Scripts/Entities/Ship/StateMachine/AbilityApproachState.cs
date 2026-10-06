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
        private const float DESTINATION_UPDATE_RANGE_FRACTION = 0.1f;

        private readonly IShipMovement _movement;

        private IEntity _target;
        private Transform _targetTransform;
        private Vector3 _destination;
        private float _range;

        private Vector3 TargetPosition => _targetTransform.position;
        private float DestinationUpdateDistance =>
            Mathf.Max(_movement.NavigationRadius, _range * DESTINATION_UPDATE_RANGE_FRACTION);

        public bool IsInRange => PlanarGeometry.Distance(_movement.CurrentPosition, TargetPosition) <= _range;

        public bool IsComplete => _target.HealthModel.IsDestroyed || _target.IsCloaked() || IsInRange;

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
            if (IsComplete) return;
            MoveToTarget();
        }

        public void Tick(float deltaTime)
        {
            if (IsComplete) return;
            float updateDistance = DestinationUpdateDistance;
            if ((TargetPosition - _destination).sqrMagnitude >= updateDistance * updateDistance) MoveToTarget();
        }

        public void Exit()
        {
        }

        private void MoveToTarget()
        {
            _destination = TargetPosition;
            _movement.MoveToPosition(_destination, preserveCourse: true);
        }
    }
}
