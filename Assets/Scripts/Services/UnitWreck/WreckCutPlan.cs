using System;

namespace EmpireAtWar.Services.UnitWreck
{
    /// <summary>
    /// Where a wreck breaks, as fractions of the ship's length (0 = one end, 1 = the other).
    /// Two parts: one cut splitting the ship at the given ratio (0.5 = in half, 0.7 = 70/30).
    /// Three parts: the bigger of the two parts is split again the same way.
    /// </summary>
    public readonly struct WreckCutPlan
    {
        private WreckCutPlan(float firstCut, float secondCut, int partCount)
        {
            FirstCut = firstCut;
            SecondCut = secondCut;
            PartCount = partCount;
        }

        public float FirstCut { get; }
        /// <summary>Only meaningful when <see cref="PartCount"/> is 3; always after <see cref="FirstCut"/>.</summary>
        public float SecondCut { get; }
        public int PartCount { get; }

        public static WreckCutPlan Create(float threePartChance, float minRatio, float maxRatio, Random random)
        {
            float cut = PickCut(0f, 1f, minRatio, maxRatio, random);
            if (random.NextDouble() >= threePartChance)
            {
                return new WreckCutPlan(cut, 1f, 2);
            }

            bool isFirstPartBigger = cut > 0.5f;
            float extraCut = isFirstPartBigger
                ? PickCut(0f, cut, minRatio, maxRatio, random)
                : PickCut(cut, 1f, minRatio, maxRatio, random);
            return new WreckCutPlan(Math.Min(cut, extraCut), Math.Max(cut, extraCut), 3);
        }

        // A cut inside [start, end] that leaves `ratio` of the span on a random side.
        private static float PickCut(float start, float end, float minRatio, float maxRatio, Random random)
        {
            float ratio = minRatio + (maxRatio - minRatio) * (float)random.NextDouble();
            float side = random.NextDouble() < 0.5 ? ratio : 1f - ratio;
            return start + (end - start) * side;
        }
    }
}
