using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Game;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
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
                    fleetAdvantage: EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).RequiredAttackRatio,
                    hasOwnBase: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [TestCase(BattleVictoryCondition.DestroyOpponentBase, EnemyStrategicState.AssaultBase)]
        [TestCase(BattleVictoryCondition.DestroyEnemyFleet, EnemyStrategicState.HuntFleet)]
        public void ClearAdvantage_AttacksInsteadOfCapturing(
            BattleVictoryCondition victoryCondition, EnemyStrategicState expected)
        {
            float pressAdvantage = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).RequiredAttackRatio *
                                   EnemyStrategicDecisionModel.PRESS_ADVANTAGE_SCALE;
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(victoryCondition, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 12, enemyShipCount: 2, ownedCapturableZoneCount: 0,
                    fleetAdvantage: pressAdvantage, hasThreatenedSite: true));

            Assert.That(decision.State, Is.EqualTo(expected));
        }

        [Test]
        public void ModestAdvantage_KeepsCapturingTheMapControlFloor()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 8, enemyShipCount: 6, ownedCapturableZoneCount: 0, fleetAdvantage: 1.3f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void AfterRetreat_StaysWithdrawnUntilRegroupedAndEvenAgain()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicDecision Evaluate(float fleetAdvantage, bool hasLocalThreat, float localAdvantage,
                bool isRegrouped) => model.Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 10, enemyShipCount: 12, ownedCapturableZoneCount: 1,
                    fleetAdvantage: fleetAdvantage, hasThreatenedSite: true,
                    hasLocalThreat: hasLocalThreat, localAdvantage: localAdvantage, isRegrouped: isRegrouped));

            Assert.That(Evaluate(0.7f, true, 0.4f, true).State, Is.EqualTo(EnemyStrategicState.RetreatValue));
            // The superior force left the local radius, but the fleet is still weaker overall.
            Assert.That(Evaluate(0.7f, false, CombatMatchup.MAX_ADVANTAGE, true).State,
                Is.EqualTo(EnemyStrategicState.RetreatValue));
            // Strong enough again, but ships are still scattered.
            Assert.That(Evaluate(1.2f, false, CombatMatchup.MAX_ADVANTAGE, false).State,
                Is.EqualTo(EnemyStrategicState.RetreatValue));
            Assert.That(Evaluate(1.2f, false, CombatMatchup.MAX_ADVANTAGE, true).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void Recovering_StillDefendsTheBase()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard);
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                ownShipCount: 10, enemyShipCount: 12, ownedCapturableZoneCount: 1, fleetAdvantage: 0.7f,
                hasLocalThreat: true, localAdvantage: 0.4f));

            EnemyStrategicDecision decision = model.Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 10, enemyShipCount: 12, ownedCapturableZoneCount: 1, fleetAdvantage: 0.7f,
                    baseThreatRatio: profile.DefenseThreatRatio * 2f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.DefendBase));
        }

        [TestCase(BattleVictoryCondition.DestroyEnemyFleet)]
        [TestCase(BattleVictoryCondition.DestroyOpponentBase)]
        public void UltraHard_NeitherAssaultsNorHuntsWhenOutmatched(BattleVictoryCondition victoryCondition)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(victoryCondition, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 10, enemyShipCount: 12, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.7f, hasCaptureTarget: false, hasOwnBase: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.Hold));
        }

        [TestCase(BattleVictoryCondition.DestroyEnemyFleet)]
        [TestCase(BattleVictoryCondition.DestroyOpponentBase)]
        public void NoEnemyShipsAndNothingToCapture_AssaultsBase(BattleVictoryCondition victoryCondition)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(victoryCondition, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 18, enemyShipCount: 0, ownedCapturableZoneCount: 1,
                    fleetAdvantage: 7.67f, hasCaptureTarget: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [Test]
        public void OutmatchedOverall_EngagesWeakerHostilesNearFleet()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 115, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.52f, hasCaptureTarget: false,
                    hasLocalThreat: true, localAdvantage: 1.5f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(decision.CommittedShipCount, Is.EqualTo(20));
        }

        [Test]
        public void OutmatchedNearFleet_RetreatsInsteadOfEngaging()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 115, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.52f, hasCaptureTarget: false,
                    hasLocalThreat: true, localAdvantage: 0.3f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.RetreatValue));
        }

        [Test]
        public void OutmatchedWithNothingToCapture_RaidsUndefendedStructure()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 115, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.52f, hasCaptureTarget: false, hasRaidTarget: true));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.RaidStructures));
        }

        [Test]
        public void OutmatchedWithNoTargetAtAll_Holds()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 115, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.52f, hasCaptureTarget: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.Hold));
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
            // Ahead, but not by enough to press the attack (see ClearAdvantage_AttacksInsteadOfCapturing).
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 10, enemyShipCount: 4, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 1.2f));

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
                    fleetAdvantage: 1f, localAdvantage: profile.RetreatAdvantage));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.RetreatValue));
            Assert.That(decision.CommittedShipCount, Is.EqualTo(6));
        }

        [Test]
        public void OutmatchedArmyFarAway_KeepsCapturingInsteadOfRetreating()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 3, enemyShipCount: 15, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.05f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void EvenTeams_ExpandBeforeHunting()
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 5, enemyShipCount: 5, ownedCapturableZoneCount: profile.MinimumControlledZones,
                    fleetAdvantage: 1.05f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void NothingLeftToCapture_HuntsWhenMatchupAllows()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 5, enemyShipCount: 5, ownedCapturableZoneCount: 4,
                    fleetAdvantage: 1.05f, hasCaptureTarget: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
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
                ownedCapturableZoneCount: profile.MinimumControlledZones, fleetAdvantage: 1.2f);

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
            bool hasThreatenedSite = false,
            float localAdvantage = CombatMatchup.MAX_ADVANTAGE,
            bool hasLocalThreat = false,
            bool hasRaidTarget = false,
            bool isRegrouped = true)
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
                hasThreatenedSite: hasThreatenedSite,
                localAdvantage: localAdvantage,
                hasLocalThreat: hasLocalThreat,
                hasRaidTarget: hasRaidTarget,
                isRegrouped: isRegrouped);
        }
    }
}
