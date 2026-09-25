using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    // Finds the hull yaw turn that brings a target inside the most weapon arcs.
    public sealed class WeaponFacingSolver
    {
        private const int SAMPLE_COUNT = 180;
        private const float SAMPLE_STEP = 360f / SAMPLE_COUNT;

        private readonly int[] _scores = new int[SAMPLE_COUNT];

        // Each arc (x = min, y = max) is the range of hull turns in degrees that keeps the
        // target inside one weapon's yaw limits. Returns the centre of the best-covered turn
        // range closest to the current heading, so symmetric ships keep their nearer side.
        public float FindTurn(IReadOnlyList<Vector2> turnArcs)
        {
            int best = 0;
            for (int i = 0; i < SAMPLE_COUNT; i++)
            {
                float turn = i * SAMPLE_STEP;
                int score = 0;
                for (int a = 0; a < turnArcs.Count; a++)
                    if (Contains(turnArcs[a], turn)) score++;
                _scores[i] = score;
                if (score > best) best = score;
            }

            int gap = 0;
            while (gap < SAMPLE_COUNT && _scores[gap] == best) gap++;
            if (best == 0 || gap == SAMPLE_COUNT) return 0f;

            float bestTurn = 0f;
            float bestDistance = float.MaxValue;
            int runStart = -1;
            for (int step = 1; step <= SAMPLE_COUNT; step++)
            {
                int index = (gap + step) % SAMPLE_COUNT;
                if (_scores[index] == best)
                {
                    if (runStart < 0) runStart = gap + step;
                    continue;
                }
                if (runStart < 0) continue;

                float start = runStart * SAMPLE_STEP;
                float end = (gap + step - 1) * SAMPLE_STEP;
                float distance = DistanceFromZero(start, end);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTurn = Mathf.DeltaAngle(0f, (start + end) * 0.5f);
                }
                runStart = -1;
            }

            return bestTurn;
        }

        private static bool Contains(Vector2 arc, float turn)
        {
            float width = arc.y - arc.x;
            return width >= 360f || Mathf.Repeat(turn - arc.x, 360f) <= width;
        }

        private static float DistanceFromZero(float start, float end)
        {
            if (Mathf.Repeat(-start, 360f) <= end - start) return 0f;
            return Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(0f, start)),
                Mathf.Abs(Mathf.DeltaAngle(0f, end)));
        }
    }
}
