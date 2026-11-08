namespace EmpireAtWar.Sandbox.Multiplayer
{
    // 10 B movement keyframe: position, heading, speed, turn rate. Clients dead-reckon between keyframes.
    public struct SpikeUnitState
    {
        public ushort Index;
        public short X;
        public short Z;
        public ushort Heading;
        public byte Speed;
        public sbyte TurnRate;
    }
}
