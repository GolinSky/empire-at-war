using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Republic and CIS ships keep Republic at War 1.5.5 weapon barrels; capitals carry the +50% hull / +100% shield buff.</summary>
    [Category("Balance")]
    public sealed class OriginalValuesRebalanceTests
    {
        private const string WEAPONS = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";
        private const string SHIPS = "Assets/Settings/Data/Ship/";
        private const WeaponFamily LASER = WeaponFamily.Laser;
        private const WeaponFamily TURBO = WeaponFamily.Turbolaser;
        private const WeaponFamily ION = WeaponFamily.Ion;
        private const WeaponFamily MISSILE = WeaponFamily.Missile;
        private const WeaponFamily TORPEDO = WeaponFamily.Torpedo;
        private const WeaponFamily PD = WeaponFamily.PointDefense;

        private static WeaponsData Weapons => AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);

        // Barrels per family: a dual battery counts two.
        private static IEnumerable<TestCaseData> Ships()
        {
            yield return Ship("Republic/CR70", 750, 650, (LASER, 7));
            yield return Ship("Republic/StealthCorvette", 850, 900, (MISSILE, 2), (LASER, 2));
            yield return Ship("Republic/Carrack", 1600, 1000, (LASER, 24));
            yield return Ship("Republic/Centax", 2000, 1300, (TURBO, 2), (LASER, 8), (TORPEDO, 2));
            yield return Ship("Republic/Pelta", 2200, 1000, (TURBO, 4), (LASER, 4), (ION, 2));
            yield return Ship("Republic/Arquitens", 2900, 1400, (TURBO, 16), (LASER, 4), (MISSILE, 1));
            yield return Ship("Republic/HeavyDreadnought", 3600, 3000, (LASER, 8), (TURBO, 8));
            yield return Ship("Republic/Acclamator", 3500, 1500, (TORPEDO, 1), (TURBO, 12), (PD, 2));
            yield return Ship("Empire/Victory", 4900, 3100, (TURBO, 24), (LASER, 8), (MISSILE, 8), (TORPEDO, 1));
            yield return Ship("Republic/Venator", 7500, 8000, (TURBO, 20), (LASER, 4), (PD, 2), (TORPEDO, 1),
                (WeaponFamily.Beam, 1));
            yield return Ship("Republic/Resolute", 10500, 10000, (TURBO, 20), (LASER, 4), (PD, 2), (TORPEDO, 1),
                (WeaponFamily.Beam, 1));
            yield return Ship("Republic/Imperator", 11250, 7000, (TURBO, 26), (ION, 9));
            yield return Ship("Republic/Mandator", 75000, 100000, (TURBO, 52), (PD, 14), (ION, 8), (MISSILE, 8),
                (TORPEDO, 4));
            yield return Ship("Republic/Rothana", 15450, 8000, (TURBO, 20), (LASER, 8), (PD, 2), (ION, 4), (TORPEDO, 1));
            yield return Ship("Separatist/PatrolFrigate", 900, 700, (LASER, 8));
            yield return Ship("Separatist/Munificent", 2000, 2000, (TURBO, 22), (ION, 2), (PD, 4));
            yield return Ship("Separatist/Captor", 3400, 1000, (LASER, 12), (TURBO, 2), (ION, 2));
            yield return Ship("Separatist/Dispatcher", 3500, 1000, (ION, 3), (TURBO, 15), (LASER, 4), (PD, 4));
            yield return Ship("Separatist/Recusant", 3800, 5000, (LASER, 8), (PD, 4), (TURBO, 9));
            yield return Ship("Separatist/Providence", 7500, 10000, (TURBO, 32), (LASER, 8), (ION, 2), (TORPEDO, 2));
            yield return Ship("Separatist/Invincible", 22500, 24000, (TURBO, 35), (LASER, 10), (TORPEDO, 4));
            yield return Ship("Separatist/Lucrehulk", 30000, 30000, (TURBO, 32), (ION, 4), (PD, 6), (LASER, 4),
                (MISSILE, 2));
            yield return Ship("Separatist/Malevolence", 50130, 30000, (TURBO, 36), (LASER, 20),
                (WeaponFamily.MassDriver, 8));
        }

        private static TestCaseData Ship(string ship, int hull, int shields, params (WeaponFamily, int)[] barrels) =>
            new TestCaseData(ship, hull, shields, barrels).SetName($"RepublicCisShips_MatchOriginalValues({ship})");

        [TestCaseSource(nameof(Ships))]
        public void RepublicCisShips_MatchOriginalValues(string ship, int hull, int shields, (WeaponFamily, int)[] barrels)
        {
            ShipData data = Load(ship);
            WeaponsData weapons = Weapons;

            Assert.That(data.Hull, Is.EqualTo(hull));
            Assert.That(data.Shields, Is.EqualTo(shields));
            var mounted = data.WeaponLoadout
                .Select(entry => (definition: weapons.GetDefinition(entry.WeaponType), entry.Count))
                .GroupBy(entry => entry.definition.Family)
                .Select(group => (group.Key, group.Sum(entry => (int)entry.definition.Mount * entry.Count)));
            Assert.That(mounted, Is.EquivalentTo(barrels));
        }

        [Test]
        // RaW quad turbolaser projectiles share the standard bolt scale (0.175): medium caliber, never heavy.
        public void QuadTurbolasers_AreMediumOnRepublicAndCisShips()
        {
            WeaponsData weapons = Weapons;
            foreach (string faction in new[] { "Republic", "Separatist" })
            foreach (ShipData data in WeaponLoadoutBaker.LoadAll<ShipData>(SHIPS + faction))
            foreach (WeaponHardPoint weapon in WeaponLoadoutBaker.LoadShipView(data)
                         .GetComponentsInChildren<WeaponHardPoint>(true))
            {
                WeaponDefinition definition = weapons.GetDefinition(weapon.WeaponType);
                if (definition.Family == WeaponFamily.Turbolaser && definition.Mount == WeaponMount.Quad)
                    Assert.That(definition.Caliber, Is.Not.EqualTo(WeaponCaliber.Heavy), $"{data.name} / {weapon.name}");
            }
        }

        [Test]
        public void Corvettes_StayInCorvetteBand()
        {
            foreach (ShipData data in WeaponLoadoutBaker.LoadAll<ShipData>(WeaponLoadoutBaker.SHIP_DATA_FOLDER)
                         .Where(data => data.ShipClass == ShipClass.Corvette))
            {
                Assert.That(data.Hull, Is.InRange(550, 1600), data.name);
                Assert.That(data.Shields, Is.InRange(500, 1000), data.name);
            }

            ShipData lander = Load("Separatist/C9979");
            Assert.That(lander.WeaponLoadout.Sum(entry => entry.Count), Is.InRange(6, 8));
        }

        // Regeneration is the class lever: each tier out-regenerates the one below. A ship runs on its own class's
        // tier unless listed here; the Executor keeps its own.
        private static readonly Dictionary<string, ShieldRegenTier> REGEN_TIER_OVERRIDES =
            new Dictionary<string, ShieldRegenTier>
            {
                { "ExecutorShipData", ShieldRegenTier.Executor },
                { "CarrackShipData", ShieldRegenTier.Frigate },
            };

        [Test]
        public void ShieldRegen_FollowsShipClassTiers_AndRisesWithClass()
        {
            var classTiers = new Dictionary<ShipClass, ShieldRegenTier>
            {
                { ShipClass.Corvette, ShieldRegenTier.Corvette },
                { ShipClass.Frigate, ShieldRegenTier.Frigate },
                { ShipClass.Cruiser, ShieldRegenTier.Cruiser },
                { ShipClass.Capital, ShieldRegenTier.Capital },
                { ShipClass.HeavyCapital, ShieldRegenTier.HeavyCapital },
            };
            foreach (ShipData data in WeaponLoadoutBaker.LoadAll<ShipData>(WeaponLoadoutBaker.SHIP_DATA_FOLDER))
            {
                ShieldRegenTier expected = REGEN_TIER_OVERRIDES.TryGetValue(data.name, out ShieldRegenTier tier)
                    ? tier : classTiers[data.ShipClass];
                Assert.That(data.ShieldRegenTier, Is.EqualTo(expected), data.name);
                Assert.That(data.ShieldRegenerateValue, Is.GreaterThan(0f), data.name);
            }

            ShipShieldRegenTiersData tiers = AssetDatabase.LoadAssetAtPath<ShipShieldRegenTiersData>(
                "Assets/Settings/Data/Ship/Shared/ShipShieldRegenTiersData.asset");
            ShieldRegenTier[] ordered =
                { ShieldRegenTier.Corvette, ShieldRegenTier.Frigate, ShieldRegenTier.Cruiser, ShieldRegenTier.Capital,
                  ShieldRegenTier.HeavyCapital, ShieldRegenTier.Executor };
            for (int i = 1; i < ordered.Length; i++)
                Assert.That(tiers.GetRegenerateValue(ordered[i]), Is.GreaterThan(tiers.GetRegenerateValue(ordered[i - 1])),
                    ordered[i].ToString());
        }

        private static ShipData Load(string ship) =>
            AssetDatabase.LoadAssetAtPath<ShipData>(SHIPS + ship + "ShipData.asset");
    }
}
