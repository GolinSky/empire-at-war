using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Timing;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    // One obstacle raster and its region labels, kept across frames while the obstacles stay the same.
    // Searches are specific to the current origin. Jobs complete before reuse; the navigation cache disposes buffers.
    internal sealed class ShipPathGrid : IDisposable
    {
        private const float MINIMUM_CELL_SIZE = 4f;

        private const int MAXIMUM_CELLS_PER_AXIS = 160;

        private const float HALF_DIAGONAL_FACTOR = 0.7072f;

        private readonly NavigationGridCells _cells;
        private float2 _origin;
        private readonly Vector2 _mapMin;
        private readonly Vector2 _mapMax;
        private readonly float _clearance;
        private NativeArray<float3> _obstacles;
        private NativeArray<byte> _blocked;
        private NativeArray<int> _regions;
        private NativeArray<float> _costSoFar;
        private NativeArray<float> _extraCost;
        private readonly IHazardSource[] _hazards;
        private bool _hasExtraCost;
        private NativeArray<int> _cameFrom;
        private NativeArray<byte> _closed;
        private NativeArray<uint> _visitedGeneration;
        private uint _searchGeneration;
        private NativeList<NavigationRouteJob.OpenNode> _open;
        private NativeList<float2> _rawPath;
        private NativeList<float2> _path;
        private NativeArray<int> _status;

        private readonly float _inflation;

        private int _originRegion;
        // Unreachable answers start only after this plan's first search, so direct-route
        // candidates keep the same ordering as before any search.
        private bool _hasSearched;

        public ShipPathGrid(
            IReadOnlyList<RadarContact> obstacles,
            Vector2Range mapRange,
            Vector3 origin,
            float clearance,
            IReadOnlyList<IHazardSource> hazards)
        {
            _hazards = new IHazardSource[hazards.Count];
            for (int i = 0; i < hazards.Count; i++) _hazards[i] = hazards[i];
            Vector2 size = mapRange.Max - mapRange.Min;
            float cellSize = Mathf.Max(
                MINIMUM_CELL_SIZE,
                Mathf.Max(size.x, size.y) / MAXIMUM_CELLS_PER_AXIS);
            _cells = new NavigationGridCells(
                new float2(mapRange.Min.x, mapRange.Min.y),
                cellSize,
                Mathf.FloorToInt(size.x / cellSize) + 1,
                Mathf.FloorToInt(size.y / cellSize) + 1);
            // Inflating by half a cell diagonal guarantees that the straight
            // segment between two adjacent walkable nodes keeps full clearance.
            _inflation = clearance + cellSize * HALF_DIAGONAL_FACTOR;
            _mapMin = mapRange.Min;
            _mapMax = mapRange.Max;
            _clearance = clearance;
            _obstacles = new NativeArray<float3>(obstacles.Count, Allocator.Persistent);
            for (int i = 0; i < obstacles.Count; i++)
            {
                RadarContact obstacle = obstacles[i];
                _obstacles[i] = new float3(
                    obstacle.Position.x,
                    obstacle.Position.z,
                    obstacle.Radius);
            }

            SetOrigin(origin);
        }

        public void Dispose()
        {
            if (_blocked.IsCreated) _blocked.Dispose();
            if (_regions.IsCreated) _regions.Dispose();
            if (_costSoFar.IsCreated) _costSoFar.Dispose();
            if (_extraCost.IsCreated) _extraCost.Dispose();
            if (_cameFrom.IsCreated) _cameFrom.Dispose();
            if (_closed.IsCreated) _closed.Dispose();
            if (_visitedGeneration.IsCreated) _visitedGeneration.Dispose();
            if (_open.IsCreated) _open.Dispose();
            if (_rawPath.IsCreated) _rawPath.Dispose();
            if (_path.IsCreated) _path.Dispose();
            if (_status.IsCreated) _status.Dispose();
            if (_obstacles.IsCreated) _obstacles.Dispose();
        }

        public bool Matches(IReadOnlyList<RadarContact> obstacles, Vector2Range mapRange, float clearance,
            IReadOnlyList<IHazardSource> hazards)
        {
            if (_hazards.Length != hazards.Count) return false;
            for (int i = 0; i < hazards.Count; i++)
                if (!ReferenceEquals(_hazards[i], hazards[i])) return false;
            if (!_mapMin.Equals(mapRange.Min) || !_mapMax.Equals(mapRange.Max) ||
                _clearance != clearance || _obstacles.Length != obstacles.Count) return false;
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
            _originRegion = -1;
            _hasSearched = false;
        }

        public bool ContainsRoute(EmpireAtWar.Components.Ship.Movement.ShipBezierRoute route) =>
            route.IsWithinBounds(_mapMin, _mapMax);

        public bool IsUnpenalizedSegment(Vector3 from, Vector3 to)
        {
            if (_hazards.Length == 0) return true;
            EnsureBuilt();
            NavigationTravelCost cost = new NavigationTravelCost(_cells, _extraCost);
            float2 start = new float2(from.x, from.z);
            float2 end = new float2(to.x, to.z);
            return cost.Segment(start, end) <= math.distance(start, end) + 0.01f;
        }

        public bool PreservesTravelCost(ShipBezierRoute route, IReadOnlyList<Vector3> waypoints)
        {
            if (_hazards.Length == 0) return true;
            EnsureBuilt();
            if (!_hasExtraCost) return true;
            NavigationTravelCost cost = new NavigationTravelCost(_cells, _extraCost);
            float originalCost = 0f;
            for (int i = 1; i < waypoints.Count; i++)
                originalCost += cost.Segment(new float2(waypoints[i - 1].x, waypoints[i - 1].z),
                    new float2(waypoints[i].x, waypoints[i].z));
            int samples = Mathf.Max(1, Mathf.CeilToInt(route.Length / (_cells.CellSize * 0.5f)));
            float routeCost = 0f;
            for (int i = 0; i < samples; i++)
            {
                Vector3 point = route.EvaluateNormalizedDistance((i + 0.5f) / samples, out _);
                routeCost += 1f + _extraCost[_cells.GetCell(new float2(point.x, point.z))];
            }
            return routeCost * route.Length / samples <= originalCost * 1.05f + 0.01f;
        }

        public bool TryFindPath(Vector3 destination, float height, List<Vector3> waypoints)
        {
            BeginSearch();
            _searchGeneration = _searchGeneration == uint.MaxValue ? 1 : _searchGeneration + 1;
            NavigationRouteJob job = new NavigationRouteJob
            {
                Blocked = _blocked,
                Regions = _regions,
                ExtraCost = _extraCost,
                HasExtraCost = _hasExtraCost,
                Obstacles = _obstacles,
                CostSoFar = _costSoFar,
                CameFrom = _cameFrom,
                Closed = _closed,
                VisitedGeneration = _visitedGeneration,
                Generation = _searchGeneration,
                Open = _open,
                Cells = _cells,
                Start = _origin,
                Goal = new float2(destination.x, destination.z),
                LineOfSightInflation = _inflation,
                RawPath = _rawPath,
                Waypoints = _path,
                Status = _status
            };
            // Same-frame contract: the caller needs the route now, so the
            // job is completed immediately (Burst still runs the search).
            using (BattleProfilerMarkers.NavigationPathSearch.Auto())
            {
                job.Schedule().Complete();
            }

            waypoints.Clear();
            if (_status[0] != NavigationRouteJob.STATUS_FOUND)
            {
                return false;
            }

            for (int i = 0; i < _path.Length; i++)
            {
                waypoints.Add(new Vector3(_path[i].x, height, _path[i].y));
            }

            return true;
        }

        /// <summary>
        /// True only after this plan has searched and the cell a route to the point would end on lies outside
        /// the ship's region. Matches <see cref="TryFindPath"/>: a point inside the grid's inflation
        /// (e.g. snapped to an obstacle edge) uses its nearest walkable cell.
        /// </summary>
        public bool IsKnownUnreachable(Vector3 position)
        {
            if (!_hasSearched)
            {
                return false;
            }

            int cell = _cells.FindNearestWalkableCell(_blocked, new float2(position.x, position.z));
            return cell < 0 || _originRegion < 0 || _regions[cell] != _originRegion;
        }

        /// <summary>The grid node closest to <paramref name="position"/> that the ship can reach.</summary>
        public bool TryGetNearestReachable(Vector3 position, float height, out Vector3 nearest)
        {
            BeginSearch();
            using (BattleProfilerMarkers.NavigationNearestReachable.Auto())
            {
                new NavigationNearestReachableJob
                {
                    Blocked = _blocked,
                    Regions = _regions,
                    Cells = _cells,
                    Start = _origin,
                    Target = new float2(position.x, position.z),
                    Result = _status
                }.Schedule().Complete();
            }

            int best = _status[0];
            if (best < 0)
            {
                nearest = default;
                return false;
            }

            float2 node = _cells.GetNodePosition(best);
            nearest = new Vector3(node.x, height, node.y);
            return true;
        }

        private void BeginSearch()
        {
            EnsureBuilt();
            if (_hasSearched)
            {
                return;
            }

            _hasSearched = true;
            int originCell = _cells.FindNearestWalkableCell(_blocked, _origin);
            _originRegion = originCell < 0 ? -1 : _regions[originCell];
        }

        private void EnsureBuilt()
        {
            if (_blocked.IsCreated)
            {
                return;
            }

            using (BattleProfilerMarkers.NavigationGridBuild.Auto())
            {
                Build();
            }
        }

        private void Build()
        {
            int cellCount = _cells.Count;
            _blocked = new NativeArray<byte>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _regions = new NativeArray<int>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _costSoFar = new NativeArray<float>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _extraCost = new NativeArray<float>(cellCount, Allocator.Persistent);
            for (int i = 0; i < cellCount; i++)
            {
                float2 point = _cells.GetNodePosition(i);
                foreach (IHazardSource hazard in _hazards)
                    if (hazard.Contains(new Vector3(point.x, 0f, point.y), 0f, false))
                        _extraCost[i] += hazard.Data.PathCost;
                _hasExtraCost |= _extraCost[i] > 0f;
            }
            _cameFrom = new NativeArray<int>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _closed = new NativeArray<byte>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _visitedGeneration = new NativeArray<uint>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _open = new NativeList<NavigationRouteJob.OpenNode>(256, Allocator.Persistent);
            _rawPath = new NativeList<float2>(64, Allocator.Persistent);
            _path = new NativeList<float2>(16, Allocator.Persistent);
            _status = new NativeArray<int>(1, Allocator.Persistent);
            NativeArray<int> queue = new NativeArray<int>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            try
            {
                JobHandle blocked = new NavigationGridBlockJob
                {
                    Obstacles = _obstacles,
                    Blocked = _blocked,
                    GridOrigin = _cells.Origin,
                    CellSize = _cells.CellSize,
                    Width = _cells.Width,
                    Inflation = _inflation
                }.Schedule();
                new NavigationRegionJob
                {
                    Blocked = _blocked,
                    Cells = _cells,
                    Regions = _regions,
                    Queue = queue
                }.Schedule(blocked).Complete();
            }
            finally
            {
                queue.Dispose();
            }
        }
    }
}
