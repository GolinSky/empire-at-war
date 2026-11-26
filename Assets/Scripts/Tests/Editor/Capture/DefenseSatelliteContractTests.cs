using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Construction;
using EmpireAtWar.Components.Repair;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Repair;
using EmpireAtWar.Services.Tooltip;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Capture")]
    public sealed class DefenseSatelliteContractTests
    {
        private sealed class StubHealthTooltip : IHealthTooltipObserver
        {
            public float Hull => 10f;
            public float MaxHull => 20f;
            public float Shields => 5f;
            public float MaxShields => 10f;
            public bool IsShieldRecovering => false;
            public float ShieldRegeneration => 0f;
            public float ShieldRegenerationInterval => 0f;
        }

        [TestCase(SiteFacilityType.QuadLaserSatellite, DefendPlatformType.QuadLaserSatellite, WeaponType.DualRepeatingPointDefense)]
        [TestCase(SiteFacilityType.TurbolaserSatellite, DefendPlatformType.TurbolaserSatellite, WeaponType.DualMediumTurboLaser)]
        [TestCase(SiteFacilityType.IonSatellite, DefendPlatformType.IonSatellite, WeaponType.MediumIonCannon)]
        [TestCase(SiteFacilityType.MassDriverSatellite, DefendPlatformType.MassDriverSatellite, WeaponType.MassDriver)]
        [TestCase(SiteFacilityType.MissileSatellite, DefendPlatformType.MissileSatellite, WeaponType.HeavyAssaultMissile)]
        public void Satellite_HasRegisteredWeaponPrefabAndData(SiteFacilityType facility,
            DefendPlatformType platform, WeaponType weapon)
        {
            Assert.That(facility.ToSatelliteType(), Is.EqualTo(platform));
            DefendPlatformData data = LoadData(platform);
            Assert.That(data.Income, Is.Zero);
            Assert.That(data.WeaponLoadout.Sum(entry => entry.Count), Is.GreaterThan(0));
            Assert.That(data.WeaponLoadout.All(entry => entry.WeaponType == weapon), Is.True);
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            Assert.That(data.ComponentData.Range, Is.GreaterThanOrEqualTo(weapons.GetProfile(weapon).Range),
                "Stations only engage radar contacts; a shorter radar wastes weapon reach.");
            Assert.That(data.ComponentData.VisionRange, Is.EqualTo(data.ComponentData.Range));
            var catalog = AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(
                "Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
            Assert.That(catalog.Entries.ContainsKey(platform), Is.True);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Models/DefendStation/{platform}View.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one * 3f));
            HealthComponent health = prefab.GetComponentInChildren<HealthComponent>(true);
            Assert.That(health, Is.Not.Null);
            Assert.That(health.ShipUnits.Count, Is.EqualTo(data.WeaponLoadout.Sum(entry => entry.Count)),
                "Prefab mounts must match the data loadout.");
            Assert.That(health.ShipUnits.Distinct().Count(), Is.EqualTo(health.ShipUnits.Count));
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
                Assert.That(mount.TryGetWeaponType(out WeaponType mounted), Is.True);
                Assert.That(mounted, Is.EqualTo(weapon), "Prefab mounts must match the data loadout the AI scores.");
                Assert.That(bindings.Injectables, Contains.Item(mount), "Hardpoint destruction needs injected VFX and fog services.");
                SerializedObject point = new SerializedObject(mount);
                if (point.FindProperty("spawnDestroyedExplosion").boolValue)
                    Assert.That(point.FindProperty("destroyedVfxAnchor").objectReferenceValue, Is.Not.Null);

                var stats = data.ComponentData.HardPointHealth.Single(entry => entry.HardPointType == mount.HardPointType);
                Assert.That(stats.Health, Is.GreaterThan(0f));
                Assert.That(stats.HullDamageMultiplier, Is.GreaterThan(0f));
            }
            var sites = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            Assert.That(sites.GetAllowedFacilities(CaptureSiteKind.Satellite), Contains.Item(facility));
            Assert.That(sites.GetCost(facility).Icon, Is.Not.Null);
            Assert.That(sites.GetCost(facility).Icon.rect.size, Is.EqualTo(new Vector2(512f, 512f)));
        }

        [TestCase(DefendPlatformType.QuadLaserSatellite)]
        [TestCase(DefendPlatformType.TurbolaserSatellite)]
        [TestCase(DefendPlatformType.IonSatellite)]
        [TestCase(DefendPlatformType.MassDriverSatellite)]
        [TestCase(DefendPlatformType.MissileSatellite)]
        [TestCase(DefendPlatformType.RepairSatellite)]
        public void Satellite_ConstructionFitsReservedFootprintAndRestoresMaterials(DefendPlatformType platform)
        {
            GameObject instance = PrefabUtility.LoadPrefabContents(
                $"Assets/Prefabs/Models/DefendStation/{platform}View.prefab");
            try
            {
                instance.transform.position = new Vector3(113f, -40f, 217f);
                var construction = instance.GetComponent<ConstructionComponent>();
                Assert.That(construction, Is.Not.Null);
                var serialized = new SerializedObject(construction);
                SerializedProperty references = serialized.FindProperty("renderers");
                Renderer[] renderers = Enumerable.Range(0, references.arraySize)
                    .Select(i => (Renderer)references.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                Assert.That(renderers, Is.Not.Empty);
                Assert.That(renderers.All(renderer => renderer != null), Is.True);
                Material[][] originalMaterials = renderers.Select(renderer => renderer.sharedMaterials).ToArray();
                var constructionMaterial = (Material)serialized.FindProperty("constructionMaterial").objectReferenceValue;
                Assert.That(constructionMaterial, Is.Not.Null);
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Vector3 offset = bounds.center - instance.transform.position;
                float footprint = new Vector2(Mathf.Abs(offset.x) + bounds.extents.x,
                    Mathf.Abs(offset.z) + bounds.extents.z).magnitude;
                var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                    "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
                Assert.That(footprint, Is.LessThanOrEqualTo(settings.SatelliteFootprintRadius));

                int completions = 0;
                construction.Completed += () => completions++;
                construction.Begin(30f);
                Assert.That(construction.Progress, Is.Zero);
                Assert.That(construction.IsConstructing, Is.True);
                construction.Restore(0.5f, 30f);
                var properties = new MaterialPropertyBlock();
                foreach (Renderer renderer in renderers)
                {
                    Assert.That(renderer.sharedMaterials.All(material => material == constructionMaterial), Is.True);
                    renderer.GetPropertyBlock(properties);
                    Assert.That(properties.GetFloat("_BuildProgress"), Is.EqualTo(0.5f));
                    Assert.That(properties.GetFloat("_BuildMinY"), Is.EqualTo(bounds.min.y).Within(0.001f));
                    Assert.That(properties.GetFloat("_BuildMaxY"), Is.EqualTo(bounds.max.y).Within(0.001f));
                }
                Assert.That(completions, Is.Zero);
                construction.Restore(1f, 30f);
                Assert.That(construction.IsConstructing, Is.False);
                Assert.That(completions, Is.EqualTo(1));
                for (int i = 0; i < renderers.Length; i++)
                    Assert.That(renderers[i].sharedMaterials, Is.EqualTo(originalMaterials[i]));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        [TestCase(DefendPlatformType.QuadLaserSatellite, HardPointType.PointDefense)]
        [TestCase(DefendPlatformType.TurbolaserSatellite, HardPointType.Weapon)]
        public void DestroyedSatelliteTurret_IsNotRestoredByRepairOrShieldRegeneration(
            DefendPlatformType platform, HardPointType type)
        {
            DefendPlatformData data = LoadData(platform);
            var matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
            var model = new HealthModel(data.ComponentData, matrix, new CombatModifiers(), new HardPointHealthBudget());
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
            Assert.That(model.Hull, Is.EqualTo(data.ComponentData.Hull));
            Assert.That(model.Shields, Is.EqualTo(data.ComponentData.Shields));
        }

        [TestCase(DefendPlatformType.QuadLaserSatellite, "Quad Laser Satellite")]
        [TestCase(DefendPlatformType.TurbolaserSatellite, "Turbolaser Satellite")]
        [TestCase(DefendPlatformType.IonSatellite, "Ion Satellite")]
        [TestCase(DefendPlatformType.MassDriverSatellite, "Mass Driver Satellite")]
        [TestCase(DefendPlatformType.MissileSatellite, "Missile Satellite")]
        [TestCase(DefendPlatformType.RepairSatellite, "Repair Satellite")]
        [TestCase(DefendPlatformType.Xq6Repair, "XQ-6 Repair")]
        [TestCase(DefendPlatformType.BattleAsteroid, "Battle Asteroid")]
        public void DefendPlatformTooltipFacade_ShowsPlatformTypeAsTitle(DefendPlatformType platform, string expectedTitle)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(
                "Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
            var icons = AssetDatabase.LoadAssetAtPath<TooltipIconData>(
                "Assets/Settings/Data/Tooltip/TooltipIconData.asset");
            var facade = new DefendPlatformTooltipFacade(catalog, platform, new StubHealthTooltip());

            TooltipContent content = facade.Build(new List<TooltipStat>(), "Owner: Player · Structure");

            Assert.That(content.Title, Is.EqualTo(expectedTitle));
            if (platform == DefendPlatformType.BattleAsteroid)
                Assert.That(content.Stats.Select(stat => stat.Label), Is.EquivalentTo(new[] { "Hull", "Shields" }));
            Assert.That(content.Status, Is.EqualTo("Owner: Player · Structure"));
            if (platform != DefendPlatformType.BattleAsteroid)
                Assert.That(icons.Resolve(content.IconKey), Is.Not.Null);
        }

        [Test]
        public void RepairSatellite_IsAnUnarmedBuildableSatelliteWithARepairProfileAndRangeRing()
        {
            Assert.That(SiteFacilityType.RepairSatellite.ToSatelliteType(), Is.EqualTo(DefendPlatformType.RepairSatellite));
            Assert.That(SiteFacilityBuilder.IsSatellite(SiteFacilityType.RepairSatellite), Is.True);
            DefendPlatformData data = LoadData(DefendPlatformType.RepairSatellite);
            Assert.That(data.Income, Is.Zero);
            Assert.That(data.WeaponLoadout, Is.Empty);
            RepairProfile profile = data.RepairProfile;
            Assert.That(profile, Is.Not.Null, "The repair profile enables the repair feature.");
            Assert.That(profile.Radius, Is.GreaterThan(0f));
            Assert.That(profile.Interval, Is.GreaterThan(0f));
            Assert.That(profile.PercentPerTick, Is.GreaterThan(0f).And.LessThanOrEqualTo(100f));
            Assert.That(profile.MaxTargets, Is.GreaterThanOrEqualTo(1));
            Assert.That(profile.MaxHealersPerTarget, Is.GreaterThanOrEqualTo(1));

            var catalog = AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(
                "Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
            Assert.That(catalog.Entries.ContainsKey(DefendPlatformType.RepairSatellite), Is.True);
            var sites = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            Assert.That(sites.GetAllowedFacilities(CaptureSiteKind.Satellite), Contains.Item(SiteFacilityType.RepairSatellite));
            SiteFacilityCost cost = sites.GetCost(SiteFacilityType.RepairSatellite);
            Assert.That(cost.Price, Is.GreaterThan(0f));
            Assert.That(cost.BuildTime, Is.GreaterThan(0f));
            Assert.That(cost.Icon, Is.Not.Null);
            Assert.That(cost.Icon.rect.size, Is.EqualTo(new Vector2(512f, 512f)));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/DefendStation/RepairSatelliteView.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one * 3f));
            Assert.That(prefab.GetComponentInChildren<HealthComponent>(true).ShipUnits, Is.Empty,
                "Without hardpoints the hull is the only target.");
            EntityBindings bindings = prefab.GetComponent<EntityBindings>();
            Assert.That(bindings.Injectables.All(component => component != null), Is.True);
            RepairRangeComponent ring = bindings.RepairRange;
            Assert.That(ring, Is.Not.Null);
            Assert.That(bindings.Injectables, Has.No.Member(ring), "RegisterRepair binds the ring only for repairers.");
            var renderer = (MeshRenderer)new SerializedObject(ring).FindProperty("ring").objectReferenceValue;
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.enabled, Is.False, "The range shows only while the satellite is selected.");
            Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("EmpireAtWar/Vfx/SelectionRange"));
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
