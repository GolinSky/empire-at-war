using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>
    /// Dijkstra flood from the ship: one search yields the shortest path to every
    /// reachable cell, so any number of destinations can be tested and routed without searching again.
    /// </summary>
    [BurstCompile]
    internal struct NavigationFloodJob : IJob
    {
        private const float DIAGONAL_COST = 1.41421356f;

        internal struct OpenNode
        {
            public int Cell;
            public float Cost;
        }

        [ReadOnly] public NativeArray<byte> Blocked;
        public NavigationGridCells Cells;
        public float2 Start;

        public NativeArray<float> CostSoFar;
        public NativeArray<int> CameFrom;
        public NativeArray<byte> Closed;
        public NativeList<OpenNode> Open;

        public void Execute()
        {
            for (int i = 0; i < CostSoFar.Length; i++)
            {
                CostSoFar[i] = float.PositiveInfinity;
                CameFrom[i] = -1;
                Closed[i] = 0;
            }

            Open.Clear();
            int startCell = Cells.FindNearestWalkableCell(Blocked, Start);
            if (startCell < 0)
            {
                return;
            }

            CostSoFar[startCell] = 0f;
            Push(startCell, 0f);
            while (Open.Length > 0)
            {
                int cell = Pop();
                if (Closed[cell] != 0)
                {
                    continue;
                }

                Closed[cell] = 1;
                int x = cell % Cells.Width;
                int y = cell / Cells.Width;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        int nx = x + dx;
                        int ny = y + dy;
                        if (!Cells.IsWalkable(Blocked, nx, ny))
                        {
                            continue;
                        }

                        bool isDiagonal = dx != 0 && dy != 0;
                        if (isDiagonal &&
                            (!Cells.IsWalkable(Blocked, x + dx, y) || !Cells.IsWalkable(Blocked, x, y + dy)))
                        {
                            continue;
                        }

                        int neighbor = ny * Cells.Width + nx;
                        float cost = CostSoFar[cell] + (isDiagonal ? DIAGONAL_COST : 1f);
                        if (cost >= CostSoFar[neighbor])
                        {
                            continue;
                        }

                        CostSoFar[neighbor] = cost;
                        CameFrom[neighbor] = cell;
                        Push(neighbor, cost);
                    }
                }
            }
        }

        private void Push(int cell, float cost)
        {
            Open.Add(new OpenNode { Cell = cell, Cost = cost });
            int child = Open.Length - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (Open[parent].Cost <= Open[child].Cost)
                {
                    break;
                }

                Swap(parent, child);
                child = parent;
            }
        }

        private int Pop()
        {
            int cell = Open[0].Cell;
            int last = Open.Length - 1;
            Open[0] = Open[last];
            Open.RemoveAt(last);
            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = left + 1;
                int smallest = parent;
                if (left < Open.Length && Open[left].Cost < Open[smallest].Cost)
                {
                    smallest = left;
                }

                if (right < Open.Length && Open[right].Cost < Open[smallest].Cost)
                {
                    smallest = right;
                }

                if (smallest == parent)
                {
                    return cell;
                }

                Swap(parent, smallest);
                parent = smallest;
            }
        }

        private void Swap(int first, int second)
        {
            OpenNode temporary = Open[first];
            Open[first] = Open[second];
            Open[second] = temporary;
        }
    }
}
