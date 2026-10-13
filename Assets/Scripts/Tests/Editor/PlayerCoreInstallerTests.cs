using System;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Mvc;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;
using EmpireAtWar.Services.VContainer;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PlayerCoreInstallerTests
    {
        [TestCase(FactionType.Republic)]
        [TestCase(FactionType.Separatist)]
        public void RegisterPlayerServices_ResolvesPlayerModelWithSelectedFaction(FactionType factionType)
        {
            using TestAssetService assets = new TestAssetService();
            PlayerSlot owner = new PlayerSlot(TestPlayers.Human, new TeamId(0), factionType,
                PlayerController.Human, EnemyAiDifficulty.Medium, 0);
            PlayerRoster roster = new PlayerRoster(new[] { owner });
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<ILocalPlayer>(TestPlayers.CreateLocalPlayer(roster));
            const string SHARED_PATH = "Assets/Settings/Data/Factions/Shared/";
            FactionCatalog factions = AssetDatabase.LoadAssetAtPath<FactionCatalog>(SHARED_PATH + "FactionCatalog.asset");
            builder.RegisterInstance(factions);
            builder.RegisterInstance(AssetDatabase.LoadAssetAtPath<StationLevelData>(SHARED_PATH + "StationLevelData.asset"));
            builder.RegisterInstance(AssetDatabase.LoadAssetAtPath<MiningFacilityCatalog>(SHARED_PATH + "MiningFacilityCatalog.asset"));
            builder.RegisterInstance(AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(SHARED_PATH + "DefendPlatformCatalog.asset"));
            builder.RegisterInstance(AssetDatabase.LoadAssetAtPath<SuperWeaponCatalog>(SHARED_PATH + "SuperWeaponCatalog.asset"));
            builder.RegisterInstance(assets.ReinforcementData);
            builder.RegisterInstance(assets.EconomyData);
            using IObjectResolver parent = builder.Build();
            using IScopedObjectResolver container = parent.CreateScope(childBuilder =>
            {
                childBuilder.RegisterInstance<IAssetService>(assets);
                LocalPlayerScope.RegisterPlayerServices(childBuilder, owner, factions.Get(factionType));
                Assert.That(childBuilder.Exists(typeof(IReinforcementService), includeInterfaceTypes: true, findParentScopes: false), Is.True);
            });
            PlayerFactionModel model = container.Resolve<PlayerFactionModel>();
            Assert.That(model.FactionType, Is.EqualTo(factionType));
            Assert.That(container.Resolve<IPlayerFactionModelObserver>(), Is.SameAs(model));
            Assert.That(parent.TryGetRegistration(typeof(IReinforcementService), out _), Is.False);
        }

        private sealed class TestAssetService : IAssetService, IDisposable
        {
            private readonly GameObject _unitPrefab = new GameObject("UnitPrefab");

            public ReinforcementData ReinforcementData { get; } = ScriptableObject.CreateInstance<ReinforcementData>();
            public EconomyData EconomyData { get; } = ScriptableObject.CreateInstance<EconomyData>();

            public void Dispose()
            {
                Object.DestroyImmediate(ReinforcementData);
                Object.DestroyImmediate(EconomyData);
                Object.DestroyImmediate(_unitPrefab);
            }

            // Shared scene data remains available to the child player registrations.
            public TSource Load<TSource>(string key) where TSource : Object
            {
                return (TSource)(Object)_unitPrefab;
            }

            public TComponent LoadComponent<TComponent>(string key) where TComponent : Component
            {
                throw new NotSupportedException();
            }

            public GameObject LoadPrefab(string key)
            {
                throw new NotSupportedException();
            }
        }
    }
}
