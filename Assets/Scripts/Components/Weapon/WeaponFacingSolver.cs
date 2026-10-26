using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Weapon
{
    // Finds the hull yaw turn that brings a target inside the weapon arcs with the most total weight.
    public sealed class WeaponFacingSolver
    {
        private const int SAMPLE_COUNT = 180;

        private const float SAMPLE_STEP = 360f / SAMPLE_COUNT;

        private readonly int[] _scores = new int[SAMPLE_COUNT];
        // Coverage changes per sample; one extra slot takes the end of a range that reaches the last sample.
        private readonly int[] _coverageDeltas = new int[SAMPLE_COUNT + 1];

        // Each arc (x = min, y = max) is the range of hull turns in degrees that keeps the
        // target inside one weapon's yaw limits; weights[i] is that weapon's share of the score.
        // Returns the centre of the best-covered turn range closest to the current heading,
        // so symmetric ships keep their nearer side.
        public float FindTurn(IReadOnlyList<Vector2> turnArcs, IReadOnlyList<int> weights)
        {
            // Each arc adds its weight to the run of samples it covers: arcs + samples work instead of
            // arcs x samples, which a big ship with hundreds of hardpoints pays on every facing solve.
            System.Array.Clear(_coverageDeltas, 0, _coverageDeltas.Length);
            for (int a = 0; a < turnArcs.Count; a++) AddCoverage(turnArcs[a], weights[a]);
            int best = 0;
            int score = 0;
            for (int i = 0; i < SAMPLE_COUNT; i++)
            {
                score += _coverageDeltas[i];
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

        // Marks the samples whose turn t satisfies Repeat(t - arc.x, 360) <= arc width, wrapping past 360.
        private void AddCoverage(Vector2 arc, int weight)
        {
            float width = arc.y - arc.x;
            if (width < 0f) return;
            if (width >= 360f)
            {
                AddRange(0, SAMPLE_COUNT, weight);
                return;
            }

            float start = Mathf.Repeat(arc.x, 360f);
            int first = Mathf.CeilToInt(start / SAMPLE_STEP);
            float firstOffset = first * SAMPLE_STEP - start;
            if (firstOffset > width) return;
            int count = Mathf.Min(SAMPLE_COUNT, Mathf.FloorToInt((width - firstOffset) / SAMPLE_STEP) + 1);
            first %= SAMPLE_COUNT;
            int end = first + count;
            if (end <= SAMPLE_COUNT)
            {
                AddRange(first, end, weight);
                return;
            }

            AddRange(first, SAMPLE_COUNT, weight);
            AddRange(0, end - SAMPLE_COUNT, weight);
        }

        private void AddRange(int start, int end, int weight)
        {
            _coverageDeltas[start] += weight;
            _coverageDeltas[end] -= weight;
        }

        private static float DistanceFromZero(float start, float end)
        {
            if (Mathf.Repeat(-start, 360f) <= end - start) return 0f;
            return Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(0f, start)),
                Mathf.Abs(Mathf.DeltaAngle(0f, end)));
        }
    }
}
