using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.Ship.Orders;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipOrderModelTests
    {
        [Test]
        public void Replace_WipesWaypointData()
        {
            ShipOrderModel model = new ShipOrderModel();
            FormationPoint first = new FormationPoint(1f, 2f);
            FormationPoint second = new FormationPoint(3f, 4f);
            model.Replace(ShipOrderType.WaypointMove, first,
                waypoints: new[] { first, second });
            Assert.That(model.AdvanceWaypoint(out FormationPoint next), Is.True);
            Assert.That(next, Is.EqualTo(second));

            model.Replace(ShipOrderType.Retreat, first);

            Assert.That(model.Waypoints, Is.Empty);
            Assert.That(model.WaypointIndex, Is.Zero);
            Assert.That(model.Current, Is.EqualTo(ShipOrderType.Retreat));
            model.Clear();
            Assert.That(model.Current, Is.EqualTo(ShipOrderType.None));
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
    }
}
