using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshHazardBoundaryJob : IJob
    {
        public NavigationGridCells Cells;
        [ReadOnly] public NativeArray<float> Costs;
        public NativeList<float2> Points;

        public void Execute()
        {
            // Raster samples own the square up to the midline between adjacent nodes.
            // Put vertices on both ends of a transition and on its two sides so the
            // triangulation can follow the outline even when the hazard is traversable.
            float halfCell = Cells.CellSize * 0.5f;
            for (int i = 0; i < Costs.Length; i++)
            {
                float2 p = Cells.GetNodePosition(i);
                if (i % Cells.Width + 1 < Cells.Width && Costs[i] != Costs[i + 1])
                {
                    Points.Add(p + new float2(halfCell, -halfCell));
                    Points.Add(p + new float2(halfCell, 0f));
                    Points.Add(p + new float2(halfCell, halfCell));
                    Points.Add(p);
                    Points.Add(Cells.GetNodePosition(i + 1));
                }
                if (i + Cells.Width < Costs.Length && Costs[i] != Costs[i + Cells.Width])
                {
                    Points.Add(p + new float2(-halfCell, halfCell));
                    Points.Add(p + new float2(0f, halfCell));
                    Points.Add(p + new float2(halfCell, halfCell));
                    Points.Add(p);
                    Points.Add(Cells.GetNodePosition(i + Cells.Width));
                }
            }
        }
    }
}
