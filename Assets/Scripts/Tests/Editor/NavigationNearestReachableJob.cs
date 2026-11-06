using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>The cell nearest to <see cref="Target"/> in the ship's region, or -1 when the ship has no open cell.</summary>
    [BurstCompile]
    internal struct NavigationNearestReachableJob : IJob
    {
        [ReadOnly] public NativeArray<byte> Blocked;
        [ReadOnly] public NativeArray<int> Regions;
        public NavigationGridCells Cells;
        public float2 Start;
        public float2 Target;
        public NativeArray<int> Result;

        public void Execute()
        {
            int startCell = Cells.FindNearestWalkableCell(Blocked, Start);
            Result[0] = startCell < 0 ? -1 : Cells.FindNearestCellInRegion(Regions, Regions[startCell], Target);
        }
    }
}
