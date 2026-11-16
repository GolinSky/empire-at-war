using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal struct ShipSpacingInput
    {
        public int RegistrationId;
        public int Revision;
        public float2 Position;
        public float Radius;
        public float Bottom;
        public float Top;
        public bool Eligible;
        public bool Anchored;
        public int Cursor;
    }
}
