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
    }
}
