using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.Ship.Data;

namespace EmpireAtWar.Editor
{
    /// <summary>
    /// What each <see cref="ShipRole"/> flag means in ship data: class-median comparisons and loadout shares. The
    /// proposal tool and the role validation tests both read these rules.
    /// </summary>
    public static class ShipRoleRules
    {
        /// <summary>Brawler above, Fragile below the class median durability by more than this share.</summary>
        public const float DURABILITY_BAND = 0.15f;
        /// <summary>Fast above, Slow below the class median speed by more than this share.</summary>
        public const float SPEED_BAND = 0.1f;
        /// <summary>Dps above the class median gun count by more than this share.</summary>
        public const float GUN_BAND = 0.15f;

        public static readonly WeaponFamily[] MissileFamilies = { WeaponFamily.Missile, WeaponFamily.Torpedo };
        public static readonly WeaponFamily[] IonFamilies = { WeaponFamily.Ion, WeaponFamily.IonTorpedo };
        public static readonly WeaponFamily[] AntiStrikecraftFamilies = { WeaponFamily.Laser, WeaponFamily.PointDefense };

        public static float Durability(ShipData ship) => ship.Hull + ship.Shields;

        public static int GunCount(ShipData ship) => ship.WeaponLoadout.Sum(entry => entry.Count);

        public static float Median(IEnumerable<float> values)
        {
            float[] sorted = values.OrderBy(value => value).ToArray();
            if (sorted.Length == 0) throw new ArgumentException("No values for a median.", nameof(values));
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) * 0.5f;
        }

        /// <summary>
        /// The families of <paramref name="group"/> together carry at least as many guns as any single other family.
        /// </summary>
        public static bool IsLargestShare(ShipData ship, WeaponsData weapons, IReadOnlyCollection<WeaponFamily> group)
        {
            int inGroup = 0;
            var others = new Dictionary<WeaponFamily, int>();
            foreach (WeaponLoadoutEntry entry in ship.WeaponLoadout)
            {
                WeaponFamily family = weapons.GetProfile(entry.WeaponType).Family;
                if (group.Contains(family)) inGroup += entry.Count;
                else others[family] = (others.TryGetValue(family, out int count) ? count : 0) + entry.Count;
            }
            return inGroup > 0 && others.Values.All(count => inGroup >= count);
        }

        /// <summary>The flags ship data supports; <paramref name="classMates"/> are all ships of its class, itself included.</summary>
        public static ShipRole Propose(ShipData ship, IReadOnlyCollection<ShipData> classMates, WeaponsData weapons)
        {
            ShipRole roles = ShipRole.None;
            float durability = Median(classMates.Select(Durability));
            float speed = Median(classMates.Select(mate => mate.Speed));
            float guns = Median(classMates.Select(mate => (float)GunCount(mate)));
            if (Durability(ship) > durability * (1f + DURABILITY_BAND)) roles |= ShipRole.Brawler;
            else if (Durability(ship) < durability * (1f - DURABILITY_BAND)) roles |= ShipRole.Fragile;
            else roles |= ShipRole.Balanced;
            if (ship.Speed > speed * (1f + SPEED_BAND)) roles |= ShipRole.Fast;
            else if (ship.Speed < speed * (1f - SPEED_BAND)) roles |= ShipRole.Slow;
            if (GunCount(ship) > guns * (1f + GUN_BAND)) roles |= ShipRole.Dps;
            if (ship.HangarBays.Count > 0) roles |= ShipRole.Carrier;
            if (IsLargestShare(ship, weapons, MissileFamilies)) roles |= ShipRole.MissileSpecialist;
            if (IsLargestShare(ship, weapons, IonFamilies)) roles |= ShipRole.IonSpecialist;
            if (IsLargestShare(ship, weapons, AntiStrikecraftFamilies)) roles |= ShipRole.AntiStrikecraft;
            if (ship.UsesArtilleryStandoff) roles |= ShipRole.Artillery;
            return roles;
        }
    }
}
