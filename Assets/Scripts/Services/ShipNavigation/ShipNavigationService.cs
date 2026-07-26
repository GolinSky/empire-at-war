using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    public interface IShipNavigationAgent
    {
        Vector3 NavigationPosition { get; }
        float NavigationHeight { get; }
        float NavigationRadius { get; }
        float NavigationSpeed { get; }
        float NavigationRotationSpeed { get; }
    }

    public readonly struct ShipNavigationPlan
    {
        public ShipNavigationPlan(
            Vector3 destination,
            Vector3? detour,
            ShipBezierRoute route,
            float waitDuration,
            float movementDuration)
        {
            Destination = destination;
            Detour = detour;
            Route = route ?? throw new ArgumentNullException(nameof(route));
            WaitDuration = waitDuration;
            MovementDuration = movementDuration;
        }

        public Vector3 Destination { get; }
        public Vector3? Detour { get; }
        public ShipBezierRoute Route { get; }
        public Vector3[] Trajectory => Route.Samples;
        public float WaitDuration { get; }
        public float MovementDuration { get; }
        public float TotalDuration => WaitDuration + MovementDuration;
    }

    public interface IShipNavigationService : IService
    {
        void Register(IShipNavigationAgent agent);
        void Unregister(IShipNavigationAgent agent);
        ShipNavigationPlan Plan(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange);
        void ClearPlan(IShipNavigationAgent agent);
    }

    public sealed class ShipNavigationService : Service, IShipNavigationService
    {
        private const float TRAFFIC_SAFETY_DELAY = 0.5f;

        private readonly Dictionary<IShipNavigationAgent, Reservation> _reservations =
            new Dictionary<IShipNavigationAgent, Reservation>();
        private readonly List<RadarContact> _contacts = new List<RadarContact>();

        public void Register(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (!_reservations.ContainsKey(agent))
            {
                _reservations.Add(agent, default);
            }
        }

        public void Unregister(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _reservations.Remove(agent);
        }

        public ShipNavigationPlan Plan(
            IShipNavigationAgent agent,
            Vector3 forward,
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            float heightTolerance,
            float clearance,
            Vector2Range mapRange)
        {
            if (!_reservations.ContainsKey(agent))
            {
                throw new InvalidOperationException(
                    "Ship navigation agent must be registered before planning.");
            }

            BuildSharedContacts(agent, obstacleContacts);
            Vector3 origin = agent.NavigationPosition;
            Vector3 destination = requestedDestination;
            ShipAvoidancePlanner.TryResolveDestination(
                requestedDestination,
                origin,
                _contacts,
                agent.NavigationHeight,
                heightTolerance,
                clearance,
                mapRange,
                out destination);

            Vector3? detour = null;
            if (ShipAvoidancePlanner.TryCalculateDetour(
                    origin,
                    destination,
                    _contacts,
                    agent.NavigationHeight,
                    heightTolerance,
                    clearance,
                    mapRange,
                    out Vector3 calculatedDetour))
            {
                detour = calculatedDetour;
            }

            ShipBezierRoute route = detour.HasValue
                ? ShipBezierPath.BuildAvoidanceRoute(
                    origin,
                    forward,
                    detour.Value,
                    destination)
                : ShipBezierPath.BuildDirectRoute(origin, forward, destination);
            float movementDuration =
                route.Length / Mathf.Max(agent.NavigationSpeed, Mathf.Epsilon);
            float turnDuration = ShipRotationKinematics.CalculateTurnDuration(
                Quaternion.LookRotation(
                    forward.sqrMagnitude > Mathf.Epsilon ? forward : Vector3.forward,
                    Vector3.up),
                route.InitialTangent,
                Mathf.Max(agent.NavigationRotationSpeed, Mathf.Epsilon));
            float trafficDelay = CalculateTrafficDelay(
                agent,
                route,
                heightTolerance);
            float waitDuration = Mathf.Max(turnDuration, trafficDelay);
            ShipNavigationPlan plan = new ShipNavigationPlan(
                destination,
                detour,
                route,
                waitDuration,
                movementDuration);
            _reservations[agent] = new Reservation(
                destination,
                route.Samples,
                agent.NavigationHeight,
                agent.NavigationRadius,
                plan.TotalDuration);
            return plan;
        }

        public void ClearPlan(IShipNavigationAgent agent)
        {
            if (_reservations.ContainsKey(agent))
            {
                _reservations[agent] = default;
            }
        }

        private float CalculateTrafficDelay(
            IShipNavigationAgent planningAgent,
            ShipBezierRoute route,
            float heightTolerance)
        {
            float delay = 0f;
            foreach (KeyValuePair<IShipNavigationAgent, Reservation> pair
                     in _reservations)
            {
                if (ReferenceEquals(pair.Key, planningAgent) ||
                    !pair.Value.HasTrajectory ||
                    Mathf.Abs(pair.Value.Height - planningAgent.NavigationHeight) >
                    heightTolerance)
                {
                    continue;
                }

                float safeDistance =
                    pair.Value.Radius + planningAgent.NavigationRadius;
                if (RoutesConflict(
                        route.Samples,
                        pair.Value.Trajectory,
                        safeDistance))
                {
                    delay = Mathf.Max(
                        delay,
                        pair.Value.TotalDuration + TRAFFIC_SAFETY_DELAY);
                }
            }

            return delay;
        }

        private void BuildSharedContacts(
            IShipNavigationAgent planningAgent,
            IReadOnlyList<RadarContact> obstacleContacts)
        {
            _contacts.Clear();
            if (obstacleContacts != null)
            {
                for (int i = 0; i < obstacleContacts.Count; i++)
                {
                    _contacts.Add(obstacleContacts[i]);
                }
            }

            foreach (KeyValuePair<IShipNavigationAgent, Reservation> pair
                     in _reservations)
            {
                IShipNavigationAgent other = pair.Key;
                if (ReferenceEquals(other, planningAgent))
                {
                    continue;
                }

                _contacts.Add(new RadarContact(
                    other.NavigationPosition,
                    other.NavigationRadius,
                    true));
                if (pair.Value.HasTrajectory)
                {
                    _contacts.Add(new RadarContact(
                        pair.Value.Destination,
                        other.NavigationRadius,
                        true));
                }
            }
        }

        private static bool RoutesConflict(
            IReadOnlyList<Vector3> first,
            IReadOnlyList<Vector3> second,
            float safeDistance)
        {
            float safeDistanceSquared = safeDistance * safeDistance;
            for (int firstIndex = 1; firstIndex < first.Count; firstIndex++)
            {
                Vector2 firstStart = ToPlanar(first[firstIndex - 1]);
                Vector2 firstEnd = ToPlanar(first[firstIndex]);
                for (int secondIndex = 1; secondIndex < second.Count; secondIndex++)
                {
                    Vector2 secondStart = ToPlanar(second[secondIndex - 1]);
                    Vector2 secondEnd = ToPlanar(second[secondIndex]);
                    if (SegmentDistanceSquared(
                            firstStart,
                            firstEnd,
                            secondStart,
                            secondEnd) < safeDistanceSquared)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static float SegmentDistanceSquared(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd)
        {
            Vector2 firstDirection = firstEnd - firstStart;
            Vector2 secondDirection = secondEnd - secondStart;
            Vector2 startDelta = secondStart - firstStart;
            float denominator = Cross(firstDirection, secondDirection);
            if (Mathf.Abs(denominator) > Mathf.Epsilon)
            {
                float firstParameter =
                    Cross(startDelta, secondDirection) / denominator;
                float secondParameter =
                    Cross(startDelta, firstDirection) / denominator;
                if (firstParameter >= 0f && firstParameter <= 1f &&
                    secondParameter >= 0f && secondParameter <= 1f)
                {
                    return 0f;
                }
            }

            return Mathf.Min(
                PointSegmentDistanceSquared(firstStart, secondStart, secondEnd),
                PointSegmentDistanceSquared(firstEnd, secondStart, secondEnd),
                PointSegmentDistanceSquared(secondStart, firstStart, firstEnd),
                PointSegmentDistanceSquared(secondEnd, firstStart, firstEnd));
        }

        private static float PointSegmentDistanceSquared(
            Vector2 point,
            Vector2 start,
            Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return (point - start).sqrMagnitude;
            }

            float parameter = Mathf.Clamp01(
                Vector2.Dot(point - start, segment) / lengthSquared);
            return (point - (start + segment * parameter)).sqrMagnitude;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static Vector2 ToPlanar(Vector3 point)
        {
            return new Vector2(point.x, point.z);
        }

        private readonly struct Reservation
        {
            public Reservation(
                Vector3 destination,
                Vector3[] trajectory,
                float height,
                float radius,
                float totalDuration)
            {
                Destination = destination;
                Trajectory = trajectory;
                Height = height;
                Radius = radius;
                TotalDuration = totalDuration;
            }

            public Vector3 Destination { get; }
            public Vector3[] Trajectory { get; }
            public float Height { get; }
            public float Radius { get; }
            public float TotalDuration { get; }
            public bool HasTrajectory => Trajectory != null && Trajectory.Length > 0;
        }
    }
}
