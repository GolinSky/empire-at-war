using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Audio")]
    public sealed class FactionAnnouncementTests
    {
        private const string DATA_PATH = "Assets/Settings/Data/Models/Audio/InterfaceSfxData.asset";
        private readonly List<UnityEngine.Object> _created = new();
        private RecordingAudio _audio;
        private FactionAnnouncerSet _clips;
        private ReinforcementModel _capacity;
        private FactionAnnouncementService _service;

        [SetUp]
        public void SetUp()
        {
            _audio = new RecordingAudio();
            _clips = new FactionAnnouncerSet();
            foreach (FieldInfo field in typeof(FactionAnnouncerSet).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (field.FieldType == typeof(AudioClip)) field.SetValue(_clips, Clip(field.Name));
            var capturePool = new EmpireAtWar.Utils.Random.RandomAudioClips();
            typeof(EmpireAtWar.Utils.Random.RandomFromArray<AudioClip>)
                .GetField("array", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(capturePool, new[] { Clip("Captured") });
            SetField(_clips, "miningCaptured", capturePool);
            var attackPool = new EmpireAtWar.Utils.Random.RandomAudioClips();
            typeof(EmpireAtWar.Utils.Random.RandomFromArray<AudioClip>)
                .GetField("array", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(attackPool, new[] { Clip("StationUnderAttack") });
            SetField(_clips, "stationUnderAttack", attackPool);
            ReinforcementData data = ScriptableObject.CreateInstance<ReinforcementData>();
            _created.Add(data);
            SetField(data, "<MaxUnitCapacity>k__BackingField", 20);
            _capacity = new ReinforcementModel(data);
            _service = new FactionAnnouncementService(_audio, null, null, null, null, null,
                null, _capacity, TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), null, null, null, null,
                new CameraStub());
            SetField(_service, "_clips", _clips);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object item in _created) UnityEngine.Object.DestroyImmediate(item);
            _created.Clear();
        }

        [Test]
        public void EveryFactionHasCompleteAnnouncerAndBorrowsRebellionExceptConstruction()
        {
            InterfaceSfxData data = AssetDatabase.LoadAssetAtPath<InterfaceSfxData>(DATA_PATH);
            foreach (FactionType faction in Enum.GetValues(typeof(FactionType)))
            {
                FactionAnnouncerSet set = data.GetAnnouncer(faction);
                foreach (PropertyInfo property in typeof(FactionAnnouncerSet).GetProperties())
                    Assert.That(property.GetValue(set), Is.InstanceOf<AudioClip>(), faction + "." + property.Name);
            }
            foreach (PropertyInfo property in typeof(FactionAnnouncerSet).GetProperties())
            {
                if (property.Name == nameof(FactionAnnouncerSet.StationUnderAttack) ||
                    property.Name == nameof(FactionAnnouncerSet.MiningCaptured) ||
                    property.Name == nameof(FactionAnnouncerSet.UnitProduction) ||
                    property.Name == nameof(FactionAnnouncerSet.BuildingStarted)) continue;
                Assert.That(property.GetValue(data.GetAnnouncer(FactionType.Republic)),
                    Is.SameAs(property.GetValue(data.GetAnnouncer(FactionType.Rebellion))), property.Name);
                Assert.That(property.GetValue(data.GetAnnouncer(FactionType.Separatist)),
                    Is.SameAs(property.GetValue(data.GetAnnouncer(FactionType.Rebellion))), property.Name);
            }
        }

        [TestCase(FactionType.Republic, "I000_EHD4613_ENG")]
        [TestCase(FactionType.Separatist, "I000_RHD4706_ENG")]
        [TestCase(FactionType.Rebellion, "I000_RHD4713_ENG")]
        [TestCase(FactionType.Empire, "I000_EHD4606_ENG")]
        public void UnitProductionClipsAreSplitAcrossFourFactions(FactionType faction, string clipName)
        {
            InterfaceSfxData data = AssetDatabase.LoadAssetAtPath<InterfaceSfxData>(DATA_PATH);
            Assert.That(data.GetAnnouncer(faction).UnitProduction.name, Is.EqualTo(clipName));
        }

        [TestCase(FactionType.Republic, "I000_EHD4616_ENG")]
        [TestCase(FactionType.Empire, "I000_EHD4616_ENG")]
        [TestCase(FactionType.Separatist, "I000_RHD4718_ENG")]
        [TestCase(FactionType.Rebellion, "I000_RHD4718_ENG")]
        public void BuildingStartedClipsMatchFaction(FactionType faction, string clipName)
        {
            InterfaceSfxData data = AssetDatabase.LoadAssetAtPath<InterfaceSfxData>(DATA_PATH);
            Assert.That(data.GetAnnouncer(faction).BuildingStarted.name, Is.EqualTo(clipName));
        }

        [TestCase(SiteFacilityType.Mining)]
        [TestCase(SiteFacilityType.BattleAsteroid)]
        public void LocalConstructionAnnouncesOnceAndEnemyConstructionStaysSilent(SiteFacilityType type)
        {
            Invoke("HandleConstructionStarted", TestPlayers.Enemy, type);
            Assert.That(_audio.Clips, Is.Empty);
            Invoke("HandleConstructionStarted", TestPlayers.Human, type);
            Invoke("HandleConstructionStarted", TestPlayers.Human, type);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.BuildingStarted }));
            Assert.That(_audio.Priorities, Is.EqualTo(new[] { AnnouncementPriority.Progress }));
        }

        [TestCase(SiteFacilityType.QuadLaserSatellite)]
        [TestCase(SiteFacilityType.MissileSatellite)]
        public void LocalSatelliteCompletionAndLossReuseConstructionAndUnitLossCues(SiteFacilityType type)
        {
            Invoke("HandleConstructionCompleted", TestPlayers.Enemy, type);
            Invoke("HandleLost", TestPlayers.Enemy, type);
            Invoke("HandleConstructionCompleted", TestPlayers.Human, SiteFacilityType.Mining);
            Assert.That(_audio.Clips, Is.Empty);
            Invoke("HandleConstructionCompleted", TestPlayers.Human, type);
            Invoke("HandleLost", TestPlayers.Human, type);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.ConstructionComplete, _clips.UnitDestroyed }));
            Assert.That(_audio.Priorities,
                Is.EqualTo(new[] { AnnouncementPriority.Progress, AnnouncementPriority.Notification }));
        }

        [Test]
        public void AcceptedResearchAndCancelUseDistinctProgressCuesAndThrottleRepeats()
        {
            var request = new ResearchUnitRequest(new FactionData(), default, 1);
            Invoke("HandlePurchaseAccepted", request);
            Invoke("HandlePurchaseAccepted", request);
            Invoke("HandlePurchaseCanceled", request);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UpgradeProgress, _clips.UpgradeCanceled }));
            Assert.That(_audio.Priorities, Is.All.EqualTo(AnnouncementPriority.Progress));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AcceptedUnitPurchaseAnnouncesProduction(bool squadron)
        {
            UnitRequest request = squadron
                ? new SquadronUnitRequest(new FactionData(), default)
                : new ShipUnitRequest(new FactionData(), default);
            Invoke("HandlePurchaseAccepted", request);
            Invoke("HandlePurchaseCanceled", request);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitProduction }));
            Assert.That(_audio.Priorities, Is.EqualTo(new[] { AnnouncementPriority.Progress }));
        }

        [Test]
        public void ShipAndSquadronOrdersShareProductionCooldown()
        {
            Invoke("HandlePurchaseAccepted", new ShipUnitRequest(new FactionData(), default));
            Invoke("HandlePurchaseAccepted", new SquadronUnitRequest(new FactionData(), default));
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitProduction }));
            ((Dictionary<AudioClip, float>)GetField(_service, "_readyAt"))[_clips.UnitProduction] = Time.unscaledTime;
            Invoke("HandlePurchaseAccepted", new SquadronUnitRequest(new FactionData(), default));
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitProduction, _clips.UnitProduction }));
        }

        [Test]
        public void StructurePurchaseDoesNotAnnounceUnitProduction()
        {
            Invoke("HandlePurchaseAccepted", new MiningFacilityUnitRequest(new FactionData(), default));
            Assert.That(_audio.Clips, Is.Empty);
        }

        [Test]
        public void CapacityRestoreIsSilentAndLivePlacementRearmsAfterCapacityDrops()
        {
            SetField(_capacity, "_currentUnitCapacity", 20);
            Invoke("HandleCapacityChanged", 20);
            Assert.That(_audio.Clips, Is.Empty, "Restoring capacity must never announce.");
            Invoke("HandleDeployed", new ShipUnitRequest(new FactionData(), default));
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitCapReached }));
            Invoke("HandleCapacityRejected");
            Assert.That(_audio.Clips.Count, Is.EqualTo(1), "Rejections share the cap cooldown.");
            Invoke("HandleCapacityChanged", 19);
            ((Dictionary<AudioClip, float>)GetField(_service, "_readyAt")).Clear();
            Invoke("HandleDeployed", new ShipUnitRequest(new FactionData(), default));
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitCapReached, _clips.UnitCapReached }));
        }

        [Test]
        public void StructurePlacementAtFullCapacityDoesNotAnnounceReinforcementsOrCap()
        {
            SetField(_capacity, "_currentUnitCapacity", 20);
            Invoke("HandleDeployed", new MiningFacilityUnitRequest(new FactionData(), default));
            Assert.That(_audio.Clips, Is.Empty);
        }

        [Test]
        public void MineCaptureOnlyAnnouncesLocalNeutralCapture()
        {
            Invoke("HandleCaptured", TestPlayers.Enemy, TestPlayers.Human, SiteFacilityType.Mining);
            Invoke("HandleCaptured", default(EmpireAtWar.Models.Players.PlayerId), TestPlayers.Enemy, SiteFacilityType.Mining);
            Assert.That(_audio.Clips, Is.Empty);
            Invoke("HandleCaptured", default(EmpireAtWar.Models.Players.PlayerId), TestPlayers.Human, SiteFacilityType.Mining);
            Assert.That(_audio.Clips.Count, Is.EqualTo(1));
            Assert.That(_audio.Clips[0].name, Is.EqualTo("Captured"));
        }

        [Test]
        public void MineWarningsAreLocalAndPerEntityCooldownSuppressesRepeatedDamage()
        {
            var localDamage = new DamageEvents();
            var local = new EntityStub(TestPlayers.Human, new MineFacade(), localDamage);
            var enemyDamage = new DamageEvents();
            var enemy = new EntityStub(TestPlayers.Enemy, new MineFacade(), enemyDamage);
            Invoke("HandleEntityAdded", local);
            Invoke("HandleEntityAdded", enemy);
            enemyDamage.Raise();
            Assert.That(_audio.Clips, Is.Empty);
            localDamage.Raise();
            localDamage.Raise();
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.MiningUnderAttack }));
            var subscriptions = (System.Collections.IDictionary)GetField(_service, "_subscriptions");
            SetField(subscriptions[local], "DamageReadyAt", -1f);
            ((Dictionary<AudioClip, float>)GetField(_service, "_readyAt")).Clear();
            localDamage.Raise();
            Assert.That(_audio.Clips.Count, Is.EqualTo(2));
            Invoke("HandleEntityRemoved", local);
            localDamage.Raise();
            Assert.That(_audio.Clips.Count, Is.EqualTo(2), "Removed entities must be unsubscribed.");
        }

        [Test]
        public void UnitLossThrottlesMassCasualtiesAndIgnoresDespawnAndEnemyLoss()
        {
            var first = LocalShip(new Vector3(0.5f, 0.5f, 1f));
            var second = LocalShip(new Vector3(0.5f, 0.5f, 1f));
            var enemy = new EntityStub(TestPlayers.Enemy, new UnitFacade());
            Invoke("HandleEntityRemoved", first);
            Assert.That(_audio.Clips, Is.Empty, "Alive despawn must be silent.");
            Invoke("HandleEntityAdded", first);
            enemy.Health.Destroy();
            first.Health.Destroy();
            second.Health.Destroy();
            Invoke("HandleEntityRemoved", enemy);
            Invoke("HandleEntityRemoved", first);
            Invoke("HandleEntityRemoved", second);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitDestroyed }));
        }

        [TestCase(0f, true)]
        [TestCase(4.9f, true)]
        [TestCase(5f, true)]
        [TestCase(5.1f, false)]
        public void UnitLossUsesFiveSecondCameraMemory(float secondsSinceSeen, bool expected)
        {
            var ship = LocalShip(new Vector3(0.5f, 0.5f, 1f));
            var subscriptions = (System.Collections.IDictionary)GetField(_service, "_subscriptions");
            SetField(subscriptions[ship], "LastSeenAt", Time.unscaledTime - secondsSinceSeen);
            ship.GetFacade<IEntityTransformFacade>().Transform.position = new Vector3(2f, 0.5f, 1f);
            ship.Health.Destroy();
            Invoke("HandleEntityRemoved", ship);
            Assert.That(_audio.Clips.Count, Is.EqualTo(expected ? 1 : 0));
        }

        [TestCase(2f, 0.5f, 1f)]
        [TestCase(0.5f, 0.5f, -1f)]
        public void UnitNeverSeenOnCameraIsSilent(float x, float y, float z)
        {
            var ship = LocalShip(new Vector3(x, y, z));
            _service.Tick();
            ship.Health.Destroy();
            Invoke("HandleEntityRemoved", ship);
            Assert.That(_audio.Clips, Is.Empty);
        }

        [Test]
        public void TickRemembersShipSeenAfterSpawnWhenCameraMovesAway()
        {
            var ship = LocalShip(new Vector3(2f, 0.5f, 1f));
            Transform transform = ship.GetFacade<IEntityTransformFacade>().Transform;
            transform.position = new Vector3(0.5f, 0.5f, 1f);
            _service.Tick();
            transform.position = new Vector3(2f, 0.5f, 1f);
            ship.Health.Destroy();
            Invoke("HandleEntityRemoved", ship);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitDestroyed }));
        }

        [Test]
        public void ShipOnCameraAtDestructionDoesNotWaitForNextTick()
        {
            var ship = LocalShip(new Vector3(2f, 0.5f, 1f));
            ship.GetFacade<IEntityTransformFacade>().Transform.position = new Vector3(0.5f, 0.5f, 1f);
            ship.Health.Destroy();
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitDestroyed }));
        }

        [Test]
        public void DelayedRemovalAfterTransformDestructionDoesNotRepeatLossCue()
        {
            var ship = LocalShip(new Vector3(0.5f, 0.5f, 1f));
            ship.Health.Destroy();
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitDestroyed }));
            UnityEngine.Object.DestroyImmediate(ship.GetFacade<IEntityTransformFacade>().Transform.gameObject);
            ((Dictionary<AudioClip, float>)GetField(_service, "_readyAt")).Clear();
            Assert.DoesNotThrow(() => _service.Tick());
            Assert.DoesNotThrow(() => Invoke("HandleEntityRemoved", ship));
            Assert.That(_audio.Clips.Count, Is.EqualTo(1));
        }

        [Test]
        public void SeeingDeadCorpseBeforeDelayedRemovalDoesNotAnnounceLoss()
        {
            var ship = LocalShip(new Vector3(2f, 0.5f, 1f));
            ship.Health.Destroy();
            ship.GetFacade<IEntityTransformFacade>().Transform.position = new Vector3(0.5f, 0.5f, 1f);
            _service.Tick();
            Invoke("HandleEntityRemoved", ship);
            Assert.That(_audio.Clips, Is.Empty);
        }

        [Test]
        public void DespawnUnsubscribesFromHealthDestruction()
        {
            var ship = LocalShip(new Vector3(0.5f, 0.5f, 1f));
            Invoke("HandleEntityRemoved", ship);
            ship.Health.Destroy();
            Assert.That(_audio.Clips, Is.Empty);
        }

        [Test]
        public void FogHiddenShipDoesNotAccumulateCameraVisibility()
        {
            var fog = new FogEvents();
            var ship = LocalShip(new Vector3(0.5f, 0.5f, 1f), fog);
            _service.Tick();
            ship.Health.Destroy();
            Invoke("HandleEntityRemoved", ship);
            Assert.That(_audio.Clips, Is.Empty);
        }

        [Test]
        public void EnemyFleetAnnouncesFirstRevealThenRequiresAQuietPeriod()
        {
            var fog = new FogEvents();
            var enemy = new EntityStub(TestPlayers.Enemy, new UnitFacade(), fog);
            Invoke("HandleEntityAdded", enemy);
            Assert.That(_audio.Clips, Is.Empty, "Initial binding and restore must be silent.");
            fog.SetVisible(true);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.EnemyFleetApproaching }));
            fog.SetVisible(false);
            fog.SetVisible(true);
            Assert.That(_audio.Clips.Count, Is.EqualTo(1));
            fog.SetVisible(false);
            SetField(_service, "_fleetQuietSince", Time.unscaledTime - 21f);
            ((Dictionary<AudioClip, float>)GetField(_service, "_readyAt")).Clear();
            fog.SetVisible(true);
            Assert.That(_audio.Clips.Count, Is.EqualTo(2));
        }

        [Test]
        public void DefeatPlaysOnceAndSuppressesLaterNotifications()
        {
            var result = new BattleResult(Array.Empty<FactionType>(), BattleOutcome.EnemyVictory,
                default, default, FactionType.Republic, 0, 1, false, true);
            _service.UpdateState(result);
            _service.UpdateState(result);
            Invoke("HandleCapacityRejected");
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.Defeated }));
            Assert.That(_audio.Priorities, Is.EqualTo(new[] { AnnouncementPriority.Defeat }));
        }

        [Test]
        public void SimulationResultNeverAnnouncesDefeat()
        {
            _service.UpdateState(new BattleResult(Array.Empty<FactionType>(), BattleOutcome.EnemyVictory,
                default, default, FactionType.Republic, 0, 1, false, true, GameMode.BattleSimulation));
            Assert.That(_audio.Clips, Is.Empty);
        }

        [Test]
        public void MineDeathAnnouncesDestroyedAtSiteLossAndNotAgainAtRemoval()
        {
            var mine = new EntityStub(TestPlayers.Human, new MineFacade(), new DamageEvents());
            Invoke("HandleEntityAdded", mine);
            mine.Health.Destroy();
            Invoke("HandleLost", TestPlayers.Human, SiteFacilityType.Mining);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.MiningDestroyed }));
            Assert.That(_audio.Priorities, Is.EqualTo(new[] { AnnouncementPriority.Warning }));
            ((Dictionary<AudioClip, float>)GetField(_service, "_readyAt")).Clear();
            Invoke("HandleEntityRemoved", mine);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.MiningDestroyed }));
        }

        [Test]
        public void EnemyMineAndNonMiningSiteLossStaySilent()
        {
            Invoke("HandleLost", TestPlayers.Enemy, SiteFacilityType.Mining);
            Invoke("HandleLost", TestPlayers.Human, SiteFacilityType.BattleAsteroid);
            Assert.That(_audio.Clips, Is.Empty);
        }

        [TestCase("HandleCapacityRejected")]
        [TestCase("HandleResearchCompleted")]
        public void RefusedNotificationCanPlayOnImmediateRetry(string handler)
        {
            object[] arguments = handler == "HandleResearchCompleted" ? new object[] { default(ResearchType) } : Array.Empty<object>();
            _audio.Accept = false;
            Invoke(handler, arguments);
            Assert.That(_audio.Clips, Is.Empty);
            _audio.Accept = true;
            Invoke(handler, arguments);
            Invoke(handler, arguments);
            Assert.That(_audio.Clips, Is.EqualTo(new[]
            {
                handler == "HandleResearchCompleted" ? _clips.UpgradeComplete : _clips.UnitCapReached
            }));
            Assert.That(_audio.Priorities, Is.EqualTo(new[] { AnnouncementPriority.Notification }));
        }

        [Test]
        public void RefusedUnitLossDoesNotSilenceTheNextCasualty()
        {
            var first = LocalShip(new Vector3(0.5f, 0.5f, 1f));
            var second = LocalShip(new Vector3(0.5f, 0.5f, 1f));
            _audio.Accept = false;
            first.Health.Destroy();
            _audio.Accept = true;
            second.Health.Destroy();
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.UnitDestroyed }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RefusedDamageWarningDoesNotConsumeEntityCooldown(bool station)
        {
            var damage = new DamageEvents();
            IEntityFacade facility = station ? new StationEvents() : new MineFacade();
            var entity = new EntityStub(TestPlayers.Human, facility, damage);
            Invoke("HandleEntityAdded", entity);
            _audio.Accept = false;
            damage.Raise();
            Assert.That(_audio.Clips, Is.Empty);
            _audio.Accept = true;
            damage.Raise();
            damage.Raise();
            Assert.That(_audio.Clips, Is.EqualTo(new[] { station ? _clips.StationUnderAttack : _clips.MiningUnderAttack }));
            Assert.That(_audio.Priorities, Is.EqualTo(new[] { AnnouncementPriority.Warning }));
        }

        [Test]
        public void RefusedCaptureDoesNotConsumeCaptureCooldown()
        {
            _audio.Accept = false;
            Invoke("HandleCaptured", PlayerId.None, TestPlayers.Human, SiteFacilityType.Mining);
            _audio.Accept = true;
            Invoke("HandleCaptured", PlayerId.None, TestPlayers.Human, SiteFacilityType.Mining);
            Invoke("HandleCaptured", PlayerId.None, TestPlayers.Human, SiteFacilityType.Mining);
            Assert.That(_audio.Clips.Count, Is.EqualTo(1));
            Assert.That(_audio.Clips[0].name, Is.EqualTo("Captured"));
        }

        [Test]
        public void RefusedMineDestructionDoesNotSilenceTheNextMine()
        {
            _audio.Accept = false;
            Invoke("HandleLost", TestPlayers.Human, SiteFacilityType.Mining);
            _audio.Accept = true;
            Invoke("HandleLost", TestPlayers.Human, SiteFacilityType.Mining);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.MiningDestroyed }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StationUpgradeRetriesRefusalAndOnlyFollowsWithNewUnlocks(bool unlocks)
        {
            SetField(_service, "_roster", CreateRoster(unlocks));
            _audio.Accept = false;
            Invoke("AnnounceStationUpgrade", 2);
            Assert.That(_audio.Clips, Is.Empty);
            _audio.Accept = true;
            Invoke("AnnounceStationUpgrade", 2);
            Invoke("AnnounceStationUpgrade", 2);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { _clips.StationUpgraded }));
            Assert.That(_audio.FollowUps, Is.EqualTo(new[] { unlocks ? _clips.NewConstruction : null }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StationUpgradeRoutesEnemyAndHostedProductionHost(bool hosted)
        {
            SetField(_service, "_local", TestPlayers.CreateLocalPlayer(TestPlayers.CreateTeamGame()));
            SetField(_service, "_production", new ProductionAccessStub(hosted ? TestPlayers.Ally : TestPlayers.Human));
            SetField(_service, "_roster", CreateRoster(true));
            var level = new StationEvents();
            var station = new EntityStub(hosted ? TestPlayers.Ally : TestPlayers.Enemy, level);
            Invoke("HandleEntityAdded", station);
            Assert.That(_audio.Clips, Is.Empty, "Binding a restored station must stay silent.");
            level.Upgrade(2);
            Assert.That(_audio.Clips, Is.EqualTo(new[] { hosted ? _clips.StationUpgraded : _clips.EnemyStationUpgraded }));
            Assert.That(_audio.Priorities, Is.EqualTo(new[] { AnnouncementPriority.Notification }));
            Assert.That(_audio.FollowUps.Count, Is.EqualTo(hosted ? 1 : 0));
            Invoke("HandleEntityRemoved", station);
            ((Dictionary<AudioClip, float>)GetField(_service, "_readyAt")).Clear();
            level.Upgrade(3);
            Assert.That(_audio.Clips.Count, Is.EqualTo(1), "Removed stations must be unsubscribed.");
        }

        private FactionRoster CreateRoster(bool unlocks)
        {
            var definition = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<FactionDefinition>(
                "Assets/Settings/Data/Factions/Republic/RepublicFaction.asset"));
            _created.Add(definition);
            definition.Ships.Clear();
            definition.Squadrons.Clear();
            SetField(definition, "miningFacilities", Array.Empty<MiningFacilityType>());
            SetField(definition, "defendPlatforms", Array.Empty<EmpireAtWar.Entities.DefendPlatform.DefendPlatformType>());
            SetField(definition, "superWeapons", Array.Empty<EmpireAtWar.Entities.SuperWeapons.SuperWeaponType>());
            if (unlocks)
            {
                var ship = new FactionData();
                SetField(ship, "<AvailableLevel>k__BackingField", 2);
                definition.Ships.Add(default, ship);
            }
            return new FactionRoster(definition, null, null, null);
        }

        private AudioClip Clip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 1, 1, 44100, false);
            _created.Add(clip);
            return clip;
        }

        private EntityStub LocalShip(Vector3 viewportPosition, IFogVisibilityFacade fog = null)
        {
            var root = new GameObject("Visible ship test");
            _created.Add(root);
            root.transform.position = viewportPosition;
            var ship = new EntityStub(TestPlayers.Human, new UnitFacade(),
                new TransformFacade(root.transform), fog ?? new VisibleFogFacade());
            Invoke("HandleEntityAdded", ship);
            return ship;
        }

        private void Invoke(string method, params object[] arguments) => typeof(FactionAnnouncementService)
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(_service, arguments);
        private static object GetField(object target, string field) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void SetField(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, value);

        private sealed class EntityStub : IEntity
        {
            private readonly IEntityFacade[] _facades;
            public HealthStub Health { get; } = new();
            public long Id => 1;
            public IHealthModelObserver HealthModel => Health;
            public PlayerId Owner { get; }
            public EntityStub(PlayerId owner, params IEntityFacade[] facades) { Owner = owner; _facades = facades; }
            public bool TryGetFacade<T>(out T facade) where T : IEntityFacade
            {
                foreach (IEntityFacade candidate in _facades)
                    if (candidate is T match) { facade = match; return true; }
                facade = default;
                return false;
            }
            public T GetFacade<T>() where T : IEntityFacade => TryGetFacade(out T facade)
                ? facade : throw new InvalidOperationException("Missing test facade.");
        }

        private sealed class MineFacade : IMiningFacilityFacade { }
        private sealed class StationEvents : IStationLevelFacade
        {
            public event Action<int> LevelUpgraded = delegate { };
            public int CurrentLevel { get; private set; } = 1;
            public void Upgrade(int level) { CurrentLevel = level; LevelUpgraded(level); }
        }
        private sealed class ProductionAccessStub : IProductionAccess
        {
            public bool CanProduce => true;
            public PlayerId ProductionHost { get; }
            public ProductionAccessStub(PlayerId host) => ProductionHost = host;
            public bool TryGetProductionBase(out IEntity productionBase) { productionBase = null; return false; }
        }
        private sealed class TransformFacade : IEntityTransformFacade
        {
            public Transform Transform { get; }
            public TransformFacade(Transform transform) => Transform = transform;
        }

        private sealed class CameraStub : ICameraService
        {
            public string Id => nameof(CameraStub);
            public Vector3 CameraPosition => default;
            public Transform CameraTransform => null;
            public Vector3 CameraForward => Vector3.forward;
            public float FieldOfView => 60f;
            public UnityEngine.Camera SceneCamera => null;
            public float ZoomTargetHeight => 0f;
            public Vector3 GetWorldPoint(Vector2 point, Vector3 origin) => default;
            public RaycastHit ScreenPointToRay(Vector2 point) => default;
            public int ScreenPointRaycastAll(Vector2 point, RaycastHit[] results) => 0;
            public Vector3 WorldToViewportPoint(Vector3 point) => point;
            public Vector2 WorldToScreenPoint(Vector3 point) => default;
            public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 min, Vector2 max) => Array.Empty<Vector3>();
            public void MoveTo(Vector3 point) { }
            public void SetPose(Vector3 position, Quaternion rotation) { }
            public void RestorePose(Vector3 position, Quaternion rotation, float zoomTargetHeight) { }
            public void SetCullingMask(LayerMask mask) { }
        }
        private sealed class UnitFacade : IUnitTypeFacade
        {
            public UnitTypeId UnitTypeId => UnitTypeId.Ship(default);
        }
        private sealed class DamageEvents : IHealthDamageFacade
        {
            public event Action Damaged = delegate { };
            public void Raise() => Damaged();
        }
        private sealed class FogEvents : IFogVisibilityFacade
        {
            public event Action<bool> VisibilityChanged = delegate { };
            public bool IsHiddenByFog { get; private set; } = true;
            public void SetVisible(bool visible) { IsHiddenByFog = !visible; VisibilityChanged(visible); }
        }
        private sealed class HealthStub : IHealthModelObserver
        {
            public event Action OnDestroy = delegate { };
            public event Action OnValueChanged { add { } remove { } }
            public ShipClass ShipClass => ShipClass.Cruiser;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; private set; }
            public void Destroy() { IsDestroyed = true; OnDestroy(); }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public bool HasLiveHardPoints => false;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;
            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();
        }

        private sealed class RecordingAudio : IInterfaceSfxService
        {
            public readonly List<AudioClip> Clips = new();
            public readonly List<AnnouncementPriority> Priorities = new();
            public readonly List<AudioClip> FollowUps = new();
            public bool Accept = true;
            public bool IsAnnouncing => false;
            public void PlayRejected() { }
            public void PlayAbilityTargetingCanceled() { }
            public void PlayAnnouncement(AudioClip clip) => TryPlayAnnouncement(clip, AnnouncementPriority.Warning);
            public bool TryPlayAnnouncement(AudioClip clip, AnnouncementPriority priority)
            {
                if (!Accept) return false;
                Clips.Add(clip);
                Priorities.Add(priority);
                return true;
            }
            public bool TryPlayStationUpgrade(AudioClip clip, AudioClip newConstruction)
            {
                if (!TryPlayAnnouncement(clip, AnnouncementPriority.Notification)) return false;
                FollowUps.Add(newConstruction);
                return true;
            }
        }
    }
}
