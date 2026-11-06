using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal readonly struct NavMeshBuckets
    {
        public const int SIDE = 32;
        public readonly float2 Min;
        public readonly float2 Max;
        private readonly float2 _scale;

        public NavMeshBuckets(float2 min, float2 max)
        {
            Min = min;
            Max = max;
            _scale = SIDE / (max - min);
        }

        public int2 Cell(float2 point) =>
            math.clamp((int2)math.floor((point - Min) * _scale), int2.zero, new int2(SIDE - 1));

        public static int Key(int x, int y) => x + SIDE * y;

        public void Insert(NativeParallelMultiHashMap<int, int> buckets, int item, float2 min, float2 max)
        {
            if (math.any(min > Max) || math.any(max < Min)) return;
            int2 first = Cell(min);
            int2 last = Cell(max);
            for (int y = first.y; y <= last.y; y++)
            for (int x = first.x; x <= last.x; x++)
                buckets.Add(Key(x, y), item);
        }
    }
}
