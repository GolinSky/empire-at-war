using System;
using EmpireAtWar.Extentions;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Zenject;

namespace EmpireAtWar.Components.Ship.Movement
{
    public interface IShipMoveData
    {
        float Speed { get; }
        float Height { get; }
        float RotationSpeed { get; }
        float HyperSpaceDuration { get; }
        float BodyRotationMaxAngle { get; }
        float NavigationRadius { get; }
    }

    public interface IShipMoveModelObserver : IModelObserver
    {
        Vector3 CurrentPosition { get; }
        bool IsMoving { get; }
        Vector3 HyperSpacePosition { get; }
        float RotationSpeed { get; }
        float HyperSpaceDuration { get; }
        float BodyRotationMaxAngle { get; }
        float NavigationRadius { get; }
        Vector3 JumpPosition { get; }
        Quaternion StartRotation { get; }
    }

    [Serializable]
    public class ShipMoveModel : PureModel, IShipMoveModelObserver
    {
        private const float OFFSET_HYPERSPACE_JUMP = 1000f;

        private float _speedCoefficient = 1f;
        private bool _hasTargetPosition;
        private Vector3 _targetPosition;

        [Inject] private IShipMoveData ShipMoveData { get; }
        [Inject(Id = EntityBindType.ViewTransform)]
        public LazyInject<Transform> ViewTransform { get; }

        public Vector3 CurrentPosition => ViewTransform.Value.position;
        public float Speed => ShipMoveData.Speed * _speedCoefficient;
        public float Height => ShipMoveData.Height;
        public float RotationSpeed => ShipMoveData.RotationSpeed;
        public float HyperSpaceDuration => ShipMoveData.HyperSpaceDuration;
        public float BodyRotationMaxAngle => ShipMoveData.BodyRotationMaxAngle;
        public float NavigationRadius => ShipMoveData.NavigationRadius;
        public Vector3 TargetPosition => _targetPosition;
        public bool IsMoving =>
            _hasTargetPosition && !CurrentPosition.IsEqual(_targetPosition);
        public Vector3 JumpPosition { get; private set; }
        public Quaternion StartRotation { get; private set; } = Quaternion.identity;
        public Vector3 HyperSpacePosition { get; private set; }

        public void ConfigureSpawnPose(
            Vector3 hyperSpacePosition,
            Quaternion startRotation,
            bool useHyperSpaceEntry)
        {
            HyperSpacePosition = hyperSpacePosition;
            StartRotation = startRotation;
            JumpPosition = useHyperSpaceEntry
                ? HyperSpacePosition -
                  StartRotation * Vector3.forward * OFFSET_HYPERSPACE_JUMP
                : HyperSpacePosition;
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            _speedCoefficient = coefficient;
        }

        public bool HasTargetPosition(Vector3 targetPosition)
        {
            return _hasTargetPosition && _targetPosition.IsEqual(targetPosition);
        }

        public void SetTargetPosition(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            _hasTargetPosition = true;
        }
    }
}
