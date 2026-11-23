using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyZoneCautionTests
    {
        private const string ZONE = "zone:1";

        [Test]
        public void Observe_ForceThatLeavesBeforeTheCaptureIsAFailedCapture()
        {
            var caution = new EnemyZoneCaution();

            caution.Observe(ZONE, owned: false, ownShipsPresent: true, now: 0f);
            caution.Observe(ZONE, owned: false, ownShipsPresent: false, now: 10f);

            Assert.That(caution.CountFailures(ZONE, 10f), Is.EqualTo(1));
        }

        [Test]
        public void Observe_CaptureIsNoFailure_LosingTheZoneIsARetake()
        {
            var caution = new EnemyZoneCaution();

            caution.Observe(ZONE, false, true, 0f);
            caution.Observe(ZONE, true, true, 10f);
            caution.Observe(ZONE, true, false, 20f);
            float held = caution.GetHeldSeconds(ZONE, 40f);
            caution.Observe(ZONE, false, false, 50f);

            Assert.That(held, Is.GreaterThan(0f), "Held time counts from the capture.");
            Assert.That(caution.CountRetakes(ZONE, 50f), Is.EqualTo(1));
            Assert.That(caution.CountFailures(ZONE, 50f), Is.EqualTo(1), "Only the retake counts.");
            Assert.That(caution.GetHeldSeconds(ZONE, 50f), Is.EqualTo(0f));
        }

        [Test]
        public void Failures_FadeAfterTheMemory()
        {
            var caution = new EnemyZoneCaution();
            caution.Observe(ZONE, false, true, 0f);
            caution.Observe(ZONE, false, false, 1f);

            Assert.That(caution.CountFailures(ZONE, 1f + EnemyZoneCaution.MEMORY_SECONDS + 1f), Is.EqualTo(0));
        }

        [Test]
        public void Level_RisesWithFailedCapturesLossesSpawnAccessAndANearArmada()
        {
            float calm = EnemyZoneCaution.GetLevel(0, 0f, false, float.PositiveInfinity, 1f);

            Assert.That(EnemyZoneCaution.GetLevel(2, 0f, false, float.PositiveInfinity, 1f),
                Is.GreaterThan(EnemyZoneCaution.GetLevel(1, 0f, false, float.PositiveInfinity, 1f)));
            Assert.That(EnemyZoneCaution.GetLevel(1, 0f, false, float.PositiveInfinity, 1f), Is.GreaterThan(calm));
            Assert.That(EnemyZoneCaution.GetLevel(0, 0.5f, false, float.PositiveInfinity, 1f), Is.GreaterThan(calm));
            Assert.That(EnemyZoneCaution.GetLevel(0, 0f, true, float.PositiveInfinity, 1f), Is.GreaterThan(calm));
            Assert.That(EnemyZoneCaution.GetLevel(0, 0f, false, 500f, 1f),
                Is.GreaterThan(EnemyZoneCaution.GetLevel(0, 0f, false, 2500f, 1f)));
            Assert.That(EnemyZoneCaution.GetLevel(0, 0f, false, float.PositiveInfinity, 0.5f), Is.GreaterThan(calm));
        }

        [Test]
        public void Plan_RepeatedFailuresLeadToAllyCombinedThenSkip()
        {
            float level = EnemyZoneCaution.GetLevel(EnemyZoneCaution.COMBINED_FAILURES, 0f, false,
                float.PositiveInfinity, 1f);

            EnemyZonePlan fresh = EnemyZoneCaution.SelectPlan(0f, 0, 0, false, true, true, false);
            EnemyZonePlan combined = EnemyZoneCaution.SelectPlan(level, EnemyZoneCaution.COMBINED_FAILURES, 0, false,
                important: true, gainsWithAlly: true, hostileSpawnNear: false);
            EnemyZonePlan minor = EnemyZoneCaution.SelectPlan(level, EnemyZoneCaution.COMBINED_FAILURES, 0, false,
                important: false, gainsWithAlly: true, hostileSpawnNear: false);
            EnemyZonePlan skipped = EnemyZoneCaution.SelectPlan(level, EnemyZoneCaution.SKIP_FAILURES, 0, false,
                important: true, gainsWithAlly: true, hostileSpawnNear: false);

            Assert.That(fresh, Is.EqualTo(EnemyZonePlan.TakeAndHold));
            Assert.That(combined, Is.EqualTo(EnemyZonePlan.AllyCombined));
            Assert.That(minor, Is.Not.EqualTo(EnemyZonePlan.AllyCombined), "Only an important zone waits for an ally.");
            Assert.That(skipped, Is.EqualTo(EnemyZonePlan.Skip));
        }

        [Test]
        public void Plan_FailedZoneWithoutGainEvenWithAllyHelpIsSkipped_AHeldZoneNever()
        {
            Assert.That(EnemyZoneCaution.SelectPlan(1f, 1, 0, false, true, gainsWithAlly: false, hostileSpawnNear: false),
                Is.EqualTo(EnemyZonePlan.Skip));
            Assert.That(EnemyZoneCaution.SelectPlan(1f, 1, 0, false, true, gainsWithAlly: true, hostileSpawnNear: false),
                Is.Not.EqualTo(EnemyZonePlan.Skip));
            Assert.That(EnemyZoneCaution.SelectPlan(EnemyZoneCaution.SKIP_LEVEL, EnemyZoneCaution.SKIP_FAILURES, 1,
                held: true, important: true, gainsWithAlly: false, hostileSpawnNear: true), Is.Not.EqualTo(EnemyZonePlan.Skip));
        }

        [Test]
        public void Plan_RetakenZoneWithHostileSpawnAccessIsHeldFromOutside()
        {
            float level = EnemyZoneCaution.GetLevel(1, 0f, true, float.PositiveInfinity, 1f);

            Assert.That(level, Is.GreaterThanOrEqualTo(EnemyZoneCaution.HOLD_BACK_LEVEL));
            Assert.That(EnemyZoneCaution.SelectPlan(level, 1, 1, held: true, important: false, gainsWithAlly: true,
                hostileSpawnNear: true), Is.EqualTo(EnemyZonePlan.HoldBackAndCounter));
            Assert.That(EnemyZoneCaution.SelectPlan(level, 1, 1, held: true, important: false, gainsWithAlly: true,
                hostileSpawnNear: false), Is.EqualTo(EnemyZonePlan.TakeAndHold));
        }

        [Test]
        public void Skip_LastsTheSkipTime()
        {
            var caution = new EnemyZoneCaution();

            caution.Skip(ZONE, 100f);

            Assert.That(caution.IsSkipped(ZONE, 100f + EnemyZoneCaution.SKIP_SECONDS * 0.5f), Is.True);
            Assert.That(caution.IsSkipped(ZONE, 100f + EnemyZoneCaution.SKIP_SECONDS + 1f), Is.False);
        }

        [Test]
        public void Losses_CountAtTheZoneAndFade()
        {
            var caution = new EnemyZoneCaution();

            caution.RecordLoss(ZONE, 1000f, 0f);

            Assert.That(caution.GetLosses(ZONE, 0f), Is.GreaterThan(0f));
            Assert.That(caution.GetLosses(ZONE, EnemyZoneCaution.MEMORY_SECONDS), Is.LessThan(caution.GetLosses(ZONE, 0f)));
            Assert.That(caution.GetLosses("zone:2", 0f), Is.EqualTo(0f));
        }
    }
}
