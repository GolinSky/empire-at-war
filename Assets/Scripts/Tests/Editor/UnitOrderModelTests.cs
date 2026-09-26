using EmpireAtWar.Components.Movement.Formation;
using NUnit.Framework;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitOrderModelTests
    {
        [Test]
        public void Replace_WipesWaypointData()
        {
            UnitOrderModel model = new UnitOrderModel();
            FormationPoint first = new FormationPoint(1f, 2f);
            FormationPoint second = new FormationPoint(3f, 4f);
            model.Replace(UnitOrderType.WaypointMove, first,
                waypoints: new[] { first, second });
            Assert.That(model.AdvanceWaypoint(out FormationPoint next), Is.True);
            Assert.That(next, Is.EqualTo(second));

            model.Replace(UnitOrderType.Retreat, first);

            Assert.That(model.Waypoints, Is.Empty);
            Assert.That(model.WaypointIndex, Is.Zero);
            Assert.That(model.Current, Is.EqualTo(UnitOrderType.Retreat));
            model.Clear();
            Assert.That(model.Current, Is.EqualTo(UnitOrderType.None));
        }

        [Test]
        public void Matches_UsesTypeAndDestinationTolerance()
        {
            UnitOrderModel model = new UnitOrderModel();
            model.Replace(UnitOrderType.Move, new FormationPoint(20f, 30f));

            Assert.That(model.Matches(UnitOrderType.Move,
                new FormationPoint(20.05f, 30f)), Is.True);
            Assert.That(model.Matches(UnitOrderType.AttackMove,
                new FormationPoint(20f, 30f)), Is.False);
            Assert.That(model.Matches(UnitOrderType.Move,
                new FormationPoint(21f, 30f)), Is.False);
        }
    }
}
