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
                isDestroyed: false,
                hasShields: true,
                shieldPercentage: 0.2f,
                nearbyEnemyCount: 1,
                hasAssignedTarget: true,
                isAssignedTargetAvailable: true,
                isMoving: false);

            Assert.That(model.Evaluate(snapshot, EnemyAiDifficulty.Easy), Is.EqualTo(ShipAiDecision.Flee));
            Assert.That(model.Evaluate(snapshot, EnemyAiDifficulty.Hard), Is.EqualTo(ShipAiDecision.Attack));
        }

        [Test]
        public void MovingWithoutTarget_RemainsInNavigation()
        {
            ShipAiDecisionModel model = new ShipAiDecisionModel();
            ShipAiSnapshot snapshot = new ShipAiSnapshot(
                isDestroyed: false,
                hasShields: false,
                shieldPercentage: 0f,
                nearbyEnemyCount: 0,
                hasAssignedTarget: false,
                isAssignedTargetAvailable: false,
                isMoving: true);

            Assert.That(
                model.Evaluate(snapshot, EnemyAiDifficulty.Medium),
                Is.EqualTo(ShipAiDecision.Navigate));
        }
    }
}
