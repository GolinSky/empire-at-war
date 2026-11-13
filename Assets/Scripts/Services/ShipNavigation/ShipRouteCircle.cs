using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal struct ShipRouteCircle
    {
        public float2 Center;
        public float Radius;
        public int Sign;
        public int DiskIndex;
        public float SweepHint;
        public int Previous;
        public int Next;

        public ShipRouteCircle(float2 center, float radius, int sign, int diskIndex = -1)
        {
            Center = center;
            Radius = radius;
            Sign = sign;
            DiskIndex = diskIndex;
            SweepHint = 0f;
            Previous = -1;
            Next = -1;
        }
    }
}
