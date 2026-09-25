using System;
using System.Numerics;

namespace EmpireAtWar.Components.Squadrons.Flight
{
    /// <summary>
    /// Attack-pass cycle of one fighter: approach the aim point, fly past it, extend away on a veering
    /// course and turn back for the next pass. Fighters never stop to shoot.
    /// </summary>
    public sealed class FighterManeuver
    {
        private const float MIN_BREAK_ANGLE = 20f;
        private const float MAX_BREAK_ANGLE = 55f;
        private const float MAX_BREAK_CLIMB = 0.25f;
        private const float EXTEND_SPEED_MULTIPLIER = 1.15f;
        private const float EXTEND_LOOKAHEAD = 40f;
        private const float APPROACH_OFFSET_SHARE = 0.5f;
        private const float BREAK_RADIUS_SHARE = 0.35f;
        private const float DEG_TO_RAD = MathF.PI / 180f;

        private readonly Random _random;
        private Vector3 _approachOffset;
        private Vector3 _extendDirection;

        public FighterManeuverPhase Phase { get; private set; } = FighterManeuverPhase.Approach;

        public FighterManeuver(int seed)
        {
            _random = new Random(seed);
        }

        public void Reset(float spacing)
        {
            Phase = FighterManeuverPhase.Approach;
            PickApproachOffset(spacing);
        }

        public Vector3 Resolve(Vector3 position, Vector3 forward, Vector3 aimPoint, float targetRadius,
            IFighterFlightData data, out float speed)
        {
            float distance = Vector3.Distance(position, aimPoint);
            if (Phase == FighterManeuverPhase.Approach &&
                distance <= data.BreakDistance + targetRadius * BREAK_RADIUS_SHARE)
            {
                BeginExtend(forward);
            }
            else if (Phase == FighterManeuverPhase.Extend && distance >= data.ExtendDistance + targetRadius)
            {
                Phase = FighterManeuverPhase.Approach;
                PickApproachOffset(data.FormationSpacing + targetRadius * APPROACH_OFFSET_SHARE);
            }

            if (Phase == FighterManeuverPhase.Extend)
            {
                speed = data.CombatSpeed * EXTEND_SPEED_MULTIPLIER;
                return position + _extendDirection * EXTEND_LOOKAHEAD;
            }

            speed = data.CombatSpeed;
            float offsetShare = Math.Clamp((distance - targetRadius) / data.ExtendDistance, 0f, 1f);
            return aimPoint + _approachOffset * offsetShare;
        }

        private void BeginExtend(Vector3 forward)
        {
            Phase = FighterManeuverPhase.Extend;
            float side = _random.Next(2) == 0 ? -1f : 1f;
            float angle = side * Lerp(MIN_BREAK_ANGLE, MAX_BREAK_ANGLE, (float)_random.NextDouble()) * DEG_TO_RAD;
            Vector3 flat = new Vector3(forward.X, 0f, forward.Z);
            flat = flat.LengthSquared() > 1e-6f ? Vector3.Normalize(flat) : Vector3.UnitZ;
            Vector3 veer = Vector3.Transform(flat, Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle));
            veer.Y = Lerp(-MAX_BREAK_CLIMB, MAX_BREAK_CLIMB, (float)_random.NextDouble());
            _extendDirection = Vector3.Normalize(veer);
        }

        private void PickApproachOffset(float radius)
        {
            float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
            _approachOffset = new Vector3(MathF.Cos(angle), 0.3f * MathF.Sin(angle * 2f), MathF.Sin(angle)) * radius;
        }

        private static float Lerp(float from, float to, float t) => from + (to - from) * t;
    }
}
