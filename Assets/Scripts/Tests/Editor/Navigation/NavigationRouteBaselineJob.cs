// Frozen pre-optimization kernel for exact route parity.
using EmpireAtWar.Services.ShipNavigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// A* from the ship's cell to the goal's cell, then line-of-sight simplification. A goal in another
    /// region (see <see cref="NavigationRegionJob"/>) fails before any search.
    /// </summary>
    [BurstCompile]
    internal struct NavigationRouteBaselineJob : IJob
    {
        public const int STATUS_NO_PATH = 0;
        public const int STATUS_FOUND = 1;
        private const float DIAGONAL_COST = 1.41421356f;

        [ReadOnly] public NativeArray<byte> Blocked;
        [ReadOnly] public NativeArray<int> Regions;
        // Obstacles are packed as (x, z, radius) on the navigation plane.
        [ReadOnly] public NativeArray<float3> Obstacles;
        public NativeArray<float> CostSoFar;
        public NativeArray<int> CameFrom;
        public NativeArray<byte> Closed;
        public NativeList<OpenNode> Open;
        public NavigationGridCells Cells;
        public float2 Start;
        public float2 Goal;
        public NativeList<float2> RawPath;
        public NativeList<float2> Waypoints;
        public NativeArray<int> Status;

        public float LineOfSightInflation;

        public void Execute()
        {
            Waypoints.Clear();
            RawPath.Clear();
            Status[0] = STATUS_NO_PATH;

            int startCell = Cells.FindNearestWalkableCell(Blocked, Start);
            int goalCell = Cells.FindNearestWalkableCell(Blocked, Goal);
            if (startCell < 0 || goalCell < 0 || Regions[startCell] != Regions[goalCell])
            {
                return;
            }

            Search(startCell, goalCell);
            RawPath.Add(Goal);
            for (int cell = goalCell; cell >= 0; cell = CameFrom[cell])
            {
                RawPath.Add(Cells.GetNodePosition(cell));
            }

            RawPath.Add(Start);
            Reverse(RawPath);
            PullString();
            Status[0] = STATUS_FOUND;
        }

        // The goal shares the start's region, so the search always reaches it.
        private void Search(int startCell, int goalCell)
        {
            for (int i = 0; i < CostSoFar.Length; i++)
            {
                CostSoFar[i] = float.PositiveInfinity;
                CameFrom[i] = -1;
                Closed[i] = 0;
            }

            Open.Clear();
            int goalX = goalCell % Cells.Width;
            int goalY = goalCell / Cells.Width;
            CostSoFar[startCell] = 0f;
            Push(startCell, Heuristic(startCell, goalX, goalY));
            while (Open.Length > 0)
            {
                int cell = Pop();
                if (cell == goalCell)
                {
                    return;
                }

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
                        Push(neighbor, cost + Heuristic(neighbor, goalX, goalY));
                    }
                }
            }
        }

        // Octile distance in cells: consistent with the step costs, so the first goal pop is a shortest path.
        private float Heuristic(int cell, int goalX, int goalY)
        {
            int dx = math.abs(cell % Cells.Width - goalX);
            int dy = math.abs(cell / Cells.Width - goalY);
            return math.max(dx, dy) + (DIAGONAL_COST - 1f) * math.min(dx, dy);
        }

        // Greedy line-of-sight simplification of the raw cell path. Adjacent
        // entries are always accepted, so the result never skips a required cell.
        private void PullString()
        {
            int anchor = 0;
            int last = RawPath.Length - 1;
            Waypoints.Add(RawPath[0]);
            while (anchor < last)
            {
                int next = anchor + 1;
                while (next < last && HasLineOfSight(RawPath[anchor], RawPath[next + 1]))
                {
                    next++;
                }

                Waypoints.Add(RawPath[next]);
                anchor = next;
            }
        }

        private bool HasLineOfSight(float2 from, float2 to)
        {
            float2 segment = to - from;
            float lengthSquared = math.lengthsq(segment);
            for (int i = 0; i < Obstacles.Length; i++)
            {
                float3 obstacle = Obstacles[i];
                float parameter = lengthSquared <= math.EPSILON
                    ? 0f
                    : math.saturate(math.dot(obstacle.xy - from, segment) / lengthSquared);
                float safeRadius = obstacle.z + LineOfSightInflation;
                if (math.distancesq(obstacle.xy, from + segment * parameter) <
                    safeRadius * safeRadius)
                {
                    return false;
                }
            }

            return true;
        }

        private void Push(int cell, float priority)
        {
            Open.Add(new OpenNode { Cell = cell, Cost = priority });
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

        private static void Reverse(NativeList<float2> list)
        {
            for (int i = 0, j = list.Length - 1; i < j; i++, j--)
            {
                float2 temporary = list[i];
                list[i] = list[j];
                list[j] = temporary;
            }
        }

        internal struct OpenNode
        {
            public float Cost;

            public int Cell;
        }
    }
}
