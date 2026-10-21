using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class ShipNavigationCache : IDisposable
    {
        // Keep every clearance and hull-height obstacle set for the battle: mixed fleets
        // must not evict each other's grids on every planning round.
        private readonly List<ShipPathGrid> _grids = new List<ShipPathGrid>();
        private readonly Dictionary<IShipNavigationAgent, StationaryEntry> _stationary =
            new Dictionary<IShipNavigationAgent, StationaryEntry>();

        public ShipPathGrid GetGrid(IReadOnlyList<RadarContact> contacts, Vector2Range mapRange,
            Vector3 origin, float clearance)
        {
            for (int i = 0; i < _grids.Count; i++)
            {
                ShipPathGrid grid = _grids[i];
                if (!grid.Matches(contacts, mapRange, clearance)) continue;
                grid.SetOrigin(origin);
                return grid;
            }
            ShipPathGrid created = new ShipPathGrid(contacts, mapRange, origin, clearance);
            _grids.Add(created);
            return created;
        }

        public bool TryGetStationary(IShipNavigationAgent agent, Request request,
            IReadOnlyList<RadarContact> contacts, out ShipNavigationPlan plan)
        {
            if (_stationary.TryGetValue(agent, out StationaryEntry entry) &&
                entry.Request.Matches(request) && entry.Contacts.Count == contacts.Count)
            {
                for (int i = 0; i < contacts.Count; i++)
                {
                    RadarContact previous = entry.Contacts[i];
                    RadarContact current = contacts[i];
                    if (!previous.Position.Equals(current.Position) || previous.Radius != current.Radius)
                    {
                        plan = default;
                        return false;
                    }
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var scope = BattleProfilerMarkers.NavigationStationaryReuse.Auto();
#endif
                plan = entry.Plan;
                return true;
            }
            plan = default;
            return false;
        }

        public void StoreStationary(IShipNavigationAgent agent, Request request,
            IReadOnlyList<RadarContact> contacts, ShipNavigationPlan plan)
        {
            if (!_stationary.TryGetValue(agent, out StationaryEntry entry))
            {
                entry = new StationaryEntry();
                _stationary.Add(agent, entry);
            }
            entry.Request = request;
            entry.Plan = plan;
            entry.Contacts.Clear();
            for (int i = 0; i < contacts.Count; i++) entry.Contacts.Add(contacts[i]);
        }

        public void RemoveStationary(IShipNavigationAgent agent) => _stationary.Remove(agent);

        public void Dispose()
        {
            for (int i = 0; i < _grids.Count; i++) _grids[i].Dispose();
            _grids.Clear();
            _stationary.Clear();
        }

        internal readonly struct Request
        {
            private readonly Vector3 _origin;
            private readonly Vector3 _forward;
            private readonly Vector3 _destination;
            private readonly Vector2 _mapMin;
            private readonly Vector2 _mapMax;
            private readonly float _height;
            private readonly float _speed;
            private readonly float _rotationSpeed;
            private readonly float _heightTolerance;
            private readonly float _clearance;
            private readonly int _reservationVersion;

            public Request(IShipNavigationAgent agent, Vector3 forward, Vector3 destination,
                Vector2Range mapRange, float heightTolerance, float clearance, int reservationVersion)
            {
                _origin = agent.NavigationPosition;
                _forward = forward;
                _destination = destination;
                _mapMin = mapRange.Min;
                _mapMax = mapRange.Max;
                _height = agent.NavigationHeight;
                _speed = agent.NavigationSpeed;
                _rotationSpeed = agent.NavigationRotationSpeed;
                _heightTolerance = heightTolerance;
                _clearance = clearance;
                _reservationVersion = reservationVersion;
            }

            public bool Matches(Request other) =>
                _origin.Equals(other._origin) && _forward.Equals(other._forward) &&
                _destination.Equals(other._destination) && _mapMin.Equals(other._mapMin) &&
                _mapMax.Equals(other._mapMax) && _height == other._height &&
                _speed == other._speed && _rotationSpeed == other._rotationSpeed &&
                _heightTolerance == other._heightTolerance && _clearance == other._clearance &&
                _reservationVersion == other._reservationVersion;
        }

        private sealed class StationaryEntry
        {
            public readonly List<RadarContact> Contacts = new List<RadarContact>();
            public Request Request;
            public ShipNavigationPlan Plan;
        }
    }
}
