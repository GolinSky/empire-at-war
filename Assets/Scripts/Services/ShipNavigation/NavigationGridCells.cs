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

            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < blocked.Length; i++)
            {
                if (blocked[i] != 0)
                {
                    continue;
                }

                float distance = math.distancesq(GetNodePosition(i), position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
