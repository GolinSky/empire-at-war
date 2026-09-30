using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
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
            Assert.That(model.GetReservedCount<EmpireAtWar.Controllers.Factions.ShipUnitRequest>(request.Id),
                Is.EqualTo(1));
            Assert.That(model.CanReserve<EmpireAtWar.Controllers.Factions.ShipUnitRequest>(request.Id, 1, 2, 10),
                Is.False);
            model.Release(UnitLimitKey.For<EmpireAtWar.Controllers.Factions.ShipUnitRequest>(request.Id), 2);
            Assert.That(model.GetReservedCount(UnitLimitKey.From(request)), Is.Zero);
        }

        private sealed class DerivedShipRequest : EmpireAtWar.Controllers.Factions.ShipUnitRequest
        {
            public DerivedShipRequest() : base(null, default) { }
        }

        private sealed class FakeRequest
        {
        }
    }
}
