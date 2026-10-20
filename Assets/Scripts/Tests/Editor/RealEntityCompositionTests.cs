using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Ship.Audio;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Health.Overlay;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Extensions;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
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
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.MiniMap;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.FogOfWar;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ship;
using EmpireAtWar.ViewComponents.Squadrons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
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
            Assert.That(_battle.Container.Resolve<BattleSaveService>(), Is.Not.Null);
            var human = players.Single(scope => scope.Container.Resolve<PlayerSlot>().Id == new PlayerId(0));
            var ally = players.Single(scope => scope.Container.Resolve<PlayerSlot>().Id == new PlayerId(1));
            var wave = players.Single(scope => scope.Container.Resolve<PlayerSlot>().Id == new PlayerId(2));
            Assert.That(human.Container.Resolve<PlayerFactionModel>().CurrentLevel, Is.EqualTo(5));
            Assert.That(human.Container.Resolve<EmpireAtWar.Services.Factions.FactionService>().Income, Is.EqualTo(75));
            Assert.That(ally.Container.Resolve<EmpireAtWar.Entities.EnemyFaction.Models.EnemyFactionModel>().CurrentLevel, Is.EqualTo(5));
            Assert.That(ally.Container.Resolve<EmpireAtWar.Services.Stations.IProductionAccess>().ProductionHost, Is.EqualTo(new PlayerId(0)));
            Assert.That(wave.Container.TryGetRegistration(typeof(FactionResearchModel), out _), Is.False);
            Assert.That(wave.Container.TryGetRegistration(typeof(EmpireAtWar.Models.Economy.EconomyModel), out _), Is.False);
            Assert.That(_battle.Container.Resolve<IBattleOutcomeRule>(), Is.TypeOf<SurvivalOutcomeRule>());
        }

        [Test]
        public void SavedUnits_RestoreWithIdsPoseHealthAbilitiesHangarAndOrders()
        {
            BuildSaveProject();
            MapLayout layout = BuildSaveBattle();
            LocalPlayerScope human = _battle.GetComponentsInChildren<LocalPlayerScope>(true).Single();
            PlayerSlot slot = human.Container.Resolve<PlayerSlot>();
            IEntityLocator locator = _battle.Container.Resolve<IEntityLocator>();
            ShipAbilityService abilities = _battle.Container.Resolve<ShipAbilityService>();
            Vector3 origin = layout.GetStationPosition(slot.Id);

            ShipEntity ship = human.Container.Resolve<ShipFactory>().Create(slot.Id, ShipType.Acclamator, origin);
            EntityLifetimeScope shipScope = ship.transform.parent.GetComponent<EntityLifetimeScope>();
            IEntity shipEntity = locator.GetEntity(ship.EntityId);
            Assert.That(shipEntity.GetFacade<IHangarCommand>().TryLaunchImmediately(), Is.True);
            HealthModel health = shipScope.Container.Resolve<HealthModel>();
            float[] hardPoints = health.HardPointModels.Select((point, i) =>
                point.HardPointType == HardPointType.Hangar ? 1f : i % 3 == 0 ? 0f : 0.5f).ToArray();
            health.Restore(0.4f, 0.3f, hardPoints);
            ShipAbilitySlot ability = shipEntity.GetFacade<IShipAbilityFacade>().Slots[0];
            abilities.Restore(ability, ShipAbilityState.Recovering, ability.Definition.RecoveryDelay * 3f);
            IEntity squadronEntity = locator.Entities.Single(entity => entity.Id != ship.EntityId);
            Vector3 destination = origin + new Vector3(400f, 0f, 250f);
            shipEntity.GetFacade<IMoveFacade>().MoveTo(destination);
            float[] memberHull = squadronEntity.HealthModel.HardPointModels.Select((_, i) => i == 0 ? 0f : 0.75f).ToArray();
            EntityLifetimeScope squadronScope = squadronEntity.GetFacade<IEntityTransformFacade>().Transform.parent
                .GetComponent<EntityLifetimeScope>();
            squadronScope.Container.Resolve<SquadronHealthModel>().Restore(memberHull, memberHull.Select(_ => 1f).ToArray());
            UnitSaveService units = _battle.Container.Resolve<UnitSaveService>();

            BattleSave saved = JsonUtility.FromJson<BattleSave>(BattleSave.Create(null, null, null, units.CaptureUnits(),
                Array.Empty<StructureSave>()).ToJson());
            Pose shipPose = new Pose(ship.transform.position, ship.transform.rotation);
            long squadronId = squadronEntity.Id;
            // Edit mode skips OnDestroy, where entity scopes normally leave their owner's registry.
            EntityScopeOwnership ownership = human.Container.Resolve<EntityScopeOwnership>();
            ownership.Unregister(squadronScope);
            ownership.Unregister(shipScope);
            DisposeRoot(squadronScope);
            DisposeRoot(shipScope);
            Assert.That(locator.Entities, Is.Empty);
            units.Restore(saved);

            IEntity restoredShip = locator.GetEntity(ship.EntityId);
            IEntity restoredSquadron = locator.GetEntity(squadronId);
            Transform shipTransform = restoredShip.GetFacade<IEntityTransformFacade>().Transform;
            EntityLifetimeScope restoredScope = shipTransform.parent.GetComponent<EntityLifetimeScope>();
            Assert.That(shipTransform.position, Is.EqualTo(shipPose.position));
            Assert.That(Quaternion.Angle(shipTransform.rotation, shipPose.rotation), Is.LessThan(0.01f));
            Assert.That(restoredScope.Container.Resolve<ShipMoveModel>().Phase, Is.Not.EqualTo(MovementPhase.Arriving),
                "A loaded ship is already in place, not jumping in.");
            HealthModel restoredHealth = restoredScope.Container.Resolve<HealthModel>();
            Assert.That(restoredHealth.HullPercentage, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(restoredHealth.HardPointModels.Select(point => point.HealthPercentage), Is.EqualTo(hardPoints));
            ShipAbilitySlot restoredAbility = restoredShip.GetFacade<IShipAbilityFacade>().Slots[0];
            Assert.That(restoredAbility.State, Is.EqualTo(ShipAbilityState.Recovering));
            Assert.That(restoredAbility.TimeLeft, Is.EqualTo(ability.Definition.RecoveryDelay * 3f));
            Assert.That(restoredAbility.Progress01, Is.EqualTo(1f));
            UnitOrderModel order = restoredScope.Container.Resolve<UnitOrderModel>();
            Assert.That(order.Current, Is.EqualTo(UnitOrderType.Move));
            Assert.That(order.Destination.X, Is.EqualTo(destination.x).Within(0.01f));
            Assert.That(order.Destination.Z, Is.EqualTo(destination.z).Within(0.01f));
            Assert.That(restoredSquadron.HealthModel.HardPointModels.Select(member => member.HealthPercentage), Is.EqualTo(memberHull));
            UnitOrderModel squadronOrder = restoredSquadron.GetFacade<IEntityTransformFacade>().Transform.parent
                .GetComponent<EntityLifetimeScope>().Container.Resolve<UnitOrderModel>();
            Assert.That(squadronOrder.Current, Is.EqualTo(UnitOrderType.Guard));
            Assert.That(squadronOrder.Target, Is.SameAs(restoredShip), "The squadron guards its restored carrier again.");
            HangarModel hangar = restoredScope.Container.Resolve<HangarModel>();
            Assert.That(Enumerable.Range(0, hangar.BayCount).Sum(hangar.GetActive), Is.EqualTo(1),
                "The carrier takes back the squadron it launched.");
        }

        [Test]
        public void SavedSkirmish_RestoresPlayerStateStructuresAndOwnerBookkeepingIntoNewBattle()
        {
            BuildSaveProject();
            MapLayout layout = BuildSaveBattle();
            LocalPlayerScope human = _battle.GetComponentsInChildren<LocalPlayerScope>(true).Single();
            AiPlayerScope ai = _battle.GetComponentsInChildren<AiPlayerScope>(true).Single();
            PlayerSlot humanSlot = human.Container.Resolve<PlayerSlot>();
            PlayerSlot aiSlot = ai.Container.Resolve<PlayerSlot>();
            IEntityLocator locator = _battle.Container.Resolve<IEntityLocator>();
            FactionRoster humanRoster = human.Container.Resolve<FactionRoster>();
            FactionRoster aiRoster = ai.Container.Resolve<FactionRoster>();
            IUnitRequestFactory requests = human.Container.Resolve<IUnitRequestFactory>();

            var station = human.Container.Resolve<SpaceStationFactory>()
                .Create(humanSlot.Id, humanSlot.Faction, layout.GetStationPosition(humanSlot.Id));
            ai.Container.Resolve<SpaceStationFactory>()
                .Create(aiSlot.Id, aiSlot.Faction, layout.GetStationPosition(aiSlot.Id));
            long stationId = locator.Entities.Single(entity => entity.Owner == humanSlot.Id).Id;
            ShipType humanShip = humanRoster.Ships.Keys.OrderBy(type => type).First();
            ShipEntity ship = human.Container.Resolve<ShipFactory>().Create(humanSlot.Id, humanShip,
                layout.GetStationPosition(humanSlot.Id) + Vector3.right * 300f);
            MiningFacilityType mineType = humanRoster.MiningFacilities.Keys.OrderBy(type => type).First();
            human.Container.Resolve<MiningFacilityFactory>().Create(humanSlot.Id, mineType,
                layout.GetStationPosition(humanSlot.Id) + Vector3.forward * 300f);

            human.Container.Resolve<EconomyModel>().Restore(4321f);
            // The faction UI swaps its level and research buttons with Object.Destroy, which edit mode rejects:
            // one error per level change and per completed tier, both now and on restore.
            Regex editModeDestroy = new Regex("Destroy may not be called from edit mode");
            for (int i = 0; i < 4; i++) LogAssert.Expect(LogType.Error, editModeDestroy);
            PlayerFactionModel faction = human.Container.Resolve<PlayerFactionModel>();
            faction.CurrentLevel = 2;
            FactionResearchModel research = human.Container.Resolve<FactionResearchModel>();
            ResearchType researchLine = research.ResearchTypes.OrderBy(type => type).First();
            research.Complete(researchLine);
            ShipUnitRequest queued = requests.ConstructUnitRequest(humanRoster.Ships[humanShip], humanShip);
            faction.QueueUnit(queued);
            faction.Advance(queued.FactionData.BuildTime * 0.25f);
            float queuedTimeLeft = faction.QueuedItems.Single().RemainingBuildTime;
            SquadronType reserveType = humanRoster.Squadrons.Keys.OrderBy(type => type).First();
            human.Container.Resolve<EmpireAtWar.Services.Reinforcement.ReinforcementService>()
                .Add(requests.ConstructUnitRequest(humanRoster.Squadrons[reserveType], reserveType));
            human.Container.Resolve<EmpireAtWar.Entities.SuperWeapons.SuperWeaponModel>()
                .Restore(EmpireAtWar.Entities.SuperWeapons.SuperWeaponType.IonCannon,
                    EmpireAtWar.Entities.SuperWeapons.SuperWeaponState.Ready, 30f);

            ai.Container.Resolve<EconomyModel>().Restore(99999f);
            var aiController = ai.Container.Resolve<EmpireAtWar.Entities.EnemyFaction.Controllers.EnemyFactionController>();
            ShipType aiShip = aiRoster.Ships.Keys.OrderBy(type => type).First();
            aiController.Purchase(ai.Container.Resolve<IUnitRequestFactory>()
                .ConstructUnitRequest(aiRoster.Ships[aiShip], aiShip));
            Assert.That(aiController.PendingBuilds.Count, Is.EqualTo(1), "The AI needs a living station to buy.");
            aiController.Advance(1f);
            float aiBuildLeft = aiController.PendingBuilds.Single().TimeLeft;
            var aiLimits = ai.Container.Resolve<EmpireAtWar.Entities.EnemyFaction.Models.EnemyUnitLimitModel>();
            int aiCapacity = aiLimits.CurrentUnitCapacity;
            float aiMoney = ai.Container.Resolve<EconomyModel>().Money;
            Assert.That(aiMoney, Is.LessThan(99999f), "The purchase was paid.");
            _battle.Container.Resolve<BattleVictoryModel>().RestoreObservedBases(new[] { aiSlot.Id });

            _battle.Container.Resolve<SkirmishStateModel>().SetState(BattleState.Running);
            BattleSave saved = BattleSave.FromJson(
                _battle.Container.Resolve<BattleSaveService>().Capture(BattleSaveSlot.Quicksave).ToJson());
            DisposeRoot(_battle);
            BuildSaveBattle();
            human = _battle.GetComponentsInChildren<LocalPlayerScope>(true).Single();
            ai = _battle.GetComponentsInChildren<AiPlayerScope>(true).Single();
            locator = _battle.Container.Resolve<IEntityLocator>();
            Assert.That(locator.Entities, Is.Empty);

            _battle.Container.Resolve<BattleRestorer>().Restore(saved);

            Assert.That(human.Container.Resolve<EconomyModel>().Money, Is.EqualTo(4321f));
            faction = human.Container.Resolve<PlayerFactionModel>();
            Assert.That(faction.CurrentLevel, Is.EqualTo(2));
            Assert.That(human.Container.Resolve<EmpireAtWar.Services.Factions.FactionService>().Income, Is.EqualTo(30f));
            Assert.That(human.Container.Resolve<FactionResearchModel>().GetCompletedTiers(researchLine), Is.EqualTo(1));
            ProductionQueueItem restoredItem = faction.QueuedItems.Single();
            Assert.That(restoredItem.UnitRequest.Id, Is.EqualTo(queued.Id));
            Assert.That(restoredItem.RemainingBuildTime, Is.EqualTo(queuedTimeLeft));
            var reinforcements = human.Container.Resolve<EmpireAtWar.Models.Reinforcement.ReinforcementModel>();
            Assert.That(reinforcements.Reserve.Single().Value, Is.EqualTo(1));
            Assert.That(reinforcements.CurrentUnitCapacity, Is.EqualTo(humanRoster.Ships[humanShip].UnitCapacity),
                "The restored ship counts against unit capacity again.");
            var superWeapons = human.Container.Resolve<EmpireAtWar.Entities.SuperWeapons.SuperWeaponModel>();
            Assert.That(superWeapons.GetState(EmpireAtWar.Entities.SuperWeapons.SuperWeaponType.IonCannon),
                Is.EqualTo(EmpireAtWar.Entities.SuperWeapons.SuperWeaponState.Ready));
            Assert.That(superWeapons.GetCooldownRemaining(EmpireAtWar.Entities.SuperWeapons.SuperWeaponType.IonCannon),
                Is.EqualTo(30f));
            Assert.That(faction.CanQueueUnit(requests.ConstructUnitRequest(humanRoster.MiningFacilities[mineType], mineType)),
                Is.EqualTo(humanRoster.MiningFacilities[mineType].MaxCount > 1),
                "The restored mining facility holds its structure slot again.");

            Assert.That(ai.Container.Resolve<EconomyModel>().Money, Is.EqualTo(aiMoney), "A paid build is not paid again.");
            aiController = ai.Container.Resolve<EmpireAtWar.Entities.EnemyFaction.Controllers.EnemyFactionController>();
            Assert.That(aiController.PendingBuilds.Single().TimeLeft, Is.EqualTo(aiBuildLeft));
            aiLimits = ai.Container.Resolve<EmpireAtWar.Entities.EnemyFaction.Models.EnemyUnitLimitModel>();
            Assert.That(aiLimits.CurrentUnitCapacity, Is.EqualTo(aiCapacity), "The paid build keeps its reservation.");
            Assert.That(aiLimits.ShipOrdersCount, Is.EqualTo(1));

            Assert.That(locator.GetEntity(stationId).Owner, Is.EqualTo(humanSlot.Id));
            Assert.That(locator.GetEntity(ship.EntityId), Is.Not.Null);
            Assert.That(locator.Entities.Count, Is.EqualTo(4), "Two stations, the ship and the mining facility.");
            Assert.That(_battle.Container.Resolve<BattleVictoryModel>().ObservedBases, Is.EquivalentTo(new[] { aiSlot.Id }));
            Assert.That(station, Is.Not.Null);
        }

        [Test]
        public void SavedSurvival_ResumesTheWaveInProgressWithoutQueuingItAgain()
        {
            BuildSaveProject(SurvivalPreset.Create(new SurvivalSettings(FactionType.Republic, null,
                FactionType.Separatist, MapSize.Small, EmpireAtWar.Entities.EnemyFaction.Models.EnemyAiDifficulty.Medium,
                false)).Build());
            MapLayout layout = BuildSaveBattle();
            LocalPlayerScope human = _battle.GetComponentsInChildren<LocalPlayerScope>(true).Single();
            PlayerSlot humanSlot = human.Container.Resolve<PlayerSlot>();
            human.Container.Resolve<SpaceStationFactory>()
                .Create(humanSlot.Id, humanSlot.Faction, layout.GetStationPosition(humanSlot.Id));
            var model = _battle.Container.Resolve<EmpireAtWar.Entities.Game.SurvivalModel>();
            var director = _battle.Container.Resolve<SurvivalWaveDirector>();
            IEntityLocator locator = _battle.Container.Resolve<IEntityLocator>();
            _battle.Container.Resolve<SkirmishStateModel>().SetState(BattleState.Running);
            model.StartWaveNow();
            director.Tick();
            int pending = director.Pending.Count;
            int spawned = director.Spawned;
            Assert.That(spawned, Is.EqualTo(1), "One attacker spawns per tick.");
            Assert.That(pending, Is.GreaterThan(0), "The first wave has more than one unit.");
            long[] attackers = locator.Entities.Where(entity => entity.Owner != humanSlot.Id)
                .Select(entity => entity.Id).ToArray();
            Assert.That(attackers.Length, Is.EqualTo(spawned));

            BattleSave saved = BattleSave.FromJson(
                _battle.Container.Resolve<BattleSaveService>().Capture(BattleSaveSlot.Quicksave).ToJson());
            DisposeRoot(_battle);
            BuildSaveBattle();
            model = _battle.Container.Resolve<EmpireAtWar.Entities.Game.SurvivalModel>();
            director = _battle.Container.Resolve<SurvivalWaveDirector>();
            locator = _battle.Container.Resolve<IEntityLocator>();
            _battle.Container.Resolve<BattleRestorer>().Restore(saved);

            Assert.That(model.WaveIndex, Is.EqualTo(1));
            Assert.That(model.Phase, Is.EqualTo(EmpireAtWar.Entities.Game.SurvivalPhase.WaveActive));
            Assert.That(director.Wave, Is.EqualTo(1));
            Assert.That(director.Pending.Count, Is.EqualTo(pending));
            Assert.That(director.Spawned, Is.EqualTo(spawned));
            foreach (long attacker in attackers) Assert.That(locator.TryGetEntity(attacker, out _), Is.True);
            _battle.Container.Resolve<SkirmishStateModel>().SetState(BattleState.Running);
            director.Tick();
            Assert.That(director.Pending.Count, Is.LessThanOrEqualTo(pending), "The resumed wave is not queued again.");
            Assert.That(model.Phase, Is.EqualTo(EmpireAtWar.Entities.Game.SurvivalPhase.WaveActive));
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
                _project.GameData.PlanetType, MapSize.Small, _project.GameData.VictoryCondition, 1000f, true, 0).Build();
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

        private void BuildSaveProject(BattleDefinition battle = null)
        {
            _project = InstantiateScope<ProjectScope>(PROJECT_PREFAB_PATH);
            using (LifetimeScope.EnqueueParent(null))
            using (LifetimeScope.Enqueue(builder =>
            {
                if (ReferenceEquals(builder.ApplicationOrigin, _project))
                    builder.RegisterInstance<ISettingsRepository>(new MemorySettings());
            })) _project.Build();
            _project.GameData.Battle = battle ?? SkirmishPreset.Create(TestPlayers.CreateDuel().Players,
                _project.GameData.PlanetType, MapSize.Small, _project.GameData.VictoryCondition, 1000f, true, 0).Build();
        }

        /// <summary>Builds a battle scope for the project's battle; called again, it stands in for the scene a load rebuilds.</summary>
        private MapLayout BuildSaveBattle()
        {
            // The scope prefab must not build itself on instantiation.
            _root.SetActive(false);
            _battle = InstantiateScope<SkirmishScope>(SKIRMISH_PREFAB_PATH);
            _battle.parentReference.Object = _project;
            _root.SetActive(true);
            var authored = new SerializedObject(_battle);
            authored.FindProperty("reinforcementZoneData").objectReferenceValue = AssetDatabase.GetDependencies(BATTLE_SCENE_PATH)
                .Select(AssetDatabase.LoadAssetAtPath<ReinforcementZoneData>).Single(data => data != null);
            authored.ApplyModifiedPropertiesWithoutUndo();
            var settings = (MapGenerationSettings)authored.FindProperty("settings").objectReferenceValue;
            var mapView = (MapLayoutView)authored.FindProperty("mapLayoutView").objectReferenceValue;
            MapLayout layout = _project.GameData.Battle.Map.Type == MapType.Survival
                ? new SurvivalLayoutGenerator(settings, mapView.FeatureRadii).Generate(_project.GameData.Battle)
                : new MapLayoutGenerator(settings, mapView.FeatureRadii).Generate(
                    _project.GameData.MapSize, _project.GameData.Players, new System.Random(MAP_SEED));
            using (LifetimeScope.Enqueue(builder =>
            {
                if (!ReferenceEquals(builder.ApplicationOrigin, _battle)) return;
                builder.Register<MapSetup>(Lifetime.Singleton).AsImplementedInterfaces().WithParameter(layout);
                builder.RegisterInstance<IBattleStartupSequence>(new CompletedStartup());
            })) _battle.Build();
            return layout;
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
