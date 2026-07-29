using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Ship.Mediator;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipAiDecisionModelTests
    {
        [Test]
        public void EasyDifficulty_RetreatsAtShieldLevelHardAccepts()
        {
            ShipAiDecisionModel model = new ShipAiDecisionModel();
            ShipAiSnapshot snapshot = new ShipAiSnapshot(
                false,
                true,
                0.2f,
                1,
                true,
                true,
                false);

            Assert.That(model.Evaluate(snapshot, EnemyAiDifficulty.Easy), Is.EqualTo(ShipAiDecision.Flee));
            Assert.That(model.Evaluate(snapshot, EnemyAiDifficulty.Hard), Is.EqualTo(ShipAiDecision.Attack));
        }

        [Test]
        public void MovingWithoutTarget_RemainsInNavigation()
        {
            ShipAiDecisionModel model = new ShipAiDecisionModel();
            ShipAiSnapshot snapshot = new ShipAiSnapshot(
                false,
                false,
                0f,
                0,
                false,
                false,
                true);

            Assert.That(
                model.Evaluate(snapshot, EnemyAiDifficulty.Medium),
                Is.EqualTo(ShipAiDecision.Navigate));
        }
    }
}
