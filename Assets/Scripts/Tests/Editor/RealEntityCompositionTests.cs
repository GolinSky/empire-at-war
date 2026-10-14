using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Squadrons.Flight;
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
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.FogOfWar;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ship;
using EmpireAtWar.ViewComponents.Squadrons;
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
        public void SurvivalComposition_BuildsHostAllyAndWaveScopes()
        {
            _project = InstantiateScope<ProjectScope>(PROJECT_PREFAB_PATH);
            _battle = InstantiateScope<SkirmishScope>(SKIRMISH_PREFAB_PATH);
            _battle.parentReference.Object = _project;
            _root.SetActive(true);
            using (LifetimeScope.EnqueueParent(null))
            using (LifetimeScope.Enqueue(builder =>
            {
                if (ReferenceEquals(builder.ApplicationOrigin, _project))
                    builder.RegisterInstance<ISettingsRepository>(new MemorySettings());
            })) _project.Build();
            _project.GameData.Battle = SurvivalPreset.Create(new SurvivalSettings(FactionType.Republic,
                FactionType.Republic, FactionType.Separatist, MapSize.Small,
                EmpireAtWar.Entities.EnemyFaction.Models.EnemyAiDifficulty.Medium, true)).Build();
            var authored = new SerializedObject(_battle);
            authored.FindProperty("reinforcementZoneData").objectReferenceValue = AssetDatabase.GetDependencies(BATTLE_SCENE_PATH)
                .Select(AssetDatabase.LoadAssetAtPath<ReinforcementZoneData>).Single(data => data != null);
            authored.ApplyModifiedPropertiesWithoutUndo();
            var settings = (MapGenerationSettings)authored.FindProperty("settings").objectReferenceValue;
            var mapView = (MapLayoutView)authored.FindProperty("mapLayoutView").objectReferenceValue;
            var layout = new SurvivalLayoutGenerator(settings, mapView.FeatureRadii).Generate(_project.GameData.Battle);
            using (LifetimeScope.Enqueue(builder =>
            {
                if (!ReferenceEquals(builder.ApplicationOrigin, _battle)) return;
                builder.Register<MapSetup>(Lifetime.Singleton).AsImplementedInterfaces().WithParameter(layout);
                builder.RegisterInstance<IBattleStartupSequence>(new CompletedStartup());
            })) _battle.Build();
            var players = _battle.GetComponentsInChildren<AssetLifetimeScope>(true)
                .Where(scope => scope is LocalPlayerScope || scope is AiPlayerScope).ToArray();
            Assert.That(players.Length, Is.EqualTo(3));
            var human = players.Single(scope => scope.Container.Resolve<PlayerSlot>().Id == new PlayerId(0));
            var ally = players.Single(scope => scope.Container.Resolve<PlayerSlot>().Id == new PlayerId(1));
            var wave = players.Single(scope => scope.Container.Resolve<PlayerSlot>().Id == new PlayerId(2));
            Assert.That(human.Container.Resolve<PlayerFactionModel>().CurrentLevel, Is.EqualTo(5));
            Assert.That(human.Container.Resolve<EmpireAtWar.Services.Factions.FactionService>().Income, Is.EqualTo(25));
            Assert.That(ally.Container.Resolve<EmpireAtWar.Entities.EnemyFaction.Models.EnemyFactionModel>().CurrentLevel, Is.EqualTo(5));
            Assert.That(ally.Container.Resolve<EmpireAtWar.Services.Stations.IProductionAccess>().ProductionHost, Is.EqualTo(new PlayerId(0)));
            Assert.That(wave.Container.TryGetRegistration(typeof(FactionResearchModel), out _), Is.False);
            Assert.That(wave.Container.TryGetRegistration(typeof(EmpireAtWar.Models.Economy.EconomyModel), out _), Is.False);
            Assert.That(_battle.Container.Resolve<IBattleOutcomeRule>(), Is.TypeOf<SurvivalOutcomeRule>());
        }

        [TestCase(BattleFeatures.All, PlayerRole.AiOpponent)]
        [TestCase(BattleFeatures.All & ~BattleFeatures.SuperWeapons, PlayerRole.AiOpponent)]
        [TestCase(BattleFeatures.All & ~BattleFeatures.CaptureSites, PlayerRole.AiOpponent)]
        [TestCase(BattleFeatures.All & ~BattleFeatures.MiningSites, PlayerRole.AiOpponent)]
        [TestCase(BattleFeatures.All & ~BattleFeatures.Economy, PlayerRole.AiOpponent)]
        [TestCase(BattleFeatures.All & ~BattleFeatures.Fog, PlayerRole.AiOpponent)]
        [TestCase(BattleFeatures.None, PlayerRole.AiOpponent)]
        [TestCase(BattleFeatures.None, PlayerRole.WaveAttacker)]
        public void ActualScopes_AllSevenUnitFactoriesBuildForLocalAndAiOwnersAndPoolWithoutRebuilding(BattleFeatures features, PlayerRole opponentRole)
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
            _project.GameData.Battle = SkirmishPreset.Create(TestPlayers.CreateDuel().Players,
                _project.GameData.PlanetType, MapSize.Small, _project.GameData.VictoryCondition, 1000f).Build();
            bool superWeapons = (features & BattleFeatures.SuperWeapons) != 0;
            if (features != BattleFeatures.All)
            {
                var current = _project.GameData.Battle;
                _project.GameData.Battle = new BattleDefinition(current.Mode, current.Map, current.Players,
                    features, current.Outcome, current.VictoryCondition, current.StartingMoney);
                if (opponentRole == PlayerRole.WaveAttacker)
                {
                    var wavePlayers = new[] { current.Players[0], new BattlePlayer(current.Slots[1], opponentRole, BattleFeatures.None) };
                    _project.GameData.Battle = new BattleDefinition(current.Mode, current.Map, wavePlayers,
                        features, current.Outcome, current.VictoryCondition, current.StartingMoney);
                }
            }
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
            bool fog = (features & BattleFeatures.Fog) != 0;
            Assert.That(_battle.Container.Resolve<ICaptureSitesSystem>() is NoCaptureSites,
                Is.EqualTo((features & BattleFeatures.CaptureSites) == 0));
            Assert.That(_battle.Container.Resolve<IFogOfWarSystem>() is NoFogOfWar, Is.EqualTo(!fog));
            Assert.That(_battle.Container.Resolve<IMiniMapFogMask>() is RevealedMiniMapFogMask, Is.EqualTo(!fog));
            if (features != BattleFeatures.All && features != (BattleFeatures.All & ~BattleFeatures.SuperWeapons))
            {
                foreach (AssetLifetimeScope player in players)
                {
                    PlayerSlot slot = player.Container.Resolve<PlayerSlot>();
                    if (_project.GameData.Battle.GetPlayer(slot.Id).OwnsStation)
                        Assert.That(_battle.Container.Resolve<EmpireAtWar.Services.Player.IPlayerRegistry>().GetStationSpawner(slot.Id), Is.Not.Null);
                    if (opponentRole == PlayerRole.WaveAttacker && slot.IsAi)
                    {
                        Assert.That(player.Container.TryGetRegistration(typeof(FactionResearchModel), out _), Is.False);
                        Assert.That(player.Container.TryGetRegistration(typeof(EmpireAtWar.Models.Economy.EconomyModel), out _), Is.False);
                    }
                    ShipEntity ship = player.Container.Resolve<ShipFactory>().Create(slot.Id, ShipType.Arquitens,
                        layout.GetStationPosition(slot.Id));
                    AssertEntity(ship, player, slot);
                }
                return;
            }
            List<EntityLifetimeScope> shipScopes = new List<EntityLifetimeScope>();
            ShipData sharedShipData = null;
            foreach (AssetLifetimeScope player in players)
            {
                Assert.That(player.Container.Resolve<FactionRoster>().SuperWeapons.Count == 0, Is.EqualTo(!superWeapons));
                Type superWeaponDriver = player is AiPlayerScope
                    ? typeof(EmpireAtWar.Services.Enemy.EnemySuperWeaponController)
                    : typeof(EmpireAtWar.Entities.SuperWeapons.Controller.SuperWeaponPresenter);
                Assert.That(player.Container.TryGetRegistration(superWeaponDriver, out _), Is.EqualTo(superWeapons));
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
                int[] poseSlots = AssertFightersBank(squadron);
                AssertPoolRoundTrip(squadron, squadronScope, origin + Vector3.right * 150f);
                Assert.That(AssertFightersBank(squadron), Is.EquivalentTo(poseSlots),
                    "Respawn reuses the released pose slots instead of registering fighters twice.");
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

        // A fresh squadron initializes while inactive; its pose batch slots must already hold the authored rest.
        private int[] AssertFightersBank(Squadron squadron)
        {
            const float BANK = 30f;
            SquadronFlightComponent flight = squadron.Bindings.SquadronFlight;
            List<FighterView> fighters = (List<FighterView>)typeof(SquadronFlightComponent)
                .GetField("fighters", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(flight);
            int[] slots = (int[])typeof(SquadronFlightComponent)
                .GetField("_poseSlots", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(flight);
            FighterPoseBatch poses = _battle.Container.Resolve<FighterPoseBatch>();
            Assert.That(slots.Length, Is.EqualTo(fighters.Count));
            Assert.That(slots.Distinct().Count(), Is.EqualTo(slots.Length));
            for (int i = 0; i < fighters.Count; i++)
            {
                Quaternion rest = fighters[i].BodyRestRotation;
                Assert.That(Quaternion.Dot(rest, rest), Is.EqualTo(1f).Within(0.001f), fighters[i].name);
                poses.Set(slots[i], fighters[i].Position, Vector3.forward, BANK);
            }
            poses.LateTick();
            foreach (FighterView fighter in fighters)
                Assert.That(Quaternion.Angle(fighter.Body.localRotation,
                    Quaternion.AngleAxis(BANK, Vector3.forward) * fighter.BodyRestRotation), Is.LessThan(0.01f), fighter.name);
            return slots;
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
