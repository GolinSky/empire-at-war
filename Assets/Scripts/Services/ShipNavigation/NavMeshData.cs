using System;
using andywiecko.BurstTriangulator;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class NavMeshData : IDisposable
    {
        private readonly NavMeshBuckets _layout;
        private NativeList<float2> _positions;
        private NativeList<int> _diskIndices;
        private NativeArray<float3> _disks;
        private NativeParallelMultiHashMap<int, int> _diskBuckets;
        private NativeList<int> _triangles;
        private NativeList<int> _halfedges;
        private NativeArray<int> _regions;
        private NativeParallelMultiHashMap<int, int> _buckets;
        private NativeReference<NavMeshLocator.Result> _location;

        public NativeArray<float2> Positions => _positions.AsArray();
        public NativeArray<int> DiskIndices => _diskIndices.AsArray();
        public NativeArray<float3> Disks => _disks;
        public NativeParallelMultiHashMap<int, int> DiskBuckets => _diskBuckets;
        public NavMeshBuckets Layout => _layout;
        public NativeArray<int> Triangles => _triangles.AsArray();
        public NativeArray<int> Halfedges => _halfedges.AsArray();
        public NativeArray<int> Regions => _regions;
        public NativeParallelMultiHashMap<int, int> TriangleBuckets => _buckets;

        public NavMeshData(float2 min, float2 max, float cellSize, NativeArray<float3> inflatedDisks,
            NativeArray<float2> hazardPoints, NativeParallelMultiHashMap<int, int> diskBuckets)
        {
            _layout = new NavMeshBuckets(min, max);
            try
            {
                using var points = new NativeList<float2>(128, Allocator.TempJob);
                _diskBuckets = diskBuckets;
                _diskIndices = new NativeList<int>(128, Allocator.Persistent);
                _disks = inflatedDisks;
                new NavMeshPointsJob
                {
                    Layout = _layout, CellSize = cellSize, Disks = inflatedDisks, HazardPoints = hazardPoints,
                    DiskBuckets = _diskBuckets, Points = points, DiskIndices = _diskIndices
                }.Run();
                _positions = new NativeList<float2>(points.Length, Allocator.Persistent);
                _triangles = new NativeList<int>(Allocator.Persistent);
                _halfedges = new NativeList<int>(Allocator.Persistent);
                using var status = new NativeReference<Status>(Allocator.TempJob);
                new NavMeshTriangulateJob
                {
                    Points = points.AsArray(), Positions = _positions, Triangles = _triangles,
                    Halfedges = _halfedges, Status = status
                }.Run();
                if (status.Value != Status.OK)
                    throw new InvalidOperationException($"Ship navmesh triangulation failed ({status.Value}, {points.Length} points).");

                _regions = new NativeArray<int>(_triangles.Length / 3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                _buckets = new NativeParallelMultiHashMap<int, int>(_regions.Length, Allocator.Persistent);
                new NavMeshClassifyJob
                {
                    Layout = _layout, Disks = inflatedDisks, DiskBuckets = _diskBuckets,
                    Positions = Positions, Triangles = Triangles, Regions = _regions, TriangleBuckets = _buckets
                }.Run();
                new NavMeshRegionJob { Halfedges = Halfedges, Regions = _regions }.Run();
                _location = new NativeReference<NavMeshLocator.Result>(Allocator.Persistent);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public int FindNearest(float2 point, int region, out float2 nearest, int preferredRegion = 0)
        {
            new NavMeshLocator
            {
                Layout = _layout, Point = point, Region = region, PreferredRegion = preferredRegion,
                Buckets = _buckets, Regions = Regions, Positions = Positions, Triangles = Triangles, Location = _location
            }.Run();
            nearest = _location.Value.Point;
            return _location.Value.Triangle;
        }

        public int FindEscape(float2 origin, NativeArray<float3> obstacles, float clearance, out float2 nearest)
        {
            new NavMeshEscapeJob
            {
                Origin = origin, Obstacles = obstacles, Clearance = clearance,
                Positions = Positions, Triangles = Triangles, Regions = Regions, Location = _location
            }.Run();
            nearest = _location.Value.Point;
            return _location.Value.Triangle;
        }

        public void Dispose()
        {
            if (_location.IsCreated) _location.Dispose();
            if (_buckets.IsCreated) _buckets.Dispose();
            if (_regions.IsCreated) _regions.Dispose();
            if (_halfedges.IsCreated) _halfedges.Dispose();
            if (_triangles.IsCreated) _triangles.Dispose();
            if (_positions.IsCreated) _positions.Dispose();
            if (_diskIndices.IsCreated) _diskIndices.Dispose();
        }
    }
}
