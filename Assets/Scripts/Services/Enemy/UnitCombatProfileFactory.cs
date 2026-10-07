using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Turns a unit's stats and baked weapon loadout into per-class damage rates through the damage matrix.</summary>
    public static class UnitCombatProfileFactory
    {
        public static UnitCombatProfile Create(
            ShipClass shipClass,
            float hull,
            float shields,
            IReadOnlyList<WeaponLoadoutEntry> loadout,
            WeaponsData weaponsData,
            DamageMatrixData damageMatrix,
            IReadOnlyList<UnitCombatProfile> hangar)
        {
            float[] hullDps = new float[ForceComposition.ClassSlots];
            float[] shieldDps = new float[ForceComposition.ClassSlots];
            float[] piercingDps = new float[ForceComposition.ClassSlots];
            foreach (WeaponLoadoutEntry entry in loadout)
            {
                WeaponProfile weapon = weaponsData.GetProfile(entry.WeaponType);
                if (weapon.Reload <= 0f)
                    throw new InvalidOperationException($"{entry.WeaponType} needs a positive reload to rate its damage.");

                float dps = entry.Count * weapon.Damage * weapon.ShotsPerSalvo / weapon.Reload;
                bool piercing = damageMatrix.IsShieldPiercing(weapon.DamageType);
                float shieldMultiplier = damageMatrix.GetShieldMultiplier(weapon.DamageType);
                foreach (ShipClass target in ForceComposition.Classes)
                {
                    if (weapon.StrikecraftOnly && !target.IsStrikecraft())
                        continue;

                    int index = (int)target;
                    float hitDps = dps * damageMatrix.GetAccuracy(weapon.DamageType, target);
                    float hullHitDps = hitDps * damageMatrix.GetDamageMultiplier(weapon.DamageType, target);
                    if (piercing)
                    {
                        piercingDps[index] += hullHitDps;
                    }
                    else
                    {
                        hullDps[index] += hullHitDps;
                        shieldDps[index] += hitDps * shieldMultiplier;
                    }
                }
            }

            return new UnitCombatProfile(shipClass, hull, shields, hullDps, shieldDps, piercingDps, hangar);
        }
    }
}
