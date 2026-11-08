using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Quantization used by SpikeUnitState. Range ±4096 m at 0.125 m; heading 0.0055°; speed 0.25 m/s; turn 1°/s.
    public static class SpikeQuantization
    {
        private const float POSITION_STEP = 0.125f;
        private const float SPEED_STEP = 0.25f;
        private const float HEADING_STEPS = 65536f;

        public static short EncodePosition(float value) => (short)Mathf.Clamp(Mathf.RoundToInt(value / POSITION_STEP), short.MinValue, short.MaxValue);
        public static float DecodePosition(short value) => value * POSITION_STEP;

        public static ushort EncodeHeading(float degrees) => (ushort)(Mathf.RoundToInt(Mathf.Repeat(degrees, 360f) / 360f * HEADING_STEPS) & 0xFFFF);
        public static float DecodeHeading(ushort value) => value / HEADING_STEPS * 360f;

        public static byte EncodeSpeed(float metersPerSecond) => (byte)Mathf.Clamp(Mathf.RoundToInt(metersPerSecond / SPEED_STEP), 0, byte.MaxValue);
        public static float DecodeSpeed(byte value) => value * SPEED_STEP;

        public static sbyte EncodeTurnRate(float degreesPerSecond) => (sbyte)Mathf.Clamp(Mathf.RoundToInt(degreesPerSecond), sbyte.MinValue, sbyte.MaxValue);
        public static float DecodeTurnRate(sbyte value) => value;

        public static SpikeUnitState Encode(ushort index, SpikeKinematics kinematics)
        {
            return new SpikeUnitState
            {
                Index = index,
                X = EncodePosition(kinematics.Position.x),
                Z = EncodePosition(kinematics.Position.y),
                Heading = EncodeHeading(kinematics.Heading),
                Speed = EncodeSpeed(kinematics.Speed),
                TurnRate = EncodeTurnRate(kinematics.TurnRate)
            };
        }

        public static SpikeKinematics Decode(SpikeUnitState state)
        {
            return new SpikeKinematics(
                new Vector2(DecodePosition(state.X), DecodePosition(state.Z)),
                DecodeHeading(state.Heading),
                DecodeSpeed(state.Speed),
                DecodeTurnRate(state.TurnRate));
        }
    }
}
