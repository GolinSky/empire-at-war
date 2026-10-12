using System;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PlayerCoreInstallerTests
    {
        [TestCase(FactionType.Republic)]
        [TestCase(FactionType.Separatist)]
        public void InstallBindings_ResolvesPlayerModelWithSelectedFaction(FactionType factionType)
        {
            GameObject root = new GameObject(nameof(PlayerCoreInstallerTests));
            root.SetActive(false);
            using TestAssetService assets = new TestAssetService();

            try
            {
                DiContainer parent = new DiContainer();
                parent.Bind<FactionType>().WithId(TestPlayers.Human).FromInstance(factionType);
                PlayerRoster roster = new PlayerRoster(new[]
                {
                    new PlayerSlot(TestPlayers.Human, new TeamId(0), factionType, PlayerController.Human,
                        EnemyAiDifficulty.Medium, 0)
                });
                parent.Bind<ILocalPlayer>().FromInstance(TestPlayers.CreateLocalPlayer(roster));
                const string sharedPath = "Assets/Settings/Data/Factions/Shared/";
                parent.Bind<FactionCatalog>().FromInstance(
                    AssetDatabase.LoadAssetAtPath<FactionCatalog>(sharedPath + "FactionCatalog.asset"));
                parent.Bind<StationLevelData>().FromInstance(
                    AssetDatabase.LoadAssetAtPath<StationLevelData>(sharedPath + "StationLevelData.asset"));
                parent.Bind<MiningFacilityCatalog>().FromInstance(
                    AssetDatabase.LoadAssetAtPath<MiningFacilityCatalog>(sharedPath + "MiningFacilityCatalog.asset"));
                parent.Bind<DefendPlatformCatalog>().FromInstance(
                    AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(sharedPath + "DefendPlatformCatalog.asset"));
                parent.Bind<SuperWeaponCatalog>().FromInstance(
                    AssetDatabase.LoadAssetAtPath<SuperWeaponCatalog>(sharedPath + "SuperWeaponCatalog.asset"));
                parent.Bind<ReinforcementData>().FromInstance(assets.ReinforcementData);
                parent.Bind<EconomyData>().FromInstance(assets.EconomyData);
                DiContainer container = parent.CreateSubContainer();
                container.Bind<IAssetService>().FromInstance(assets);
                container.Bind<Zenject.SceneContext>().FromInstance(root.AddComponent<Zenject.SceneContext>());
                PlayerCoreInstaller installer = root.AddComponent<PlayerCoreInstaller>();
                container.Inject(installer);

                installer.InstallBindings();
                PlayerFactionModel model = container.Resolve<PlayerFactionModel>();

                Assert.That(model.FactionType, Is.EqualTo(factionType));
                Assert.That(container.Resolve<IPlayerFactionModelObserver>(), Is.SameAs(model));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
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

            // Player data is shared from the scene container; the player installer loads only unit prefabs.
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
