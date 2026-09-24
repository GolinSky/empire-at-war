using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavigationPathJob : IJob
    {
        public const int STATUS_NO_PATH = 0;
        public const int STATUS_FOUND = 1;

        private const float DIAGONAL_COST = 1.41421356f;

        internal struct OpenNode
        {
            public int Cell;
            public float Priority;
        }

        [ReadOnly] public NativeArray<byte> Blocked;
        // Obstacles are packed as (x, z, radius) on the navigation plane.
        [ReadOnly] public NativeArray<float3> Obstacles;
        public float2 GridOrigin;
        public float CellSize;
        public int Width;
        public int Height;
        public float2 Start;
        public float2 Goal;
        public float LineOfSightInflation;

        public NativeArray<float> CostSoFar;
        public NativeArray<int> CameFrom;
        public NativeArray<byte> Closed;
        public NativeList<OpenNode> Open;
        public NativeList<float2> RawPath;

        public NativeList<float2> Waypoints;
        public NativeArray<int> Status;

        public void Execute()
        {
            Waypoints.Clear();
            RawPath.Clear();
            Open.Clear();
            Status[0] = STATUS_NO_PATH;

            int startCell = FindNearestWalkableCell(Start);
            int goalCell = FindNearestWalkableCell(Goal);
            if (startCell < 0 || goalCell < 0 || !Search(startCell, goalCell))
            {
                return;
            }

            RawPath.Add(Goal);
            for (int cell = goalCell; cell >= 0; cell = CameFrom[cell])
            {
                RawPath.Add(GetNodePosition(cell));
            }

            RawPath.Add(Start);
            Reverse(RawPath);
            PullString();
            Status[0] = STATUS_FOUND;
        }

        private bool Search(int startCell, int goalCell)
        {
            for (int i = 0; i < CostSoFar.Length; i++)
            {
                CostSoFar[i] = float.PositiveInfinity;
                CameFrom[i] = -1;
                Closed[i] = 0;
            }

            CostSoFar[startCell] = 0f;
            Push(startCell, Heuristic(startCell, goalCell));
            while (Open.Length > 0)
            {
                int cell = Pop();
                if (Closed[cell] != 0)
                {
                    continue;
                }

                if (cell == goalCell)
                {
                    return true;
                }

                Closed[cell] = 1;
                int x = cell % Width;
                int y = cell / Width;
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
                        if (!IsWalkable(nx, ny))
                        {
                            continue;
                        }

                        bool isDiagonal = dx != 0 && dy != 0;
                        if (isDiagonal && (!IsWalkable(x + dx, y) || !IsWalkable(x, y + dy)))
                        {
                            continue;
                        }

                        int neighbor = ny * Width + nx;
                        float cost = CostSoFar[cell] + (isDiagonal ? DIAGONAL_COST : 1f);
                        if (cost >= CostSoFar[neighbor])
                        {
                            continue;
                        }

                        CostSoFar[neighbor] = cost;
                        CameFrom[neighbor] = cell;
                        Push(neighbor, cost + Heuristic(neighbor, goalCell));
                    }
                }
            }

            return false;
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

        private int FindNearestWalkableCell(float2 position)
        {
            int2 cell = (int2)math.round((position - GridOrigin) / CellSize);
            cell = math.clamp(cell, int2.zero, new int2(Width - 1, Height - 1));
            int index = cell.y * Width + cell.x;
            if (Blocked[index] == 0)
            {
                return index;
            }

            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < Blocked.Length; i++)
            {
                if (Blocked[i] != 0)
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

        private bool IsWalkable(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height &&
                   Blocked[y * Width + x] == 0;
        }

        private float2 GetNodePosition(int cell)
        {
            return GridOrigin + new float2(cell % Width, cell / Width) * CellSize;
        }

        private float Heuristic(int cell, int goalCell)
        {
            float dx = math.abs(cell % Width - goalCell % Width);
            float dy = math.abs(cell / Width - goalCell / Width);
            return math.max(dx, dy) + (DIAGONAL_COST - 1f) * math.min(dx, dy);
        }

        private void Push(int cell, float priority)
        {
            Open.Add(new OpenNode { Cell = cell, Priority = priority });
            int child = Open.Length - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (Open[parent].Priority <= Open[child].Priority)
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
                if (left < Open.Length && Open[left].Priority < Open[smallest].Priority)
                {
                    smallest = left;
                }

                if (right < Open.Length && Open[right].Priority < Open[smallest].Priority)
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
    }
}
