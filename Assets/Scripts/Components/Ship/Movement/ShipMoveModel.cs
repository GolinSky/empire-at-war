using System;
using EmpireAtWar.Components.Combat;
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
        private readonly CombatModifiers _modifiers;
        private float _speedCoefficient = 1f;

        public float Speed => _shipMoveData.Speed * _speedCoefficient * _modifiers.SpeedMultiplier;
        public float Height => _shipMoveData.Height;
        public float RotationSpeed => _shipMoveData.RotationSpeed;
        public float TurnAcceleration => _shipMoveData.TurnAcceleration;
        public float HyperSpaceDuration => _shipMoveData.HyperSpaceDuration;
        public float BodyRotationMaxAngle => _shipMoveData.BodyRotationMaxAngle;
        public float NavigationRadius => _shipMoveData.NavigationRadius;
        public MovementPhase Phase { get; private set; } = MovementPhase.Arriving;
        public NumericsVector3 Destination { get; private set; }
        public NumericsVector3? PendingDestination { get; private set; }
        public NumericsVector3? LastRequest { get; private set; }
        public bool IsMoving => Phase == MovementPhase.Moving ||
            Phase == MovementPhase.Arriving && PendingDestination.HasValue;
        public bool IsBlocked => Phase == MovementPhase.Blocked;
        public NumericsVector3 JumpPosition { get; private set; }
        public NumericsQuaternion StartRotation { get; private set; } = NumericsQuaternion.Identity;
        public NumericsVector3 HyperSpacePosition { get; private set; }

        public ShipMoveModel(IShipMoveData shipMoveData, CombatModifiers modifiers)
        {
            _shipMoveData = shipMoveData;
            _modifiers = modifiers;
        }

        public void ConfigureSpawnPose(NumericsVector3 position,
            NumericsQuaternion rotation, bool useHyperSpaceEntry)
        {
            HyperSpacePosition = position;
            StartRotation = rotation;
            JumpPosition = useHyperSpaceEntry
                ? position - NumericsVector3.Transform(NumericsVector3.UnitZ,
                    rotation) * OFFSET_HYPERSPACE_JUMP
                : position;
        }

        public void ApplyMoveCoefficient(float coefficient) => _speedCoefficient = coefficient;
        public bool IsSameRequest(NumericsVector3 destination) =>
            LastRequest.HasValue && PositionsEqual(LastRequest.Value, destination);
        public bool HasDestination(NumericsVector3 destination) =>
            (Phase != MovementPhase.Arriving || PendingDestination.HasValue) &&
            PositionsEqual(Destination, destination);

        public void Request(NumericsVector3 destination)
        {
            LastRequest = destination;
            if (Phase == MovementPhase.Arriving) PendingDestination = destination;
        }

        public void Accept(NumericsVector3 destination)
        {
            Destination = destination;
            PendingDestination = null;
            Phase = MovementPhase.Moving;
        }

        public void Queue(NumericsVector3 destination)
        {
            Destination = destination;
            PendingDestination = destination;
        }

        public void Defer(NumericsVector3 destination) => PendingDestination = destination;

        public void Block(NumericsVector3 destination)
        {
            PendingDestination = destination;
            if (Phase == MovementPhase.Arriving) Destination = destination;
            else Phase = MovementPhase.Blocked;
        }

        public NumericsVector3? TakePending()
        {
            NumericsVector3? pending = PendingDestination;
            PendingDestination = null;
            if (Phase == MovementPhase.Blocked) Phase = MovementPhase.Idle;
            return pending;
        }

        public NumericsVector3? FinishArrival()
        {
            NumericsVector3? pending = TakePending();
            Phase = MovementPhase.Idle;
            return pending;
        }

        public void Arrive(NumericsVector3 position)
        {
            Destination = position;
            Phase = MovementPhase.Idle;
        }

        public void StopAt(NumericsVector3 position)
        {
            PendingDestination = null;
            LastRequest = null;
            Destination = position;
            if (Phase != MovementPhase.Arriving) Phase = MovementPhase.Idle;
        }

        private static bool PositionsEqual(NumericsVector3 first, NumericsVector3 second) =>
            Math.Abs(first.X - second.X) < POSITION_TOLERANCE &&
            Math.Abs(first.Y - second.Y) < POSITION_TOLERANCE &&
            Math.Abs(first.Z - second.Z) < POSITION_TOLERANCE;
    }
}
