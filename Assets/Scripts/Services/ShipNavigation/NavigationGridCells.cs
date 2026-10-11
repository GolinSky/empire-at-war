using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>Cell layout of the navigation grid, shared by the Burst jobs and the managed planner.</summary>
    internal readonly struct NavigationGridCells
    {
        public float2 Origin { get; }
        public float CellSize { get; }
        public int Width { get; }
        public int Height { get; }
        public int Count => Width * Height;

        public NavigationGridCells(float2 origin, float cellSize, int width, int height)
        {
            Origin = origin;
            CellSize = cellSize;
            Width = width;
            Height = height;
        }

        public float2 GetNodePosition(int cell)
        {
            return Origin + new float2(cell % Width, cell / Width) * CellSize;
        }

        public bool IsWalkable(NativeArray<byte> blocked, int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height &&
                   blocked[y * Width + x] == 0;
        }

        public int GetCell(float2 position)
        {
            int2 cell = (int2)math.round((position - Origin) / CellSize);
            cell = math.clamp(cell, int2.zero, new int2(Width - 1, Height - 1));
            return cell.y * Width + cell.x;
        }

        public int FindNearestWalkableCell(NativeArray<byte> blocked, float2 position)
        {
            int index = GetCell(position);
            if (blocked[index] == 0)
            {
                return index;
            }

            // Rings of growing Chebyshev radius around the clamped cell. A ring can only hold a closer node while
            // its inner edge is nearer than the best found so far, so the search stops long before a full-grid scan.
            // Ties keep the lowest index, which is what scanning every cell in order returns.
            int centerX = index % Width;
            int centerY = index / Width;
            float centerOffset = math.distance(GetNodePosition(index), position);
            int maxRadius = math.max(math.max(centerX, Width - 1 - centerX), math.max(centerY, Height - 1 - centerY));
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                float ringGap = radius * CellSize - centerOffset;
                if (best >= 0 && ringGap > 0f && ringGap * ringGap > bestDistance)
                {
                    break;
                }

                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (y < 0 || y >= Height)
                    {
                        continue;
                    }

                    int step = y == centerY - radius || y == centerY + radius ? 1 : 2 * radius;
                    for (int x = centerX - radius; x <= centerX + radius; x += step)
                    {
                        if (x < 0 || x >= Width)
                        {
                            continue;
                        }

                        int cell = y * Width + x;
                        if (blocked[cell] != 0)
                        {
                            continue;
                        }

                        float distance = math.distancesq(GetNodePosition(cell), position);
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
    }
}
