using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.UnitWreck
{
    /// <summary>
    /// Where a wreck breaks, as sorted fractions of the unit's length (0 = one end, 1 = the other).
    /// The first cut splits the unit at the given ratio (0.5 = in half, 0.7 = 70/30); every further
    /// cut splits the biggest part so far the same way, until the part count is reached.
    /// </summary>
    public readonly struct WreckCutPlan
    {
        public const int MIN_PARTS = 2;
        /// <summary>The shader has four cut slots.</summary>
        public const int MAX_PARTS = 5;

        private readonly float[] _cuts;

        public IReadOnlyList<float> Cuts => _cuts;
        public int PartCount => _cuts.Length + 1;

        private WreckCutPlan(float[] cuts)
        {
            _cuts = cuts;
        }

        public static WreckCutPlan Create(int minParts, int maxParts, float minRatio, float maxRatio, Random random)
        {
            if (minParts < MIN_PARTS || maxParts > MAX_PARTS || minParts > maxParts)
            {
                throw new ArgumentOutOfRangeException(nameof(minParts),
                    $"Part counts must satisfy {MIN_PARTS} <= min <= max <= {MAX_PARTS}; got {minParts}..{maxParts}.");
            }

            int partCount = random.Next(minParts, maxParts + 1);
            List<float> cuts = new List<float>(partCount - 1);
            float biggestStart = 0f;
            float biggestEnd = 1f;
            for (int i = 1; i < partCount; i++)
            {
                cuts.Add(PickCut(biggestStart, biggestEnd, minRatio, maxRatio, random));
                cuts.Sort();
                FindBiggestPart(cuts, out biggestStart, out biggestEnd);
            }

            return new WreckCutPlan(cuts.ToArray());
        }

        private static void FindBiggestPart(List<float> cuts, out float start, out float end)
        {
            start = 0f;
            end = cuts[0];
            for (int i = 0; i < cuts.Count; i++)
            {
                float partStart = cuts[i];
                float partEnd = i + 1 < cuts.Count ? cuts[i + 1] : 1f;
                if (partEnd - partStart > end - start)
                {
                    start = partStart;
                    end = partEnd;
                }
            }
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
