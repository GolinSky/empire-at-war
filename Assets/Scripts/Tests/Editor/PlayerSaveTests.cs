using System.Linq;
using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("GameFlow")]
    public sealed class PlayerSaveTests
    {
        private const string SHARED_DATA = "Assets/Settings/Data/Factions/Shared/";

        [Test]
        public void UnitRequest_EveryKind_RebuildsTheSameRequestFromFactionData()
        {
            FactionRoster roster = CreateRoster(FactionType.Empire);
            StationLevelData levels = AssetDatabase.FindAssets("t:StationLevelData")
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<StationLevelData>).First();
            UnitRequestFactory factory = new UnitRequestFactory();
            ResearchType research = roster.ResearchLines.Keys.First();
            UnitRequest[] requests =
            {
                factory.ConstructUnitRequest(roster.Ships.First().Value, roster.Ships.First().Key),
                factory.ConstructUnitRequest(roster.Squadrons.First().Value, roster.Squadrons.First().Key),
                factory.ConstructUnitRequest(levels.GetLevelFactionData(1), 1),
                factory.ConstructUnitRequest(roster.MiningFacilities.First().Value, roster.MiningFacilities.First().Key),
                factory.ConstructUnitRequest(roster.DefendPlatforms.First().Value, roster.DefendPlatforms.First().Key),
                factory.ConstructUnitRequest(roster.ResearchLines[research].Tiers.Last().FactionData, research,
                    roster.ResearchLines[research].Tiers.Length),
                factory.ConstructUnitRequest(roster.SuperWeapons.First().Value, roster.SuperWeapons.First().Key)
            };

            foreach (UnitRequest request in requests)
            {
                UnitRequestSave save = JsonUtility.FromJson<UnitRequestSave>(
                    JsonUtility.ToJson(UnitRequestSave.From(request, 3.5f)));
                UnitRequest rebuilt = save.ToRequest(factory, roster, levels);

                Assert.That(rebuilt.GetType(), Is.EqualTo(request.GetType()));
                Assert.That(rebuilt.Id, Is.EqualTo(request.Id));
                Assert.That(rebuilt.FactionData, Is.SameAs(request.FactionData), request.Id);
                Assert.That(save.TimeLeft, Is.EqualTo(3.5f));
            }
        }

        [Test]
        public void ProductionQueue_Restore_KeepsTimeLeftAndTakesStructureAndHeroReservations()
        {
            PlayerFactionModel faction = new PlayerFactionModel(null, null);
            FactionData single = CreateFactionData(maxCount: 1, buildTime: 30);
            MiningFacilityUnitRequest mine = new MiningFacilityUnitRequest(single, MiningFacilityType.CommonMiner);
            FactionData hero = CreateFactionData(maxCount: 1, buildTime: 30);
            typeof(FactionData).GetField("isHero", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hero, true);

            faction.RestoreQueue(new[]
            {
                new ProductionQueueItem(new ShipUnitRequest(hero, ShipType.HomeOne), 12f),
                new ProductionQueueItem(mine, 5f)
            });

            Assert.That(faction.QueuedItems.Select(item => item.RemainingBuildTime), Is.EqualTo(new[] { 12f, 5f }));
            Assert.That(faction.IsHeroReserved(ShipType.HomeOne), Is.True);
            Assert.That(faction.CanQueueUnit(mine), Is.False, "The queued facility holds the only structure slot.");
        }

        [Test]
        public void Reserve_ListsWaitingRequestsUntilPlaced()
        {
            ReinforcementModel reinforcements = new ReinforcementModel(null);
            SquadronUnitRequest squadron = new SquadronUnitRequest(CreateFactionData(1, 1), SquadronType.XWing);

            reinforcements.AddReinforcement(squadron);
            reinforcements.AddReinforcement(squadron);

            Assert.That(reinforcements.Reserve.Single().Key, Is.SameAs(squadron));
            Assert.That(reinforcements.Reserve.Single().Value, Is.EqualTo(2));
            reinforcements.ConsumeReinforcement(squadron);
            reinforcements.ConsumeReinforcement(squadron);
            Assert.That(reinforcements.Reserve, Is.Empty);
        }

        [Test]
        public void PlacedSquadrons_ListsOnlyLivingPlacedSquadrons()
        {
            ReinforcementModel reinforcements = new ReinforcementModel(null);
            reinforcements.UpdateSquadronData(new SquadronUnitRequest(CreateFactionData(1, 1), SquadronType.XWing));

            reinforcements.AddUnitCapacity(SquadronType.XWing, 7);
            reinforcements.AddUnitCapacity(SquadronType.XWing, 8);
            reinforcements.RemoveUnitCapacity(SquadronType.XWing, 7);

            Assert.That(reinforcements.PlacedSquadrons, Is.EqualTo(new long[] { 8 }));
        }

        [Test]
        public void SuperWeapon_RestoredReadyWeapon_FiresOnlyAfterItsCooldown()
        {
            SuperWeaponModel superWeapons = new SuperWeaponModel();

            superWeapons.Restore(SuperWeaponType.IonCannon, SuperWeaponState.Ready, 30f);

            Assert.That(superWeapons.CanFire(SuperWeaponType.IonCannon), Is.False);
            superWeapons.Tick(30f);
            Assert.That(superWeapons.CanFire(SuperWeaponType.IonCannon), Is.True);
            Assert.That(superWeapons.GetCooldownRemaining(SuperWeaponType.IonCannon), Is.Zero);
        }

        [Test]
        public void BuildQueue_CompletesEachBuildOnItsOwnTimeInOrder()
        {
            EnemyBuildQueue builds = new EnemyBuildQueue();
            FactionData data = CreateFactionData(1, 10);
            ShipUnitRequest first = new ShipUnitRequest(data, ShipType.Venator);
            DefendPlatformUnitRequest second = new DefendPlatformUnitRequest(data, DefendPlatformType.Xq6);
            builds.Add(first, 2f);
            builds.Add(second, 2f);
            builds.Add(new ShipUnitRequest(data, ShipType.Acclamator), 5f);
            System.Collections.Generic.List<UnitRequest> finished = new System.Collections.Generic.List<UnitRequest>();

            builds.Advance(2f, finished.Add);

            Assert.That(finished, Is.EqualTo(new UnitRequest[] { first, second }));
            Assert.That(builds.Builds.Single().TimeLeft, Is.EqualTo(3f));
        }

        [Test]
        public void BuildQueue_SameIdMatchesHumanTimingAndCarriesExcessTime()
        {
            EnemyBuildQueue builds = new EnemyBuildQueue();
            PlayerFactionModel human = new PlayerFactionModel(null, null);
            FactionData data = CreateFactionData(3, 10);
            UnitRequest[] requests =
            {
                new LevelUnitRequest(data, 1),
                new LevelUnitRequest(data, 2),
                new LevelUnitRequest(data, 1),
                new LevelUnitRequest(data, 1)
            };
            System.Collections.Generic.List<UnitRequest> finished = new System.Collections.Generic.List<UnitRequest>();
            System.Collections.Generic.List<UnitRequest> humanFinished = new System.Collections.Generic.List<UnitRequest>();
            human.OnUnitCompleted += humanFinished.Add;
            foreach (UnitRequest request in requests)
            {
                builds.Add(request, data.BuildTime);
                human.QueueUnit(request);
            }

            foreach (float elapsed in new[] { 4f, 8f, 18f })
            {
                builds.Advance(elapsed, finished.Add);
                human.Advance(elapsed);
                Assert.That(finished, Is.EquivalentTo(humanFinished));
                Assert.That(builds.Builds.OrderBy(build => build.Request.Id).Select(build => build.TimeLeft),
                    Is.EqualTo(human.QueuedItems.OrderBy(item => item.UnitRequest.Id)
                        .Select(item => item.RemainingBuildTime)));
            }
            Assert.That(finished.Where(request => request.Id == "1"),
                Is.EqualTo(new[] { requests[0], requests[2], requests[3] }));
        }

        [Test]
        public void BuildQueue_AdmissionMatchesHumanPipelineAndPerIdLimits()
        {
            EnemyBuildQueue builds = new EnemyBuildQueue();
            PlayerFactionModel human = new PlayerFactionModel(null, null);
            FactionData data = CreateFactionData(2, 10);
            for (int id = 1; id <= PlayerFactionModel.MAX_ACTIVE_PIPELINES; id++)
            {
                LevelUnitRequest request = new LevelUnitRequest(data, id);
                builds.Add(request, 10f);
                human.QueueUnit(request);
            }

            Assert.That(builds.CanAdd("8", data.MaxCount), Is.False);
            Assert.That(builds.CanAdd("1", data.MaxCount), Is.True);
            LevelUnitRequest second = new LevelUnitRequest(data, 1);
            builds.Add(second, 10f);
            human.QueueUnit(second);
            Assert.That(builds.CanAdd(second.Id, data.MaxCount),
                Is.EqualTo(human.CanQueueUnit(second)).And.False);

            builds.RemoveAll(request => request.Id == "2");
            Assert.That(builds.CanAdd("8", data.MaxCount), Is.True);
            builds.Clear();
            Assert.That(builds.CanAdd("1", 0), Is.False);
            Assert.That(builds.CanAdd("8", data.MaxCount), Is.True);
        }

        [Test]
        public void BuildQueue_RestoredSameIdPreservesRemainingTimesAndWaitsItsTurn()
        {
            EnemyBuildQueue restored = new EnemyBuildQueue();
            FactionData data = CreateFactionData(2, 10);
            ShipUnitRequest first = new ShipUnitRequest(data, ShipType.Venator);
            ShipUnitRequest second = new ShipUnitRequest(data, ShipType.Venator);
            restored.Add(first, 3f);
            restored.Add(second, 7f);
            System.Collections.Generic.List<UnitRequest> finished = new System.Collections.Generic.List<UnitRequest>();

            restored.Advance(5f, finished.Add);

            Assert.That(finished, Is.EqualTo(new[] { first }));
            Assert.That(restored.Builds.Single().TimeLeft, Is.EqualTo(5f));
            restored.Advance(5f, finished.Add);
            Assert.That(finished, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void BuildQueue_LegacySaveWithEightIdsRunsOnlySevenAtOnce()
        {
            EnemyBuildQueue restored = new EnemyBuildQueue();
            FactionData data = CreateFactionData(1, 10);
            for (int id = 1; id <= 8; id++)
                restored.Add(new LevelUnitRequest(data, id), 10f);
            System.Collections.Generic.List<UnitRequest> finished = new System.Collections.Generic.List<UnitRequest>();

            restored.Advance(10f, finished.Add);

            Assert.That(finished.Count, Is.EqualTo(PlayerFactionModel.MAX_ACTIVE_PIPELINES));
            Assert.That(restored.Builds.Single().TimeLeft, Is.EqualTo(10f));
            restored.Advance(10f, finished.Add);
            Assert.That(finished.Count, Is.EqualTo(8));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AiSuperWeapon_SaveLoad_ResumesPaidChargeAndCompletesOrRefundsOnce(bool hasStation)
        {
            FactionRoster roster = CreateRoster(FactionType.Empire);
            var option = roster.SuperWeapons.First();
            EnemyFactionModel faction = new EnemyFactionModel(null, roster) { CurrentLevel = option.Value.AvailableLevel };
            PlayerSlot owner = TestPlayers.CreateDuel().Get(TestPlayers.Enemy);
            ProductionAccess production = new ProductionAccess { CanProduce = true };
            EconomyModel economy = new EconomyModel(null, option.Value.Price * 3f);
            SuperWeaponModel model = new SuperWeaponModel();
            model.Restore(option.Key, SuperWeaponState.Unavailable, 45f);
            EnemySuperWeaponController controller = new EnemySuperWeaponController(
                null, null, null, production, model, faction, economy, owner, new QueuePurchaseProcessor(), null, null);
            typeof(EnemySuperWeaponController).GetMethod("TryPurchase", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { option.Key, option.Value });
            controller.AdvanceCharging(option.Value.BuildTime / 2f);
            float remaining = option.Value.BuildTime / 2f;
            PlayerSave save = PlayerSave.Create(owner.Id);
            new AiSuperWeaponSaveSource(new PlayerSaveRegistry(), owner, model, controller).Capture(save);
            save = JsonUtility.FromJson<PlayerSave>(JsonUtility.ToJson(save));

            SuperWeaponModel restoredModel = new SuperWeaponModel();
            EconomyModel restoredEconomy = new EconomyModel(null, economy.Money);
            production.CanProduce = hasStation;
            QueuePurchaseProcessor restoredProduction = new QueuePurchaseProcessor();
            EnemySuperWeaponController restored = new EnemySuperWeaponController(
                null, null, null, production, restoredModel, faction, restoredEconomy, owner, restoredProduction, null, null);
            new AiSuperWeaponSaveSource(new PlayerSaveRegistry(), owner, restoredModel, restored).RestoreBeforeUnits(save);

            Assert.That(restored.GetChargeTimeRemaining(option.Key), Is.EqualTo(remaining));
            Assert.That(restoredProduction.CanQueue(option.Key.ToString(), 1), Is.False);
            Assert.That(restoredModel.GetCooldownRemaining(option.Key), Is.EqualTo(45f));
            restored.AdvanceCharging(remaining - 0.5f);
            Assert.That(restoredModel.GetState(option.Key), Is.EqualTo(SuperWeaponState.Charging));
            restored.AdvanceCharging(0.5f);
            restored.AdvanceCharging(remaining);
            Assert.That(restoredModel.GetState(option.Key),
                Is.EqualTo(hasStation ? SuperWeaponState.Ready : SuperWeaponState.Unavailable));
            Assert.That(restoredProduction.CanQueue(option.Key.ToString(), 1), Is.True);
            Assert.That(restoredEconomy.Money,
                Is.EqualTo(economy.Money + (hasStation ? 0 : option.Value.Price)));
            Assert.That(restoredModel.CanFire(option.Key), Is.False, "Loading must preserve the independent cooldown.");
            restoredModel.Tick(45f);
            Assert.That(restoredModel.CanFire(option.Key), Is.EqualTo(hasStation));
        }

        [Test]
        public void AiSuperWeapon_LegacyChargingSave_RestartsPaidChargeWithoutSpending()
        {
            FactionRoster roster = CreateRoster(FactionType.Empire);
            var option = roster.SuperWeapons.First();
            PlayerSlot owner = TestPlayers.CreateDuel().Get(TestPlayers.Enemy);
            SuperWeaponModel model = new SuperWeaponModel();
            EconomyModel economy = new EconomyModel(null, 1f);
            EnemySuperWeaponController controller = new EnemySuperWeaponController(null, null, null,
                new ProductionAccess { CanProduce = true }, model, new EnemyFactionModel(null, roster), economy, owner,
                new QueuePurchaseProcessor(), null, null);
            PlayerSave save = PlayerSave.Create(owner.Id);
            save.SuperWeapons = new[] { JsonUtility.FromJson<SuperWeaponSave>(
                "{\"type\":" + (int)option.Key + ",\"state\":" + (int)SuperWeaponState.Charging + ",\"cooldown\":0}") };

            new AiSuperWeaponSaveSource(new PlayerSaveRegistry(), owner, model, controller).RestoreBeforeUnits(save);

            Assert.That(controller.GetChargeTimeRemaining(option.Key), Is.EqualTo(option.Value.BuildTime));
            controller.AdvanceCharging(option.Value.BuildTime);
            Assert.That(model.CanFire(option.Key), Is.True);
            Assert.That(economy.Money, Is.EqualTo(1f));
        }

        [Test]
        public void AiSuperWeapon_SharesSevenPipelinesWithNormalBuilds()
        {
            FactionRoster roster = CreateRoster(FactionType.Empire);
            var option = roster.SuperWeapons.First();
            EnemyFactionModel faction = new EnemyFactionModel(null, roster) { CurrentLevel = option.Value.AvailableLevel };
            EconomyModel economy = new EconomyModel(null, option.Value.Price * 3f);
            SuperWeaponModel model = new SuperWeaponModel();
            QueuePurchaseProcessor production = new QueuePurchaseProcessor();
            FactionData data = CreateFactionData(1, 10);
            for (int id = 1; id <= PlayerFactionModel.MAX_ACTIVE_PIPELINES; id++)
                production.Queue.Add(new LevelUnitRequest(data, id), 10f);
            EnemySuperWeaponController controller = new EnemySuperWeaponController(
                null, null, null, new ProductionAccess { CanProduce = true }, model, faction, economy,
                TestPlayers.CreateDuel().Get(TestPlayers.Enemy), production, null, null);
            MethodInfo purchase = typeof(EnemySuperWeaponController).GetMethod(
                "TryPurchase", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(purchase.Invoke(controller, new object[] { option.Key, option.Value }), Is.False);
            Assert.That(economy.Money, Is.EqualTo(option.Value.Price * 3f));
            production.Queue.RemoveAll(request => request.Id == "7");
            Assert.That(purchase.Invoke(controller, new object[] { option.Key, option.Value }), Is.True);
            Assert.That(production.CanQueue("7", 1), Is.False);
            Assert.That(production.CanQueue(option.Key.ToString(), 1), Is.False);

            controller.AdvanceCharging(option.Value.BuildTime);
            Assert.That(model.GetState(option.Key), Is.EqualTo(SuperWeaponState.Ready));
            Assert.That(production.CanQueue("7", 1), Is.True);
        }

        private sealed class QueuePurchaseProcessor : IEnemyPurchaseProcessor
        {
            public EnemyBuildQueue Queue { get; } = new EnemyBuildQueue();
            public bool IsUpgradingStation => false;
            public bool CanQueue(string requestId, int maxCount) => Queue.CanAdd(requestId, maxCount);
            public void ReserveExternalPipeline(string requestId) => Queue.ReserveExternalPipeline(requestId);
            public void ReleaseExternalPipeline(string requestId) => Queue.ReleaseExternalPipeline(requestId);
            public void Purchase(UnitRequest request) => Queue.Add(request, request.FactionData.BuildTime);
        }

        private sealed class ProductionAccess : IProductionAccess
        {
            public bool CanProduce { get; set; }
            public PlayerId ProductionHost => TestPlayers.Enemy;
            public bool TryGetProductionBase(out EmpireAtWar.Entities.BaseEntity.IEntity productionBase) =>
                throw new System.NotSupportedException();
        }

        private static FactionRoster CreateRoster(FactionType faction) => new FactionRoster(
            Load<FactionCatalog>().Get(faction), Load<MiningFacilityCatalog>(), Load<DefendPlatformCatalog>(),
            Load<SuperWeaponCatalog>());

        private static T Load<T>() where T : ScriptableObject
        {
            T data = AssetDatabase.LoadAssetAtPath<T>(SHARED_DATA + typeof(T).Name + ".asset");
            Assert.That(data, Is.Not.Null, typeof(T).Name);
            return data;
        }

        private static FactionData CreateFactionData(int maxCount, int buildTime)
        {
            FactionData data = new FactionData();
            SetBackingField(data, nameof(FactionData.MaxCount), maxCount);
            SetBackingField(data, nameof(FactionData.BuildTime), buildTime);
            return data;
        }

        private static void SetBackingField<T>(object target, string propertyName, T value) =>
            target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
