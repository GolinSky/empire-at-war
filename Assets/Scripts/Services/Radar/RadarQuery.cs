using Unity.Mathematics;

namespace EmpireAtWar.Services.Radar
{
    internal readonly struct RadarQuery
    {
        // Planar (x, z) centre: radar ranges ignore height tiers.
        public readonly float2 Center;
        public readonly float RangeSquared;
        public readonly int OwnerSlot;

        public RadarQuery(float2 center, float rangeSquared, int ownerSlot)
        {
            Center = center;
            RangeSquared = rangeSquared;
            OwnerSlot = ownerSlot;
        }
    }
}
