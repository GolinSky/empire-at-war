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
                WreckCutPlan plan = WreckCutPlan.Create(2, 2, MIN_RATIO, MAX_RATIO, random);

                Assert.That(plan.PartCount, Is.EqualTo(2));
                float biggerPart = Math.Max(plan.Cuts[0], 1f - plan.Cuts[0]);
                Assert.That(biggerPart, Is.InRange(MIN_RATIO - TOLERANCE, MAX_RATIO + TOLERANCE));
            }
        }

        [Test]
        public void Create_PartCountStaysInRange_CutsAreSortedInsideTheUnit()
        {
            Random random = new Random(2);
            for (int i = 0; i < 500; i++)
            {
                WreckCutPlan plan = WreckCutPlan.Create(2, WreckCutPlan.MAX_PARTS, MIN_RATIO, MAX_RATIO, random);

                Assert.That(plan.PartCount, Is.InRange(2, WreckCutPlan.MAX_PARTS));
                Assert.That(plan.Cuts.Count, Is.EqualTo(plan.PartCount - 1));
                for (int cut = 0; cut < plan.Cuts.Count; cut++)
                {
                    Assert.That(plan.Cuts[cut], Is.GreaterThan(0f).And.LessThan(1f));
                    if (cut > 0) Assert.That(plan.Cuts[cut], Is.GreaterThan(plan.Cuts[cut - 1]));
                }
            }
        }

        [Test]
        public void Create_MoreThanShaderCutSlots_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                WreckCutPlan.Create(2, WreckCutPlan.MAX_PARTS + 1, MIN_RATIO, MAX_RATIO, new Random(3)));
        }
    }
}
