using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct NavigationTravelCost
    {
        private readonly NavigationGridCells _cells;
        private readonly NativeArray<float> _extraCost;

        public NavigationTravelCost(NavigationGridCells cells, NativeArray<float> extraCost)
        {
            _cells = cells;
            _extraCost = extraCost;
        }

        public float Segment(float2 from, float2 to)
        {
            float length = math.distance(from, to);
            int samples = math.max(1, (int)math.ceil(length / (_cells.CellSize * 0.5f)));
            float sum = 0f;
            for (int i = 0; i < samples; i++)
            {
                float2 point = math.lerp(from, to, (i + 0.5f) / samples);
                sum += 1f + _extraCost[_cells.GetCell(point)];
            }

            return length * sum / samples;
        }
    }
}
