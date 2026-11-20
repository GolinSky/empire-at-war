using EmpireAtWar.Services.Reinforcement;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
    public sealed class ReinforcementFacingTests
    {
        private const float AUTO_YAW = 30f;
        private const float STEP = 15f;

        [Test]
        public void Untouched_FollowsAutomaticHeading()
        {
            var facing = new ReinforcementFacing();

            Assert.That(facing.GetYaw(AUTO_YAW), Is.EqualTo(AUTO_YAW));
            Assert.That(facing.GetYaw(200f), Is.EqualTo(200f));
        }

        [Test]
        public void TurnPress_StepsLeftAndRight_AndOverridesLaterAutomaticHeadings()
        {
            var facing = new ReinforcementFacing();

            facing.Turn(1, true, 0.016f, AUTO_YAW);
            AssertYaw(facing.GetYaw(AUTO_YAW), AUTO_YAW + STEP);
            facing.Turn(-1, true, 0.016f, AUTO_YAW);
            facing.Turn(-1, true, 0.016f, AUTO_YAW);
            AssertYaw(facing.GetYaw(AUTO_YAW), AUTO_YAW - STEP);
            // Moving the preview changes the automatic heading; the manual one must stay.
            AssertYaw(facing.GetYaw(170f), AUTO_YAW - STEP);
        }

        [Test]
        public void TurnHeld_KeepsTurningAfterThePress()
        {
            var facing = new ReinforcementFacing();
            facing.Turn(1, true, 0.016f, AUTO_YAW);
            float afterPress = facing.GetYaw(AUTO_YAW);

            for (int i = 0; i < 100; i++)
                facing.Turn(1, false, 0.02f, AUTO_YAW);

            Assert.That(Mathf.DeltaAngle(afterPress, facing.GetYaw(AUTO_YAW)), Is.GreaterThan(0f));
        }

        [Test]
        public void Aim_TurnsTowardDragDirection_OutsideDeadZoneOnly()
        {
            var facing = new ReinforcementFacing();

            facing.Aim(Vector3.zero, new Vector3(0.5f, 0f, 0f), 1f);
            Assert.That(facing.GetYaw(AUTO_YAW), Is.EqualTo(AUTO_YAW));

            facing.Aim(Vector3.zero, new Vector3(10f, 5f, 0f), 1f);
            AssertYaw(facing.GetYaw(AUTO_YAW), 90f);
        }

        [Test]
        public void Reset_RestoresAutomaticHeading()
        {
            var facing = new ReinforcementFacing();
            facing.Aim(Vector3.zero, Vector3.back * 10f, 1f);

            facing.Reset();

            Assert.That(facing.GetYaw(AUTO_YAW), Is.EqualTo(AUTO_YAW));
        }

        private static void AssertYaw(float actual, float expected) =>
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(actual, expected)), Is.LessThan(0.01f));
    }
}
