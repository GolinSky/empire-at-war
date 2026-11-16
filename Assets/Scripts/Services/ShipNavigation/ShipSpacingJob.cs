using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct ShipSpacingJob : IJobParallelFor
    {
        internal const int MAX_VISITED = 64;
        internal const float SEPARATION_MARGIN = 0.1f;

        [ReadOnly] public NativeArray<ShipSpacingInput> Inputs;
        [ReadOnly] public NativeParallelHashMap<int2, int2> Ranges;
        [ReadOnly] public NativeParallelHashMap<int2, uint> OccupancyHashes;
        [ReadOnly] public NativeArray<int> Occupants;
        [WriteOnly] public NativeArray<ShipSpacingResult> Results;
        public float CellSize;

        public void Execute(int index)
        {
            ShipSpacingInput own = Inputs[index];
            var result = new ShipSpacingResult
            {
                RegistrationId = own.RegistrationId,
                Revision = own.Revision,
                BlockerId = -1,
                BlockerIndex = -1,
                NextCursor = own.Cursor
            };
            if (!own.Eligible)
            {
                Results[index] = result;
                return;
            }

            // CellSize >= twice the largest radius: every penetrating center is in these cells.
            int2 center = (int2)math.floor(own.Position / CellSize);
            var ranges = new FixedList128Bytes<int2>();
            int total = 0;
            for (int z = -1; z <= 1; z++)
            for (int x = -1; x <= 1; x++)
            {
                int2 cell = center + new int2(x, z);
                if (!Ranges.TryGetValue(cell, out int2 range)) continue;
                ranges.Add(range);
                total += range.y;
                // Cell aggregates cover even unvisited occupants when the query saturates.
                result.OccupancyHash ^= math.hash(new uint2(math.hash(cell), OccupancyHashes[cell]));
            }

            if (total == 0)
            {
                Results[index] = result;
                return;
            }

            int start = own.Cursor % total;
            int rangeIndex = 0;
            int offset = start;
            while (offset >= ranges[rangeIndex].y)
                offset -= ranges[rangeIndex++].y;
            int count = math.min(total, MAX_VISITED);
            for (int visited = 0; visited < count; visited++)
            {
                int otherIndex = Occupants[ranges[rangeIndex].x + offset];
                if (++offset == ranges[rangeIndex].y)
                {
                    offset = 0;
                    rangeIndex = (rangeIndex + 1) % ranges.Length;
                }
                if (otherIndex == index) continue;
                ShipSpacingInput other = Inputs[otherIndex];
                if (own.Bottom >= other.Top || other.Bottom >= own.Top) continue;
                float2 separation = own.Position - other.Position;
                float radius = own.Radius + other.Radius;
                float squaredDistance = math.lengthsq(separation);
                if (squaredDistance >= radius * radius) continue;
                result.HasOverlap = true;
                if (other.Eligible && !other.Anchored && own.RegistrationId < other.RegistrationId) continue;
                float distance = math.sqrt(squaredDistance);
                float penetration = radius - distance;
                if (result.HasCorrection && (penetration < result.Penetration ||
                    penetration == result.Penetration && other.RegistrationId > result.BlockerId)) continue;
                float2 direction = distance > 0.0001f
                    ? separation / distance
                    : CoincidentDirection(own.RegistrationId, other.RegistrationId);
                result.HasCorrection = true;
                result.BlockerId = other.RegistrationId;
                result.BlockerIndex = otherIndex;
                result.Penetration = penetration;
                result.Goal = other.Position + direction * (radius + SEPARATION_MARGIN);
            }

            result.Visited = count;
            result.Saturated = total > count;
            result.NextCursor = (start + count) % total;
            Results[index] = result;
        }

        internal static uint HashOccupant(ShipSpacingInput input)
        {
            uint footprint = math.hash(new int3(input.RegistrationId,
                (int)math.floor(input.Position.x * 10f),
                (int)math.floor(input.Position.y * 10f)));
            return math.hash(new uint4(footprint,
                math.asuint(input.Radius), math.asuint(input.Bottom), math.asuint(input.Top)));
        }

        private static float2 CoincidentDirection(int ownId, int otherId)
        {
            uint hash = math.hash(new int2(math.min(ownId, otherId), math.max(ownId, otherId)));
            float angle = (hash & 65535u) * (2f * math.PI / 65536f);
            math.sincos(angle, out float sine, out float cosine);
            return new float2(cosine, sine) * (ownId > otherId ? 1f : -1f);
        }
    }
}
