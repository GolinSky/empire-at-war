using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Repository;
using EmpireAtWar.Repository.Data;
using EmpireAtWar.ViewComponents.Wreck;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleAssetPreloaderTests
    {
        [TestCase(GameMode.Skirmish, FactionType.Republic, FactionType.Separatist)]
        [TestCase(GameMode.Skirmish, FactionType.Empire, FactionType.Rebellion)]
        [TestCase(GameMode.Survival, FactionType.Republic, FactionType.Separatist)]
        [TestCase(GameMode.Survival, FactionType.Empire, FactionType.Rebellion)]
        [TestCase(GameMode.BattleSimulation, FactionType.Republic, FactionType.Separatist)]
        [TestCase(GameMode.BattleSimulation, FactionType.Empire, FactionType.Rebellion)]
        public void Prepare_TransfersTypedCacheAndItsHandleOwnership(GameMode mode, FactionType first, FactionType second)
        {
            using (var loader = new EditorAssetLoader())
            using (var preloader = new BattleAssetPreloader(loader))
            {
                preloader.PrepareAsync(CreateBattle(mode, first, second), true, CancellationToken.None).GetAwaiter().GetResult();
                int loadedCount = loader.Handles.Count;
                using (CachedAssetService assets = preloader.TakeAssets())
                {
                    preloader.Dispose();
                    Assert.That(loader.Handles.All(handle => handle.IsValid()), Is.True);
                    FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
                    ShipsData ships = assets.Load<ShipsData>(nameof(ShipsData));
                    SpaceStationData stations = assets.Load<SpaceStationData>(nameof(SpaceStationData));
                    foreach (FactionType factionType in new[] { first, second })
                    {
                        FactionDefinition faction = factions.Get(factionType);
                        assets.LoadComponent<EmpireAtWar.Entities.SpaceStation.SpaceStation>(factionType + "SpaceStationView");
                        foreach (ShipType ship in faction.Ships.Keys)
                        {
                            ShipData data = assets.Load<ShipData>(ships.GetShipDataPath(ship));
                            assets.LoadComponent<ShipEntity>(ship + "ShipView");
                            if (data.Wreck != null) assets.LoadComponent<UnitWreckView>(data.Wreck.PrefabKey);
                        }
                        foreach (SquadronType squadron in faction.Squadrons.Keys)
                        {
                            assets.Load<SquadronData>(squadron + "SquadronData");
                            assets.LoadComponent<Squadron>(squadron + "SquadronView");
                        }
                        if (stations.TryGetWreck(factionType, out var stationWreck))
                            assets.LoadComponent<UnitWreckView>(stationWreck.PrefabKey);
                    }
                    assets.LoadPrefab("CoreGameUi");
                    if (mode == GameMode.Survival) assets.LoadPrefab("SurvivalHudUi");
                    if (mode == GameMode.BattleSimulation) assets.LoadPrefab("SimulationHudUi");
                    Assert.That(loader.Handles.Count, Is.EqualTo(loadedCount), "Factory keys and types must hit the preload cache.");
                    Assert.That(loader.Handles.All(handle => handle.IsValid()), Is.True);
                    Assert.Throws<InvalidOperationException>(() => preloader.TakeAssets());
                }
                Assert.That(loader.Handles.All(handle => !handle.IsValid()), Is.True);
            }
        }

        [TestCase(FactionType.Republic, FactionType.Separatist)]
        [TestCase(FactionType.Empire, FactionType.Rebellion)]
        public void Prepare_WithoutRestoreLeavesUnitViewsAndWrecksToTheWarmup(FactionType first, FactionType second)
        {
            BattleDefinition battle = CreateBattle(GameMode.Skirmish, first, second);
            using (var loader = new EditorAssetLoader())
            using (var preloader = new BattleAssetPreloader(loader))
            {
                preloader.PrepareAsync(battle, false, CancellationToken.None).GetAwaiter().GetResult();
                using (CachedAssetService assets = preloader.TakeAssets())
                {
                    int requiredCount = loader.Handles.Count;
                    List<string> deferred = BattleAssetPreload.DeferredPrefabKeys(assets, battle);
                    Assert.That(loader.Handles.Count, Is.EqualTo(requiredCount), "Deferred keys come from preloaded data.");
                    Assert.That(deferred, Has.Some.EndsWith("ShipView"));

                    foreach (string key in deferred) assets.BeginPreload<GameObject>(key);
                    assets.CompletePreloadsAsync(CancellationToken.None).GetAwaiter().GetResult();
                    Assert.That(loader.Handles.Count, Is.EqualTo(requiredCount + deferred.Distinct().Count()),
                        "Required preload must not contain deferred prefabs.");

                    int warmedCount = loader.Handles.Count;
                    FactionCatalog factions = assets.Load<FactionCatalog>(nameof(FactionCatalog));
                    foreach (FactionType factionType in new[] { first, second })
                    {
                        foreach (ShipType ship in factions.Get(factionType).Ships.Keys)
                            assets.LoadComponent<ShipEntity>(ship + "ShipView");
                    }
                    assets.LoadComponent<BoxCollider>("MiningFacilityView");
                    Assert.That(loader.Handles.Count, Is.EqualTo(warmedCount), "Factory keys must hit the warmed cache.");
                }
                Assert.That(loader.Handles.All(handle => !handle.IsValid()), Is.True);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Prepare_CancellationOrFailureReleasesAllPendingHandles(bool cancel)
        {
            using (var cancellation = new CancellationTokenSource())
            using (var loader = new EditorAssetLoader())
            using (var preloader = new BattleAssetPreloader(loader))
            {
                if (cancel)
                    loader.AfterLoad = () => { if (loader.Handles.Count == 4) cancellation.Cancel(); };
                else
                    loader.FailKey = "StationLevelData";
                TestDelegate prepare = () => preloader.PrepareAsync(
                    SkirmishPreset.Default(), false, cancellation.Token).GetAwaiter().GetResult();
                if (cancel) Assert.Throws<OperationCanceledException>(prepare);
                else Assert.Throws<InvalidOperationException>(prepare);
                Assert.That(loader.Handles.Count, Is.GreaterThan(1));
                Assert.That(loader.Handles.All(handle => !handle.IsValid()), Is.True);
                Assert.Throws<InvalidOperationException>(() => preloader.TakeAssets());
            }
        }

        private static BattleDefinition CreateBattle(GameMode mode, FactionType first, FactionType second)
        {
            if (mode == GameMode.Survival)
                return SurvivalPreset.Create(new SurvivalSettings(first, first, second, MapSize.Small,
                    EnemyAiDifficulty.Medium, true)).Build();
            if (mode == GameMode.BattleSimulation)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(
                    AssetDatabase.FindAssets("t:FactionCatalog").Single()));
                var firstFleet = new[] { new SimulationFleetEntry(UnitTypeId.Ship(catalog.Get(first).Ships.Keys.First()), 1) };
                var secondFleet = new[] { new SimulationFleetEntry(UnitTypeId.Ship(catalog.Get(second).Ships.Keys.First()), 1) };
                return SimulationPreset.Create(new SimulationSetup(first, second, firstFleet, secondFleet, 1000f, 30f, false)).Build();
            }
            var players = new[]
            {
                new PlayerSlot(new PlayerId(0), new TeamId(0), first, PlayerController.Human, EnemyAiDifficulty.Medium, 0),
                new PlayerSlot(new PlayerId(1), new TeamId(1), second, PlayerController.Ai, EnemyAiDifficulty.Medium, 1)
            };
            return SkirmishPreset.Create(players, PlanetType.Coruscant, MapSize.Small,
                BattleVictoryCondition.DestroyEnemyFleet, 1000f, true, 123).Build();
        }

        private sealed class EditorAssetLoader : IAssetHandleLoader, IDisposable
        {
            private readonly ResourceManager _resourceManager = new ResourceManager();
            private readonly AssetMappingData _mapping = AssetDatabase.LoadAssetAtPath<AssetMappingData>(
                "Assets/Settings/AssetMappingData.asset");
            private readonly AddressableAssetSettings _settings = AddressableAssetSettingsDefaultObject.Settings;

            public List<AsyncOperationHandle> Handles { get; } = new List<AsyncOperationHandle>();
            public string FailKey { get; set; }
            public Action AfterLoad { get; set; } = () => { };

            public AsyncOperationHandle<TSource> Load<TSource>(string key) where TSource : Object
            {
                if (key == FailKey) throw new InvalidOperationException("Injected asset-load failure.");
                string resolved = _mapping.GetAssetKey(key);
                AddressableAssetEntry entry = _settings.FindAssetEntry(resolved) ?? _settings.groups
                    .SelectMany(group => group.entries).SingleOrDefault(candidate => candidate.address == resolved);
                Assert.That(entry, Is.Not.Null, $"Missing Addressable key: {key} ({resolved}).");
                TSource asset = AssetDatabase.LoadAssetAtPath<TSource>(entry.AssetPath);
                Assert.That(asset, Is.Not.Null, $"{key} must resolve as {typeof(TSource).Name}: {entry.AssetPath}.");
                AsyncOperationHandle<TSource> handle = _resourceManager.CreateCompletedOperation(asset, string.Empty);
                Handles.Add(handle);
                AfterLoad();
                return handle;
            }

            public void Dispose()
            {
                foreach (AsyncOperationHandle handle in Handles)
                    if (handle.IsValid()) handle.Release();
                _resourceManager.Dispose();
            }
        }
    }
}
