using System;
using EmpireAtWar.Mvc;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Components.Ship.Movement
{
    [Serializable]
    public class ShipMoveModel : PureModel
    {
        private const float OFFSET_HYPERSPACE_JUMP = 1000f;
        private const float POSITION_TOLERANCE = 0.05f;

        private readonly IShipMoveData _shipMoveData;
        private float _speedCoefficient = 1f;
        private bool _hasTargetPosition;
        private NumericsVector3 _targetPosition;

        public float Speed => _shipMoveData.Speed * _speedCoefficient;
        public float Height => _shipMoveData.Height;
        public float RotationSpeed => _shipMoveData.RotationSpeed;
        public float HyperSpaceDuration => _shipMoveData.HyperSpaceDuration;
        public float BodyRotationMaxAngle => _shipMoveData.BodyRotationMaxAngle;
        public float NavigationRadius => _shipMoveData.NavigationRadius;
        public NumericsVector3 TargetPosition => _targetPosition;
        public NumericsVector3 JumpPosition { get; private set; }
        public NumericsQuaternion StartRotation { get; private set; } = NumericsQuaternion.Identity;
        public NumericsVector3 HyperSpacePosition { get; private set; }

        public ShipMoveModel(IShipMoveData shipMoveData)
        {
            _shipMoveData = shipMoveData ?? throw new ArgumentNullException(nameof(shipMoveData));
        }

        public void ConfigureSpawnPose(
            NumericsVector3 hyperSpacePosition,
            NumericsQuaternion startRotation,
            bool useHyperSpaceEntry)
        {
            HyperSpacePosition = hyperSpacePosition;
            StartRotation = startRotation;
            JumpPosition = useHyperSpaceEntry
                ? HyperSpacePosition -
                  NumericsVector3.Transform(NumericsVector3.UnitZ, StartRotation) * OFFSET_HYPERSPACE_JUMP
                : HyperSpacePosition;
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            _speedCoefficient = coefficient;
        }

        public bool IsMoving(NumericsVector3 currentPosition)
        {
            return _hasTargetPosition && !PositionsEqual(currentPosition, _targetPosition);
        }

        public bool HasTargetPosition(NumericsVector3 targetPosition)
        {
            return _hasTargetPosition && PositionsEqual(_targetPosition, targetPosition);
        }

        public void SetTargetPosition(NumericsVector3 targetPosition)
        {
            _targetPosition = targetPosition;
            _hasTargetPosition = true;
        }

        private static bool PositionsEqual(NumericsVector3 first, NumericsVector3 second)
        {
            return Math.Abs(first.X - second.X) < POSITION_TOLERANCE &&
                Math.Abs(first.Y - second.Y) < POSITION_TOLERANCE &&
                Math.Abs(first.Z - second.Z) < POSITION_TOLERANCE;
        }
    }
}
