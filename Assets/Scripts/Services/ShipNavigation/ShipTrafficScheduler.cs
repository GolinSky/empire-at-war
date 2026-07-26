using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Movement;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class ShipTrafficScheduler
    {
        private const float TRAFFIC_SAFETY_DELAY = 0.5f;
        private const int MINIMUM_TRAFFIC_SAMPLE_COUNT = 16;
        private const int MAXIMUM_TRAFFIC_SAMPLE_COUNT = 128;
        private const int MAXIMUM_SCHEDULING_ITERATIONS = 128;

        private readonly List<TrafficConflict> _conflicts =
            new List<TrafficConflict>();

        public Vector3[] CreateTrajectory(
            ShipBezierRoute route,
            float navigationRadius)
        {
            int sampleCount = Mathf.Clamp(
                Mathf.CeilToInt(
                    route.Length / Mathf.Max(navigationRadius, 1f)),
                MINIMUM_TRAFFIC_SAMPLE_COUNT,
                MAXIMUM_TRAFFIC_SAMPLE_COUNT);
            Vector3[] trajectory = new Vector3[sampleCount + 1];
            for (int i = 0; i <= sampleCount; i++)
            {
                trajectory[i] = route.EvaluateNormalizedDistance(
                    i / (float)sampleCount,
                    out _);
            }

            return trajectory;
        }

        public float CalculateDelay(
            IShipNavigationAgent planningAgent,
            IReadOnlyList<Vector3> trajectory,
            float movementDuration,
            float heightTolerance,
            IReadOnlyDictionary<IShipNavigationAgent, ShipTrafficReservation>
                reservations)
        {
            _conflicts.Clear();
            foreach (KeyValuePair<IShipNavigationAgent, ShipTrafficReservation> pair
                     in reservations)
            {
                ShipTrafficReservation reservation = pair.Value;
                if (ReferenceEquals(pair.Key, planningAgent) ||
                    !reservation.HasTrajectory ||
                    Mathf.Abs(reservation.Height - planningAgent.NavigationHeight) >
                    heightTolerance)
                {
                    continue;
                }

                float safeDistance =
                    reservation.Radius + planningAgent.NavigationRadius;
                float safeTime =
                    safeDistance /
                    Mathf.Max(
                        Mathf.Min(
                            planningAgent.NavigationSpeed,
                            reservation.Speed),
                        Mathf.Epsilon) +
                    TRAFFIC_SAFETY_DELAY;
                CollectConflicts(
                    trajectory,
                    movementDuration,
                    reservation,
                    safeDistance,
                    safeTime);
            }

            return ResolveDelay();
        }

        private float ResolveDelay()
        {
            float delay = 0f;
            for (int iteration = 0;
                 iteration < MAXIMUM_SCHEDULING_ITERATIONS;
                 iteration++)
            {
                float requiredDelay = delay;
                for (int i = 0; i < _conflicts.Count; i++)
                {
                    TrafficConflict conflict = _conflicts[i];
                    float planningTime = delay + conflict.PlanningTime;
                    if (Mathf.Abs(planningTime - conflict.ReservedTime) >=
                        conflict.SafeTime)
                    {
                        continue;
                    }

                    requiredDelay = Mathf.Max(
                        requiredDelay,
                        conflict.ReservedTime + conflict.SafeTime -
                        conflict.PlanningTime);
                }

                if (requiredDelay <= delay + Mathf.Epsilon)
                {
                    return delay;
                }

                delay = requiredDelay;
            }

            return delay;
        }

        private void CollectConflicts(
            IReadOnlyList<Vector3> planningTrajectory,
            float planningMovementDuration,
            ShipTrafficReservation reservation,
            float safeDistance,
            float safeTime)
        {
            float safeDistanceSquared = safeDistance * safeDistance;
            for (int planningIndex = 1;
                 planningIndex < planningTrajectory.Count;
                 planningIndex++)
            {
                for (int reservedIndex = 1;
                     reservedIndex < reservation.Trajectory.Length;
                     reservedIndex++)
                {
                    if (!ShipTrafficConflictDetector.TryGetSegmentConflict(
                            planningTrajectory[planningIndex - 1],
                            planningTrajectory[planningIndex],
                            reservation.Trajectory[reservedIndex - 1],
                            reservation.Trajectory[reservedIndex],
                            safeDistanceSquared,
                            out float planningParameter,
                            out float reservedParameter))
                    {
                        continue;
                    }

                    float planningProgress =
                        (planningIndex - 1 + planningParameter) /
                        (planningTrajectory.Count - 1f);
                    float reservedProgress =
                        (reservedIndex - 1 + reservedParameter) /
                        (reservation.Trajectory.Length - 1f);
                    _conflicts.Add(new TrafficConflict(
                        planningProgress * planningMovementDuration,
                        reservation.WaitDuration +
                        reservedProgress * reservation.MovementDuration,
                        safeTime));
                }
            }
        }

        private readonly struct TrafficConflict
        {
            public TrafficConflict(
                float planningTime,
                float reservedTime,
                float safeTime)
            {
                PlanningTime = planningTime;
                ReservedTime = reservedTime;
                SafeTime = safeTime;
            }

            public float PlanningTime { get; }
            public float ReservedTime { get; }
            public float SafeTime { get; }
        }
    }

    internal readonly struct ShipTrafficReservation
    {
        public ShipTrafficReservation(
            Vector3 destination,
            Vector3[] trajectory,
            float height,
            float radius,
            float speed,
            float waitDuration,
            float movementDuration)
        {
            Destination = destination;
            Trajectory = trajectory;
            Height = height;
            Radius = radius;
            Speed = speed;
            WaitDuration = waitDuration;
            MovementDuration = movementDuration;
        }

        public Vector3 Destination { get; }
        public Vector3[] Trajectory { get; }
        public float Height { get; }
        public float Radius { get; }
        public float Speed { get; }
        public float WaitDuration { get; }
        public float MovementDuration { get; }
        public bool HasTrajectory => Trajectory != null && Trajectory.Length > 0;
    }
}
