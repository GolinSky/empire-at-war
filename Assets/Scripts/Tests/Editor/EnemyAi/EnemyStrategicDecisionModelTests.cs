using System;
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
                    fleetAdvantage: 1.7f, hasOwnBase: false, hasCaptureTarget: false));

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
                    hasOwnBase: false, hasCaptureTarget: false));

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
        public void ModestAdvantage_KeepsExpanding()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 8, enemyShipCount: 6, ownedCapturableZoneCount: 0, fleetAdvantage: 1.3f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void AfterLocalFight_ScatteredFleetCanCaptureWithoutGlobalParity()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                ownShipCount: 10, enemyShipCount: 12, ownedCapturableZoneCount: 0,
                fleetAdvantage: 0.16f, hasLocalThreat: true, localAdvantage: 0.4f));

            EnemyStrategicDecision scattered = model.Evaluate(CreateSnapshot(
                BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                ownShipCount: 10, enemyShipCount: 12, ownedCapturableZoneCount: 0,
                fleetAdvantage: 0.16f, hasLocalThreat: true, localAdvantage: 14.39f, isRegrouped: false));
            Assert.That(scattered.State, Is.EqualTo(EnemyStrategicState.CaptureZone));

            EnemyStrategicDecision regrouped = model.Evaluate(CreateSnapshot(
                BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                ownShipCount: 10, enemyShipCount: 12, ownedCapturableZoneCount: 0,
                fleetAdvantage: 0.16f, hasLocalThreat: true, localAdvantage: 14.39f, isRegrouped: true));
            Assert.That(regrouped.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void Engagement_ContinuesWithoutGlobalParity()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 3, enemyShipCount: 20, ownedCapturableZoneCount: 1,
                fleetAdvantage: 0.16f, hasCaptureTarget: false, hasLocalThreat: true, localAdvantage: 0.4f));

            EnemyStrategicDecision decision = model.Evaluate(CreateSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 3, enemyShipCount: 20, ownedCapturableZoneCount: 1,
                fleetAdvantage: 0.16f, hasCaptureTarget: false, hasLocalThreat: true,
                localAdvantage: 14.39f, isRegrouped: true));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.EngageLocal));
        }

        [TestCase(true, EnemyStrategicState.CaptureZone)]
        [TestCase(false, EnemyStrategicState.EngageLocal)]
        public void AfterStationLoss_ScatteredFleetCanTakeAViableLocalObjective(
            bool hasCaptureTarget, EnemyStrategicState expected)
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 3, enemyShipCount: 20, ownedCapturableZoneCount: 0,
                fleetAdvantage: 0.16f, hasLocalThreat: true, localAdvantage: 0.4f));

            EnemyStrategicDecision decision = model.Evaluate(CreateSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 3, enemyShipCount: 20, ownedCapturableZoneCount: 0,
                fleetAdvantage: 0.16f, hasCaptureTarget: hasCaptureTarget, hasOwnBase: false,
                hasSafeRetreat: false, hasLocalThreat: true, localAdvantage: 14.39f,
                isRegrouped: false));

            Assert.That(decision.State, Is.EqualTo(expected));
        }

        [Test]
        public void AfterStationLoss_NoTargetOrSiteToGuardHolds()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 3, enemyShipCount: 20, ownedCapturableZoneCount: 1,
                fleetAdvantage: 0.16f, hasLocalThreat: true, localAdvantage: 0.4f));

            EnemyStrategicDecision decision = model.Evaluate(CreateSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 3, enemyShipCount: 20, ownedCapturableZoneCount: 1,
                fleetAdvantage: 0.16f, hasCaptureTarget: false, hasOwnBase: false, hasSafeRetreat: false,
                isRegrouped: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.Hold));
        }

        [Test]
        public void CorneredWithoutSafeRetreat_EngagesLocalThreatInsteadOfHolding()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 7, enemyShipCount: 20, ownedCapturableZoneCount: 1,
                fleetAdvantage: 0.16f, hasLocalThreat: true, localAdvantage: 0.4f));

            EnemyStrategicDecision decision = model.Evaluate(CreateSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                ownShipCount: 7, enemyShipCount: 20, ownedCapturableZoneCount: 1,
                fleetAdvantage: 0.16f, hasCaptureTarget: false, hasOwnBase: true, hasSafeRetreat: false,
                hasLocalThreat: true, localAdvantage: 0.4f, isRegrouped: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.EngageLocal));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void ActualStationPressure_TriggersDefenseDespiteLowThreatRatio(EnemyAiDifficulty difficulty)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 10, enemyShipCount: 1, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 10f, baseThreatRatio: 0.12f, hasStationPressure: true));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.DefendBase));
            Assert.That(decision.CommittedShipCount, Is.GreaterThan(0));
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
            // A winnable local fight commits the difficulty's share; the rest stays as reserve.
            float share = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).CommittedFleetRatio;
            Assert.That(decision.CommittedShipCount, Is.EqualTo((int)Math.Ceiling(20 * (decimal)share)));
            Assert.That(decision.CommittedShipCount, Is.InRange(1, 20));
        }

        [Test]
        public void OutmatchedAwayFromHeldGround_PullsBackToTheGatheringPoint()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                DisengageSnapshot(localAdvantage: 0.3f, canDisengage: true), now: 0f);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.RetreatValue));
            Assert.That(decision.CommittedShipCount, Is.EqualTo(5));
        }

        [Test]
        public void OutmatchedWithoutAGatheringPoint_StillFightsInPlace()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                DisengageSnapshot(localAdvantage: 0.3f, canDisengage: false), now: 0f);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.EngageLocal));
        }

        [Test]
        public void PullBack_HoldsAgainstAnImmediateReturnToTheFight()
        {
            var model = new EnemyStrategicDecisionModel();
            model.Evaluate(DisengageSnapshot(localAdvantage: 0.3f, canDisengage: true), now: 0f);

            Assert.That(model.Evaluate(DisengageSnapshot(localAdvantage: 1.5f, canDisengage: true), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.RetreatValue));
            Assert.That(model.Evaluate(DisengageSnapshot(localAdvantage: 1.5f, canDisengage: true),
                now: EnemyStrategicDecisionModel.MIN_STATE_SECONDS).State, Is.EqualTo(EnemyStrategicState.EngageLocal));
        }

        private static EnemyStrategicSnapshot DisengageSnapshot(float localAdvantage, bool canDisengage) =>
            new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyOpponentBase, EnemyAiDifficulty.UltraHard,
                ownShipCount: 5, enemyShipCount: 12, ownedCapturableZoneCount: 0, fleetAdvantage: 0.4f,
                baseThreatRatio: 0f, hasCaptureTarget: false, hasEnemyBaseTarget: true, hasOwnBase: true,
                localAdvantage: localAdvantage, hasLocalThreat: true, canDisengage: canDisengage);

        [Test]
        public void OutmatchedNearFleet_CommitsEveryoneToTheFight()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 115, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.52f, hasCaptureTarget: false,
                    hasLocalThreat: true, localAdvantage: 0.3f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(decision.CommittedShipCount, Is.EqualTo(20));
        }

        [TestCase(1.2f, EnemyStrategicState.GuardSite)]
        [TestCase(3f, EnemyStrategicState.AssaultBase)]
        public void CentreGuard_YieldsOnlyToDecisiveAssault(float fleetAdvantage, EnemyStrategicState expected)
        {
            var snapshot = new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 8, 5, 2, fleetAdvantage, 0f, false, true, true,
                hasRaidTarget: true, hasEconomyRaidTarget: true, hasGuardSite: true, isCentreGuardGoal: true, canAssaultBase: true);

            Assert.That(new EnemyStrategicDecisionModel().Evaluate(snapshot).State, Is.EqualTo(expected));
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
        [TestCase(EnemyAiDifficulty.UltraHard, 9)]
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

        [Test]
        public void AvailableObjective_CapturesRegardlessOfOwnedZoneCount(
            [Values] EnemyAiDifficulty difficulty, [Values(0, 1, 2, 8)] int ownedZoneCount)
        {
            // Ahead, but not by enough to press the attack (see ClearAdvantage_AttacksInsteadOfCapturing).
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 10, enemyShipCount: 4, ownedCapturableZoneCount: ownedZoneCount,
                    fleetAdvantage: EnemyAiDifficultyProfile.Get(difficulty).RequiredAttackRatio));

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
                    ownShipCount: 4, enemyShipCount: 4, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 4f, baseThreatRatio: profile.DefenseThreatRatio * 0.5f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
        }

        [Test]
        public void OutmatchedFleet_FightsEvenWithSafeHome()
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 6, enemyShipCount: 2, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 1f, hasLocalThreat: true, localAdvantage: profile.RetreatAdvantage));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.EngageLocal));
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
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 5, enemyShipCount: 5, ownedCapturableZoneCount: 2,
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
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                    ownShipCount: 6, enemyShipCount: 3, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 0.85f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void ExistingHunt_ResumesExpansionWhenClearAdvantageEnds()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicDecision initial = model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                ownShipCount: 6, enemyShipCount: 3, ownedCapturableZoneCount: 2,
                fleetAdvantage: 3f));
            Assert.That(initial.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
            EnemyStrategicSnapshot modestAdvantage = CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Medium, ownShipCount: 6, enemyShipCount: 3,
                ownedCapturableZoneCount: 2, fleetAdvantage: 1.2f);

            Assert.That(model.Evaluate(modestAdvantage).State, Is.EqualTo(EnemyStrategicState.CaptureZone));
            Assert.That(new EnemyStrategicDecisionModel().Evaluate(modestAdvantage).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void NothingToCapture_BaseObjectiveAssaultsAtDifficultyThreshold(EnemyAiDifficulty difficulty)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase, difficulty,
                    ownShipCount: 6, enemyShipCount: 4, ownedCapturableZoneCount: 2,
                    fleetAdvantage: profile.RequiredAttackRatio, hasCaptureTarget: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void ClearAdvantage_FleetObjectiveHuntsEnemy(EnemyAiDifficulty difficulty)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, difficulty,
                    ownShipCount: 10, enemyShipCount: 1, ownedCapturableZoneCount: 2,
                    fleetAdvantage: 10f));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
        }

        [TestCase(EnemyAiDifficulty.Easy, 4f, 0.5f, 1.5f)]
        [TestCase(EnemyAiDifficulty.Medium, 2.5f, 0.65f, 1.25f)]
        [TestCase(EnemyAiDifficulty.Hard, 1.25f, 0.8f, 1f)]
        [TestCase(EnemyAiDifficulty.UltraHard, 0.5f, 0.85f, 0.75f)]
        public void DifficultyProfile_UsesExpectedStrategicConfiguration(
            EnemyAiDifficulty difficulty,
            float decisionInterval,
            float committedFleetRatio,
            float defenseThreatRatio)
        {
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(difficulty);

            Assert.That(profile.DecisionInterval, Is.EqualTo(decisionInterval));
            Assert.That(profile.CommittedFleetRatio, Is.EqualTo(committedFleetRatio));
            Assert.That(profile.DefenseThreatRatio, Is.EqualTo(defenseThreatRatio));
            Assert.That(profile.RetreatAdvantage, Is.LessThan(profile.HuntAdvantage));
            Assert.That(EnemyAiDifficultyProfile.Get(difficulty), Is.SameAs(profile));
        }

        [Test]
        public void SmallRaid_IsAnsweredByADetachmentNotTheWholeFleet()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 1, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 10f, baseThreatRatio: 0.2f, hasStationPressure: true));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.DefendBase));
            Assert.That(decision.CommittedShipCount, Is.GreaterThan(0).And.LessThan(20));
        }

        [Test]
        public void CriticalStation_IsDefendedByTheWholeFleet()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 1, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 10f, baseThreatRatio: 0.2f, hasStationPressure: true, isStationCritical: true));

            Assert.That(decision.CommittedShipCount, Is.EqualTo(20));
        }

        [Test]
        public void StrongerAttackers_GetALargerDefenseDetachment()
        {
            int Committed(float ratio) => new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 1, ownedCapturableZoneCount: 0,
                    fleetAdvantage: 10f, baseThreatRatio: ratio, hasStationPressure: true)).CommittedShipCount;

            Assert.That(Committed(0.6f), Is.GreaterThan(Committed(0.2f)));
        }

        [Test]
        public void DifficultyProfile_HarderAiDefendsEarlier()
        {
            Assert.That(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).DefenseThreatRatio,
                Is.LessThan(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Hard).DefenseThreatRatio));
            Assert.That(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Hard).DefenseThreatRatio,
                Is.LessThan(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium).DefenseThreatRatio));
            Assert.That(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium).DefenseThreatRatio,
                Is.LessThan(EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Easy).DefenseThreatRatio));
        }

        [Test]
        public void CaptureDetachment_KeepsExpandingDuringMainFleetDefense()
        {
            EnemyStrategicSnapshot snapshot = CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, ownShipCount: 1, enemyShipCount: 3,
                ownedCapturableZoneCount: 0, fleetAdvantage: 3f, hasStationPressure: true);

            Assert.That(new EnemyStrategicDecisionModel().Evaluate(snapshot).State,
                Is.EqualTo(EnemyStrategicState.DefendBase));
            Assert.That(new EnemyStrategicDecisionModel().Evaluate(snapshot, captureDetachment: true).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void CaptureDetachment_FightsItsOwnThreatWithoutCaptureTarget()
        {
            EnemyStrategicSnapshot snapshot = CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, ownShipCount: 1, enemyShipCount: 3,
                ownedCapturableZoneCount: 0, fleetAdvantage: 0.2f, localAdvantage: 0.2f,
                hasCaptureTarget: false, hasLocalThreat: true, hasStationPressure: true);

            Assert.That(new EnemyStrategicDecisionModel().Evaluate(snapshot, captureDetachment: true).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
        }

        [Test]
        public void ExposedMine_InterruptsExpansionDespiteCaptureHysteresis()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            Assert.That(model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 3, 3, 1, 1f)).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));

            Assert.That(model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 3, 3, 1, 1f,
                hasRaidTarget: true, hasEconomyRaidTarget: true)).State,
                Is.EqualTo(EnemyStrategicState.RaidStructures));
        }

        [TestCase(true, false, 100f, 1f, EnemyStrategicState.DefendBase)]
        [TestCase(false, true, 100f, 1f, EnemyStrategicState.CaptureZone)]
        [TestCase(false, false, 0.5f, 1f, EnemyStrategicState.EngageLocal)]
        [TestCase(false, false, 100f, 3f, EnemyStrategicState.AssaultBase)]
        public void MiningRaid_DoesNotOverrideEmergencyOrDecisiveAttack(bool stationPressure,
            bool threatenedSite, float localAdvantage, float fleetAdvantage, EnemyStrategicState expected)
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 3, 3, 1, 1f,
                hasRaidTarget: true, hasEconomyRaidTarget: true));

            Assert.That(model.Evaluate(CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 3, 3, 1, fleetAdvantage,
                hasRaidTarget: true, hasEconomyRaidTarget: true, hasStationPressure: stationPressure,
                // A low local advantage only means something when hostiles are actually near the fleet.
                hasThreatenedSite: threatenedSite, localAdvantage: localAdvantage,
                hasLocalThreat: localAdvantage < 1f)).State, Is.EqualTo(expected));
        }

        [Test]
        public void CaptureDetachment_KeepsCapturingWhileCombatGroupCanRaidMines()
        {
            EnemyStrategicSnapshot snapshot = CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 1, 3, 1, 1f,
                hasRaidTarget: true, hasEconomyRaidTarget: true);

            Assert.That(new EnemyStrategicDecisionModel().Evaluate(snapshot, captureDetachment: true).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [TestCase(true, EnemyStrategicState.CaptureZone)]
        [TestCase(false, EnemyStrategicState.Hold)]
        public void InsufficientTargetForce_ReconsidersNonOffensiveObjectives(bool capture, EnemyStrategicState expected)
        {
            EnemyStrategicSnapshot snapshot = CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 3, 3, 1, 3f, hasCaptureTarget: capture,
                hasRaidTarget: true, hasEconomyRaidTarget: true);
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            Assert.That(model.Evaluate(snapshot).State, Is.EqualTo(EnemyStrategicState.AssaultBase));
            Assert.That(model.Evaluate(snapshot, allowOffense: false).State, Is.EqualTo(expected));
        }

        [Test]
        public void SecuredCentre_ConsolidatesUntilTheAssaultGateOpens_AndReturnsWhenItCloses()
        {
            var model = new EnemyStrategicDecisionModel();
            EnemyStrategicSnapshot held = CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 8, 0, 3, 10f, hasCaptureTarget: false,
                canAssaultBase: false, canHuntFleet: false, hasSecuredCentre: true);
            Assert.That(model.Evaluate(held).State, Is.EqualTo(EnemyStrategicState.Consolidate));

            EnemyStrategicSnapshot attack = CreateSnapshot(BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard, 8, 0, 3, 10f, hasCaptureTarget: false,
                canAssaultBase: true, canHuntFleet: false, hasSecuredCentre: true);
            Assert.That(model.Evaluate(attack).State, Is.EqualTo(EnemyStrategicState.AssaultBase));
            Assert.That(model.Evaluate(held).State, Is.EqualTo(EnemyStrategicState.Consolidate));
        }

        // S1 / S2: stations die in every mode. Against the enemy fleet a hunt target away from a station comes first;
        // with none (the fleet sits at its station) the station is assaulted.
        [TestCase(true, EnemyStrategicState.HuntFleet)]
        [TestCase(false, EnemyStrategicState.AssaultBase)]
        public void FleetObjective_AssaultsTheStationWhenNoHuntTargetIsLeft(bool canHuntFleet,
            EnemyStrategicState expected)
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 12, enemyShipCount: 6, ownedCapturableZoneCount: 2,
                    fleetAdvantage: EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).RequiredAttackRatio *
                        EnemyStrategicDecisionModel.PRESS_ADVANTAGE_SCALE,
                    hasCaptureTarget: false, canHuntFleet: canHuntFleet));

            Assert.That(decision.State, Is.EqualTo(expected));
        }

        // S4: in the end game the whole fleet assaults without the centre and before further expansion.
        [Test]
        public void EndGame_WholeFleetAssaultsWithoutTheCentre()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 8, ownedCapturableZoneCount: 2,
                    fleetAdvantage: EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).RequiredAttackRatio,
                    canHuntFleet: false, isEndGame: true));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
            Assert.That(decision.CommittedShipCount, Is.EqualTo(20));
        }

        [Test]
        public void MidGame_SameFleetKeepsExpanding()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 20, enemyShipCount: 8, ownedCapturableZoneCount: 2,
                    fleetAdvantage: EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).RequiredAttackRatio,
                    canHuntFleet: false));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        // S6: allied human ships at a hostile station pull the main force into their assault.
        [Test]
        public void AllyAssault_JoinsTheAlliedAssault()
        {
            EnemyStrategicDecision decision = new EnemyStrategicDecisionModel().Evaluate(
                CreateSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.UltraHard,
                    ownShipCount: 8, enemyShipCount: 8, ownedCapturableZoneCount: 2,
                    fleetAdvantage: EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.UltraHard).RequiredAttackRatio,
                    canHuntFleet: false, joinsAllyAssault: true));

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
            Assert.That(decision.Reason, Is.EqualTo("Joining an allied assault."));
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
            bool isRegrouped = true,
            bool hasStationPressure = false,
            bool hasSafeRetreat = true,
            bool hasEconomyRaidTarget = false,
            bool isStationCritical = false, bool canAssaultBase = true, bool canHuntFleet = true,
            bool hasSecuredCentre = false, bool isEndGame = false, bool joinsAllyAssault = false)
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
                isStationCritical: isStationCritical,
                isRegrouped: isRegrouped,
                hasStationPressure: hasStationPressure,
                hasSafeRetreat: hasSafeRetreat,
                hasEconomyRaidTarget: hasEconomyRaidTarget,
                canAssaultBase: canAssaultBase, canHuntFleet: canHuntFleet,
                hasSecuredCentre: hasSecuredCentre, hasGuardSite: hasSecuredCentre,
                isCentreGuardGoal: hasSecuredCentre && !canAssaultBase,
                isEndGame: isEndGame, joinsAllyAssault: joinsAllyAssault);
        }
    }
}
