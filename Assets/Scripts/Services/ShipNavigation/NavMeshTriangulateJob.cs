using andywiecko.BurstTriangulator;
using andywiecko.BurstTriangulator.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshTriangulateJob : IJob
    {
        [ReadOnly] public NativeArray<float2> Points;
        public NativeList<float2> Positions;
        public NativeList<int> Triangles;
        public NativeList<int> Halfedges;
        public NativeReference<Status> Status;

        public void Execute()
        {
            new UnsafeTriangulator<float2>().Triangulate(
                new NativeInputData<float2> { Positions = Points },
                new NativeOutputData<float2>
                {
                    Positions = Positions,
                    Triangles = Triangles,
                    Halfedges = Halfedges,
                    Status = Status
                },
                // Points are deduplicated by the builder. The package's optional validation
                // compares every pair of points; the triangulation itself still reports failures.
                Args.Default(validateInput: false, verbose: false),
                Allocator.Temp);
        }
    }
}
