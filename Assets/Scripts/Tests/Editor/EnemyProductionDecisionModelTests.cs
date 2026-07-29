using EmpireAtWar.Entities.EnemyFaction.Models;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyProductionDecisionModelTests
    {
        [Test]
        public void UltraHard_BuildsMiningBeforeMoreShips()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.HuntFleet,
                EnemyAiDifficulty.UltraHard,
                0,
                true,
                true,
                true,
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Mining));
        }

        [Test]
        public void UltraHard_SavesForMiningInsteadOfBuyingAffordableShip()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.HuntFleet,
                EnemyAiDifficulty.UltraHard,
                1,
                true,
                true,
                false,
                true,
                false);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.None));
        }

        [Test]
        public void HardDefenseState_PrioritizesDefenseAfterEconomicFloor()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.DefendBase,
                EnemyAiDifficulty.Hard,
                2,
                true,
                true,
                true,
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Defense));
        }

        [Test]
        public void EasyDefenseState_PrioritizesShipsAfterEconomicFloor()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.DefendBase,
                EnemyAiDifficulty.Easy,
                1,
                true,
                true,
                true,
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Ship));
        }

        [Test]
        public void HardHoldState_PrioritizesTechnologyAfterEconomicFloor()
        {
            EnemyProductionCategory result = Evaluate(
                EnemyStrategicState.Hold,
                EnemyAiDifficulty.UltraHard,
                3,
                true,
                true,
                true,
                true,
                true);

            Assert.That(result, Is.EqualTo(EnemyProductionCategory.Level));
        }

        private static EnemyProductionCategory Evaluate(
            EnemyStrategicState state,
            EnemyAiDifficulty difficulty,
            int miningFacilityCount,
            bool hasMiningOption,
            bool canBuildShip,
            bool canBuildMining,
            bool canBuildDefense,
            bool canLevelUp)
        {
            return new EnemyProductionDecisionModel().Evaluate(
                new EnemyProductionSnapshot(
                    state,
                    difficulty,
                    miningFacilityCount,
                    hasMiningOption,
                    canBuildShip,
                    canBuildMining,
                    canBuildDefense,
                    canLevelUp));
        }
    }
}
