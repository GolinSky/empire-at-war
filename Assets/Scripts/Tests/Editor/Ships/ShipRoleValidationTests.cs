using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Editor;
using EmpireAtWar.Entities.Ship.Data;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>Each role flag on ship data agrees with the data it stands for; no tuned value is pinned.</summary>
    [Category("Ships")]
    public sealed class ShipRoleValidationTests
    {
        private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        private List<ShipData> _ships;
        private WeaponsData _weapons;

        [OneTimeSetUp]
        public void LoadShips()
        {
            _weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
            _ships = AssetDatabase.FindAssets("t:ShipData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
            Assert.That(_ships, Is.Not.Empty);
        }

        private IEnumerable<ShipData> ClassMates(ShipData ship) => _ships.Where(mate => mate.ShipClass == ship.ShipClass);

        [Test]
        public void EveryShip_HasARole_AndOneDurabilityFlag()
        {
            foreach (ShipData ship in _ships)
            {
                Assert.That(ship.Roles, Is.Not.EqualTo(ShipRole.None), ship.name);
                int durabilityFlags = new[] { ShipRole.Balanced, ShipRole.Brawler, ShipRole.Fragile }
                    .Count(flag => ship.Roles.HasFlag(flag));
                Assert.That(durabilityFlags, Is.EqualTo(1), $"{ship.name}: {ship.Roles}");
                Assert.That(ship.Roles.HasFlag(ShipRole.Fast) && ship.Roles.HasFlag(ShipRole.Slow), Is.False, ship.name);
            }
        }

        [Test]
        public void Carrier_HasHangarBays()
        {
            foreach (ShipData ship in _ships.Where(ship => ship.Roles.HasFlag(ShipRole.Carrier)))
                Assert.That(ship.HangarBays.Count, Is.GreaterThan(0), ship.name);
        }

        [Test]
        public void WeaponSpecialists_CarryThatFamilyAsTheLargestShare()
        {
            foreach (ShipData ship in _ships)
            {
                if (ship.Roles.HasFlag(ShipRole.MissileSpecialist))
                    Assert.That(ShipRoleRules.IsLargestShare(ship, _weapons, ShipRoleRules.MissileFamilies), Is.True,
                        ship.name);
                if (ship.Roles.HasFlag(ShipRole.IonSpecialist))
                    Assert.That(ShipRoleRules.IsLargestShare(ship, _weapons, ShipRoleRules.IonFamilies), Is.True,
                        ship.name);
            }
        }

        [Test]
        public void FastAndSlow_AreAboveAndBelowTheClassMedianSpeed()
        {
            foreach (ShipData ship in _ships)
            {
                float median = ShipRoleRules.Median(ClassMates(ship).Select(mate => mate.Speed));
                if (ship.Roles.HasFlag(ShipRole.Fast)) Assert.That(ship.Speed, Is.GreaterThan(median), ship.name);
                if (ship.Roles.HasFlag(ShipRole.Slow)) Assert.That(ship.Speed, Is.LessThan(median), ship.name);
            }
        }

        [Test]
        public void BrawlerAndFragile_AreAboveAndBelowTheClassMedianDurability()
        {
            foreach (ShipData ship in _ships)
            {
                float median = ShipRoleRules.Median(ClassMates(ship).Select(ShipRoleRules.Durability));
                if (ship.Roles.HasFlag(ShipRole.Brawler))
                    Assert.That(ShipRoleRules.Durability(ship), Is.GreaterThan(median), ship.name);
                if (ship.Roles.HasFlag(ShipRole.Fragile))
                    Assert.That(ShipRoleRules.Durability(ship), Is.LessThan(median), ship.name);
            }
        }
    }
}
