using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Editor
{
    /// <summary>
    /// What each <see cref="UnitRole"/> flag means in ship data. Durability, speed, guns and anti-capital damage compare a
    /// ship with its own faction's ships of the same class (every faction's when that class is too small, see
    /// <see cref="ShipRoleRoster.ClassMates"/>), so a faction-wide trait (every Empire ship is slow) is the
    /// faction's mark (<see cref="ProposeFactionMarks"/>), not a flag on every ship. Durability is weighed against price:
    /// a cheap small capital is not fragile. The proposal tool and the role validation tests both read these rules.
    /// </summary>
    public static class ShipRoleRules
    {
        /// <summary>Brawler above, Fragile below the faction-class median durability per credit by more than this share.</summary>
        public const float DURABILITY_BAND = 0.15f;
        /// <summary>Fast above, Slow below the faction-class median speed by more than this share.</summary>
        public const float SPEED_BAND = 0.05f;
        /// <summary>Dps above the faction-class median gun count by more than this share.</summary>
        public const float GUN_BAND = 0.15f;
        /// <summary>AntiCapital above the faction-class median damage against capitals by more than this share.</summary>
        public const float ANTI_CAPITAL_BAND = 0.15f;
        /// <summary>A faction mark needs its class medians this far from the all-faction class medians on average.</summary>
        public const float FACTION_MARK_BAND = 0.1f;

        public static readonly WeaponFamily[] MissileFamilies = { WeaponFamily.Missile, WeaponFamily.Torpedo };
        public static readonly WeaponFamily[] IonFamilies = { WeaponFamily.Ion, WeaponFamily.IonTorpedo };
        public static readonly WeaponFamily[] AntiStrikecraftFamilies = { WeaponFamily.Laser, WeaponFamily.PointDefense };
        /// <summary>Families counted together for a weapon share; every other family counts alone.</summary>
        private static readonly WeaponFamily[][] FamilyGroups = { MissileFamilies, IonFamilies, AntiStrikecraftFamilies };
        /// <summary>Abilities that make a ship a support ship.</summary>
        public static readonly ShipAbilityId[] SupportAbilities = { ShipAbilityId.Interdict };
        /// <summary>Abilities that make a ship an ion specialist whatever its guns (Malevolence's ion pulse).</summary>
        public static readonly ShipAbilityId[] IonAbilities = { ShipAbilityId.IonPulse, ShipAbilityId.IonShot };
        /// <summary>
        /// Abilities that make a ship anti-capital whatever its guns: Malevolence's ion pulse disables a capital ship and
        /// strips its shields, and targets dreadnoughts first.
        /// </summary>
        public static readonly ShipAbilityId[] AntiCapitalAbilities = { ShipAbilityId.IonPulse };
        /// <summary>Carrier needs at least this many hangar bays; one bay is a few escorts, not a carrier.</summary>
        public const int MIN_CARRIER_BAYS = 2;

        public static float Durability(ShipData ship) => ship.Hull + ship.Shields;

        public static float DurabilityPerCredit(ShipRoleEntry entry) => Durability(entry.Ship) / Math.Max(1f, entry.Price);

        public static int GunCount(ShipData ship) => ship.WeaponLoadout.Sum(entry => entry.Count);

        /// <summary>Sustained damage per second against a capital ship, with each weapon's size bias.</summary>
        public static float CapitalDamage(ShipData ship, WeaponsData weapons) =>
            ship.WeaponLoadout.Sum(entry =>
            {
                WeaponProfile profile = weapons.GetProfile(entry.WeaponType);
                return profile.DamagePerSecond * profile.GetDamageBias(ShipClass.Capital) * entry.Count;
            });

        public static float Median(IEnumerable<float> values)
        {
            float[] sorted = values.OrderBy(value => value).ToArray();
            if (sorted.Length == 0) throw new ArgumentException("No values for a median.", nameof(values));
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) * 0.5f;
        }

        /// <summary>
        /// <paramref name="group"/> is the ship's largest weapon group: the most guns, with missiles + torpedoes, ion +
        /// ion torpedoes and lasers + point defense each counted as one group and every other family alone. A gun-count
        /// tie goes to the group with more damage per second, so at most one group is the largest.
        /// </summary>
        public static bool IsLargestShare(ShipData ship, WeaponsData weapons, IReadOnlyCollection<WeaponFamily> group)
        {
            var largest = ship.WeaponLoadout
                .Select(entry => (Profile: weapons.GetProfile(entry.WeaponType), entry.Count))
                .GroupBy(item => GroupKey(item.Profile.Family))
                .Select(items => (Key: items.Key, Guns: items.Sum(item => item.Count),
                    Damage: items.Sum(item => item.Profile.DamagePerSecond * item.Count)))
                .OrderByDescending(total => total.Guns)
                .ThenByDescending(total => total.Damage)
                .FirstOrDefault();
            return largest.Guns > 0 && group.Contains(largest.Key);
        }

        /// <summary>The first family of the group holding <paramref name="family"/>, or the family itself.</summary>
        private static WeaponFamily GroupKey(WeaponFamily family)
        {
            foreach (WeaponFamily[] group in FamilyGroups)
                if (group.Contains(family)) return group[0];
            return family;
        }

        public static bool HasSupportAbility(ShipData ship) => ship.Abilities.Any(SupportAbilities.Contains);

        public static bool HasIonAbility(ShipData ship) => ship.Abilities.Any(IonAbilities.Contains);

        public static bool HasAntiCapitalAbility(ShipData ship) => ship.Abilities.Any(AntiCapitalAbilities.Contains);

        /// <summary>Ion weapons are its largest weapon group, or it has an ion ability.</summary>
        public static bool IsIonSpecialist(ShipData ship, WeaponsData weapons) =>
            IsLargestShare(ship, weapons, IonFamilies) || HasIonAbility(ship);

        /// <summary>Enough hangar bays and a squadron cap that lets it launch (0 = launches nothing).</summary>
        public static bool IsCarrier(ShipData ship) =>
            ship.HangarBays.Count >= MIN_CARRIER_BAYS && ship.HangarMaxActive != 0;

        /// <summary>
        /// The flags ship data supports; <paramref name="classMates"/> are the ships of its faction and class, itself
        /// included.
        /// </summary>
        public static UnitRole Propose(ShipRoleEntry entry, IReadOnlyCollection<ShipRoleEntry> classMates,
            WeaponsData weapons)
        {
            ShipData ship = entry.Ship;
            UnitRole roles = UnitRole.None;
            float durability = Median(classMates.Select(DurabilityPerCredit));
            float speed = Median(classMates.Select(mate => mate.Ship.Speed));
            float guns = Median(classMates.Select(mate => (float)GunCount(mate.Ship)));
            float capitalDamage = Median(classMates.Select(mate => CapitalDamage(mate.Ship, weapons)));
            if (DurabilityPerCredit(entry) > durability * (1f + DURABILITY_BAND)) roles |= UnitRole.Brawler;
            else if (DurabilityPerCredit(entry) < durability * (1f - DURABILITY_BAND)) roles |= UnitRole.Fragile;
            else roles |= UnitRole.Balanced;
            if (ship.Speed > speed * (1f + SPEED_BAND)) roles |= UnitRole.Fast;
            else if (ship.Speed < speed * (1f - SPEED_BAND)) roles |= UnitRole.Slow;
            if (GunCount(ship) > guns * (1f + GUN_BAND)) roles |= UnitRole.Dps;
            if (CapitalDamage(ship, weapons) > capitalDamage * (1f + ANTI_CAPITAL_BAND) || HasAntiCapitalAbility(ship))
                roles |= UnitRole.AntiCapital;
            if (IsCarrier(ship)) roles |= UnitRole.Carrier;
            if (IsLargestShare(ship, weapons, MissileFamilies)) roles |= UnitRole.MissileSpecialist;
            if (IsIonSpecialist(ship, weapons)) roles |= UnitRole.IonSpecialist;
            if (IsLargestShare(ship, weapons, AntiStrikecraftFamilies)) roles |= UnitRole.AntiStrikecraft;
            if (ship.UsesArtilleryStandoff) roles |= UnitRole.Artillery;
            if (HasSupportAbility(ship)) roles |= UnitRole.Support;
            return roles;
        }

        /// <summary>
        /// What a faction's whole fleet shares: per class, its median speed, raw durability and gun count against every
        /// faction's median for that class, averaged over its classes. Fast / Slow, Brawler / Fragile and Dps when the
        /// average is more than <see cref="FACTION_MARK_BAND"/> away.
        /// </summary>
        public static UnitRole ProposeFactionMarks(IReadOnlyCollection<ShipRoleEntry> faction,
            IReadOnlyCollection<ShipRoleEntry> roster)
        {
            float speed = AverageRatio(faction, roster, entry => entry.Ship.Speed);
            float durability = AverageRatio(faction, roster, entry => Durability(entry.Ship));
            float guns = AverageRatio(faction, roster, entry => GunCount(entry.Ship));
            UnitRole marks = UnitRole.None;
            if (speed > 1f + FACTION_MARK_BAND) marks |= UnitRole.Fast;
            else if (speed < 1f - FACTION_MARK_BAND) marks |= UnitRole.Slow;
            if (durability > 1f + FACTION_MARK_BAND) marks |= UnitRole.Brawler;
            else if (durability < 1f - FACTION_MARK_BAND) marks |= UnitRole.Fragile;
            if (guns > 1f + FACTION_MARK_BAND) marks |= UnitRole.Dps;
            return marks;
        }

        /// <summary>Average over the faction's classes of its class median divided by every faction's class median.</summary>
        public static float AverageRatio(IReadOnlyCollection<ShipRoleEntry> faction,
            IReadOnlyCollection<ShipRoleEntry> roster, Func<ShipRoleEntry, float> value) =>
            faction.GroupBy(entry => entry.Ship.ShipClass)
                .Select(group => Median(group.Select(value)) /
                                 Median(roster.Where(entry => entry.Ship.ShipClass == group.Key).Select(value)))
                .Average();
    }
}
