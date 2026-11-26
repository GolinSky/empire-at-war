using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;

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
            IReadOnlyList<UnitCombatProfile> hangar,
            float warheadDamageMultiplier = 1f,
            float warheadReloadMultiplier = 1f,
            float shieldRegen = 0f,
            float speed = 0f,
            float price = 0f,
            FactionType? faction = null,
            UnitRole roles = UnitRole.None)
        {
            float[] hullDps = new float[ForceComposition.ClassSlots];
            float[] shieldDps = new float[ForceComposition.ClassSlots];
            float[] piercingDps = new float[ForceComposition.ClassSlots];
            foreach (WeaponLoadoutEntry entry in loadout)
            {
                WeaponProfile weapon = weaponsData.GetProfile(entry.WeaponType)
                    .WithWarheadMultipliers(warheadDamageMultiplier, warheadReloadMultiplier);
                if (weapon.Reload <= 0f)
                    throw new InvalidOperationException($"{entry.WeaponType} needs a positive reload to rate its damage.");

                // The unit's own faction scales its hits in combat (WeaponComponent.ApplyDamage), so the rating does too.
                float factionScale = faction.HasValue
                    ? damageMatrix.GetFactionDamageMultiplier(faction.Value, weapon.DamageType) : 1f;
                float dps = entry.Count * weapon.Damage * weapon.ShotsPerSalvo / weapon.Reload * factionScale;
                bool piercing = damageMatrix.IsShieldPiercing(weapon.DamageType);
                foreach (ShipClass target in ForceComposition.Classes)
                {
                    if (weapon.StrikecraftOnly && !target.IsStrikecraft())
                        continue;

                    int index = (int)target;
                    float accuracy = Math.Min(1f, damageMatrix.GetAccuracy(weapon.DamageType, target) *
                        weapon.GetAccuracyBias(target));
                    float hitDps = dps * accuracy * weapon.GetDamageBias(target);
                    float hullHitDps = hitDps * damageMatrix.GetDamageMultiplier(weapon.DamageType, target);
                    if (piercing)
                    {
                        piercingDps[index] += hullHitDps;
                    }
                    else
                    {
                        float shieldMultiplier = target.IsStrikecraft()
                            ? damageMatrix.GetSquadronShieldMultiplier(weapon.DamageType)
                            : damageMatrix.GetShipShieldMultiplier(weapon.DamageType);
                        hullDps[index] += hullHitDps;
                        shieldDps[index] += hitDps * shieldMultiplier;
                    }
                }
            }

            return new UnitCombatProfile(shipClass, hull, shields, hullDps, shieldDps, piercingDps, hangar, shieldRegen,
                speed, price, roles);
        }
    }
}
