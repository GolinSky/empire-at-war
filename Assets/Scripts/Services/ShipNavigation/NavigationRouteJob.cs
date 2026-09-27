using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>Extracts one route from a finished <see cref="NavigationFloodJob"/> and simplifies it.</summary>
    [BurstCompile]
    internal struct NavigationRouteJob : IJob
    {
        public const int STATUS_NO_PATH = 0;
        public const int STATUS_FOUND = 1;

        [ReadOnly] public NativeArray<byte> Blocked;
        // Obstacles are packed as (x, z, radius) on the navigation plane.
        [ReadOnly] public NativeArray<float3> Obstacles;
        [ReadOnly] public NativeArray<float> CostSoFar;
        [ReadOnly] public NativeArray<int> CameFrom;
        public NavigationGridCells Cells;
        public float2 Start;
        public float2 Goal;
        public float LineOfSightInflation;

        public NativeList<float2> RawPath;
        public NativeList<float2> Waypoints;
        public NativeArray<int> Status;

        public void Execute()
        {
            Waypoints.Clear();
            RawPath.Clear();
            Status[0] = STATUS_NO_PATH;

            int goalCell = Cells.FindNearestWalkableCell(Blocked, Goal);
            if (goalCell < 0 || float.IsPositiveInfinity(CostSoFar[goalCell]))
            {
                return;
            }

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
