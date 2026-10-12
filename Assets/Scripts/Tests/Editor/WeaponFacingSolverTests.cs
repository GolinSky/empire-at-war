using System.Collections.Generic;
using EmpireAtWar.Components.Weapon;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class WeaponFacingSolverTests
    {
        private const int SAMPLE_COUNT = 180;
        private const float SAMPLE_STEP = 360f / SAMPLE_COUNT;

        [Test]
        public void FindTurn_MatchesPerSampleReference()
        {
            WeaponFacingSolver solver = new WeaponFacingSolver();
            System.Random random = new System.Random(1234);
            List<Vector2> arcs = new List<Vector2>();
            for (int trial = 0; trial < 2000; trial++)
            {
                arcs.Clear();
                int count = random.Next(0, 60);
                for (int i = 0; i < count; i++)
                {
                    // Hardpoint arcs: target yaw (any float) minus integer yaw limits; some full-circle turrets.
                    float yaw = (float)(random.NextDouble() * 360.0 - 180.0);
                    float maxYaw = random.Next(0, 4) == 0 ? 180f : random.Next(5, 120);
                    float minYaw = -random.Next(5, 120);
                    arcs.Add(new Vector2(yaw - maxYaw, yaw - minYaw));
                }

                Assert.That(solver.FindTurn(arcs), Is.EqualTo(ReferenceFindTurn(arcs)), $"trial {trial}");
            }
        }

        [Test]
        public void FindTurn_ArcsCentredAhead_KeepsHeading()
        {
            WeaponFacingSolver solver = new WeaponFacingSolver();

            Assert.That(solver.FindTurn(new[] { new Vector2(-45f, 45f), new Vector2(-30f, 30f) }), Is.EqualTo(0f));
        }

        [Test]
        public void FindTurn_NoArcs_KeepsHeading()
        {
            Assert.That(new WeaponFacingSolver().FindTurn(new List<Vector2>()), Is.EqualTo(0f));
        }

        // The former per-sample implementation: every sample tested against every arc.
        private static float ReferenceFindTurn(IReadOnlyList<Vector2> turnArcs)
        {
            int[] scores = new int[SAMPLE_COUNT];
            int best = 0;
            for (int i = 0; i < SAMPLE_COUNT; i++)
            {
                float turn = i * SAMPLE_STEP;
                int score = 0;
                for (int a = 0; a < turnArcs.Count; a++)
                {
                    float width = turnArcs[a].y - turnArcs[a].x;
                    if (width >= 360f || Mathf.Repeat(turn - turnArcs[a].x, 360f) <= width) score++;
                }

                scores[i] = score;
                if (score > best) best = score;
            }

            int gap = 0;
            while (gap < SAMPLE_COUNT && scores[gap] == best) gap++;
            if (best == 0 || gap == SAMPLE_COUNT) return 0f;

            float bestTurn = 0f;
            float bestDistance = float.MaxValue;
            int runStart = -1;
            for (int step = 1; step <= SAMPLE_COUNT; step++)
            {
                int index = (gap + step) % SAMPLE_COUNT;
                if (scores[index] == best)
                {
                    if (runStart < 0) runStart = gap + step;
                    continue;
                }

                if (runStart < 0) continue;
                float start = runStart * SAMPLE_STEP;
                float end = (gap + step - 1) * SAMPLE_STEP;
                float distance = Mathf.Repeat(-start, 360f) <= end - start
                    ? 0f
                    : Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(0f, start)), Mathf.Abs(Mathf.DeltaAngle(0f, end)));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTurn = Mathf.DeltaAngle(0f, (start + end) * 0.5f);
                }

                runStart = -1;
            }

            return bestTurn;
        }
    }
}
