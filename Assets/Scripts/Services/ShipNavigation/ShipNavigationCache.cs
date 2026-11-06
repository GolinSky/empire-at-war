using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class ShipNavigationCache : IDisposable
    {
        // Keep every clearance and hull-height obstacle set for the battle: mixed fleets
        // must not evict each other's graphs on every planning round.
        private readonly List<IShipPathGraph> _graphs = new List<IShipPathGraph>();
        private readonly List<NavigationHazardRaster> _rasters = new List<NavigationHazardRaster>();
        private readonly Dictionary<IShipNavigationAgent, StationaryEntry> _stationary =
            new Dictionary<IShipNavigationAgent, StationaryEntry>();

        public IShipPathGraph GetGraph(IReadOnlyList<RadarContact> contacts, Vector2Range mapRange,
            Vector3 origin, float clearance, IReadOnlyList<IHazardSource> hazards)
        {
            for (int i = 0; i < _graphs.Count; i++)
            {
                IShipPathGraph graph = _graphs[i];
                if (!graph.Matches(contacts, mapRange, clearance, hazards)) continue;
                graph.SetOrigin(origin);
                return graph;
            }
            var created = new ShipNavMesh(contacts, origin, clearance, GetRaster(mapRange, hazards));
            _graphs.Add(created);
            return created;
        }

        private NavigationHazardRaster GetRaster(Vector2Range mapRange, IReadOnlyList<IHazardSource> hazards)
        {
            foreach (NavigationHazardRaster raster in _rasters)
                if (raster.Matches(mapRange, hazards)) return raster;
            using var marker = BattleProfilerMarkers.NavigationGridBuild.Auto();
            var created = new NavigationHazardRaster(mapRange, hazards);
            _rasters.Add(created);
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
            foreach (IShipPathGraph graph in _graphs) graph.Dispose();
            _graphs.Clear();
            foreach (NavigationHazardRaster raster in _rasters) raster.Dispose();
            _rasters.Clear();
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
