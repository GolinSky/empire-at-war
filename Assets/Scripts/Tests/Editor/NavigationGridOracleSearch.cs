using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal static class NavigationGridOracleSearch
    {
        public static bool IsWalkable(this NavigationGridCells cells, NativeArray<byte> blocked, int x, int y)
        {
            return x >= 0 && y >= 0 && x < cells.Width && y < cells.Height &&
                   blocked[y * cells.Width + x] == 0;
        }

        public static int FindNearestWalkableCell(this NavigationGridCells cells, NativeArray<byte> blocked, float2 position)
        {
            return FindNearestCell(cells, new WalkableCells { Blocked = blocked }, position);
        }

        /// <summary>The node nearest to <paramref name="position"/> whose region label is <paramref name="label"/>.</summary>
        public static int FindNearestCellInRegion(this NavigationGridCells cells, NativeArray<int> regions, int label, float2 position)
        {
            return FindNearestCell(cells, new RegionCells { Regions = regions, Label = label }, position);
        }

        private static int FindNearestCell<TFilter>(NavigationGridCells cells, TFilter filter, float2 position) where TFilter : struct, ICellFilter
        {
            int index = cells.GetCell(position);
            if (filter.Accepts(index))
            {
                return index;
            }

            // Rings of growing Chebyshev radius around the clamped cell. A ring can only hold a closer node while
            // its inner edge is nearer than the best found so far, so the search stops long before a full-grid scan.
            // Ties keep the lowest index, which is what scanning every cell in order returns.
            int centerX = index % cells.Width;
            int centerY = index / cells.Width;
            float centerOffset = math.distance(cells.GetNodePosition(index), position);
            int maxRadius = math.max(math.max(centerX, cells.Width - 1 - centerX), math.max(centerY, cells.Height - 1 - centerY));
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                float ringGap = radius * cells.CellSize - centerOffset;
                if (best >= 0 && ringGap > 0f && ringGap * ringGap > bestDistance)
                {
                    break;
                }

                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (y < 0 || y >= cells.Height)
                    {
                        continue;
                    }

                    int step = y == centerY - radius || y == centerY + radius ? 1 : 2 * radius;
                    for (int x = centerX - radius; x <= centerX + radius; x += step)
                    {
                        if (x < 0 || x >= cells.Width)
                        {
                            continue;
                        }

                        int cell = y * cells.Width + x;
                        if (!filter.Accepts(cell))
                        {
                            continue;
                        }

                        float distance = math.distancesq(cells.GetNodePosition(cell), position);
                        if (distance < bestDistance || distance == bestDistance && cell < best)
                        {
                            bestDistance = distance;
                            best = cell;
                        }
                    }
                }
            }

            return best;
        }

        private interface ICellFilter
        {
            bool Accepts(int cell);
        }

        private struct WalkableCells : ICellFilter
        {
            public NativeArray<byte> Blocked;

            public bool Accepts(int cell) => Blocked[cell] == 0;
        }

        private struct RegionCells : ICellFilter
        {
            public NativeArray<int> Regions;
            public int Label;

            public bool Accepts(int cell) => Regions[cell] == Label;
        }
    }
}
