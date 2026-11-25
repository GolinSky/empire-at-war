using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal struct ShipSpacingInput
    {
        public int RegistrationId;
        public int Revision;
        public float2 Position;
        // Hold target and planar heading: the nudge slides around the target and never turns the stern to it.
        public float2 TargetPosition;
        public float2 Forward;
        public float Radius;
        public float Bottom;
        public float Top;
        public bool Eligible;
        public bool Moving;
        public bool Anchored;
        public int Cursor;
    }
}
