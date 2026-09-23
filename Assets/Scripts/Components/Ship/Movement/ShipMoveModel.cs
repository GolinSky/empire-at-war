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
        private NumericsVector3? _requestedDestination;
        private NumericsVector3? _acceptedDestination;
        private NumericsVector3? _queuedDestination;
        private NumericsVector3? _deferredDestination;
        private NumericsVector3? _blockedDestination;

        public event Action<MovementPhase> PhaseChanged;
        public event Action<NumericsVector3> DestinationAccepted;
        public event Action Stopped;

        public float Speed => _shipMoveData.Speed * _speedCoefficient;
        public float Height => _shipMoveData.Height;
        public float RotationSpeed => _shipMoveData.RotationSpeed;
        public float TurnAcceleration => _shipMoveData.TurnAcceleration;
        public float HyperSpaceDuration => _shipMoveData.HyperSpaceDuration;
        public float BodyRotationMaxAngle => _shipMoveData.BodyRotationMaxAngle;
        public float NavigationRadius => _shipMoveData.NavigationRadius;
        public NumericsVector3 TargetPosition => _targetPosition;
        public MovementPhase Phase { get; private set; } = MovementPhase.Arriving;
        public bool IsHyperSpaceComplete { get; private set; }
        public NumericsVector3? RequestedDestination => _requestedDestination;
        public NumericsVector3? AcceptedDestination => _acceptedDestination;
        public NumericsVector3? QueuedDestination => _queuedDestination;
        public NumericsVector3? DeferredDestination => _deferredDestination;
        public NumericsVector3? BlockedDestination => _blockedDestination;
        public bool IsBlocked => _blockedDestination.HasValue;
        public bool IsNavigating => Phase == MovementPhase.Turning ||
            Phase == MovementPhase.Moving;
        public NumericsVector3 JumpPosition { get; private set; }
        public NumericsQuaternion StartRotation { get; private set; } = NumericsQuaternion.Identity;
        public NumericsVector3 HyperSpacePosition { get; private set; }

        public ShipMoveModel(IShipMoveData shipMoveData)
        {
            _shipMoveData = shipMoveData;
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

        public bool IsSameRequest(NumericsVector3 destination)
        {
            return _requestedDestination.HasValue &&
                PositionsEqual(_requestedDestination.Value, destination);
        }

        public void RequestDestination(NumericsVector3 destination)
        {
            _requestedDestination = destination;
            if (!IsHyperSpaceComplete)
            {
                _queuedDestination = destination;
            }
        }

        public void AcceptDestination(NumericsVector3 destination, bool turning)
        {
            _acceptedDestination = destination;
            _blockedDestination = null;
            _deferredDestination = null;
            SetTargetPosition(destination);
            SetPhase(turning ? MovementPhase.Turning : MovementPhase.Moving);
            DestinationAccepted?.Invoke(destination);
        }

        public void QueueDestination(NumericsVector3 destination)
        {
            _queuedDestination = destination;
            _acceptedDestination = destination;
            SetTargetPosition(destination);
        }

        public void ReserveDestination(NumericsVector3 destination)
        {
            _acceptedDestination = destination;
        }

        public NumericsVector3? FinishArrival()
        {
            NumericsVector3? queued = _queuedDestination;
            _queuedDestination = null;
            IsHyperSpaceComplete = true;
            SetPhase(MovementPhase.Idle);
            return queued;
        }

        public void DeferDestination(NumericsVector3 destination)
        {
            _deferredDestination = destination;
        }

        public NumericsVector3? TakeDeferredDestination()
        {
            NumericsVector3? deferred = _deferredDestination;
            _deferredDestination = null;
            return deferred;
        }

        public void BlockDestination(NumericsVector3 destination)
        {
            _blockedDestination = destination;
            _deferredDestination = null;
            if (IsHyperSpaceComplete)
            {
                SetPhase(MovementPhase.Blocked);
            }
        }

        public NumericsVector3? TakeBlockedDestination()
        {
            NumericsVector3? blocked = _blockedDestination;
            _blockedDestination = null;
            return blocked;
        }

        public void ClearPendingDestinations()
        {
            _queuedDestination = null;
            _deferredDestination = null;
            _blockedDestination = null;
        }

        public void StartMoving()
        {
            SetPhase(MovementPhase.Moving);
        }

        public void Arrive(NumericsVector3 position)
        {
            SetTargetPosition(position);
            _acceptedDestination = position;
            SetPhase(MovementPhase.Idle);
        }

        public void StopAt(NumericsVector3 position)
        {
            _queuedDestination = null;
            _deferredDestination = null;
            _blockedDestination = null;
            _requestedDestination = null;
            Arrive(position);
            Stopped?.Invoke();
        }

        private void SetPhase(MovementPhase phase)
        {
            if (Phase == phase)
            {
                return;
            }

            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private static bool PositionsEqual(NumericsVector3 first, NumericsVector3 second)
        {
            return Math.Abs(first.X - second.X) < POSITION_TOLERANCE &&
                Math.Abs(first.Y - second.Y) < POSITION_TOLERANCE &&
                Math.Abs(first.Z - second.Z) < POSITION_TOLERANCE;
        }
    }
}
