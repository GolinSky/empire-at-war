using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.CaptureSites;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class DefenseSatelliteContractTests
    {
        [TestCase(SiteFacilityType.QuadLaserSatellite, DefendPlatformType.QuadLaserSatellite, WeaponType.DualRepeatingPointDefense, 300f, 21f)]
        [TestCase(SiteFacilityType.TurbolaserSatellite, DefendPlatformType.TurbolaserSatellite, WeaponType.DualMediumTurboLaser, 500f, 25f)]
        [TestCase(SiteFacilityType.IonSatellite, DefendPlatformType.IonSatellite, WeaponType.MediumIonCannon, 400f, 23f)]
        [TestCase(SiteFacilityType.MassDriverSatellite, DefendPlatformType.MassDriverSatellite, WeaponType.MassDriver, 1600f, 47f)]
        [TestCase(SiteFacilityType.MissileSatellite, DefendPlatformType.MissileSatellite, WeaponType.HeavyAssaultMissile, 1000f, 35f)]
        public void Satellite_HasRegisteredSixWeaponPrefabAndBalancedData(SiteFacilityType facility,
            DefendPlatformType platform, WeaponType weapon, float price, float buildTime)
        {
            Assert.That(SiteFacilityBuilder.SatelliteType(facility), Is.EqualTo(platform));
            DefendPlatformData data = LoadData(platform);
            Assert.That(data.ComponentData.Hull, Is.EqualTo(1000f));
            Assert.That(data.ComponentData.Shields, Is.EqualTo(1000f));
            Assert.That(data.Income, Is.Zero);
            Assert.That(data.WeaponLoadout.Sum(entry => entry.Count), Is.EqualTo(6));
            Assert.That(data.WeaponLoadout.All(entry => entry.WeaponType == weapon), Is.True);
            var catalog = AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(
                "Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
            Assert.That(catalog.Entries.ContainsKey(platform), Is.True);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Models/DefendStation/{platform}View.prefab");
            Assert.That(prefab, Is.Not.Null);
            HealthComponent health = prefab.GetComponentInChildren<HealthComponent>(true);
            Assert.That(health, Is.Not.Null);
            Assert.That(health.ShipUnits.Count, Is.EqualTo(6));
            Assert.That(health.ShipUnits.Distinct().Count(), Is.EqualTo(6));
            EntityBindings bindings = prefab.GetComponent<EntityBindings>();
            Assert.That(bindings.Injectables.All(component => component != null), Is.True,
                "Deleted template components must not remain in the injection list.");
            UnitRenderStateComponent renderState = bindings.Injectables.OfType<UnitRenderStateComponent>().Single();
            SerializedProperty renderers = new SerializedObject(renderState).FindProperty("meshRenderers");
            Assert.That(renderers.arraySize, Is.GreaterThan(0));
            for (int i = 0; i < renderers.arraySize; i++)
                Assert.That(renderers.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);

            foreach (var mount in health.ShipUnits)
            {
                Assert.That(mount, Is.Not.Null);
                Assert.That(bindings.Injectables, Contains.Item(mount), "Hardpoint destruction needs injected VFX and fog services.");
                SerializedObject point = new SerializedObject(mount);
                if (point.FindProperty("spawnDestroyedExplosion").boolValue)
                    Assert.That(point.FindProperty("destroyedVfxAnchor").objectReferenceValue, Is.Not.Null);

                var stats = data.ComponentData.HardPointHealth.Single(entry => entry.HardPointType == mount.HardPointType);
                Assert.That(stats.Health, Is.EqualTo(300f));
                Assert.That(stats.HullDamageMultiplier, Is.EqualTo(1f));
            }
            var sites = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            Assert.That(sites.GetAllowedFacilities(CaptureSiteKind.Satellite), Contains.Item(facility));
            Assert.That(sites.GetCost(facility).Icon, Is.Not.Null);
            Assert.That(sites.GetCost(facility).Icon.rect.size, Is.EqualTo(new Vector2(512f, 512f)));
            Assert.That(sites.GetCost(facility).Price, Is.EqualTo(price));
            Assert.That(sites.GetCost(facility).BuildTime, Is.EqualTo(buildTime));
        }

        [TestCase(DefendPlatformType.QuadLaserSatellite, HardPointType.PointDefense)]
        [TestCase(DefendPlatformType.TurbolaserSatellite, HardPointType.Weapon)]
        public void DestroyedSatelliteTurret_IsNotRestoredByRepairOrShieldRegeneration(
            DefendPlatformType platform, HardPointType type)
        {
            DefendPlatformData data = LoadData(platform);
            var matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
            var model = new HealthModel(data.ComponentData, matrix, new CombatModifiers());
            var turret = new HardPointModel(type, 0);
            model.InitializeHardPoints(new[] { turret });
            turret.ApplyDamage(turret.MaxHealth);
            for (int i = 0; i < 10; i++)
            {
                turret.Repair(1000f);
                model.RegenerateShields(1000f);
                model.RefreshStatModifiers();
            }
            Assert.That(turret.IsDestroyed, Is.True);
            Assert.That(turret.Health, Is.Zero);
            Assert.That(model.Hull, Is.EqualTo(1000f));
            Assert.That(model.Shields, Is.EqualTo(1000f));
        }

        private static DefendPlatformData LoadData(DefendPlatformType platform)
        {
            var data = AssetDatabase.LoadAssetAtPath<DefendPlatformData>(
                $"Assets/Settings/Data/Models/DefendPlatform/{platform}Data.asset");
            Assert.That(data, Is.Not.Null);
            return data;
        }
    }
}
