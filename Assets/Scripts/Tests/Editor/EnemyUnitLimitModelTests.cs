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

            Assert.That(model.TryReserve("ship", 2, 1, 10), Is.True);
            Assert.That(model.TryReserve("ship", 2, 1, 10), Is.True);
            Assert.That(model.TryReserve("ship", 2, 1, 10), Is.False);
        }

        [Test]
        public void TryReserve_CountsQueuedCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();

            Assert.That(model.TryReserve("ship-a", 10, 3, 5), Is.True);
            Assert.That(model.TryReserve("ship-b", 10, 3, 5), Is.False);
            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(3));
        }

        [Test]
        public void Release_FreesCountAndCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve("ship", 1, 3, 3);

            model.Release("ship", 3);

            Assert.That(model.CurrentUnitCapacity, Is.Zero);
            Assert.That(model.GetReservedCount("ship"), Is.Zero);
            Assert.That(model.TryReserve("ship", 1, 3, 3), Is.True);
        }

        [Test]
        public void Release_IncrementsVersionOnlyWhenAReservationIsReleased()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve("ship", 2, 1, 3);
            model.TryReserve("ship", 2, 1, 3);

            model.Release("missing", 3);

            Assert.That(model.ReleaseVersion, Is.Zero);

            model.Release("ship", 1);

            Assert.That(model.ReleaseVersion, Is.EqualTo(1));

            model.Release("ship", 1);

            Assert.That(model.ReleaseVersion, Is.EqualTo(2));

            model.Reset();

            Assert.That(model.ReleaseVersion, Is.Zero);
        }

        [Test]
        public void GetReservedCount_WithRequestTypeUsesControllerIdentifier()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            string unitId = $"{typeof(FakeRequest).FullName}:mine";
            model.TryReserve(unitId, 2, 1, 10);

            Assert.That(
                model.GetReservedCount<FakeRequest>("mine"),
                Is.EqualTo(1));
        }

        [Test]
        public void CanReserve_ReportsLimitWithoutChangingCountOrCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve("ship", 1, 3, 10);

            bool canReserveSameShip = model.CanReserve("ship", 1, 3, 10);
            bool canReserveOtherShip = model.CanReserve("other", 1, 3, 10);

            Assert.That(canReserveSameShip, Is.False);
            Assert.That(canReserveOtherShip, Is.True);
            Assert.That(model.GetReservedCount("ship"), Is.EqualTo(1));
            Assert.That(model.GetReservedCount("other"), Is.Zero);
            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(3));
        }

        [Test]
        public void CanReserve_RejectsOptionThatExceedsRemainingCapacity()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve("existing", 2, 4, 5);

            Assert.That(model.CanReserve("ship", 2, 2, 5), Is.False);
            Assert.That(model.CurrentUnitCapacity, Is.EqualTo(4));
        }

        [Test]
        public void ShipOrderProgress_SurvivesCombatLossButExcludesCanceledBuilds()
        {
            EnemyUnitLimitModel model = new EnemyUnitLimitModel();
            model.TryReserve("ship", 2, 3, 10);
            model.RecordShipOrder();
            model.Release("ship", 3);
            model.RecordShipOrder();
            model.CancelShipOrder();

            Assert.That(model.ShipOrdersCount, Is.EqualTo(1));

            model.Reset();

            Assert.That(model.ShipOrdersCount, Is.Zero);
        }

        private sealed class FakeRequest
        {
        }
    }
}
