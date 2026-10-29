using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class CorvetteLoadoutTests
    {
        private const string RAIDER_DATA = "Assets/Settings/Data/Ship/Empire/RaiderCorvetteShipData.asset";
        private const string STEALTH_DATA = "Assets/Settings/Data/Ship/Republic/StealthCorvetteShipData.asset";
        // Republic at War 1.5.5 gives these corvettes no point defense.
        private static readonly string[] NO_POINT_DEFENSE =
        {
            "Assets/Settings/Data/Ship/Republic/CarrackShipData.asset",
            "Assets/Settings/Data/Ship/Republic/CR70ShipData.asset",
            STEALTH_DATA,
            "Assets/Settings/Data/Ship/Separatist/PatrolFrigateShipData.asset",
        };

        [Test]
        public void SavedCorvettes_HaveExpectedPointDefenseMountsAndMatchingTargetableLoadouts()
        {
            BalanceRegistration registry = BalanceInventory.BuildCombat();
            WeaponsData weapons = BalanceRegistration.One<WeaponsData>();
            BalanceUnit[] corvettes = registry.Units.Where(unit =>
                unit.Data is ShipData data && data.ShipClass == ShipClass.Corvette).ToArray();
            Assert.That(corvettes.Length, Is.EqualTo(9));

            foreach (BalanceUnit unit in corvettes)
            {
                ShipData data = (ShipData)unit.Data;
                WeaponHardPoint[] mounts = unit.Components.OfType<WeaponComponent>()
                    .SelectMany(component => BalancePrefabBindings.References(component, "hardPoints"))
                    .Cast<WeaponHardPoint>().ToArray();
                var healthBindings = BalancePrefabBindings.References(
                    unit.Components.OfType<HealthComponent>().Single(), BalanceRegistration.Auto("ShipUnits"));
                bool isRaider = AssetDatabase.GetAssetPath(data) == RAIDER_DATA;
                bool hasPointDefense = !NO_POINT_DEFENSE.Contains(AssetDatabase.GetAssetPath(data));

                Assert.That(mounts, Is.Not.Empty, unit.Caption);
                Assert.That(mounts.Distinct().Count(), Is.EqualTo(mounts.Length), unit.Caption);
                Assert.That(mounts, Is.EquivalentTo(unit.Components.OfType<WeaponHardPoint>()), unit.Caption);
                Assert.That(mounts, Is.EquivalentTo(unit.Components.OfType<EntityBindings>()
                    .SelectMany(bindings => BalancePrefabBindings.References(bindings, BalanceRegistration.Auto("Injectables")))
                    .OfType<WeaponHardPoint>()), unit.Caption);
                Assert.That(mounts.Select(mount => mount.Id).Distinct().Count(), Is.EqualTo(mounts.Length), unit.Caption);
                Assert.That(mounts.Count(mount => weapons.GetDefinition(mount.WeaponType).Family ==
                    WeaponFamily.PointDefense), Is.EqualTo(hasPointDefense ? 2 : 0), unit.Caption);
                Assert.That(data.WeaponLoadout.Select(entry => entry.WeaponType).Distinct().Count(),
                    Is.EqualTo(data.WeaponLoadout.Count), unit.Caption);
                Assert.That(data.WeaponLoadout.Select(entry => (entry.WeaponType, entry.Count)),
                    Is.EquivalentTo(mounts.GroupBy(mount => mount.WeaponType)
                        .Select(group => (group.Key, group.Count()))), unit.Caption);

                foreach (WeaponHardPoint mount in mounts)
                {
                    WeaponDefinition definition = weapons.GetDefinition(mount.WeaponType);
                    string context = unit.Caption + " / " + mount.name;
                    Assert.That(healthBindings, Does.Contain(mount), context);
                    Assert.That(definition.Caliber, Is.Not.EqualTo(WeaponCaliber.Heavy), context);
                    // The original Stealth Corvette carries concussion missiles.
                    if (AssetDatabase.GetAssetPath(data) != STEALTH_DATA)
                        Assert.That(definition.Family, Is.Not.EqualTo(WeaponFamily.Missile), context);
                    Assert.That(definition.Family, Is.Not.EqualTo(WeaponFamily.Torpedo), context);
                    Assert.That(definition.Family, Is.Not.EqualTo(WeaponFamily.IonTorpedo), context);
                    if (isRaider || definition.Family == WeaponFamily.PointDefense)
                    {
                        Assert.That(mount.MinYaw, Is.LessThanOrEqualTo(-180f), context);
                        Assert.That(mount.MaxYaw, Is.GreaterThanOrEqualTo(180f), context);
                    }
                }
            }
        }

        [Test]
        public void SavedCorvettes_HaveEngineAndShieldGeneratorHardpointsWithHealthAndInjectionBindings()
        {
            BalanceRegistration registry = BalanceInventory.BuildCombat();
            BalanceUnit[] corvettes = registry.Units.Where(unit =>
                unit.Data is ShipData data && data.ShipClass == ShipClass.Corvette).ToArray();
            Assert.That(corvettes.Length, Is.EqualTo(9));

            foreach (BalanceUnit unit in corvettes)
            {
                ShipData data = (ShipData)unit.Data;
                var healthBindings = BalancePrefabBindings.References(
                    unit.Components.OfType<HealthComponent>().Single(), BalanceRegistration.Auto("ShipUnits"));
                var injectables = unit.Components.OfType<EntityBindings>()
                    .SelectMany(bindings => BalancePrefabBindings.References(
                        bindings, BalanceRegistration.Auto("Injectables"))).ToArray();

                foreach (HardPointType type in new[] { HardPointType.Engines, HardPointType.ShieldGenerator })
                {
                    string context = unit.Caption + " / " + type;
                    HardPoint[] hardPoints = unit.Components.OfType<HardPoint>()
                        .Where(point => point.HardPointType == type).ToArray();
                    Assert.That(hardPoints, Is.Not.Empty, context);
                    Assert.That(healthBindings.OfType<HardPoint>().Where(point => point.HardPointType == type),
                        Is.EquivalentTo(hardPoints), context);
                    Assert.That(injectables.OfType<HardPoint>().Where(point => point.HardPointType == type),
                        Is.EquivalentTo(hardPoints), context);
                    Assert.That(data.UsesHullHardPointBudget, Is.True, context);
                    Assert.That(hardPoints.All(point => point.HealthWeight > 0f), Is.True, context);
                    Assert.That(hardPoints.All(point => point.UnlockLevel == 1), Is.True, context);
                }
            }
        }
    }
}
