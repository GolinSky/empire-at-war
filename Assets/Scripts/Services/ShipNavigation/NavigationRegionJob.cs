using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>
    /// Labels every walkable cell with its connected region (blocked cells get -1). Two cells are mutually
    /// reachable exactly when their labels match, so reachability needs no search. Connectivity matches
    /// <see cref="NavigationRouteJob"/>: eight neighbours, diagonals only when both orthogonal cells are open.
    /// </summary>
    [BurstCompile]
    internal struct NavigationRegionJob : IJob
    {
        public const int BLOCKED_REGION = -1;
        private const int UNLABELED = -2;

        [ReadOnly] public NativeArray<byte> Blocked;
        public NavigationGridCells Cells;
        public NativeArray<int> Regions;
        public NativeArray<int> Queue;

        public void Execute()
        {
            for (int i = 0; i < Regions.Length; i++)
            {
                Regions[i] = Blocked[i] == 0 ? UNLABELED : BLOCKED_REGION;
            }

            int label = 0;
            for (int seed = 0; seed < Regions.Length; seed++)
            {
                if (Regions[seed] != UNLABELED)
                {
                    continue;
                }

                Fill(seed, label);
                label++;
            }
        }

        private void Fill(int seed, int label)
        {
            int head = 0;
            int tail = 0;
            Regions[seed] = label;
            Queue[tail++] = seed;
            while (head < tail)
            {
                int cell = Queue[head++];
                int x = cell % Cells.Width;
                int y = cell / Cells.Width;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0 || !Cells.IsWalkable(Blocked, x + dx, y + dy))
                        {
                            continue;
                        }

                        if (dx != 0 && dy != 0 &&
                            (!Cells.IsWalkable(Blocked, x + dx, y) || !Cells.IsWalkable(Blocked, x, y + dy)))
                        {
                            continue;
                        }

                        int neighbor = (y + dy) * Cells.Width + x + dx;
                        if (Regions[neighbor] != UNLABELED)
                        {
                            continue;
                        }

                        Regions[neighbor] = label;
                        Queue[tail++] = neighbor;
                    }
                }
            }
        }
    }
}
