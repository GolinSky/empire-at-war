using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    public static class EntityKeyframeCodec
    {
        public const int ENCODED_BYTES = 14;
        private const float POSITION_STEP = 0.25f;
        private const float SPEED_STEP = 0.5f;
        private const float CLIMB_STEP = 0.25f;
        private const float HEADING_STEPS = 65536f;

        public static EntityKeyframe Encode(ushort index, EntityMotion motion) => new EntityKeyframe
        {
            Index = index,
            X = ToShort(motion.Position.x / POSITION_STEP),
            Y = ToShort(motion.Position.y / POSITION_STEP),
            Z = ToShort(motion.Position.z / POSITION_STEP),
            Heading = (ushort)(Mathf.RoundToInt(Mathf.Repeat(motion.Heading, 360f) / 360f * HEADING_STEPS) & 0xFFFF),
            Speed = (byte)Mathf.Clamp(Mathf.RoundToInt(motion.Speed / SPEED_STEP), 0, byte.MaxValue),
            TurnRate = ToSByte(motion.TurnRate),
            ClimbRate = ToSByte(motion.ClimbRate / CLIMB_STEP)
        };

        public static EntityMotion Decode(EntityKeyframe keyframe) => new EntityMotion(
            new Vector3(keyframe.X, keyframe.Y, keyframe.Z) * POSITION_STEP,
            keyframe.Heading / HEADING_STEPS * 360f,
            keyframe.Speed * SPEED_STEP,
            keyframe.TurnRate,
            keyframe.ClimbRate * CLIMB_STEP);

        private static short ToShort(float value) =>
            (short)Mathf.Clamp(Mathf.RoundToInt(value), short.MinValue, short.MaxValue);

        private static sbyte ToSByte(float value) =>
            (sbyte)Mathf.Clamp(Mathf.RoundToInt(value), sbyte.MinValue, sbyte.MaxValue);
    }
}
