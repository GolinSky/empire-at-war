using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>
    /// Derives a weapon's stats from its taxonomy:
    /// family base → caliber → range class → mount → variant multipliers.
    /// </summary>
    public static class WeaponProfileFactory
    {
        public static WeaponProfile Create(WeaponDefinition definition, WeaponFamilyRule family,
            WeaponCaliberRule caliber, WeaponRangeClassRule rangeClass)
        {
            int barrels = (int)definition.Mount;
            float damage = family.Damage * caliber.DamageMultiplier * rangeClass.DamageMultiplier * barrels *
                           definition.DamageMultiplier;
            float reload = family.Reload * rangeClass.ReloadMultiplier * definition.ReloadMultiplier;
            int shots = family.ShotsPerSalvo;
            float interval = family.ShotInterval;

            float range = family.Range * caliber.RangeMultiplier * rangeClass.RangeMultiplier * definition.RangeMultiplier;
            // Squadrons aim by flying, so their weapons never turn.
            float turnRate = family.Platform == WeaponPlatform.Squadron ? 0f : family.TurnRate * caliber.TurnRateMultiplier;

            ShotEffect shotPrefab = barrels > 1 && family.MultiBarrelShotPrefab != null
                ? family.MultiBarrelShotPrefab
                : family.ShotPrefab;
            if (definition.Mount == WeaponMount.Quad && family.QuadShotPrefab != null)
                shotPrefab = family.QuadShotPrefab;
            if (definition.Mount == WeaponMount.Octuple && family.OctupleShotPrefab != null)
                shotPrefab = family.OctupleShotPrefab;
            float projectileSpeed = family.ProjectileSpeed;
            Vector3 size = family.Size;
            if (definition.OverrideVisual)
            {
                shotPrefab = definition.ShotPrefab;
                projectileSpeed = definition.ProjectileSpeed;
                size = definition.Size;
            }

            if (family.Family is WeaponFamily.Laser or WeaponFamily.Turbolaser or WeaponFamily.Ion
                or WeaponFamily.FighterLaser or WeaponFamily.FighterIon)
            {
                // Stretched billboards use Y for length; caliber never changes bolt width.
                size.y *= caliber.Caliber switch
                {
                    WeaponCaliber.Light => 0.3f,
                    WeaponCaliber.Medium => 0.5f,
                    WeaponCaliber.Heavy => 0.8f,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(caliber))
                };
            }

            Color color = definition.OverrideColor ? definition.Color : family.Color;
            bool usesFactionColor = family.FactionColored && !definition.OverrideColor;

            return new WeaponProfile(definition.WeaponType, definition.DisplayName, family.Family, family.DamageType,
                damage, shots, interval, reload, range, family.StrikecraftOnly, family.Interceptable, shotPrefab,
                projectileSpeed, color, size, usesFactionColor, family.TargetSizeBias, caliber.DamageBias,
                caliber.AccuracyBias, rangeClass.AccuracyMultiplier, turnRate, family.PreferredTarget);
        }
    }
}
