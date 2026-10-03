using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Timing;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    // Owns the native buffers of one planning pass from one origin. All methods
    // run on the main thread; every scheduled job is completed before its buffers
    // are read, and every allocation is released in Dispose within the same frame.
    internal sealed class ShipPathGrid : IDisposable
    {
        private const float MINIMUM_CELL_SIZE = 4f;

        private const int MAXIMUM_CELLS_PER_AXIS = 160;
        private const int BLOCK_JOB_BATCH_SIZE = 256;

        private const float HALF_DIAGONAL_FACTOR = 0.7072f;

        private readonly NavigationGridCells _cells;
        private readonly float2 _origin;
        private NativeArray<float3> _obstacles;
        private NativeArray<byte> _blocked;
        private NativeArray<float> _costSoFar;
        private NativeArray<int> _cameFrom;
        private NativeList<float2> _rawPath;
        private NativeList<float2> _path;
        private NativeArray<int> _status;

        private readonly float _inflation;

        private bool _isFlooded;

        public ShipPathGrid(
            IReadOnlyList<RadarContact> obstacles,
            Vector2Range mapRange,
            Vector3 origin,
            float clearance)
        {
            Vector2 size = mapRange.Max - mapRange.Min;
            float cellSize = Mathf.Max(
                MINIMUM_CELL_SIZE,
                Mathf.Max(size.x, size.y) / MAXIMUM_CELLS_PER_AXIS);
            _cells = new NavigationGridCells(
                new float2(mapRange.Min.x, mapRange.Min.y),
                cellSize,
                Mathf.CeilToInt(size.x / cellSize) + 1,
                Mathf.CeilToInt(size.y / cellSize) + 1);
            // Inflating by half a cell diagonal guarantees that the straight
            // segment between two adjacent walkable nodes keeps full clearance.
            _inflation = clearance + cellSize * HALF_DIAGONAL_FACTOR;
            _origin = new float2(origin.x, origin.z);
            _obstacles = new NativeArray<float3>(obstacles.Count, Allocator.TempJob);
            for (int i = 0; i < obstacles.Count; i++)
            {
                RadarContact obstacle = obstacles[i];
                _obstacles[i] = new float3(
                    obstacle.Position.x,
                    obstacle.Position.z,
                    obstacle.Radius);
            }
        }

        public void Dispose()
        {
            if (_isFlooded)
            {
                _blocked.Dispose();
                _costSoFar.Dispose();
                _cameFrom.Dispose();
                _rawPath.Dispose();
                _path.Dispose();
                _status.Dispose();
                _isFlooded = false;
            }

            if (_obstacles.IsCreated)
            {
                _obstacles.Dispose();
            }
        }

        public bool TryFindPath(Vector3 destination, float height, List<Vector3> waypoints)
        {
            EnsureFlood();
            NavigationRouteJob job = new NavigationRouteJob
            {
                Blocked = _blocked,
                Obstacles = _obstacles,
                CostSoFar = _costSoFar,
                CameFrom = _cameFrom,
                Cells = _cells,
                Start = _origin,
                Goal = new float2(destination.x, destination.z),
                LineOfSightInflation = _inflation,
                RawPath = _rawPath,
                Waypoints = _path,
                Status = _status
            };
            // Same-frame contract: the caller needs the route now, so the
            // job is completed immediately (Burst still runs the extraction).
            job.Schedule().Complete();

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
        /// True only when the flood has already run and cannot reach the cell a route
        /// to the point would end on. Matches <see cref="TryFindPath"/>: a point inside
        /// the grid's inflation (e.g. snapped to an obstacle edge) uses its nearest walkable cell.
        /// </summary>
        public bool IsKnownUnreachable(Vector3 position)
        {
            if (!_isFlooded)
            {
                return false;
            }

            int cell = _cells.FindNearestWalkableCell(_blocked, new float2(position.x, position.z));
            return cell < 0 || float.IsPositiveInfinity(_costSoFar[cell]);
        }

        /// <summary>The reachable grid node closest to <paramref name="position"/>.</summary>
        public bool TryGetNearestReachable(Vector3 position, float height, out Vector3 nearest)
        {
            EnsureFlood();
            using (BattleProfilerMarkers.NavigationNearestReachable.Auto())
            {
                return TryFindNearestReachableCell(position, height, out nearest);
            }
        }

        private bool TryFindNearestReachableCell(Vector3 position, float height, out Vector3 nearest)
        {
            float2 target = new float2(position.x, position.z);
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int cell = 0; cell < _costSoFar.Length; cell++)
            {
                if (float.IsPositiveInfinity(_costSoFar[cell]))
                {
                    continue;
                }

                float distance = math.distancesq(_cells.GetNodePosition(cell), target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = cell;
                }
            }

            if (best < 0)
            {
                nearest = default;
                return false;
            }

            float2 node = _cells.GetNodePosition(best);
            nearest = new Vector3(node.x, height, node.y);
            return true;
        }

        private void EnsureFlood()
        {
            if (_isFlooded)
            {
                return;
            }

            using (BattleProfilerMarkers.NavigationFlood.Auto())
            {
                Flood();
            }
        }

        private void Flood()
        {
            int cellCount = _cells.Count;
            _blocked = new NativeArray<byte>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            new NavigationGridBlockJob
            {
                Obstacles = _obstacles,
                Blocked = _blocked,
                GridOrigin = _cells.Origin,
                CellSize = _cells.CellSize,
                Width = _cells.Width,
                Inflation = _inflation
            }.Schedule(cellCount, BLOCK_JOB_BATCH_SIZE).Complete();

            _costSoFar = new NativeArray<float>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            _cameFrom = new NativeArray<int>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            _rawPath = new NativeList<float2>(64, Allocator.TempJob);
            _path = new NativeList<float2>(16, Allocator.TempJob);
            _status = new NativeArray<int>(1, Allocator.TempJob);
            _isFlooded = true;

            NativeArray<byte> closed = new NativeArray<byte>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeList<NavigationFloodJob.OpenNode> open =
                new NativeList<NavigationFloodJob.OpenNode>(cellCount, Allocator.TempJob);
            try
            {
                new NavigationFloodJob
                {
                    Blocked = _blocked,
                    Cells = _cells,
                    Start = _origin,
                    CostSoFar = _costSoFar,
                    CameFrom = _cameFrom,
                    Closed = closed,
                    Open = open
                }.Schedule().Complete();
            }
            finally
            {
                closed.Dispose();
                open.Dispose();
            }
        }
    }
}
