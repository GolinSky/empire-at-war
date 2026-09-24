using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.Ship.Orders;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipOrderModelTests
    {
        [Test]
        public void Replace_WipesWaypointAndRetreatData()
        {
            ShipOrderModel model = new ShipOrderModel();
            FormationPoint first = new FormationPoint(1f, 2f);
            FormationPoint second = new FormationPoint(3f, 4f);
            model.Replace(ShipOrderType.WaypointMove, first,
                waypoints: new[] { first, second });
            Assert.That(model.AdvanceWaypoint(out FormationPoint next), Is.True);
            Assert.That(next, Is.EqualTo(second));

            model.Replace(ShipOrderType.Retreat, first, retreatDelay: 5f);

            Assert.That(model.Waypoints, Is.Empty);
            Assert.That(model.WaypointIndex, Is.Zero);
            Assert.That(model.RetreatRemaining, Is.EqualTo(5f));
            Assert.That(model.AdvanceRetreat(2f), Is.False);
            Assert.That(model.RetreatRemaining, Is.EqualTo(3f));
            model.Clear();
            Assert.That(model.Current, Is.EqualTo(ShipOrderType.None));
            Assert.That(model.RetreatRemaining, Is.Zero);
        }

        [Test]
        public void Matches_UsesTypeAndDestinationTolerance()
        {
            ShipOrderModel model = new ShipOrderModel();
            model.Replace(ShipOrderType.Move, new FormationPoint(20f, 30f));

            Assert.That(model.Matches(ShipOrderType.Move,
                new FormationPoint(20.05f, 30f)), Is.True);
            Assert.That(model.Matches(ShipOrderType.AttackMove,
                new FormationPoint(20f, 30f)), Is.False);
            Assert.That(model.Matches(ShipOrderType.Move,
                new FormationPoint(21f, 30f)), Is.False);
        }

        [Test]
        public void RetreatCountdown_StartsOnceAndClearCancelsIt()
        {
            ShipOrderModel model = new ShipOrderModel();
            model.Replace(ShipOrderType.Retreat, new FormationPoint(5f, 5f),
                retreatDelay: 3f);
            Assert.That(model.AdvanceRetreat(1f), Is.False);
            Assert.That(model.Matches(ShipOrderType.Retreat,
                new FormationPoint(5f, 5f)), Is.True);
            Assert.That(model.RetreatRemaining, Is.EqualTo(2f));
            Assert.That(model.AdvanceRetreat(2f), Is.True);
            Assert.That(model.RetreatStarted, Is.True);
            model.Clear();
            Assert.That(model.AdvanceRetreat(10f), Is.False);
        }
    }
}
