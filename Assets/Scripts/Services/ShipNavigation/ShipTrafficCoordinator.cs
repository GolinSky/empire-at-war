using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct ShipTrafficSchedule
    {
        public ShipTrafficSchedule(
            float waitDuration,
            int exactConflictCheckCount)
        {
            WaitDuration = waitDuration;
            ExactConflictCheckCount = exactConflictCheckCount;
        }

        public float WaitDuration { get; }
        public int ExactConflictCheckCount { get; }
    }

    internal sealed class ShipTrafficCoordinator
    {
        private readonly Dictionary<IShipNavigationAgent, ShipTrafficReservation>
            _reservations =
                new Dictionary<IShipNavigationAgent, ShipTrafficReservation>();
        private readonly ShipTrafficScheduler _scheduler =
            new ShipTrafficScheduler();

        public bool IsRegistered(IShipNavigationAgent agent)
        {
            return _reservations.ContainsKey(agent);
        }

        public void Register(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (!_reservations.ContainsKey(agent))
            {
                _reservations.Add(agent, CreateStationaryReservation(agent));
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

        public void Clear(IShipNavigationAgent agent)
        {
            if (agent == null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (_reservations.ContainsKey(agent))
            {
                _reservations[agent] = CreateStationaryReservation(agent);
            }
        }

        public void AppendPredictedContacts(
            IShipNavigationAgent planningAgent,
            List<RadarContact> contacts)
        {
            if (planningAgent == null)
            {
                throw new ArgumentNullException(nameof(planningAgent));
            }

            if (contacts == null)
            {
                throw new ArgumentNullException(nameof(contacts));
            }

            foreach (KeyValuePair<IShipNavigationAgent, ShipTrafficReservation>
                     pair in _reservations)
            {
                IShipNavigationAgent other = pair.Key;
                if (ReferenceEquals(other, planningAgent))
                {
                    continue;
                }

                AddContactIfUnique(
                    contacts,
                    new RadarContact(
                        other.NavigationPosition,
                        other.NavigationRadius,
                        true));
                if (pair.Value.HasDestination)
                {
                    AddContactIfUnique(
                        contacts,
                        new RadarContact(
                            pair.Value.Destination,
                            pair.Value.Radius,
                            true));
                }
            }
        }

        public ShipTrafficSchedule Reserve(
            IShipNavigationAgent agent,
            Vector3 destination,
            ShipBezierRoute route,
            float turnDuration,
            float movementDuration,
            float heightTolerance)
        {
            ShipTrafficPath trafficPath = _scheduler.CreateTrajectory(
                route,
                agent.NavigationRadius);
            float trafficDelay = _scheduler.CalculateDelay(
                agent,
                trafficPath,
                movementDuration,
                heightTolerance,
                _reservations);
            float waitDuration = Mathf.Max(turnDuration, trafficDelay);
            _reservations[agent] = new ShipTrafficReservation(
                destination,
                trafficPath,
                agent.NavigationHeight,
                agent.NavigationRadius,
                agent.NavigationSpeed,
                waitDuration,
                movementDuration);
            return new ShipTrafficSchedule(
                waitDuration,
                _scheduler.LastExactConflictCheckCount);
        }

        private static ShipTrafficReservation CreateStationaryReservation(
            IShipNavigationAgent agent)
        {
            return new ShipTrafficReservation(
                agent.NavigationPosition,
                agent.NavigationHeight,
                agent.NavigationRadius,
                agent.NavigationSpeed);
        }

        private static void AddContactIfUnique(
            List<RadarContact> contacts,
            RadarContact contact)
        {
            for (int i = 0; i < contacts.Count; i++)
            {
                RadarContact existing = contacts[i];
                if (existing.Position == contact.Position &&
                    Mathf.Approximately(existing.Radius, contact.Radius) &&
                    existing.IsShip == contact.IsShip)
                {
                    return;
                }
            }

            contacts.Add(contact);
        }
    }
}
