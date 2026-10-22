using Unity.Mathematics;

namespace EmpireAtWar.Tests.Benchmarks
{
    /// <summary>Layout copy of <c>RadarQuery</c>; this assembly cannot reference game code.</summary>
    internal readonly struct RadarBenchmarkQuery
    {
        public readonly float2 Center;
        public readonly float RangeSquared;
        public readonly int OwnerSlot;

        public RadarBenchmarkQuery(float2 center, float rangeSquared, int ownerSlot)
        {
            Center = center;
            RangeSquared = rangeSquared;
            OwnerSlot = ownerSlot;
        }
    }
}
