using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.SkirmishCamera;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    // Owns the native buffers of one planning pass. All methods run on the main
    // thread; every scheduled job is completed before its buffers are read or
    // disposed, and every allocation is released in Dispose within the same frame.
    internal sealed class ShipPathGrid : IDisposable
    {
        private const float MINIMUM_CELL_SIZE = 4f;
        private const int MAXIMUM_CELLS_PER_AXIS = 160;
        private const int BLOCK_JOB_BATCH_SIZE = 256;
        private const float HALF_DIAGONAL_FACTOR = 0.7072f;

        private readonly float2 _gridOrigin;
        private readonly float _cellSize;
        private readonly int _width;
        private readonly int _height;
        private readonly float _clearance;
        private NativeArray<float3> _obstacles;
        private NativeArray<byte> _blocked;
        private bool _isBlockedGridBuilt;

        public ShipPathGrid(
            IReadOnlyList<RadarContact> obstacles,
            float clearance,
            Vector2Range mapRange)
        {
            Vector2 size = mapRange.Max - mapRange.Min;
            _cellSize = Mathf.Max(
                MINIMUM_CELL_SIZE,
                Mathf.Max(size.x, size.y) / MAXIMUM_CELLS_PER_AXIS);
            _gridOrigin = new float2(mapRange.Min.x, mapRange.Min.y);
            _width = Mathf.CeilToInt(size.x / _cellSize) + 1;
            _height = Mathf.CeilToInt(size.y / _cellSize) + 1;
            _clearance = clearance;
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

        public bool TryFindPath(
            Vector3 origin,
            Vector3 destination,
            float height,
            List<Vector3> waypoints)
        {
            EnsureBlockedGrid();
            int cellCount = _width * _height;
            NativeArray<float> costSoFar = new NativeArray<float>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> cameFrom = new NativeArray<int>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<byte> closed = new NativeArray<byte>(
                cellCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeList<NavigationPathJob.OpenNode> open =
                new NativeList<NavigationPathJob.OpenNode>(cellCount, Allocator.TempJob);
            NativeList<float2> rawPath = new NativeList<float2>(64, Allocator.TempJob);
            NativeList<float2> path = new NativeList<float2>(16, Allocator.TempJob);
            NativeArray<int> status = new NativeArray<int>(1, Allocator.TempJob);
            try
            {
                NavigationPathJob job = new NavigationPathJob
                {
                    Blocked = _blocked,
                    Obstacles = _obstacles,
                    GridOrigin = _gridOrigin,
                    CellSize = _cellSize,
                    Width = _width,
                    Height = _height,
                    Start = new float2(origin.x, origin.z),
                    Goal = new float2(destination.x, destination.z),
                    LineOfSightInflation = _clearance + _cellSize * HALF_DIAGONAL_FACTOR,
                    CostSoFar = costSoFar,
                    CameFrom = cameFrom,
                    Closed = closed,
                    Open = open,
                    RawPath = rawPath,
                    Waypoints = path,
                    Status = status
                };
                // Same-frame contract: the caller needs the route now, so the
                // job is completed immediately (Burst still runs the search).
                job.Schedule().Complete();

                waypoints.Clear();
                if (status[0] != NavigationPathJob.STATUS_FOUND)
                {
                    return false;
                }

                for (int i = 0; i < path.Length; i++)
                {
                    waypoints.Add(new Vector3(path[i].x, height, path[i].y));
                }

                return true;
            }
            finally
            {
                costSoFar.Dispose();
                cameFrom.Dispose();
                closed.Dispose();
                open.Dispose();
                rawPath.Dispose();
                path.Dispose();
                status.Dispose();
            }
        }

        public void Dispose()
        {
            if (_blocked.IsCreated)
            {
                _blocked.Dispose();
            }

            if (_obstacles.IsCreated)
            {
                _obstacles.Dispose();
            }
        }

        private void EnsureBlockedGrid()
        {
            if (_isBlockedGridBuilt)
            {
                return;
            }

            _blocked = new NativeArray<byte>(
                _width * _height, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            new NavigationGridBlockJob
            {
                Obstacles = _obstacles,
                Blocked = _blocked,
                GridOrigin = _gridOrigin,
                CellSize = _cellSize,
                Width = _width,
                // Inflating by half a cell diagonal guarantees that the straight
                // segment between two adjacent walkable nodes keeps full clearance.
                Inflation = _clearance + _cellSize * HALF_DIAGONAL_FACTOR
            }.Schedule(_blocked.Length, BLOCK_JOB_BATCH_SIZE).Complete();
            _isBlockedGridBuilt = true;
        }
    }
}
