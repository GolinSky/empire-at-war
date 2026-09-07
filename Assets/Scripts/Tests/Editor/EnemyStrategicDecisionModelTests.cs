using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyStrategicDecisionModelTests
    {
        [Test]
        public void FleetObjective_HuntsEnemyFleet()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Hard,
                5,
                3,
                true,
                true,
                false,
                2,
                0);

            EnemyStrategicDecision decision = model.Evaluate(snapshot);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
        }

        [Test]
        public void BaseObjective_AssaultsWhenDifficultyThresholdIsMet()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard,
                3,
                3,
                true,
                true,
                false,
                2,
                0);

            EnemyStrategicDecision decision = model.Evaluate(snapshot);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [Test]
        public void EasyDifficulty_CapturesZoneUntilSaferBaseThresholdIsMet()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.Easy,
                3,
                3,
                true,
                true,
                true,
                0,
                0);

            EnemyStrategicDecision decision = model.Evaluate(snapshot);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void Difficulty_ChangesCommittedFleetSize()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();

            EnemyStrategicDecision easy = model.Evaluate(new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Easy,
                10,
                1,
                false,
                false,
                false,
                0,
                0));
            EnemyStrategicDecision ultra = model.Evaluate(new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.UltraHard,
                10,
                1,
                false,
                false,
                false,
                0,
                0));

            Assert.That(easy.CommittedShipCount, Is.EqualTo(5));
            Assert.That(ultra.CommittedShipCount, Is.EqualTo(10));
        }

        [TestCase(EnemyAiDifficulty.Easy, 5)]
        [TestCase(EnemyAiDifficulty.Medium, 7)]
        [TestCase(EnemyAiDifficulty.Hard, 8)]
        [TestCase(EnemyAiDifficulty.UltraHard, 10)]
        public void Difficulty_CommitsExpectedFleetShare(
            EnemyAiDifficulty difficulty,
            int expectedCommittedShips)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                new EnemyStrategicSnapshot(
                    BattleVictoryCondition.DestroyEnemyFleet,
                    difficulty,
                    ownShipCount: 10,
                    enemyShipCount: 1,
                    hasCaptureTarget: false,
                    hasEnemyBaseTarget: false,
                    hasOwnBase: false,
                    ownedCapturableZoneCount: 0,
                    enemyShipsNearOwnBase: 0));

            Assert.That(decision.CommittedShipCount, Is.EqualTo(expectedCommittedShips));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void AvailableZone_BelowDifficultyControlFloor_Captures(
            EnemyAiDifficulty difficulty)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(
                    BattleVictoryCondition.DestroyEnemyFleet,
                    difficulty,
                    ownShipCount: 10,
                    enemyShipCount: 4,
                    ownedCapturableZoneCount: 0,
                    enemyShipsNearOwnBase: 0));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [TestCase(EnemyAiDifficulty.Easy, 3)]
        [TestCase(EnemyAiDifficulty.Medium, 4)]
        [TestCase(EnemyAiDifficulty.Hard, 5)]
        [TestCase(EnemyAiDifficulty.UltraHard, 6)]
        public void ActualBaseThreat_PreemptsMapControl(
            EnemyAiDifficulty difficulty,
            int nearbyThreatCount)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(
                    BattleVictoryCondition.DestroyEnemyFleet,
                    difficulty,
                    ownShipCount: 4,
                    enemyShipCount: nearbyThreatCount,
                    ownedCapturableZoneCount: 0,
                    enemyShipsNearOwnBase: nearbyThreatCount));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.DefendBase));
        }

        [TestCase(EnemyAiDifficulty.Easy, 7)]
        [TestCase(EnemyAiDifficulty.Medium, 5)]
        [TestCase(EnemyAiDifficulty.Hard, 4)]
        [TestCase(EnemyAiDifficulty.UltraHard, 3)]
        public void MapControlFloorMet_BaseObjectiveAssaultsAtDifficultyThreshold(
            EnemyAiDifficulty difficulty,
            int ownShipCount)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(
                    BattleVictoryCondition.DestroyOpponentBase,
                    difficulty,
                    ownShipCount,
                    enemyShipCount: 4,
                    ownedCapturableZoneCount: profile.MinimumControlledZones,
                    enemyShipsNearOwnBase: 0));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void MapControlFloorMet_FleetObjectiveHuntsEnemy(
            EnemyAiDifficulty difficulty)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(
                    BattleVictoryCondition.DestroyEnemyFleet,
                    difficulty,
                    ownShipCount: 10,
                    enemyShipCount: 1,
                    ownedCapturableZoneCount: profile.MinimumControlledZones,
                    enemyShipsNearOwnBase: 0));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
        }

        [TestCase(EnemyAiDifficulty.Easy, 4f, 0.5f, 1, 1, 0.75f)]
        [TestCase(EnemyAiDifficulty.Medium, 2.5f, 0.65f, 1, 1, 1f)]
        [TestCase(EnemyAiDifficulty.Hard, 1.25f, 0.8f, 2, 2, 1.25f)]
        [TestCase(EnemyAiDifficulty.UltraHard, 0.5f, 1f, 3, 2, 1.5f)]
        public void DifficultyProfile_UsesExpectedStrategicConfiguration(
            EnemyAiDifficulty difficulty,
            float decisionInterval,
            float committedFleetRatio,
            int minimumMiningFacilities,
            int minimumControlledZones,
            float defenseThreatRatio)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);

            Assert.That(profile.DecisionInterval, Is.EqualTo(decisionInterval));
            Assert.That(profile.CommittedFleetRatio, Is.EqualTo(committedFleetRatio));
            Assert.That(profile.MinimumMiningFacilities, Is.EqualTo(minimumMiningFacilities));
            Assert.That(profile.MinimumControlledZones, Is.EqualTo(minimumControlledZones));
            Assert.That(profile.DefenseThreatRatio, Is.EqualTo(defenseThreatRatio));
            Assert.That(EnemyAiDifficultyProfile.Get(difficulty), Is.SameAs(profile));
        }

        private static EnemyStrategicSnapshot CreateSnapshot(
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty difficulty,
            int ownShipCount,
            int enemyShipCount,
            int ownedCapturableZoneCount,
            int enemyShipsNearOwnBase)
        {
            return new EnemyStrategicSnapshot(
                victoryCondition,
                difficulty,
                ownShipCount,
                enemyShipCount,
                hasCaptureTarget: true,
                hasEnemyBaseTarget: true,
                hasOwnBase: true,
                ownedCapturableZoneCount: ownedCapturableZoneCount,
                enemyShipsNearOwnBase: enemyShipsNearOwnBase);
        }
    }
}
