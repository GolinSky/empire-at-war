using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.Stations;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class FactionServiceStationLossTests
    {
        [Test]
        public void QueuedShip_ReachesReinforcementPoolAfterOwnStationFalls()
        {
            ShipUnitRequest request = CreateShipRequest();
            Run(TestPlayers.Human, request, out RecordingPool pool, out RecordingWallet wallet);

            Assert.That(pool.Added, Is.EqualTo(new[] { request }));
            Assert.That(wallet.Refunded, Is.Empty);
        }

        [Test]
        public void HostedQueuedShip_IsRefundedAfterHostStationFalls()
        {
            ShipUnitRequest request = CreateShipRequest();
            Run(TestPlayers.Ally, request, out RecordingPool pool, out RecordingWallet wallet);

            Assert.That(pool.Added, Is.Empty);
            Assert.That(wallet.Refunded, Is.EqualTo(new[] { request }));
        }

        private static void Run(PlayerId host, UnitRequest request, out RecordingPool pool, out RecordingWallet wallet)
        {
            PlayerSlot owner = TestPlayers.CreateTeamGame().Get(TestPlayers.Human);
            PlayerFactionModel model = new PlayerFactionModel(null, null);
            pool = new RecordingPool();
            wallet = new RecordingWallet();
            FactionService service = new FactionService(new NullSelection(), wallet, pool, new NullEconomy(),
                new LostStationAccess(host), model, null, null, owner, new EntityLocator());
            service.Initialize();
            try
            {
                model.QueueUnit(request);
                model.Advance(10f);
            }
            finally
            {
                service.LateDispose();
            }
        }

        private static ShipUnitRequest CreateShipRequest()
        {
            FactionData factionData = new FactionData();
            SetBackingField(factionData, nameof(FactionData.MaxCount), 1);
            SetBackingField(factionData, nameof(FactionData.BuildTime), 10);
            return new ShipUnitRequest(factionData, ShipType.Venator);
        }

        private static void SetBackingField<T>(object target, string propertyName, T value)
        {
            FieldInfo field = target.GetType().GetField($"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class LostStationAccess : IProductionAccess
        {
            public PlayerId ProductionHost { get; }
            public bool CanProduce => false;

            public LostStationAccess(PlayerId host) => ProductionHost = host;

            public bool TryGetProductionBase(out IEntity productionBase)
            {
                productionBase = null;
                return false;
            }
        }

        private sealed class RecordingPool : IReinforcementPool
        {
            public readonly List<UnitRequest> Added = new List<UnitRequest>();
            public void Add(UnitRequest request) => Added.Add(request);
        }

        private sealed class RecordingWallet : IWallet
        {
            public readonly List<UnitRequest> Refunded = new List<UnitRequest>();
            public bool TrySpend(UnitRequest unitRequest) => true;
            public void Refund(UnitRequest unitRequest) => Refunded.Add(unitRequest);
        }

        private sealed class NullEconomy : IEconomyProvider
        {
            public void AddMoney(float amount) { }
            public void AddProvider(IIncomeProvider incomeProvider) { }
            public void RemoveProvider(IIncomeProvider incomeProvider) { }
            public void RecalculateIncome(IIncomeProvider incomeProvider) { }
        }

        private sealed class NullSelection : ISelectionService
        {
            public string Id => nameof(NullSelection);
            public ISelectionContext PlayerSelectionContext => null;
            public ISelectionContext OtherSelectionContext => null;
            public void RemoveSelectable(ISelectionContext selectionContext) { }
            public void SelectCurrentUnitsByType(UnitTypeId unitTypeId) { }
            public void AddObserver(IObserver<ISelectionSubject> observer) { }
            public void RemoveObserver(IObserver<ISelectionSubject> observer) { }
        }
    }
}
