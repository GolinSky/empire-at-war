using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    /// <summary>
    /// Picks how close a unit moves to a target class: the longest distance at which the guns that reach
    /// still deliver most of the unit's effective damage against that class.
    /// </summary>
    public static class WeaponEngagementRange
    {
        // Share of the unit's effective damage that must be able to fire at the chosen distance.
        public const float DAMAGE_COVERAGE = 0.6f;

        public static float Calculate(IReadOnlyList<WeaponProfile> weapons, ShipClass targetClass, DamageMatrixData matrix)
        {
            float total = 0f;
            float maxRange = 0f;
            List<(float range, float damage)> byRange = new List<(float, float)>(weapons.Count);
            foreach (WeaponProfile weapon in weapons)
            {
                maxRange = Mathf.Max(maxRange, weapon.Range);
                float damage = EffectiveDamagePerSecond(weapon, targetClass, matrix);
                if (damage <= 0f) continue;
                byRange.Add((weapon.Range, damage));
                total += damage;
            }

            // A unit that cannot hurt this class still approaches to its longest reach.
            if (total <= 0f) return maxRange;

            byRange.Sort((a, b) => b.range.CompareTo(a.range));
            float covered = 0f;
            foreach ((float range, float damage) in byRange)
            {
                covered += damage;
                if (covered >= total * DAMAGE_COVERAGE) return range;
            }

            return byRange[byRange.Count - 1].range;
        }

        public static float EffectiveDamagePerSecond(WeaponProfile weapon, ShipClass targetClass, DamageMatrixData matrix)
        {
            if (weapon.StrikecraftOnly && !targetClass.IsStrikecraft()) return 0f;
            float hitChance = Mathf.Min(1f, matrix.GetAccuracy(weapon.DamageType, targetClass) *
                                            weapon.GetAccuracyBias(targetClass) * weapon.AccuracyMultiplier);
            return weapon.DamagePerSecond * hitChance * matrix.GetDamageMultiplier(weapon.DamageType, targetClass) *
                   weapon.GetDamageBias(targetClass);
        }
    }
}
