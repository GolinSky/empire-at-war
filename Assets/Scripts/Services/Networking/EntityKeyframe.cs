namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Quantized motion keyframe (14 B): net index, position at 0.25 m (±8192 m; maps are ±4500 m),
    /// 16-bit heading, speed 0.5 m/s, turn rate 1°/s, climb rate 0.25 m/s.
    /// </summary>
    public struct EntityKeyframe
    {
        public ushort Index;
        public short X;
        public short Y;
        public short Z;
        public ushort Heading;
        public byte Speed;
        public sbyte TurnRate;
        public sbyte ClimbRate;
    }
}
