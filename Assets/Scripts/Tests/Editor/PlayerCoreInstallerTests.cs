using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using NUnit.Framework;
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
            FactionsData factionsData = ScriptableObject.CreateInstance<FactionsData>();
            using TestAssetService assets = new TestAssetService();

            try
            {
                DiContainer parent = new DiContainer();
                parent.Bind<FactionType>().WithId(PlayerType.Player).FromInstance(factionType);
                parent.Bind<FactionsData>().FromInstance(factionsData);
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
                Object.DestroyImmediate(factionsData);
                Object.DestroyImmediate(root);
            }
        }

        private sealed class TestAssetService : IAssetService, IDisposable
        {
            private readonly GameObject _unitPrefab = new GameObject("UnitPrefab");
            private readonly Dictionary<Type, ScriptableObject> _data = new Dictionary<Type, ScriptableObject>
            {
                { typeof(ReinforcementData), ScriptableObject.CreateInstance<ReinforcementData>() },
                { typeof(EconomyData), ScriptableObject.CreateInstance<EconomyData>() }
            };

            public TSource Load<TSource>(string key) where TSource : Object
            {
                return typeof(TSource) == typeof(GameObject)
                    ? (TSource)(Object)_unitPrefab
                    : (TSource)(Object)_data[typeof(TSource)];
            }

            public TComponent LoadComponent<TComponent>(string key) where TComponent : Component
            {
                throw new NotSupportedException();
            }

            public GameObject LoadPrefab(string key)
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                foreach (ScriptableObject data in _data.Values)
                {
                    Object.DestroyImmediate(data);
                }

                Object.DestroyImmediate(_unitPrefab);
            }
        }
    }
}
