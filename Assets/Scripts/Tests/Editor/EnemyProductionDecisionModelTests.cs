using EmpireAtWar.Entities.EnemyFaction.Models;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyProductionDecisionModelTests
    {
        [Test]
        public void HardDefenseState_PrioritizesDefense()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();

            EnemyProductionCategory result = model.Evaluate(new EnemyProductionSnapshot(
                EnemyStrategicState.DefendBase,
                EnemyAiDifficulty.Hard,
                true,
                true,
                true,
                true));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Defense));
        }

        [Test]
        public void EasyDefenseState_PrioritizesShips()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();

            EnemyProductionCategory result = model.Evaluate(new EnemyProductionSnapshot(
                EnemyStrategicState.DefendBase,
                EnemyAiDifficulty.Easy,
                true,
                true,
                true,
                true));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [Test]
        public void HardHoldState_PrioritizesTechnology()
        {
            EnemyProductionDecisionModel model = new EnemyProductionDecisionModel();

            EnemyProductionCategory result = model.Evaluate(new EnemyProductionSnapshot(
                EnemyStrategicState.Hold,
                EnemyAiDifficulty.UltraHard,
                true,
                true,
                true,
                true));

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Level));
        }
    }
}
