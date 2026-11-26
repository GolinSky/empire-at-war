using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Each role flag on ship data agrees with the data it stands for, compared within the ship's own faction and class
    /// (every faction's when that class is too small);
    /// each faction mark agrees with the faction's fleet against the others. No tuned value is pinned.
    /// </summary>
    [Category("Ships")]
    public sealed class ShipRoleValidationTests
    {
        private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        private List<ShipRoleEntry> _roster;
        private List<ShipRoleEntry> _owners;
        private WeaponsData _weapons;

        [OneTimeSetUp]
        public void LoadShips()
        {
            _weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
            _roster = ShipRoleRoster.Load();
            _owners = _roster.Where(entry => entry.IsOwner).ToList();
            Assert.That(_owners, Is.Not.Empty);
        }

        private List<ShipRoleEntry> ClassMates(ShipRoleEntry entry) => ShipRoleRoster.ClassMates(_roster, entry);

        [Test]
        public void EveryShip_HasARole_AndOneDurabilityFlag()
        {
            foreach (ShipRoleEntry entry in _owners)
            {
                ShipData ship = entry.Ship;
                Assert.That(ship.Roles, Is.Not.EqualTo(UnitRole.None), ship.name);
                int durabilityFlags = new[] { UnitRole.Balanced, UnitRole.Brawler, UnitRole.Fragile }
                    .Count(flag => ship.Roles.HasFlag(flag));
                Assert.That(durabilityFlags, Is.EqualTo(1), $"{ship.name}: {ship.Roles}");
                Assert.That(ship.Roles.HasFlag(UnitRole.Fast) && ship.Roles.HasFlag(UnitRole.Slow), Is.False, ship.name);
            }
        }

        [Test]
        public void Carrier_HasEnoughHangarBays()
        {
            foreach (ShipRoleEntry entry in _owners)
                Assert.That(entry.Ship.Roles.HasFlag(UnitRole.Carrier), Is.EqualTo(ShipRoleRules.IsCarrier(entry.Ship)),
                    $"{entry.Ship.name}: {entry.Ship.HangarBays.Count} bays");
        }

        [Test]
        public void ClassMates_HaveEnoughShipsForAMedian()
        {
            foreach (ShipRoleEntry entry in _owners)
                Assert.That(ClassMates(entry), Has.Count.GreaterThanOrEqualTo(ShipRoleRoster.MIN_CLASS_MATES),
                    $"{entry.Ship.name} ({entry.Ship.ShipClass})");
        }

        [Test]
        public void Support_HasASupportAbility()
        {
            foreach (ShipRoleEntry entry in _owners.Where(entry => entry.Ship.Roles.HasFlag(UnitRole.Support)))
                Assert.That(ShipRoleRules.HasSupportAbility(entry.Ship), Is.True, entry.Ship.name);
        }

        [Test]
        public void WeaponSpecialists_CarryThatFamilyAsTheLargestShare()
        {
            foreach (ShipRoleEntry entry in _owners)
            {
                ShipData ship = entry.Ship;
                if (ship.Roles.HasFlag(UnitRole.MissileSpecialist))
                    Assert.That(ShipRoleRules.IsLargestShare(ship, _weapons, ShipRoleRules.MissileFamilies), Is.True,
                        ship.name);
                if (ship.Roles.HasFlag(UnitRole.IonSpecialist))
                    Assert.That(ShipRoleRules.IsIonSpecialist(ship, _weapons), Is.True, ship.name);
                int largestGroups = new[]
                    {
                        ShipRoleRules.MissileFamilies, ShipRoleRules.IonFamilies, ShipRoleRules.AntiStrikecraftFamilies
                    }
                    .Count(group => ShipRoleRules.IsLargestShare(ship, _weapons, group));
                Assert.That(largestGroups, Is.LessThanOrEqualTo(1), ship.name);
            }
        }

        [Test]
        public void FastAndSlow_AreAboveAndBelowTheFactionClassMedianSpeed()
        {
            foreach (ShipRoleEntry entry in _owners)
            {
                float median = ShipRoleRules.Median(ClassMates(entry).Select(mate => mate.Ship.Speed));
                if (entry.Ship.Roles.HasFlag(UnitRole.Fast))
                    Assert.That(entry.Ship.Speed, Is.GreaterThan(median), entry.Ship.name);
                if (entry.Ship.Roles.HasFlag(UnitRole.Slow))
                    Assert.That(entry.Ship.Speed, Is.LessThan(median), entry.Ship.name);
            }
        }

        [Test]
        public void BrawlerAndFragile_AreAboveAndBelowTheFactionClassMedianDurabilityPerCredit()
        {
            foreach (ShipRoleEntry entry in _owners)
            {
                float median = ShipRoleRules.Median(ClassMates(entry).Select(ShipRoleRules.DurabilityPerCredit));
                if (entry.Ship.Roles.HasFlag(UnitRole.Brawler))
                    Assert.That(ShipRoleRules.DurabilityPerCredit(entry), Is.GreaterThan(median), entry.Ship.name);
                if (entry.Ship.Roles.HasFlag(UnitRole.Fragile))
                    Assert.That(ShipRoleRules.DurabilityPerCredit(entry), Is.LessThan(median), entry.Ship.name);
            }
        }

        [Test]
        public void FactionMarks_AgreeWithTheFleetAgainstEveryFaction()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FactionDefinition)))
            {
                FactionDefinition faction =
                    AssetDatabase.LoadAssetAtPath<FactionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                List<ShipRoleEntry> fleet = _roster.Where(entry => entry.Faction == faction.FactionType).ToList();
                if (fleet.Count == 0) continue;
                UnitRole marks = faction.ShipMarks;
                string name = faction.FactionType.ToString();
                Assert.That(marks.HasFlag(UnitRole.Fast) && marks.HasFlag(UnitRole.Slow), Is.False, name);
                float speed = ShipRoleRules.AverageRatio(fleet, _roster, entry => entry.Ship.Speed);
                float durability = ShipRoleRules.AverageRatio(fleet, _roster, entry => ShipRoleRules.Durability(entry.Ship));
                float guns = ShipRoleRules.AverageRatio(fleet, _roster, entry => ShipRoleRules.GunCount(entry.Ship));
                if (marks.HasFlag(UnitRole.Fast)) Assert.That(speed, Is.GreaterThan(1f), name);
                if (marks.HasFlag(UnitRole.Slow)) Assert.That(speed, Is.LessThan(1f), name);
                if (marks.HasFlag(UnitRole.Brawler)) Assert.That(durability, Is.GreaterThan(1f), name);
                if (marks.HasFlag(UnitRole.Fragile)) Assert.That(durability, Is.LessThan(1f), name);
                if (marks.HasFlag(UnitRole.Dps)) Assert.That(guns, Is.GreaterThan(1f), name);
            }
        }
    }
}
