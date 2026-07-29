using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Movement;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class ShipTrafficScheduler
    {
        private const float TRAFFIC_SAFETY_DELAY = 0.5f;
        private const float MAXIMUM_TRAFFIC_START_DELAY =
            TRAFFIC_SAFETY_DELAY;
        private const int MINIMUM_TRAFFIC_SAMPLE_COUNT = 16;
        private const int MAXIMUM_TRAFFIC_SAMPLE_COUNT = 128;
        private const int MAXIMUM_SCHEDULING_ITERATIONS = 128;

        private readonly List<TrafficConflict> _conflicts =
            new List<TrafficConflict>();

        public int LastExactConflictCheckCount { get; private set; }

        public ShipTrafficPath CreateTrajectory(
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

            return new ShipTrafficPath(trajectory);
        }

        public float CalculateDelay(
            IShipNavigationAgent planningAgent,
            ShipTrafficPath trajectory,
            float movementDuration,
            float heightTolerance,
            IReadOnlyDictionary<IShipNavigationAgent, ShipTrafficReservation>
                reservations)
        {
            _conflicts.Clear();
            LastExactConflictCheckCount = 0;
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
                if (!trajectory.Bounds.Overlaps(
                        reservation.Path.Bounds,
                        safeDistance))
                {
                    continue;
                }

                CollectConflicts(
                    trajectory,
                    movementDuration,
                    reservation,
                    safeDistance,
                    safeTime);
            }

            return Mathf.Min(
                ResolveDelay(),
                MAXIMUM_TRAFFIC_START_DELAY);
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
            ShipTrafficPath planningTrajectory,
            float planningMovementDuration,
            ShipTrafficReservation reservation,
            float safeDistance,
            float safeTime)
        {
            float safeDistanceSquared = safeDistance * safeDistance;
            for (int planningIndex = 0;
                 planningIndex < planningTrajectory.Segments.Length;
                 planningIndex++)
            {
                ShipTrafficSegment planningSegment =
                    planningTrajectory.Segments[planningIndex];
                for (int reservedIndex = 0;
                     reservedIndex < reservation.Path.Segments.Length;
                     reservedIndex++)
                {
                    ShipTrafficSegment reservedSegment =
                        reservation.Path.Segments[reservedIndex];
                    if (!planningSegment.Bounds.Overlaps(
                            reservedSegment.Bounds,
                            safeDistance))
                    {
                        continue;
                    }

                    LastExactConflictCheckCount++;
                    if (!ShipTrafficConflictDetector.TryGetSegmentConflict(
                            planningSegment.Start,
                            planningSegment.End,
                            reservedSegment.Start,
                            reservedSegment.End,
                            safeDistanceSquared,
                            out float planningParameter,
                            out float reservedParameter))
                    {
                        continue;
                    }

                    float planningProgress =
                        (planningIndex + planningParameter) /
                        planningTrajectory.Segments.Length;
                    float reservedProgress =
                        (reservedIndex + reservedParameter) /
                        reservation.Path.Segments.Length;
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
}
