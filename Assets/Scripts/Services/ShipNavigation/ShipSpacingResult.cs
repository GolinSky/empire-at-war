using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal struct ShipSpacingResult
    {
        public int RegistrationId;
        public int Revision;
        public int BlockerId;
        public int BlockerIndex;
        public float2 Goal;
        public float Penetration;
        public bool HasOverlap;
        public bool HasCorrection;
        public int Visited;
        public bool Saturated;
        public int NextCursor;
        public uint OccupancyHash;
    }
}
