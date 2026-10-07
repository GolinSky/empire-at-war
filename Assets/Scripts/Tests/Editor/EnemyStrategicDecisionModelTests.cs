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
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Hard,
                    ownShipCount: 5, enemyShipCount: 3, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 1.7f, hasOwnBase: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
        }

        [Test]
        public void ThreatenedCaptureSite_IsDefendedBeforeHuntingFleet()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Hard,
                    ownShipCount: 5, enemyShipCount: 3, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 1.7f, hasOwnBase: false, hasThreatenedSite: true));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void BaseObjective_AssaultsWhenDifficultyThresholdIsMet()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 3, enemyShipCount: 3, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 1f, hasOwnBase: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [Test]
        public void EasyDifficulty_CapturesZoneUntilSaferBaseThresholdIsMet()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.Easy,
                    ownShipCount: 3, enemyShipCount: 3, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 1f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void NoShips_RebuildsFleet()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 0, enemyShipCount: 4, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 0.01f, baseThreatRatio: 5f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.RebuildFleet));
            Assert.That(decision.CommittedShipCount, Is.EqualTo(0));
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
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 10, enemyShipCount: 1, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 10f, hasCaptureTarget: false, hasOwnBase: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
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
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 10, enemyShipCount: 4, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 2.5f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void BaseThreatAtDifficultyRatio_PreemptsMapControl(EnemyAiDifficulty difficulty)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 4, enemyShipCount: 4, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 1f, baseThreatRatio: profile.DefenseThreatRatio));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.DefendBase));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void WeakBaseThreat_DoesNotPullFleetHome(EnemyAiDifficulty difficulty)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 4, enemyShipCount: 4, ownedCapturableZoneCount: profile.MinimumControlledZones,
                    fleetAdvantage: 3f, baseThreatRatio: profile.DefenseThreatRatio * 0.5f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
        }

        [Test]
        public void OutmatchedFleet_RetreatsToOwnBase()
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 6, enemyShipCount: 2, ownedCapturableZoneCount: profile.MinimumControlledZones,
                    fleetAdvantage: profile.RetreatAdvantage));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.RetreatValue));
            Assert.That(decision.CommittedShipCount, Is.EqualTo(6));
        }

        [Test]
        public void UnfavorableMatchup_TakesMapControlInsteadOfHunting()
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 6, enemyShipCount: 3, ownedCapturableZoneCount: profile.MinimumControlledZones,
                    fleetAdvantage: 0.85f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void NearEqualOptions_KeepCurrentState()
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium);
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                ownShipCount: 6, enemyShipCount: 3, ownedCapturableZoneCount: profile.MinimumControlledZones,
                fleetAdvantage: 2f));
            EnemyStrategicSnapshot slightlyWorse = CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Medium, ownShipCount: 6, enemyShipCount: 3,
                ownedCapturableZoneCount: profile.MinimumControlledZones, fleetAdvantage: 0.94f);

            Assert.That(model.Evaluate(slightlyWorse).State, Is.EqualTo(EnemyStrategicState.HuntFleet));
            Assert.That(new EnemyStrategicDecisionModel().Evaluate(slightlyWorse).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void MapControlFloorMet_BaseObjectiveAssaultsAtDifficultyThreshold(EnemyAiDifficulty difficulty)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, difficulty,
                    ownShipCount: 6, enemyShipCount: 4, ownedCapturableZoneCount: profile.MinimumControlledZones,
                    fleetAdvantage: profile.RequiredAttackRatio));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void MapControlFloorMet_FleetObjectiveHuntsEnemy(EnemyAiDifficulty difficulty)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 10, enemyShipCount: 1, ownedCapturableZoneCount: profile.MinimumControlledZones,
                    fleetAdvantage: 10f));

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
            Assert.That(profile.RetreatAdvantage, Is.LessThan(profile.HuntAdvantage));
            Assert.That(EnemyAiDifficultyProfile.Get(difficulty), Is.SameAs(profile));
        }

        private static EnemyStrategicSnapshot CreateSnapshot(
            BattleVictoryCondition victoryCondition,
            EnemyAiDifficulty difficulty,
            int ownShipCount,
            int enemyShipCount,
            int ownedCapturableZoneCount,
            float fleetAdvantage,
            float baseThreatRatio = 0f,
            bool hasCaptureTarget = true,
            bool hasOwnBase = true,
            bool hasThreatenedSite = false)
        {
            return new EnemyStrategicSnapshot(
                victoryCondition: victoryCondition,
                difficulty: difficulty,
                ownShipCount: ownShipCount,
                enemyShipCount: enemyShipCount,
                ownedCapturableZoneCount: ownedCapturableZoneCount,
                fleetAdvantage: fleetAdvantage,
                baseThreatRatio: baseThreatRatio,
                hasCaptureTarget: hasCaptureTarget,
                hasEnemyBaseTarget: true,
                hasOwnBase: hasOwnBase,
                hasThreatenedSite: hasThreatenedSite);
        }
    }
}
