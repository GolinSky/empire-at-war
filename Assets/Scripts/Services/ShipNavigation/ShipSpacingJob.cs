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
        // Overlap below this share of the pair's contact distance is left alone: navigation circles are larger
        // than the hulls, and nudging held ships for a few metres moved them out of their firing positions.
        internal const float MIN_PENETRATION_FRACTION = 0.1f;
        // A bow-on holder needs ~90° for any sideways slide; larger turns are refused and the pair stays overlapped.
        internal const float MAX_SETTLE_TURN_DEGREES = 100f;

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
            if (!own.Eligible || own.Moving)
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
                if (other.Moving) continue;
                if (own.Bottom >= other.Top || other.Bottom >= own.Top) continue;
                float2 separation = own.Position - other.Position;
                float radius = own.Radius + other.Radius;
                float squaredDistance = math.lengthsq(separation);
                if (squaredDistance >= radius * radius) continue;
                result.HasOverlap = true;
                if (other.Eligible && !other.Anchored && own.RegistrationId < other.RegistrationId) continue;
                float distance = math.sqrt(squaredDistance);
                float penetration = radius - distance;
                if (penetration < radius * MIN_PENETRATION_FRACTION) continue;
                if (result.HasCorrection && (penetration < result.Penetration ||
                    penetration == result.Penetration && other.RegistrationId > result.BlockerId)) continue;
                result.HasCorrection = true;
                result.BlockerId = other.RegistrationId;
                result.BlockerIndex = otherIndex;
                result.Penetration = penetration;
                result.Goal = SlideGoal(own, other, separation, distance, radius + SEPARATION_MARGIN);
            }

            // Settling flies the hop as a forward route: a goal behind the ship turns its stern to the target.
            if (result.HasCorrection &&
                math.dot(math.normalize(result.Goal - own.Position), own.Forward) <
                    math.cos(math.radians(MAX_SETTLE_TURN_DEGREES)))
                result.HasCorrection = false;

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
            if (input.Moving) footprint ^= 0x9e3779b9u;
            return math.hash(new uint4(footprint,
                math.asuint(input.Radius), math.asuint(input.Bottom), math.asuint(input.Top)));
        }

        // Slides sideways around the hold target until the pair clears, keeping range and the target abeam or ahead.
        private static float2 SlideGoal(ShipSpacingInput own, ShipSpacingInput other, float2 separation,
            float distance, float clearance)
        {
            float2 fromTarget = own.Position - own.TargetPosition;
            if (math.lengthsq(fromTarget) < 0.0001f)
            {
                float2 away = distance > 0.0001f
                    ? separation / distance
                    : CoincidentDirection(own.RegistrationId, other.RegistrationId);
                return other.Position + away * clearance;
            }
            float2 radial = math.normalize(fromTarget);
            float2 tangent = new float2(-radial.y, radial.x);
            // Away from the blocker's side; a blocker on the ship–target line falls back to heading, then ids.
            float side = math.dot(tangent, separation);
            if (math.abs(side) < 0.0001f) side = math.dot(tangent, own.Forward);
            if (math.abs(side) < 0.0001f)
                side = math.dot(tangent, CoincidentDirection(own.RegistrationId, other.RegistrationId));
            if (side < 0f) tangent = -tangent;
            float along = math.dot(separation, tangent);
            float step = -along + math.sqrt(along * along + clearance * clearance - math.lengthsq(separation));
            return own.Position + tangent * step;
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
