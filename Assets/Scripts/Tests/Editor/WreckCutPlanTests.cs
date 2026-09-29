using System;
using EmpireAtWar.Services.UnitWreck;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class WreckCutPlanTests
    {
        private const float MIN_RATIO = 0.5f;
        private const float MAX_RATIO = 0.7f;
        private const float TOLERANCE = 0.0001f;

        [Test]
        public void Create_TwoParts_BiggerPartIsWithinRatio()
        {
            Random random = new Random(1);
            for (int i = 0; i < 200; i++)
            {
                WreckCutPlan plan = WreckCutPlan.Create(0f, MIN_RATIO, MAX_RATIO, random);

                Assert.That(plan.PartCount, Is.EqualTo(2));
                float biggerPart = Math.Max(plan.FirstCut, 1f - plan.FirstCut);
                Assert.That(biggerPart, Is.InRange(MIN_RATIO - TOLERANCE, MAX_RATIO + TOLERANCE));
            }
        }

        [Test]
        public void Create_ThreeParts_CutsAreOrderedInsideTheShip()
        {
            Random random = new Random(2);
            for (int i = 0; i < 200; i++)
            {
                WreckCutPlan plan = WreckCutPlan.Create(1f, MIN_RATIO, MAX_RATIO, random);

                Assert.That(plan.PartCount, Is.EqualTo(3));
                Assert.That(plan.FirstCut, Is.GreaterThan(0f));
                Assert.That(plan.SecondCut, Is.GreaterThan(plan.FirstCut));
                Assert.That(plan.SecondCut, Is.LessThan(1f));
            }
        }
    }
}
