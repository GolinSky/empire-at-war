using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>
    /// Hit-and-run cycle: strike → pull back together → recover → strike. Used only when a straight fight is bad,
    /// one strike pays, the strikers can break contact and the ships left at the fight survive their absence.
    /// </summary>
    [Category("EnemyAi")]
    public sealed class EnemyStrikeCycleModelTests
    {
        private const float MIN_PHASE = 5f;
        private const float MAX_RECOVER = 60f;
        private const float PULL_BACK = 0.25f;
        private const float RESUME = 0.85f;
        private const float RESUME_HULL = 0.8f;

        [Test]
        public void Evaluate_AllConditionsMet_StartsAStrike()
        {
            EnemyStrikeCycleModel model = Model();

            Assert.That(model.Evaluate(Input()), Is.EqualTo(EnemyStrikePhase.Strike));
            Assert.That(model.IsActive, Is.True);
        }

        [Test]
        public void Evaluate_NotInAFight_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.IsInFight = false;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Evaluate_WithoutABigShipTarget_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.HasTarget = false;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Evaluate_GroupAlreadyWinsAStraightFight_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.GroupAdvantage = 1.5f;
            EnemyStrikeCycleModel model = Model();

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
            Assert.That(model.Reason, Is.EqualTo("groupWins"));
        }

        [Test]
        public void Evaluate_NoSafePoint_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.HasSafePoint = false;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Evaluate_TargetUnderGunsTheGroupCannotBeat_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.TargetUnderUnbeatableDefenses = true;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Evaluate_OneStriker_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.StrikerCount = 1;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Evaluate_ShipsLeftBehindDieBeforeTheStrikersReturn_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            EnemyStrikeCycleModel model = Model();
            input.LeftBehindSurvivalSeconds = model.GetAwaySeconds(input) - 1f;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
            Assert.That(model.Reason, Is.EqualTo("leftBehind"));
        }

        [Test]
        public void Evaluate_NoShipLeftBehind_StartsAStrike()
        {
            EnemyStrikeCycleInput input = Input();
            input.NonStrikerCount = 0;
            input.LeftBehindSurvivalSeconds = 0f;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
        }

        [Test]
        public void Evaluate_TargetAsFastAsTheSlowestStriker_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.TargetSpeed = input.SlowestStrikerSpeed;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Evaluate_SlowStrikersWithStationCoverInsideTheTargetWeaponRange_StartsAStrike()
        {
            EnemyStrikeCycleInput input = Input();
            input.TargetSpeed = input.SlowestStrikerSpeed;
            input.SafePointIsStationCover = true;
            input.TargetWeaponRange = 500f;
            input.SafePointDistance = 300f;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
        }

        [Test]
        public void Evaluate_TargetShieldRegenMatchesTheStrikersShieldDamage_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.TargetShieldRegen = input.StrikerShieldDps;
            EnemyStrikeCycleModel model = Model();

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
            Assert.That(model.Reason, Is.EqualTo("strikeDoesNotPay"));
        }

        [Test]
        public void Evaluate_HostileRepairOutpacesTheHullDamage_StaysIdle()
        {
            EnemyStrikeCycleInput input = Input();
            input.HostileRepairPerSecond = 1000f;

            Assert.That(Model().Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Pays_FieldRepairsOfTheTargetCountOnlyWhenNoShipKeepsItUnderFire()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Input();
            input.TargetSelfRepairs = true;
            // Field Repairs alone out-heals the strike; with other repair it would hide the effect.
            input.TargetMaxHull = 100000f;

            Assert.That(model.Pays(input), Is.True, "Ships left at the fight stop the Field Repairs of the target.");
            input.NonStrikerCount = 0;
            Assert.That(model.Pays(input), Is.False, "With every ship away the target repairs itself.");
            input.TargetSelfRepairs = false;
            Assert.That(model.Pays(input), Is.True);
        }

        [Test]
        public void Strike_LowAverageShieldsAfterTheMinimumPhase_PullsBack()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Input();
            model.Evaluate(input);
            input.AverageShieldFraction = PULL_BACK - 0.05f;

            input.Now = MIN_PHASE - 1f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike), "The strike lasts its minimum time.");
            input.Now = MIN_PHASE;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.PullBack));
        }

        [Test]
        public void Strike_OneDamagedStrikerDoesNotPullTheGroupBack()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Input();
            model.Evaluate(input);
            // One of three strikers at zero shields, two at full: the average stays above the line.
            input.AverageShieldFraction = 2f / 3f;
            input.Now = MIN_PHASE * 4f;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
        }

        [Test]
        public void Strike_ReactionDelayHoldsThePullBack()
        {
            const float DELAY = 3f;
            EnemyStrikeCycleModel model = Model(reactionDelay: DELAY);
            EnemyStrikeCycleInput input = Input();
            model.Evaluate(input);
            input.AverageShieldFraction = 0f;

            input.Now = MIN_PHASE * 2f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
            input.Now = MIN_PHASE * 2f + DELAY - 0.5f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
            input.Now = MIN_PHASE * 2f + DELAY;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.PullBack));
        }

        [Test]
        public void Strike_LowShieldsButShipsLeftBehindWouldDie_StrikersStayAndTheCycleEnds()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Input();
            model.Evaluate(input);
            input.AverageShieldFraction = 0f;
            input.LeftBehindSurvivalSeconds = 1f;
            input.Now = MIN_PHASE;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void PullBack_Arrived_RecoversUntilShieldsReachTheResumeLine()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = PullBack(model);

            input.Now += 1f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.PullBack), "Strikers still on the way.");
            input.HaveStrikersArrived = true;
            float arrivedAt = input.Now += 1f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Recover));

            input.AverageShieldFraction = RESUME + 0.05f;
            input.Now = arrivedAt + MIN_PHASE - 1f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Recover), "The recovery lasts its minimum time.");
            input.Now = arrivedAt + MIN_PHASE;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
        }

        [Test]
        public void Recover_ShieldsBelowTheResumeLine_KeepsWaiting()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Recover(model);
            input.AverageShieldFraction = RESUME - 0.05f;
            input.Now += MIN_PHASE * 2f;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Recover));
        }

        // Station pressure → shields only. Research or a repairer → shields and hull. Neither → shields only.
        [TestCase(true, true, EnemyStrikePhase.Strike)]
        [TestCase(false, true, EnemyStrikePhase.Recover)]
        [TestCase(false, false, EnemyStrikePhase.Strike)]
        public void Recover_WaitsForHullOnlyWithRepairAndNoStationPressure(bool stationPressure, bool hullRepair,
            EnemyStrikePhase expected)
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Recover(model);
            input.AverageShieldFraction = 1f;
            input.AverageHullFraction = RESUME_HULL - 0.3f;
            input.HasStationPressure = stationPressure;
            input.IsHullRepairAvailable = hullRepair;
            input.Now += MIN_PHASE;

            Assert.That(model.Evaluate(input), Is.EqualTo(expected));
        }

        [Test]
        public void Recover_HullRepaired_ReturnsToTheStrike()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Recover(model);
            input.AverageShieldFraction = 1f;
            input.AverageHullFraction = RESUME_HULL;
            input.IsHullRepairAvailable = true;
            input.Now += MIN_PHASE;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
        }

        [Test]
        public void Recover_MaxRecoverSecondsEndsARecoveryThatNeverFinishes()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Recover(model);
            input.AverageShieldFraction = 0f;
            float startedAt = input.Now;

            input.Now = startedAt + MAX_RECOVER - 1f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Recover));
            input.Now = startedAt + MAX_RECOVER;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
        }

        [Test]
        public void Recover_Chased_StrikesAtOnce()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Recover(model);
            input.AverageShieldFraction = 0f;
            input.IsChased = true;
            input.Now += 0.5f;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Strike));
        }

        [Test]
        public void Recover_GroupNowWinsAStraightFight_EndsTheCycle()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = Recover(model);
            input.AverageShieldFraction = 1f;
            input.GroupAdvantage = 2f;
            input.Now += MIN_PHASE;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Away_ShipsLeftBehindCanNoLongerHold_StrikersReturnAndTheCycleEnds()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = PullBack(model);
            input.LeftBehindSurvivalSeconds = 1f;
            input.Now += 1f;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
            Assert.That(model.Reason, Is.EqualTo("leftBehind"));
        }

        [Test]
        public void Away_TargetLost_EndsTheCycle()
        {
            EnemyStrikeCycleModel model = Model();
            EnemyStrikeCycleInput input = PullBack(model);
            input.HasTarget = false;
            input.Now += 1f;

            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Idle));
        }

        [Test]
        public void Stop_EndsARunningCycle()
        {
            EnemyStrikeCycleModel model = Model();
            model.Evaluate(Input());

            model.Stop("state", 1f);

            Assert.That(model.Phase, Is.EqualTo(EnemyStrikePhase.Idle));
            Assert.That(model.Reason, Is.EqualTo("state"));
        }

        [Test]
        public void GetAwaySeconds_IsTravelBothWaysPlusTheRecovery()
        {
            EnemyStrikeCycleInput input = Input();
            float recover = (RESUME - PULL_BACK) * input.StrikerMaxShields / input.StrikerShieldRegen;

            Assert.That(Model().GetAwaySeconds(input), Is.EqualTo(2f * input.TravelSeconds + recover).Within(0.01f));
            input.StrikerShieldRegen = 0f;
            Assert.That(Model().GetAwaySeconds(input), Is.EqualTo(2f * input.TravelSeconds + MAX_RECOVER).Within(0.01f));
        }

        private static EnemyStrikeCycleModel Model(float reactionDelay = 0f) =>
            new EnemyStrikeCycleModel(TestHitAndRunSettings.Create(straightFightAdvantage: 1.2f,
                pullBackShieldFraction: PULL_BACK, resumeShieldFraction: RESUME, resumeHullFraction: RESUME_HULL,
                minPhaseSeconds: MIN_PHASE, maxRecoverSeconds: MAX_RECOVER, reactionDelaySeconds: reactionDelay));

        // Strike 30 s (600 usable shields at 20 net damage), break the target shields in 5 s, away 60 s.
        private static EnemyStrikeCycleInput Input() => new EnemyStrikeCycleInput
        {
            Now = 0f,
            IsInFight = true,
            HasTarget = true,
            StrikerCount = 3,
            NonStrikerCount = 2,
            GroupAdvantage = 0.8f,
            StrikerMaxShields = 1000f,
            StrikerShieldRegen = 20f,
            HostileShieldDps = 40f,
            StrikerShieldDps = 62f,
            StrikerHullDps = 50f,
            StrikerPiercingDps = 0f,
            TargetShields = 300f,
            TargetMaxShields = 300f,
            TargetShieldRegen = 2f,
            TargetMaxHull = 5000f,
            HasSafePoint = true,
            TravelSeconds = 15f,
            SlowestStrikerSpeed = 40f,
            TargetSpeed = 20f,
            LeftBehindSurvivalSeconds = 500f,
            RegeneratingStrikerCount = 3,
            AverageShieldFraction = 1f,
            AverageHullFraction = 1f
        };

        private static EnemyStrikeCycleInput PullBack(EnemyStrikeCycleModel model)
        {
            EnemyStrikeCycleInput input = Input();
            model.Evaluate(input);
            input.AverageShieldFraction = 0f;
            input.Now = MIN_PHASE;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.PullBack));
            return input;
        }

        private static EnemyStrikeCycleInput Recover(EnemyStrikeCycleModel model)
        {
            EnemyStrikeCycleInput input = PullBack(model);
            input.HaveStrikersArrived = true;
            input.Now += 1f;
            Assert.That(model.Evaluate(input), Is.EqualTo(EnemyStrikePhase.Recover));
            return input;
        }
    }
}
