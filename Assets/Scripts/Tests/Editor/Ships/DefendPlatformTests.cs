using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Construction;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Repair;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Player;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class DefendPlatformTests
    {
        [Test]
        public void Initialize_AppliesInjectedStartPosition()
        {
            GameObject gameObject = new GameObject(nameof(DefendPlatformTests));
            DefendPlatformData data = ScriptableObject.CreateInstance<DefendPlatformData>();

            try
            {
                DefendPlatform platform = gameObject.AddComponent<DefendPlatform>();
                HealthComponentStub healthComponent = new HealthComponentStub();
                Vector3 startPosition = new Vector3(12f, 0f, -34f);

                SetField(platform, "_healthComponent", healthComponent);
                SetField(platform, "_radarModel", new RadarModelStub());
                SetField(platform, "_startPosition", startPosition);
                AttachCompletedConstruction(platform);
                SetField(platform, "_teamIncome", new TeamIncomeSource(0f, platform.Construction,
                    new ResearchStub(), TestPlayers.CreateTeamGame(), new PlayerRegistry(), TestPlayers.Human));
                typeof(DefendPlatform).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(platform, data);

                platform.Initialize();

                Assert.That(platform.transform.position, Is.EqualTo(startPosition));
                Assert.That(platform.Income, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Xq6_IsArmedAndDoesNotRepair()
        {
            DefendPlatformData data = LoadData(DefendPlatformType.Xq6);
            Assert.That(data.RepairProfile, Is.Null, "Healing belongs to the XQ6 repair platform.");

            GameObject prefab = LoadView(DefendPlatformType.Xq6);
            EntityBindings bindings = prefab.GetComponent<EntityBindings>();
            Assert.That(bindings.RepairRange, Is.Null);
            Assert.That(prefab.GetComponentsInChildren<RepairRangeComponent>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<WeaponHardPoint>(true), Is.Not.Empty);
        }

        [Test]
        public void Xq6Repair_IsAnUnarmedCopyOfTheXq6WithARepairProfileAndAHiddenRangeRing()
        {
            DefendPlatformData armed = LoadData(DefendPlatformType.Xq6);
            DefendPlatformData data = LoadData(DefendPlatformType.Xq6Repair);
            Assert.That(data.RepairProfile, Is.Not.Null, "The repair profile enables healing.");
            Assert.That(data.WeaponLoadout, Is.Empty);
            Assert.That(data.ComponentData.Hull, Is.EqualTo(armed.ComponentData.Hull));
            Assert.That(data.ComponentData.Shields, Is.EqualTo(armed.ComponentData.Shields));
            Assert.That(data.ComponentData.ShipClass, Is.EqualTo(armed.ComponentData.ShipClass));

            var catalog = AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(
                "Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
            FactionData armedEntry = catalog.Get(DefendPlatformType.Xq6);
            FactionData repairEntry = catalog.Get(DefendPlatformType.Xq6Repair);
            Assert.That(repairEntry.Price, Is.EqualTo(armedEntry.Price), "Same price as the armed XQ6.");
            Assert.That(repairEntry.BuildTime, Is.EqualTo(armedEntry.BuildTime));
            Assert.That(repairEntry.MaxCount, Is.EqualTo(armedEntry.MaxCount));
            Assert.That(repairEntry.AvailableLevel, Is.EqualTo(armedEntry.AvailableLevel));
            Assert.That(repairEntry.Icon, Is.Not.Null.And.Not.SameAs(armedEntry.Icon));

            GameObject prefab = LoadView(DefendPlatformType.Xq6Repair);
            Assert.That(prefab.GetComponentsInChildren<WeaponHardPoint>(true), Is.Empty, "No guns.");
            Assert.That(prefab.GetComponentInChildren<HealthComponent>(true).ShipUnits.All(unit => unit != null), Is.True);
            EntityBindings bindings = prefab.GetComponent<EntityBindings>();
            Assert.That(bindings.Injectables.All(component => component != null), Is.True);
            RepairRangeComponent ring = bindings.RepairRange;
            Assert.That(ring, Is.Not.Null);
            Assert.That(bindings.Injectables, Has.No.Member(ring), "RegisterRepair binds the ring only for repairers.");
            var renderer = (MeshRenderer)new SerializedObject(ring).FindProperty("ring").objectReferenceValue;
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.enabled, Is.False, "The range shows only while the platform is selected.");
            Material hull = prefab.transform.Find("default").GetComponent<MeshRenderer>().sharedMaterial;
            Material armedHull = LoadView(DefendPlatformType.Xq6).transform.Find("default").GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(hull, Is.Not.SameAs(armedHull), "The repair platform has its own hull material.");
            Assert.That(hull.IsKeywordEnabled("_RADIAL_HULL_MARKINGS"), Is.True, "The medical emblems are projected markings.");
            Texture marking = hull.GetTexture("_HullMarkingMap");
            Assert.That(marking, Is.Not.Null);
            Assert.That(marking, Is.Not.SameAs(armedHull.GetTexture("_BaseMap")), "The marking is the medical emblem, not the hull albedo.");
        }

        [Test]
        public void Xq6Repair_IsOfferedByEveryFactionThatOffersTheXq6()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:FactionDefinition"))
            {
                var faction = AssetDatabase.LoadAssetAtPath<FactionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (faction.DefendPlatforms.Contains(DefendPlatformType.Xq6))
                    Assert.That(faction.DefendPlatforms, Contains.Item(DefendPlatformType.Xq6Repair), faction.name);
            }
        }

        private static DefendPlatformData LoadData(DefendPlatformType type) =>
            AssetDatabase.LoadAssetAtPath<DefendPlatformData>(
                $"Assets/Settings/Data/Models/DefendPlatform/{DefendPlatformAssets.GetDataName(type)}.asset");

        private static GameObject LoadView(DefendPlatformType type) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/Models/DefendStation/{DefendPlatformAssets.GetViewName(type)}.prefab");

        [Test]
        public void BattleAsteroid_SplitsIncomeWithAlliesAndRecalculatesResearchUntilReleased()
        {
            GameObject gameObject = new GameObject(nameof(DefendPlatformTests));
            try
            {
                DefendPlatform platform = gameObject.AddComponent<DefendPlatform>();
                DefendPlatformData data = AssetDatabase.LoadAssetAtPath<DefendPlatformData>(
                    "Assets/Settings/Data/Models/DefendPlatform/AsteroidDefendPlatformData.asset");
                Assert.That(data.Income, Is.GreaterThan(0f));
                ResearchStub research = new ResearchStub();
                PlayerRegistry registry = new PlayerRegistry();
                EconomyProbe owner = new EconomyProbe();
                EconomyProbe ally = new EconomyProbe();
                EconomyProbe enemy = new EconomyProbe();
                registry.RegisterEconomy(TestPlayers.Human, owner);
                registry.RegisterEconomy(TestPlayers.Ally, ally);
                registry.RegisterEconomy(TestPlayers.Enemy, enemy);
                SetField(platform, "_healthComponent", new HealthComponentStub());
                SetField(platform, "_radarModel", new RadarModelStub());
                SetField(platform, "_componentLifecycle", new EntityComponentLifecycle(Array.Empty<IMonoComponent>()));
                AttachCompletedConstruction(platform);
                SetField(platform, "_teamIncome", new TeamIncomeSource(data.Income, platform.Construction,
                    research, TestPlayers.CreateTeamGame(), registry, TestPlayers.Human));
                typeof(DefendPlatform).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(platform, data);

                platform.Initialize();
                // The owner and one ally split the platform income evenly.
                float share = data.Income / 2f;
                Assert.That(owner.Income, Is.EqualTo(share).Within(0.0001f));
                Assert.That(ally.Income, Is.EqualTo(share).Within(0.0001f));
                Assert.That(enemy.Income, Is.Zero);

                research.Complete(1.2f);
                Assert.That(owner.Income, Is.EqualTo(share * 1.2f).Within(0.0001f));
                Assert.That(ally.Income, Is.EqualTo(share * 1.2f).Within(0.0001f));
                research.Complete(1.4f);
                Assert.That(owner.Income, Is.EqualTo(share * 1.4f).Within(0.0001f));
                Assert.That(ally.Income, Is.EqualTo(share * 1.4f).Within(0.0001f));

                platform.LateDispose();
                Assert.That(owner.Income, Is.Zero);
                Assert.That(ally.Income, Is.Zero);
                research.Complete(1.4f);
                Assert.That(owner.Income, Is.Zero);
                Assert.That(ally.Income, Is.Zero);
                platform.LateDispose();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [TestCase("DefendPlatformData")]
        [TestCase("AotrEmpressDefensePlatformData")]
        [TestCase("AotrGolanIIIDefensePlatformData")]
        public void OtherDefendPlatforms_HaveNoIncome(string assetName)
        {
            DefendPlatformData data = AssetDatabase.LoadAssetAtPath<DefendPlatformData>(
                $"Assets/Settings/Data/Models/DefendPlatform/{assetName}.asset");
            Assert.That(data.Income, Is.Zero);
        }

        private sealed class EconomyProbe : IEconomyProvider
        {
            public float Income { get; private set; }
            public void AddMoney(float amount) { }
            public void AddProvider(IIncomeProvider provider) => Income = provider.Income;
            public void RemoveProvider(IIncomeProvider provider) => Income = 0f;
            public void RecalculateIncome(IIncomeProvider provider) => Income = provider.Income;
        }

        private sealed class ResearchStub : IFactionResearchModelObserver
        {
            public event Action<ResearchType> OnResearchCompleted;
            public IReadOnlyCollection<ResearchType> ResearchTypes => Array.Empty<ResearchType>();
            public float IncomeMultiplier { get; private set; } = 1f;
            public float StationIncomeMultiplier => 1f;
            public bool TryGetNextTier(ResearchType type, out ResearchTierData tier) { tier = null; return false; }
            public int GetCompletedTiers(ResearchType type) => 0;
            public float GetMultiplier(ResearchStat stat, ShipClass shipClass) => 1f;
            public void Complete(float multiplier)
            {
                IncomeMultiplier = multiplier;
                OnResearchCompleted?.Invoke(ResearchType.IncreasedProduction);
            }
        }

        // A zero build time completes construction on Begin, so the platform runs at full health.
        private static void AttachCompletedConstruction(DefendPlatform platform)
        {
            ConstructionComponent construction = platform.gameObject.AddComponent<ConstructionComponent>();
            typeof(ConstructionComponent).GetField("renderers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(construction, Array.Empty<Renderer>());
            CombatModifiers modifiers = new CombatModifiers();
            SetField(platform, "construction", construction);
            SetField(platform, "_combatModifiers", modifiers);
            SetField(platform, "_healthModel", new HealthModel(new HealthDataStub(), null, modifiers, new HardPointHealthBudget()));
        }

        private static void SetField<T>(DefendPlatform platform, string fieldName, T value)
        {
            FieldInfo field = typeof(DefendPlatform).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(platform, value);
        }

        private sealed class RadarModelStub : IRadarModelObserver
        {
            public float Range => 0f;
            public float Delay => 0f;
            public ObservableList<IEntity> Enemies { get; } = new ObservableList<IEntity>();
        }

        private sealed class HealthDataStub : IHealthData
        {
            public ShipClass ShipClass => ShipClass.Structure;
            public float Hull => 100f;
            public float Shields => 100f;
            public float ShieldRegenerateValue => 1f;
            public float ShieldRegenerateDelay => 1f;
            public bool UsesHullHardPointBudget => false;
            public IReadOnlyList<HardPointHealth> HardPointHealth => Array.Empty<HardPointHealth>();
        }

        private sealed class HealthComponentStub : IHealthComponent
        {
            public string Id => nameof(HealthComponentStub);
            public bool Destroyed => false;
            public IHealthModelObserver HealthModelObserver { get; } = new HealthModelStub();

            public void ApplyDamage(float damage, DamageType damageType, int shipUnitId)
            {
            }

            public bool Equal(IHealthModelObserver modelObserver)
            {
                return false;
            }
        }

        private sealed class HealthModelStub : IHealthModelObserver
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public ShipClass ShipClass => ShipClass.Structure;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }
    }
}
