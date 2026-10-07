using System;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Compares forces by how long each needs to destroy the other. Only ratios leave this class,
    /// so the absolute scale of hull, shields and damage does not matter.
    /// </summary>
    public static class CombatMatchup
    {
        /// <summary>Kill time used when a force cannot hurt a class at all.</summary>
        public const float MAX_TIME = 1e6f;
        public const float MIN_ADVANTAGE = 0.01f;
        public const float MAX_ADVANTAGE = 100f;

        /// <summary>
        /// Seconds <paramref name="attacker"/> needs to destroy <paramref name="defender"/> when it focuses one class
        /// at a time with all weapons that can hurt it. Classes it cannot hurt stretch the time by their share of the
        /// defender's hull and shields instead of making the defender unbeatable.
        /// </summary>
        public static float TimeToDestroy(ForceComposition attacker, ForceComposition defender)
        {
            float killableTime = 0f;
            float killableDurability = 0f;
            float totalDurability = 0f;
            foreach (ShipClass target in ForceComposition.Classes)
            {
                float hull = defender.Hull(target);
                if (hull <= 0f)
                {
                    continue;
                }

                float shields = defender.Shields(target);
                totalDurability += hull + shields;
                float time = TimeToDestroyClass(
                    hull,
                    shields,
                    attacker.HullDps(target),
                    attacker.ShieldDps(target),
                    attacker.PiercingDps(target));
                if (time >= MAX_TIME)
                {
                    continue;
                }

                killableTime += time;
                killableDurability += hull + shields;
            }

            if (killableDurability <= 0f)
            {
                return totalDurability > 0f ? MAX_TIME : 0f;
            }

            return Math.Min(MAX_TIME, killableTime * totalDurability / killableDurability);
        }

        /// <summary>
        /// How much stronger <paramref name="own"/> is than <paramref name="hostile"/>: 1 is an even fight,
        /// 2 is roughly twice the equivalent force. Clamped to [<see cref="MIN_ADVANTAGE"/>, <see cref="MAX_ADVANTAGE"/>].
        /// </summary>
        public static float Advantage(ForceComposition own, ForceComposition hostile)
        {
            if (hostile.IsEmpty)
            {
                return MAX_ADVANTAGE;
            }

            if (own.IsEmpty)
            {
                return MIN_ADVANTAGE;
            }

            float ownSurvival = TimeToDestroy(hostile, own);
            float hostileSurvival = TimeToDestroy(own, hostile);
            // Kill times scale with the square of force size (Lanchester), so the root reads as a force ratio.
            float advantage = (float)Math.Sqrt(ownSurvival / Math.Max(hostileSurvival, float.Epsilon));
            return Math.Min(MAX_ADVANTAGE, Math.Max(MIN_ADVANTAGE, advantage));
        }

        /// <summary>
        /// Shields absorb shield-blocked weapons while piercing weapons already strip the hull;
        /// once shields fall, every weapon hits the hull.
        /// </summary>
        private static float TimeToDestroyClass(
            float hull,
            float shields,
            float hullDps,
            float shieldDps,
            float piercingDps)
        {
            if (shields <= 0f || shieldDps <= 0f)
            {
                float dps = shields > 0f ? piercingDps : hullDps + piercingDps;
                return dps > 0f ? Math.Min(MAX_TIME, hull / dps) : MAX_TIME;
            }

            float shieldTime = shields / shieldDps;
            if (piercingDps * shieldTime >= hull)
            {
                return hull / piercingDps;
            }

            float remainingHull = hull - piercingDps * shieldTime;
            return Math.Min(MAX_TIME, shieldTime + remainingHull / (hullDps + piercingDps));
        }
    }
}
