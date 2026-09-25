using System;
using System.Numerics;

namespace EmpireAtWar.Components.Squadrons.Flight
{
    /// <summary>Loose wedge: the leader in front, wingmen alternating left and right behind it.</summary>
    public static class SquadronFormation
    {
        private const float RANK_DEPTH = 0.8f;
        private const float RANK_STAGGER = 0.12f;

        /// <summary>Local slot offset (X right, Y up, Z forward).</summary>
        public static Vector3 GetSlot(int index, float spacing)
        {
            if (index == 0)
            {
                return Vector3.Zero;
            }

            int rank = (index + 1) / 2;
            float side = index % 2 == 1 ? -1f : 1f;
            float height = (rank % 2 == 0 ? RANK_STAGGER : -RANK_STAGGER) * spacing;
            return new Vector3(side * rank * spacing, height, -rank * spacing * RANK_DEPTH);
        }

        public static Vector3 ToWorld(Vector3 localOffset, Vector3 heading)
        {
            float yaw = MathF.Atan2(heading.X, heading.Z);
            return Vector3.Transform(localOffset, Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw));
        }
    }
}
