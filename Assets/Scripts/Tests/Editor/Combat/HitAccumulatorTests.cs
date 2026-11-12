using EmpireAtWar.Components.Weapon;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class HitAccumulatorTests
    {
        [TestCase(0.05f)]
        [TestCase(0.33f)]
        [TestCase(0.6f)]
        [TestCase(0.95f)]
        public void Roll_HitRatioMatchesChance(float chance)
        {
            System.Random random = new System.Random(7);
            HitAccumulator hits = new HitAccumulator((float)random.NextDouble());
            const int SHOTS = 10000;
            int hitCount = 0;
            for (int i = 0; i < SHOTS; i++)
                if (hits.Roll(chance, (float)random.NextDouble())) hitCount++;

            Assert.That(hitCount / (float)SHOTS, Is.EqualTo(chance).Within(0.01f));
        }

        [Test]
        public void Roll_NeverMissesLongerThanTheChanceAllows()
        {
            System.Random random = new System.Random(11);
            HitAccumulator hits = new HitAccumulator((float)random.NextDouble());
            int misses = 0;
            int longestMisses = 0;
            for (int i = 0; i < 10000; i++)
            {
                if (hits.Roll(0.33f, (float)random.NextDouble()))
                {
                    misses = 0;
                    continue;
                }

                misses++;
                if (misses > longestMisses) longestMisses = misses;
            }

            // Pure random at 33 % misses 10+ in a row many times over 10 000 shots. Here the credit starts a drought
            // at 0 or more and gains at least 0.33 - 0.15 per shot, so it reaches 1 within 6 shots: at most 5 misses.
            Assert.That(longestMisses, Is.LessThanOrEqualTo(5));
        }

        [Test]
        public void Roll_CertainChanceAlwaysHitsAndZeroNeverHits()
        {
            HitAccumulator hits = new HitAccumulator(0.5f);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(hits.Roll(1.2f, 0f), Is.True);
                Assert.That(hits.Roll(0f, 0.99f), Is.False);
            }
        }
    }
}
