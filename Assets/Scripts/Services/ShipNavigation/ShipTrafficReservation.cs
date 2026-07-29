using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct ShipTrafficReservation
    {
        public ShipTrafficReservation(
            Vector3 destination,
            float height,
            float radius,
            float speed)
            : this(destination, null, height, radius, speed, 0f, 0f)
        {
        }

        public ShipTrafficReservation(
            Vector3 destination,
            ShipTrafficPath path,
            float height,
            float radius,
            float speed,
            float waitDuration,
            float movementDuration)
        {
            Destination = destination;
            Path = path;
            Height = height;
            Radius = radius;
            Speed = speed;
            WaitDuration = waitDuration;
            MovementDuration = movementDuration;
            HasDestination = true;
        }

        public Vector3 Destination { get; }
        public ShipTrafficPath Path { get; }
        public float Height { get; }
        public float Radius { get; }
        public float Speed { get; }
        public float WaitDuration { get; }
        public float MovementDuration { get; }
        public bool HasDestination { get; }
        public bool HasTrajectory => Path != null;
    }
}
