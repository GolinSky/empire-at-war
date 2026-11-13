using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
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
        private NativeArray<float3> _disks;
        private NativeParallelMultiHashMap<int, int> _diskBuckets;
        private readonly NavMeshBuckets _layout;
        private NavMeshData _mesh;
        private NavMeshRouteWorkspace _search;
        private float2 _origin;
        private float2 _entry;
        private int _originTriangle;
        private bool _hasSearched;
        private readonly List<NavMeshCorner> _routeCorners = new List<NavMeshCorner>();

        public IReadOnlyList<NavMeshCorner> RouteCorners => _routeCorners;
        public NativeArray<float3> Disks => _disks;
        public NativeParallelMultiHashMap<int, int> DiskBuckets => _diskBuckets;
        public NavMeshBuckets Layout => _layout;

        public ShipNavMesh(IReadOnlyList<RadarContact> obstacles, Vector3 origin, float clearance,
            NavigationHazardRaster hazards)
        {
            _hazards = hazards;
            _clearance = clearance;
            _obstacles = new NativeArray<float3>(obstacles.Count, Allocator.Persistent);
            _disks = new NativeArray<float3>(obstacles.Count, Allocator.Persistent);
            _diskBuckets = new NativeParallelMultiHashMap<int, int>(128, Allocator.Persistent);
            _layout = new NavMeshBuckets(new float2(hazards.Min.x, hazards.Min.y),
                new float2(hazards.Max.x, hazards.Max.y));
            for (int i = 0; i < obstacles.Count; i++)
            {
                RadarContact obstacle = obstacles[i];
                _obstacles[i] = new float3(obstacle.Position.x, obstacle.Position.z, obstacle.Radius);
                float3 disk = new float3(obstacle.Position.x, obstacle.Position.z, obstacle.Radius + clearance);
                _disks[i] = disk;
                _layout.Insert(_diskBuckets, i, disk.xy - disk.z, disk.xy + disk.z);
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
            float2 planar = new float2(origin.x, origin.z);
            // Spawn checks ask from the same station repeatedly; its located origin stays valid.
            if (_hasSearched && _origin.Equals(planar)) return;
            _origin = planar;
            _hasSearched = false;
        }

        public bool IsUnpenalizedSegment(Vector3 from, Vector3 to) => _hazards.IsUnpenalizedSegment(from, to);

        public bool TryFindPath(Vector3 destination, float height, List<Vector3> waypoints)
        {
            waypoints.Clear();
            _routeCorners.Clear();
            BeginSearch();
            float2 goal = new float2(destination.x, destination.z);
            int triangle = FindReachableGoal(goal, out float2 exit);
            if (triangle < 0) return false;
            if (!_search.TryFindPath(_mesh, _originTriangle, triangle, _entry, exit, _hazards.Cells,
                    _hazards.Costs, _hazards.HasExtraCost)) return false;

            waypoints.Add(new Vector3(_origin.x, height, _origin.y));
            _routeCorners.Add(new NavMeshCorner(waypoints[0], -1, 0, 0f));
            if (!_entry.Equals(_origin))
            {
                waypoints.Add(new Vector3(_entry.x, height, _entry.y));
                _routeCorners.Add(new NavMeshCorner(waypoints[waypoints.Count - 1], -1, 0, 0f));
            }
            var path = _search.Waypoints;
            for (int i = 0; i < path.Length; i++)
            {
                var point = new Vector3(path[i].x, height, path[i].y);
                if (waypoints[waypoints.Count - 1].Equals(point)) continue;
                waypoints.Add(point);
                float3 tag = _search.CornerTags[i];
                _routeCorners.Add(new NavMeshCorner(point, (int)tag.x, (int)tag.y, tag.z));
            }
            var end = new Vector3(goal.x, height, goal.y);
            if (waypoints.Count == 1 || !waypoints[waypoints.Count - 1].Equals(end))
            {
                waypoints.Add(end);
                _routeCorners.Add(new NavMeshCorner(end, -1, 0, 0f));
            }
            return true;
        }

        public bool IsKnownUnreachable(Vector3 position)
        {
            // The planner tries its direct course before forcing any triangulation.
            return _hasSearched && FindReachableGoal(new float2(position.x, position.z), out _) < 0;
        }

        public bool IsReachable(Vector3 position)
        {
            BeginSearch();
            return FindReachableGoal(new float2(position.x, position.z), out _) >= 0;
        }

        private int FindReachableGoal(float2 goal, out float2 exit)
        {
            exit = goal;
            // A goal inside an inflated disk always fails the exit leg check below; reject it
            // before the locator falls back to scanning every triangle.
            if (_originTriangle < 0 || IsInsideObstacle(goal)) return -1;
            int originRegion = _mesh.Regions[_originTriangle];
            int triangle = _mesh.FindNearest(goal, 0, out exit, originRegion);
            if (triangle < 0 || _mesh.Regions[triangle] != originRegion) return -1;
            return NavMeshEscapeJob.CanConnect(exit, goal, _obstacles, _clearance, false) ? triangle : -1;
        }

        // Same strict test as NavMeshEscapeJob.CanConnect, so the early rejection never changes its answer.
        private bool IsInsideObstacle(float2 point)
        {
            for (int i = 0; i < _obstacles.Length; i++)
            {
                float3 disk = _obstacles[i];
                double radius = disk.z + _clearance;
                if (math.lengthsq((double2)point - disk.xy) < radius * radius) return true;
            }
            return false;
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
            return new NavMeshData(_layout.Min, _layout.Max, _hazards.Cells.CellSize, _disks,
                _hazards.Boundary, _diskBuckets);
        }

        public void Dispose()
        {
            if (_search != null) _search.Dispose();
            if (_mesh != null) _mesh.Dispose();
            if (_obstacles.IsCreated) _obstacles.Dispose();
            if (_disks.IsCreated) _disks.Dispose();
            if (_diskBuckets.IsCreated) _diskBuckets.Dispose();
        }
    }
}
