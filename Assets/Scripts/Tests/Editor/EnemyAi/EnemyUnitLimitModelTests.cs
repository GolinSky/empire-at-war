using System.Reflection;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyUnitLimitModelTests
    {
        [Test]
        public void TryReserve_StopsAtPerUnitLimit()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();

            Assert.That(model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 2, 1, 10), Is.True);
            Assert.That(model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 2, 1, 10), Is.True);
            Assert.That(model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 2, 1, 10), Is.False);
        }

        [Test]
        public void TryReserve_CountsQueuedCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();

            Assert.That(model.TryReserve(UnitLimitKey.For<FakeRequest>("ship-a"), 10, 3, 5), Is.True);
            Assert.That(model.TryReserve(UnitLimitKey.For<FakeRequest>("ship-b"), 10, 3, 5), Is.False);
            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(3));
        }

        [Test]
        public void Release_FreesCountAndCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 1, 3, 3);

            model.Release(UnitLimitKey.For<FakeRequest>("ship"), 3);

            Assert.That(model.CurrentUnitCapacity, Is.Zero);
            Assert.That(model.GetReservedCount(UnitLimitKey.For<FakeRequest>("ship")), Is.Zero);
            Assert.That(model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 1, 3, 3), Is.True);
        }

        [Test]
        public void Release_IncrementsVersionOnlyWhenAReservationIsReleased()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 2, 1, 3);
            model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 2, 1, 3);

            model.Release(UnitLimitKey.For<FakeRequest>("missing"), 3);

            Assert.That(model.ReleaseVersion, Is.Zero);

            model.Release(UnitLimitKey.For<FakeRequest>("ship"), 1);

            Assert.That(model.ReleaseVersion, Is.EqualTo(1));

            model.Release(UnitLimitKey.For<FakeRequest>("ship"), 1);

            Assert.That(model.ReleaseVersion, Is.EqualTo(2));

            model.Reset();

            Assert.That(model.ReleaseVersion, Is.Zero);
        }

        [Test]
        public void GetReservedCount_WithRequestTypeUsesControllerIdentifier()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            UnitLimitKey unitId = UnitLimitKey.For<FakeRequest>("mine");
            model.TryReserve(unitId, 2, 1, 10);

            Assert.That(
                model.GetReservedCount<FakeRequest>("mine"),
                Is.EqualTo(1));
        }

        [Test]
        public void CanReserve_ReportsLimitWithoutChangingCountOrCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 1, 3, 10);

            bool canReserveSameShip = model.CanReserve(UnitLimitKey.For<FakeRequest>("ship"), 1, 3, 10);
            bool canReserveOtherShip = model.CanReserve(UnitLimitKey.For<FakeRequest>("other"), 1, 3, 10);

            Assert.That(canReserveSameShip, Is.False);
            Assert.That(canReserveOtherShip, Is.True);
            Assert.That(model.GetReservedCount(UnitLimitKey.For<FakeRequest>("ship")), Is.EqualTo(1));
            Assert.That(model.GetReservedCount(UnitLimitKey.For<FakeRequest>("other")), Is.Zero);
            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(3));
        }

        [Test]
        public void CanReserve_RejectsOptionThatExceedsRemainingCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve(UnitLimitKey.For<FakeRequest>("existing"), 2, 4, 5);

            Assert.That(model.CanReserve(UnitLimitKey.For<FakeRequest>("ship"), 2, 2, 5), Is.False);
            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(4));
        }

        [Test]
        public void ShipOrderProgress_SurvivesCombatLossButExcludesCanceledBuilds()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve(UnitLimitKey.For<FakeRequest>("ship"), 2, 3, 10);
            model.RecordShipOrder();
            model.Release(UnitLimitKey.For<FakeRequest>("ship"), 3);
            model.RecordShipOrder();
            model.CancelShipOrder();

            Assert.That(model.ShipOrdersCount, Is.EqualTo(1));

            model.Reset();

            Assert.That(model.ShipOrdersCount, Is.Zero);
        }

        [Test]
        public void DerivedRequest_UsesTheSameReservationAsItsRequestKind()
        {
            var request = new DerivedShipRequest();
            var model = new EnemyUnitLimitModel();
            model.TryReserve(UnitLimitKey.From(request), 1, 2, 10);
            Assert.That(model.GetReservedCount<ShipUnitRequest>(request.Id),
                Is.EqualTo(1));
            Assert.That(model.CanReserve<ShipUnitRequest>(request.Id, 1, 2, 10),
                Is.False);
            model.Release(UnitLimitKey.For<ShipUnitRequest>(request.Id), 2);
            Assert.That(model.GetReservedCount(UnitLimitKey.From(request)), Is.Zero);
        }

        private sealed class DerivedShipRequest : ShipUnitRequest
        {
            public DerivedShipRequest() : base(null, default) { }
        }

        // D6: the AI's purchase limits are the human ones (PlayerFactionModel.IsUnitLimitReached).
        [Test]
        public void HumanRules_AShipTypeHasNoPurchaseLimit_AHeroExistsOnce()
        {
            var model = new EnemyUnitLimitModel();
            FactionData ship = CreateData(maxCount: 1, unitCapacity: 1);
            for (int i = 0; i < ship.MaxCount + 2; i++)
                Assert.That(model.TryReserve(new ShipUnitRequest(ship, ShipType.Venator), 100), Is.True,
                    "MaxCount does not limit a ship type.");

            FactionData hero = CreateData(maxCount: 1, unitCapacity: 1, isHero: true);
            var heroRequest = new ShipUnitRequest(hero, ShipType.Recusant);
            Assert.That(model.TryReserve(heroRequest, 100), Is.True);
            Assert.That(model.TryReserve(heroRequest, 100), Is.False, "A hero exists once while queued or alive.");
            model.Release(heroRequest);
            Assert.That(model.TryReserve(heroRequest, 100), Is.True, "A destroyed hero can be bought again.");
        }

        [Test]
        public void HumanRules_StructuresStopAtMaxCountAndUseNoPopulation()
        {
            var model = new EnemyUnitLimitModel();
            FactionData platform = CreateData(maxCount: 2, unitCapacity: 5);
            var request = new DefendPlatformUnitRequest(platform, DefendPlatformType.Xq6);
            for (int i = 0; i < platform.MaxCount; i++)
                Assert.That(model.TryReserve(request, 0), Is.True);

            Assert.That(model.TryReserve(request, 100), Is.False);
            Assert.That(model.CurrentUnitCapacity, Is.Zero);
            model.Release(request);
            Assert.That(model.GetReservedCount(UnitLimitKey.From(request)), Is.EqualTo(platform.MaxCount - 1));
            Assert.That(model.CurrentUnitCapacity, Is.Zero);
        }

        [Test]
        public void HumanRules_PopulationLimitsShipsAndSquadrons()
        {
            var model = new EnemyUnitLimitModel();
            FactionData ship = CreateData(maxCount: 10, unitCapacity: 3);
            int maxCapacity = ship.UnitCapacity * 2 - 1;

            Assert.That(model.TryReserve(new ShipUnitRequest(ship, ShipType.Venator), maxCapacity), Is.True);
            Assert.That(model.TryReserve(new SquadronUnitRequest(ship, default), maxCapacity), Is.False);
            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(ship.UnitCapacity));
        }

        // User 2026-11-26: like a human, the AI buys past the fleet cap into a held reserve of one more fleet.
        [Test]
        public void PurchaseCapacityLimit_IsTheFleetCapPlusTheReserve()
        {
            const int MAX_UNIT_CAPACITY = 120;
            Assert.That(EnemyUnitLimitModel.GetPurchaseCapacityLimit(MAX_UNIT_CAPACITY),
                Is.EqualTo(MAX_UNIT_CAPACITY * (1 + EnemyUnitLimitModel.RESERVE_FLEETS)));
            Assert.That(EnemyUnitLimitModel.GetPurchaseCapacityLimit(MAX_UNIT_CAPACITY), Is.GreaterThan(MAX_UNIT_CAPACITY));
        }

        // Only units on the map count against the live cap; a held reserve unit counts only once it deploys.
        [Test]
        public void DeployedCapacity_CountsLandedUnitsOnly_AndFallsWhenTheyAreLost()
        {
            var model = new EnemyUnitLimitModel();
            FactionData ship = CreateData(maxCount: 10, unitCapacity: 3);
            var landed = new ShipUnitRequest(ship, ShipType.Venator);
            var held = new ShipUnitRequest(ship, ShipType.Venator);
            Assert.That(model.TryReserve(landed, 100), Is.True);
            Assert.That(model.TryReserve(held, 100), Is.True);

            model.MarkDeployed(landed);
            model.MarkDeployed(landed);

            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(2 * ship.UnitCapacity));
            Assert.That(model.DeployedUnitCapacity, Is.EqualTo(ship.UnitCapacity), "Marking twice counts once.");

            model.Release(held);
            Assert.That(model.DeployedUnitCapacity, Is.EqualTo(ship.UnitCapacity), "A refunded reserve unit never landed.");
            model.Release(landed);
            Assert.That(model.DeployedUnitCapacity, Is.Zero);
            Assert.That(model.CurrentUnitCapacity, Is.Zero);
        }

        // Heroes and dreadnoughts (isUnique) share the human one-at-a-time limit; a destroyed one can be rebuilt.
        [TestCase("isHero")]
        [TestCase("isUnique")]
        public void HumanRules_AUniqueShipExistsOnce_AndIsRebuiltAfterItsLoss(string uniqueFlag)
        {
            var model = new EnemyUnitLimitModel();
            FactionData data = CreateData(maxCount: 1, unitCapacity: 1);
            typeof(FactionData).GetField(uniqueFlag, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(data, true);
            var request = new ShipUnitRequest(data, ShipType.Venator);

            Assert.That(data.IsUnique, Is.True);
            Assert.That(model.TryReserve(request, 100), Is.True);
            Assert.That(model.TryReserve(request, 100), Is.False, "One exists while queued or alive.");
            model.Release(request);
            Assert.That(model.TryReserve(request, 100), Is.True, "A destroyed one can be bought again.");
        }

        private static FactionData CreateData(int maxCount, int unitCapacity, bool isHero = false)
        {
            var data = new FactionData();
            const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(FactionData).GetField("<MaxCount>k__BackingField", PRIVATE_INSTANCE).SetValue(data, maxCount);
            typeof(FactionData).GetField("<UnitCapacity>k__BackingField", PRIVATE_INSTANCE).SetValue(data, unitCapacity);
            typeof(FactionData).GetField("isHero", PRIVATE_INSTANCE).SetValue(data, isHero);
            return data;
        }

        private sealed class FakeRequest
        {
        }
    }
}
