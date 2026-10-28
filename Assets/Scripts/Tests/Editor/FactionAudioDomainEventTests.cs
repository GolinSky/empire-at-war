using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Audio")]
    public sealed class FactionAudioDomainEventTests
    {
        private const string LEVELS_PATH = "Assets/Settings/Data/Factions/Shared/StationLevelData.asset";

        [Test]
        public void HumanLevelRestoreIsSilentButCommandReachesStationFacade()
        {
            PlayerFactionModel model = new PlayerFactionModel(null, null);
            StationLevelFacade facade = new StationLevelFacade(model);
            List<int> levels = new List<int>();
            facade.LevelUpgraded += levels.Add;

            model.CurrentLevel = 3;
            Assert.That(levels, Is.Empty);
            model.CompleteLevelUpgrade();

            Assert.That(levels, Is.EqualTo(new[] { 4 }));
            Assert.That(facade.CurrentLevel, Is.EqualTo(4));
        }

        [Test]
        public void AiLevelRestoreIsSilentButCommandReachesStationFacade()
        {
            EnemyFactionModel model = new EnemyFactionModel(null, null);
            StationLevelFacade facade = new StationLevelFacade(model);
            List<int> levels = new List<int>();
            facade.LevelUpgraded += levels.Add;

            model.CurrentLevel = 2;
            Assert.That(levels, Is.Empty);
            model.CompleteLevelUpgrade();

            Assert.That(levels, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void AcceptedPurchaseAndUserCancelEmitOnceButRejectedCommandsStaySilent()
        {
            FactionService service = CreateService(out PlayerFactionModel model, out Wallet wallet);
            LevelUnitRequest request = new LevelUnitRequest(model.GetCurrentLevelFactionData(), 1);
            List<UnitRequest> accepted = new List<UnitRequest>();
            List<UnitRequest> canceled = new List<UnitRequest>();
            service.PurchaseAccepted += accepted.Add;
            service.PurchaseCanceled += canceled.Add;

            wallet.CanSpend = false;
            service.TryPurchaseUnit(request);
            Assert.That(accepted, Is.Empty);
            wallet.CanSpend = true;
            service.TryPurchaseUnit(request);
            service.CancelBuilding(request.Id);
            service.CancelBuilding(request.Id);

            Assert.That(accepted, Is.EqualTo(new[] { request }));
            Assert.That(canceled, Is.EqualTo(new[] { request }));
        }

        [Test]
        public void ForceUpgradeRefundsQueuedUpgradeWithoutCancelAnnouncement()
        {
            FactionService service = CreateService(out PlayerFactionModel model, out Wallet wallet);
            LevelUnitRequest request = new LevelUnitRequest(model.GetCurrentLevelFactionData(), 1);
            service.TryPurchaseUnit(request);
            int canceled = 0;
            int upgraded = 0;
            service.PurchaseCanceled += _ => canceled++;
            service.StationUpgraded += () => upgraded++;

            Assert.That(service.ForceUpgradeStation(), Is.True);

            Assert.That(canceled, Is.Zero);
            Assert.That(upgraded, Is.EqualTo(1));
            Assert.That(wallet.Refunds, Is.EqualTo(1));
            Assert.That(model.CurrentLevel, Is.EqualTo(2));
            Assert.That(model.GetProductionQueueSnapshots(), Is.Empty);
        }

        [Test]
        public void RestoreLevelAndQueueEmitNoCommandEvents()
        {
            FactionService service = CreateService(out PlayerFactionModel model, out _);
            LevelUnitRequest request = new LevelUnitRequest(model.GetCurrentLevelFactionData(), 1);
            int events = 0;
            service.PurchaseAccepted += _ => events++;
            service.PurchaseCanceled += _ => events++;
            service.StationUpgraded += () => events++;
            service.UnitProduced += _ => events++;
            service.ResearchCompleted += _ => events++;

            model.CurrentLevel = 3;
            model.RestoreQueue(new[] { new ProductionQueueItem(request, 5f) });

            Assert.That(events, Is.Zero);
        }

        [Test]
        public void ProductionCompletionEmitsAfterReserveAddition()
        {
            PlayerFactionModel model = new PlayerFactionModel(null, null);
            Pool pool = new Pool();
            FactionService service = new FactionService(null, new Wallet(), pool, new Economy(),
                new ProductionAccess(), model, null, null, TestPlayers.CreateDuel().Get(TestPlayers.Human), null);
            ShipUnitRequest request = new ShipUnitRequest(new FactionData(), ShipType.Venator);
            UnitRequest produced = null;
            service.UnitProduced += value =>
            {
                Assert.That(pool.Added, Is.SameAs(value));
                produced = value;
            };

            typeof(FactionService).GetMethod("BuildUnit", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(service, new object[] { request });

            Assert.That(produced, Is.SameAs(request));
        }

        [Test]
        public void CaptureAndLossEmitOwnershipWhileRestoreStaysSilent()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            CaptureSiteModel model = new CaptureSiteModel(roster, 1f, 1f);
            int captures = 0;
            int losses = 0;
            model.Captured += (oldOwner, newOwner, type) =>
            {
                Assert.That(oldOwner, Is.EqualTo(PlayerId.None));
                Assert.That(newOwner, Is.EqualTo(TestPlayers.Human));
                Assert.That(type, Is.EqualTo(SiteFacilityType.Mining));
                captures++;
            };
            model.Lost += (owner, type) =>
            {
                Assert.That(owner, Is.EqualTo(TestPlayers.Human));
                losses++;
            };

            model.Restore(PlayerId.None, CaptureSiteState.Neutral, PlayerId.None, 0f,
                SiteFacilityType.Mining, 0f);
            Assert.That(captures + losses, Is.Zero);
            model.TickCapture(1f, TestPlayers.DuelTally(roster, 1, 0));
            model.ReleaseFacility();
            model.ReleaseFacility();

            Assert.That(captures, Is.EqualTo(1));
            Assert.That(losses, Is.EqualTo(1));
        }

        private static FactionService CreateService(out PlayerFactionModel model, out Wallet wallet)
        {
            StationLevelData levels = AssetDatabase.LoadAssetAtPath<StationLevelData>(LEVELS_PATH);
            Assert.That(levels, Is.Not.Null);
            model = new PlayerFactionModel(levels, null);
            wallet = new Wallet();
            return new FactionService(null, wallet, new Pool(), new Economy(), new ProductionAccess(),
                model, null, null, TestPlayers.CreateDuel().Get(TestPlayers.Human), null);
        }

        private sealed class Wallet : IWallet
        {
            public bool CanSpend = true;
            public int Refunds;
            public bool TrySpend(UnitRequest request) => CanSpend;
            public void Refund(UnitRequest request) => Refunds++;
        }

        private sealed class Pool : IReinforcementPool
        {
            public UnitRequest Added;
            public void Add(UnitRequest request) => Added = request;
        }

        private sealed class Economy : IEconomyProvider
        {
            public void AddMoney(float amount) { }
            public void AddProvider(IIncomeProvider provider) { }
            public void RemoveProvider(IIncomeProvider provider) { }
            public void RecalculateIncome(IIncomeProvider provider) { }
        }

        private sealed class ProductionAccess : IProductionAccess
        {
            public PlayerId ProductionHost => TestPlayers.Human;
            public bool CanProduce => true;
            public bool TryGetProductionBase(out IEntity entity) { entity = null; return false; }
        }
    }
}
