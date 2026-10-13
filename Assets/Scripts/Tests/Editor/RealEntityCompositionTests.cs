using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Health.Overlay;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extensions;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using global::VContainer;
using global::VContainer.Internal;
using global::VContainer.Unity;
using Object = UnityEngine.Object;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class RealEntityCompositionTests
    {
        private const string PROJECT_PREFAB_PATH = "Assets/Resources/ProjectContext.prefab";
        private const string SKIRMISH_PREFAB_PATH = "Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab";
        private const string BATTLE_SCENE_PATH = "Assets/Scenes/Battle/Battle.unity";
        private const int MAP_SEED = 73517;
        private readonly HashSet<long> _entityIds = new HashSet<long>();
        private readonly HashSet<CombatModifiers> _modifiers = new HashSet<CombatModifiers>();
        private readonly HashSet<IHealthModelObserver> _health = new HashSet<IHealthModelObserver>();
        private GameObject _root;
        private ProjectScope _project;
        private SkirmishScope _battle;
        private float _timeScale;
        private int _vSync;
        private int _frameRate;
        private int _quality;

        [SetUp]
        public void SetUp()
        {
            _timeScale = Time.timeScale;
            _vSync = QualitySettings.vSyncCount;
            _frameRate = Application.targetFrameRate;
            _quality = QualitySettings.GetQualityLevel();
            _root = new GameObject(nameof(RealEntityCompositionTests));
            _root.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                try { DisposeRoot(_battle); }
                finally { DisposeRoot(_project); }
            }
            finally
            {
                Object.DestroyImmediate(_root);
                Time.timeScale = _timeScale;
                if (QualitySettings.GetQualityLevel() != _quality) QualitySettings.SetQualityLevel(_quality, true);
                QualitySettings.vSyncCount = _vSync;
                Application.targetFrameRate = _frameRate;
                _entityIds.Clear();
                _modifiers.Clear();
                _health.Clear();
            }
        }

        [Test]
        public void ActualScopes_AllSevenUnitFactoriesBuildForLocalAndAiOwnersAndPoolWithoutRebuilding()
        {
            Assert.That(Application.isPlaying, Is.False);
            MemorySettings repository = new MemorySettings();
            CompletedStartup startup = new CompletedStartup();
            _project = InstantiateScope<ProjectScope>(PROJECT_PREFAB_PATH);
            _battle = InstantiateScope<SkirmishScope>(SKIRMISH_PREFAB_PATH);
            _battle.parentReference.Object = _project;
            _root.SetActive(true);
            using (LifetimeScope.EnqueueParent(null))
            using (LifetimeScope.Enqueue(builder =>
            {
                if (ReferenceEquals(builder.ApplicationOrigin, _project))
                    builder.RegisterInstance<ISettingsRepository>(repository);
            }))
                _project.Build();
            _project.GameData.GameMode = GameMode.Skirmish;
            _project.GameData.MapSize = MapSize.Small;
            _project.GameData.Players = TestPlayers.CreateDuel().Players;
            SerializedObject authored = new SerializedObject(_battle);
            // Battle's scene instance supplies this override; the standalone scope prefab leaves it unassigned.
            ReinforcementZoneData reinforcementZoneData = AssetDatabase.GetDependencies(BATTLE_SCENE_PATH)
                .Select(AssetDatabase.LoadAssetAtPath<ReinforcementZoneData>)
                .Single(data => data != null);
            authored.FindProperty("reinforcementZoneData").objectReferenceValue = reinforcementZoneData;
            authored.ApplyModifiedPropertiesWithoutUndo();
            MapGenerationSettings settings = (MapGenerationSettings)authored.FindProperty("settings").objectReferenceValue;
            MapLayoutView mapView = (MapLayoutView)authored.FindProperty("mapLayoutView").objectReferenceValue;
            MapLayout layout = new MapLayoutGenerator(settings, mapView.FeatureRadii).Generate(
                _project.GameData.MapSize, _project.GameData.Players, new System.Random(MAP_SEED));
            using (LifetimeScope.Enqueue(builder =>
            {
                if (!ReferenceEquals(builder.ApplicationOrigin, _battle)) return;
                builder.Register<MapSetup>(Lifetime.Singleton).AsImplementedInterfaces().WithParameter(layout);
                builder.RegisterInstance<IBattleStartupSequence>(startup);
            }))
                _battle.Build();
            MapModel map = _battle.Container.Resolve<MapModel>();
            Assert.That(repository.SaveCount, Is.Zero, "The real settings repository must stay outside this fixture.");
            Assert.That(startup.CallCount, Is.EqualTo(1));
            AssetLifetimeScope[] players = _battle.GetComponentsInChildren<AssetLifetimeScope>(true)
                .Where(scope => scope is LocalPlayerScope || scope is AiPlayerScope).ToArray();
            Assert.That(players.Length, Is.EqualTo(2));
            Assert.That(players.OfType<LocalPlayerScope>().Count(), Is.EqualTo(1));
            Assert.That(players.OfType<AiPlayerScope>().Count(), Is.EqualTo(1));
            PlanetView planet = _battle.Container.Resolve<PlanetFactory>().Create(
                _project.GameData.PlanetType, _project.GameData.MapSize, layout.PlanetPosition);
            PlanetScope planetScope = planet.GetComponentInParent<PlanetScope>();
            Assert.That(planetScope.Parent, Is.SameAs(_battle));
            Assert.That(planetScope.Assets, Is.SameAs(_project.Assets));
            Assert.That(planetScope.View, Is.SameAs(planet));
            Assert.That(planetScope.Container.Resolve<PlanetData>(), Is.Not.Null);
            List<EntityLifetimeScope> shipScopes = new List<EntityLifetimeScope>();
            ShipData sharedShipData = null;
            foreach (AssetLifetimeScope player in players)
            {
                PlayerSlot slot = player.Container.Resolve<PlayerSlot>();
                Vector3 origin = map.GetStationPosition(slot.Id);
                ShipEntity ship = player.Container.Resolve<ShipFactory>().Create(slot.Id, ShipType.Acclamator, origin);
                EntityLifetimeScope shipScope = AssertEntity(ship, player, slot);
                shipScopes.Add(shipScope);
                ShipData data = shipScope.Container.Resolve<ShipData>();
                if (sharedShipData == null) sharedShipData = data;
                else Assert.That(data, Is.SameAs(sharedShipData), "Ship data is shared; runtime state belongs to its entity scope.");
                Assert.That(ship.EntityId, Is.EqualTo(shipScope.Container.Resolve<IEntity>().Id));
                AssertVoiceCapability(shipScope, slot);
                AssertDecoratedHealth(shipScope, data);
                SquadronType squadronType = player.Container.Resolve<FactionRoster>().Squadrons.Keys.OrderBy(type => type).First();
                Squadron squadron = player.Container.Resolve<SquadronFactory>().Create(
                    slot.Id, squadronType, origin + Vector3.right * 100f, Quaternion.identity);
                EntityLifetimeScope squadronScope = AssertEntity(squadron, player, slot);
                AssertVoiceCapability(squadronScope, slot);
                AssertPoolRoundTrip(squadron, squadronScope, origin + Vector3.right * 150f);
                EntityLifetimeScope station = AssertEntity(player.Container.Resolve<SpaceStationFactory>()
                    .Create(slot.Id, slot.Faction, origin + Vector3.forward * 150f), player, slot);
                AssertDecoratedHealth(station, station.Container.Resolve<SpaceStationData>().ComponentData);
                EntityLifetimeScope mining = AssertEntity(player.Container.Resolve<MiningFacilityFactory>()
                    .Create(slot.Id, MiningFacilityType.CommonMiner, origin + Vector3.forward * 200f), player, slot);
                AssertDecoratedHealth(mining, mining.Container.Resolve<MiningFacilityData>().ComponentData);
                EntityLifetimeScope asteroidMining = AssertEntity(player.Container.Resolve<AsteroidMiningFacilityFactory>()
                    .Create(slot.Id, MiningFacilityType.AsteroidMiner, origin + Vector3.forward * 250f), player, slot);
                AssertDecoratedHealth(asteroidMining, asteroidMining.Container.Resolve<MiningFacilityData>().ComponentData);
                EntityLifetimeScope platform = AssertEntity(player.Container.Resolve<DefendPlatformFactory>()
                    .Create(slot.Id, DefendPlatformType.Xq6, origin + Vector3.forward * 300f), player, slot);
                AssertDecoratedHealth(platform, platform.Container.Resolve<DefendPlatformData>().ComponentData);
                EntityLifetimeScope asteroidPlatform = AssertEntity(player.Container.Resolve<AsteroidDefendPlatformFactory>()
                    .Create(slot.Id, DefendPlatformType.BattleAsteroid, origin + Vector3.forward * 350f), player, slot);
                AssertDecoratedHealth(asteroidPlatform, asteroidPlatform.Container.Resolve<DefendPlatformData>().ComponentData);
            }
            Assert.That(_entityIds.Count, Is.EqualTo(14));
            CombatModifiers first = shipScopes[0].Container.Resolve<CombatModifiers>();
            CombatModifiers second = shipScopes[1].Container.Resolve<CombatModifiers>();
            first.SetCloaked(true);
            Assert.That(first.IsCloaked, Is.True);
            Assert.That(second.IsCloaked, Is.False, "A different owner must not share mutable entity state.");
            first.SetCloaked(false);
            AssertPlayerOwnershipTeardown(players);
        }

        private static void DisposeRoot(LifetimeScope scope)
        {
            if (scope == null) return;
            try { scope.DisposeCore(); }
            finally { Object.DestroyImmediate(scope.gameObject); }
        }

        private void AssertPlayerOwnershipTeardown(AssetLifetimeScope[] players)
        {
            IEntityLocator entities = _battle.Container.Resolve<IEntityLocator>();
            EntityScopeOwnership[] owners = players.Select(player => player.Container.Resolve<EntityScopeOwnership>()).ToArray();
            EntityLifetimeScope[][] children = players.Select(player => player.GetComponentsInChildren<EntityLifetimeScope>(true)).ToArray();
            PlayerSlot[] slots = players.Select(player => player.Container.Resolve<PlayerSlot>()).ToArray();
            for (int index = 0; index < players.Length; index++)
            {
                Assert.That(owners[index].Count, Is.EqualTo(children[index].Length));
                Assert.That(children[index].Length, Is.GreaterThanOrEqualTo(7), "Active and pooled scopes belong to the player.");
                players[index].DisposeCore();
                Assert.That(owners[index].Count, Is.Zero);
                foreach (EntityLifetimeScope child in children[index])
                    Assert.That(child.Container, Is.Null, "Entity scopes close before player services leave the battle.");
                Assert.That(entities.Entities.Any(entity => entity.Owner == slots[index].Id), Is.False);
                Assert.That(_battle.Container.Resolve<IEntityLocator>(), Is.SameAs(entities));
                if (index + 1 < players.Length)
                {
                    Assert.That(owners[index + 1].Count, Is.EqualTo(children[index + 1].Length));
                    Assert.That(children[index + 1].All(child => child.Container != null), Is.True);
                    Assert.That(entities.Entities.Any(entity => entity.Owner == slots[index + 1].Id), Is.True);
                }
            }
        }

        private TScope InstantiateScope<TScope>(string path) where TScope : LifetimeScope
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            TScope source = prefab.GetComponent<TScope>();
            Assert.That(source, Is.Not.Null, path);
            TScope scope = Object.Instantiate(source, _root.transform, false);
            scope.autoRun = false;
            return scope;
        }

        private EntityLifetimeScope AssertEntity(EntityView view, AssetLifetimeScope player, PlayerSlot slot)
        {
            EntityLifetimeScope scope = view.transform.parent.GetComponent<EntityLifetimeScope>();
            Assert.That(scope, Is.Not.Null, view.name);
            Assert.That(scope.Parent, Is.SameAs(player));
            Assert.That(scope.Assets, Is.SameAs(_project.Assets));
            Assert.That(scope.Container.Resolve<PlayerId>(), Is.EqualTo(slot.Id));
            IEntity entity = scope.Container.Resolve<IEntity>();
            Assert.That(entity.Owner, Is.EqualTo(slot.Id));
            Assert.That(_entityIds.Add(entity.Id), Is.True, "Each new entity needs one unique identity.");
            Assert.That(_battle.Container.Resolve<IEntityLocator>().GetEntity(entity.Id), Is.SameAs(entity));
            Assert.That(scope.Container.Resolve<Transform>(EntityBindType.ViewTransform), Is.SameAs(view.transform));
            Assert.That(entity.GetFacade<IEntityTransformFacade>().Transform, Is.SameAs(view.transform));
            Assert.That(entity.HealthModel, Is.SameAs(scope.Container.Resolve<IHealthModelObserver>()));
            Assert.That(entity.HealthModel.HasUnits, Is.True);
            Assert.That(_health.Add(entity.HealthModel), Is.True, "Health belongs to the newly created entity.");
            Assert.That(_modifiers.Add(scope.Container.Resolve<CombatModifiers>()), Is.True);
            Assert.That(scope.Container.Resolve<ContainerLocal<IReadOnlyList<EmpireAtWar.Mvc.IMonoComponent>>>().Value.Count,
                Is.GreaterThan(0));
            Assert.That(view.gameObject.activeSelf, Is.True);
            return scope;
        }

        private static void AssertVoiceCapability(EntityLifetimeScope scope, PlayerSlot owner) =>
            Assert.That(scope.Container.Resolve<ContainerLocal<IReadOnlyList<IAudioDialogShipComponent>>>().Value.Count,
                Is.EqualTo(owner.IsAi ? 0 : 1));

        private static void AssertDecoratedHealth(EntityLifetimeScope scope, IHealthData baseData)
        {
            Assert.That(scope.Container.Resolve<IHealthData>(EntityBindType.HealthData), Is.SameAs(baseData));
            Assert.That(scope.Container.Resolve<IHealthData>(), Is.TypeOf<ResearchHealthData>());
            Assert.That(scope.Container.Resolve<HealthModel>(), Is.Not.Null);
        }

        private void AssertPoolRoundTrip(Squadron squadron, EntityLifetimeScope scope, Vector3 position)
        {
            IEntity entity = scope.Container.Resolve<IEntity>();
            IObjectResolver container = scope.Container;
            LifecycleTickGate gate = container.Resolve<LifecycleTickGate>();
            IReadOnlyList<EmpireAtWar.Lifecycle.IInitializable> callbacks =
                container.Resolve<ContainerLocal<IReadOnlyList<EmpireAtWar.Lifecycle.IInitializable>>>().Value;
            int callbackCount = callbacks.Count;
            squadron.Stow();
            Assert.That(scope.gameObject.activeSelf, Is.False);
            Assert.That(gate.IsTicking, Is.False);
            Assert.That(_battle.Container.Resolve<IEntityLocator>().TryGetEntity(entity.Id, out _), Is.False);
            position.y = container.Resolve<SquadronData>().Height;
            squadron.Respawn(position, Quaternion.Euler(0f, 40f, 0f));
            Assert.That(scope.Container, Is.SameAs(container), "Pool reuse retains the built child scope.");
            Assert.That(scope.gameObject.activeSelf, Is.True);
            Assert.That(gate.IsTicking, Is.True);
            Assert.That(_battle.Container.Resolve<IEntityLocator>().GetEntity(entity.Id), Is.SameAs(entity));
            Assert.That(entity.HealthModel.HasUnits, Is.True);
            Assert.That(entity.HealthModel.HardPointModels.All(unit => !unit.IsDestroyed), Is.True);
            Assert.That(callbacks.Count, Is.EqualTo(callbackCount));
            Assert.That(entity.GetFacade<IShipAbilityFacade>().Entity, Is.SameAs(entity));
        }

        [LifecycleOrder(Initialize = -1000)]
        private sealed class MapSetup : EmpireAtWar.Lifecycle.IInitializable
        {
            private readonly MapModel _map;
            private readonly MapLayout _layout;
            private readonly HealthOverlayView _healthOverlay;

            public MapSetup(MapModel map, MapLayout layout, HealthOverlayView healthOverlay)
            {
                _map = map;
                _layout = layout;
                _healthOverlay = healthOverlay;
            }

            public void Initialize()
            {
                _map.SetLayout(_layout);
                // EditMode does not dispatch Awake on ordinary MonoBehaviours created by the scope.
                typeof(HealthOverlayView).GetMethod("Awake",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(_healthOverlay, null);
            }
        }

        private sealed class MemorySettings : ISettingsRepository
        {
            public int SaveCount { get; private set; }

            public SettingsLoadStatus Load(out SettingsData data)
            {
                data = new SettingsData();
                data.Display.WindowMode = Screen.fullScreenMode switch
                {
                    FullScreenMode.ExclusiveFullScreen => DisplayWindowMode.ExclusiveFullscreen,
                    FullScreenMode.FullScreenWindow => DisplayWindowMode.Borderless,
                    _ => DisplayWindowMode.Windowed,
                };
                data.Display.SetResolution(Screen.width, Screen.height);
                data.Graphics.VSync = QualitySettings.vSyncCount != 0;
                data.Graphics.FrameRateLimit = Math.Max(0, Application.targetFrameRate);
                return SettingsLoadStatus.Loaded;
            }

            public void Save(SettingsData data) => SaveCount++;
        }

        private sealed class CompletedStartup : IBattleStartupSequence
        {
            public int CallCount { get; private set; }

            public Awaitable RunAsync(CancellationToken cancellationToken)
            {
                CallCount++;
                AwaitableCompletionSource completion = new AwaitableCompletionSource();
                completion.SetResult();
                return completion.Awaitable;
            }
        }
    }
}
