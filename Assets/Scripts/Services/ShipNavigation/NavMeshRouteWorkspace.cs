using System;
using EmpireAtWar.Services.Timing;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class NavMeshRouteWorkspace : IDisposable
    {
        private NativeArray<NavMeshRouteJob.SearchNode> _nodes;
        private NativeArray<int> _corridorGenerations;
        private NativeList<NavMeshRouteJob.HeapEntry> _open;
        private NativeList<int> _corridor;
        private NativeList<float2> _waypoints;
        private NativeList<float3> _cornerTags;
        private int _generation;

        public NativeList<float2> Waypoints => _waypoints;
        public NativeList<float3> CornerTags => _cornerTags;

        public NavMeshRouteWorkspace(int triangleCount)
        {
            try
            {
                _nodes = new NativeArray<NavMeshRouteJob.SearchNode>(triangleCount * 3 + 2, Allocator.Persistent);
                _corridorGenerations = new NativeArray<int>(triangleCount, Allocator.Persistent);
                _open = new NativeList<NavMeshRouteJob.HeapEntry>(Allocator.Persistent);
                _corridor = new NativeList<int>(Allocator.Persistent);
                _waypoints = new NativeList<float2>(Allocator.Persistent);
                _cornerTags = new NativeList<float3>(Allocator.Persistent);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public bool TryFindPath(NavMeshData mesh, int startTriangle, int goalTriangle, float2 start, float2 goal,
            NavigationGridCells cells, NativeArray<float> extraCost, bool hasExtraCost)
        {
            if (_generation == int.MaxValue)
            {
                for (int i = 0; i < _nodes.Length; i++)
                    _nodes[i] = default;
                for (int i = 0; i < _corridorGenerations.Length; i++)
                    _corridorGenerations[i] = 0;
                _generation = 0;
            }

            NavMeshRouteJob job = new NavMeshRouteJob
            {
                Positions = mesh.Positions, DiskIndices = mesh.DiskIndices, Triangles = mesh.Triangles, Halfedges = mesh.Halfedges,
                Regions = mesh.Regions, StartTriangle = startTriangle, GoalTriangle = goalTriangle,
                Start = start, Goal = goal, Cells = cells, ExtraCost = extraCost, HasExtraCost = hasExtraCost,
                Generation = ++_generation, Nodes = _nodes, CorridorGenerations = _corridorGenerations,
                Open = _open, Corridor = _corridor, Waypoints = _waypoints, CornerTags = _cornerTags
            };
            using (BattleProfilerMarkers.NavigationPathSearch.Auto())
                job.Schedule().Complete();
            return _waypoints.Length != 0;
        }

        public void Dispose()
        {
            if (_nodes.IsCreated) _nodes.Dispose();
            if (_corridorGenerations.IsCreated) _corridorGenerations.Dispose();
            if (_open.IsCreated) _open.Dispose();
            if (_corridor.IsCreated) _corridor.Dispose();
            if (_waypoints.IsCreated) _waypoints.Dispose();
            if (_cornerTags.IsCreated) _cornerTags.Dispose();
        }
    }
}
