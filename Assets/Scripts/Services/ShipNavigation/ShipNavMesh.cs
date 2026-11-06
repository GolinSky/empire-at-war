using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Timing;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class ShipNavMesh : IShipPathGraph
    {
        private readonly NavigationHazardRaster _hazards;
        private readonly float _clearance;
        private NativeArray<float3> _obstacles;
        private NavMeshData _mesh;
        private NavMeshRouteWorkspace _search;
        private float2 _origin;
        private float2 _entry;
        private int _originTriangle;
        private bool _hasSearched;

        public ShipNavMesh(IReadOnlyList<RadarContact> obstacles, Vector3 origin, float clearance,
            NavigationHazardRaster hazards)
        {
            _hazards = hazards;
            _clearance = clearance;
            _obstacles = new NativeArray<float3>(obstacles.Count, Allocator.Persistent);
            for (int i = 0; i < obstacles.Count; i++)
            {
                RadarContact obstacle = obstacles[i];
                _obstacles[i] = new float3(obstacle.Position.x, obstacle.Position.z, obstacle.Radius);
            }
            SetOrigin(origin);
        }

        public bool Matches(IReadOnlyList<RadarContact> obstacles, Vector2Range mapRange, float clearance,
            IReadOnlyList<IHazardSource> hazards)
        {
            if (_clearance != clearance || _obstacles.Length != obstacles.Count || !_hazards.Matches(mapRange, hazards))
                return false;
            for (int i = 0; i < obstacles.Count; i++)
            {
                RadarContact contact = obstacles[i];
                if (_obstacles[i].x != contact.Position.x || _obstacles[i].y != contact.Position.z ||
                    _obstacles[i].z != contact.Radius) return false;
            }
            return true;
        }

        public void SetOrigin(Vector3 origin)
        {
            _origin = new float2(origin.x, origin.z);
            _hasSearched = false;
        }

        public bool ContainsRoute(ShipBezierRoute route) => route.IsWithinBounds(_hazards.Min, _hazards.Max);
        public bool IsUnpenalizedSegment(Vector3 from, Vector3 to) => _hazards.IsUnpenalizedSegment(from, to);
        public bool PreservesTravelCost(ShipBezierRoute route, IReadOnlyList<Vector3> waypoints) =>
            _hazards.PreservesTravelCost(route, waypoints);

        public bool TryFindPath(Vector3 destination, float height, List<Vector3> waypoints)
        {
            waypoints.Clear();
            BeginSearch();
            float2 goal = new float2(destination.x, destination.z);
            int triangle = FindReachableGoal(goal, out float2 exit);
            if (triangle < 0) return false;
            if (!_search.TryFindPath(_mesh, _originTriangle, triangle, _entry, exit, _hazards.Cells,
                    _hazards.Costs, _hazards.HasExtraCost)) return false;

            waypoints.Add(new Vector3(_origin.x, height, _origin.y));
            if (!_entry.Equals(_origin)) waypoints.Add(new Vector3(_entry.x, height, _entry.y));
            var path = _search.Waypoints;
            for (int i = 0; i < path.Length; i++)
            {
                var point = new Vector3(path[i].x, height, path[i].y);
                if (!waypoints[waypoints.Count - 1].Equals(point)) waypoints.Add(point);
            }
            var end = new Vector3(goal.x, height, goal.y);
            if (waypoints.Count == 1 || !waypoints[waypoints.Count - 1].Equals(end)) waypoints.Add(end);
            return true;
        }

        public bool IsKnownUnreachable(Vector3 position)
        {
            // The planner tries its direct course before forcing any triangulation.
            return _hasSearched && FindReachableGoal(new float2(position.x, position.z), out _) < 0;
        }

        private int FindReachableGoal(float2 goal, out float2 exit)
        {
            exit = goal;
            if (_originTriangle < 0) return -1;
            int originRegion = _mesh.Regions[_originTriangle];
            int triangle = _mesh.FindNearest(goal, 0, out exit, originRegion);
            if (triangle < 0 || _mesh.Regions[triangle] != originRegion) return -1;
            return NavMeshEscapeJob.CanConnect(exit, goal, _obstacles, _clearance, false) ? triangle : -1;
        }

        public bool TryGetNearestReachable(Vector3 position, float height, out Vector3 nearest)
        {
            BeginSearch();
            using (BattleProfilerMarkers.NavigationNearestReachable.Auto())
            {
                if (_originTriangle >= 0 && _mesh.FindNearest(new float2(position.x, position.z),
                        _mesh.Regions[_originTriangle], out float2 point) >= 0)
                {
                    nearest = new Vector3(point.x, height, point.y);
                    return true;
                }
            }
            nearest = default;
            return false;
        }

        private void BeginSearch()
        {
            if (_hasSearched) return;
            if (_mesh == null)
            {
                using var marker = BattleProfilerMarkers.NavigationGridBuild.Auto();
                NavMeshData mesh = BuildMesh();
                try
                {
                    _search = new NavMeshRouteWorkspace(mesh.Regions.Length);
                    _mesh = mesh;
                }
                catch
                {
                    mesh.Dispose();
                    throw;
                }
            }
            _originTriangle = _mesh.FindNearest(_origin, 0, out _entry);
            if (_originTriangle >= 0 && !NavMeshEscapeJob.CanConnect(_origin, _entry, _obstacles, _clearance, true))
                _originTriangle = _mesh.FindEscape(_origin, _obstacles, _clearance, out _entry);
            _hasSearched = true;
        }

        private NavMeshData BuildMesh()
        {
            var disks = new NativeArray<float3>(_obstacles.Length, Allocator.TempJob);
            try
            {
                for (int i = 0; i < disks.Length; i++)
                {
                    float3 disk = _obstacles[i];
                    disk.z += _clearance;
                    disks[i] = disk;
                }
                return new NavMeshData(new float2(_hazards.Min.x, _hazards.Min.y),
                    new float2(_hazards.Max.x, _hazards.Max.y), _hazards.Cells.CellSize, disks, _hazards.Boundary);
            }
            finally
            {
                disks.Dispose();
            }
        }

        public void Dispose()
        {
            if (_search != null) _search.Dispose();
            if (_mesh != null) _mesh.Dispose();
            if (_obstacles.IsCreated) _obstacles.Dispose();
        }
    }
}
