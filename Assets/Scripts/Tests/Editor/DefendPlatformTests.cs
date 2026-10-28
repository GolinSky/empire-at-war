using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Player;
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
                SetField(platform, "_startPosition", startPosition);
                typeof(DefendPlatform).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(platform, data);

                platform.Initialize();

                Assert.That(platform.transform.position, Is.EqualTo(startPosition));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void BattleAsteroid_SplitsIncomeWithAlliesAndRecalculatesResearchUntilReleased()
        {
            GameObject gameObject = new GameObject(nameof(DefendPlatformTests));
            try
            {
                DefendPlatform platform = gameObject.AddComponent<DefendPlatform>();
                DefendPlatformData data = AssetDatabase.LoadAssetAtPath<DefendPlatformData>(
                    "Assets/Settings/Data/Models/DefendPlatform/AsteroidDefendPlatformData.asset");
                Assert.That(data.Income, Is.EqualTo(10f));
                ResearchStub research = new ResearchStub();
                PlayerRegistry registry = new PlayerRegistry();
                EconomyProbe owner = new EconomyProbe();
                EconomyProbe ally = new EconomyProbe();
                EconomyProbe enemy = new EconomyProbe();
                registry.RegisterEconomy(TestPlayers.Human, owner);
                registry.RegisterEconomy(TestPlayers.Ally, ally);
                registry.RegisterEconomy(TestPlayers.Enemy, enemy);
                SetField(platform, "_healthComponent", new HealthComponentStub());
                SetField(platform, "_playerRoster", TestPlayers.CreateTeamGame());
                SetField(platform, "_playerRegistry", registry);
                SetField(platform, "_owner", TestPlayers.Human);
                SetField(platform, "_research", research);
                SetField(platform, "_componentLifecycle", new EntityComponentLifecycle(Array.Empty<IMonoComponent>()));
                typeof(DefendPlatform).GetProperty("Data", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(platform, data);

                platform.Initialize();
                // The owner and one ally split the 10 credits evenly.
                Assert.That(owner.Income, Is.EqualTo(5f));
                Assert.That(ally.Income, Is.EqualTo(5f));
                Assert.That(enemy.Income, Is.Zero);

                research.Complete(1.2f);
                Assert.That(owner.Income, Is.EqualTo(6f));
                Assert.That(ally.Income, Is.EqualTo(6f));
                research.Complete(1.4f);
                Assert.That(owner.Income, Is.EqualTo(7f));
                Assert.That(ally.Income, Is.EqualTo(7f));

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

        private static void SetField<T>(DefendPlatform platform, string fieldName, T value)
        {
            FieldInfo field = typeof(DefendPlatform).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(platform, value);
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
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
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
