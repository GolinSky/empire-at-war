using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Builds hazard clouds as chains of overlapping lobes along a curved spine.</summary>
    public static class HazardCloudGenerator
    {
        private const int MIN_STREAM_LOBES = 7;
        private const int MAX_STREAM_LOBES = 18;
        private const int MIN_HALF_LOBES = 2;
        private const int MAX_HALF_LOBES = 7;
        // Lobes and noise are sized from this share of the requested half-width, so the lobes plus their
        // noisy outline (Spread) stay within about the requested half-width instead of exceeding it.
        private const float CORE_SHARE = 0.6f;
        // Lobe spacing along the spine in core widths; denser lobes keep the minimum lobe width small.
        private const float LOBE_SPACING = 1.6f;
        private const float MAX_ASPECT = 1.8f;
        // Each lobe reaches this share of the spacing along the spine, so neighbours always overlap.
        private const float LOBE_REACH = 0.65f;
        private const float STREAM_BEND = 0.08f;
        private const float ISLAND_BOW = 0.15f;
        private const float NOISE_OFFSET_RANGE = 1000f;

        /// <summary>A free cloud: random bends, no symmetry of its own.</summary>
        public static HazardSpot CreateStream(SpaceHazardData data, Vector3 center, Vector3 along,
            float length, float halfWidth, float cellSize, Random random)
        {
            float width = halfWidth * CORE_SHARE;
            float[] bends = new float[5];
            for (int i = 0; i < bends.Length; i++)
                bends[i] = Range(random, -STREAM_BEND, STREAM_BEND) * length;
            Func<float, float> spine = x => SampleBend(bends, x / length + 0.5f);
            List<LocalLobe> lobes = new List<LocalLobe>();
            int count = Mathf.Clamp(Mathf.RoundToInt(length / (width * LOBE_SPACING)) + random.Next(2),
                MIN_STREAM_LOBES, MAX_STREAM_LOBES);
            float spacing = length / count;
            for (int i = 0; i < count; i++)
            {
                float x = (i + 0.5f + Range(random, -0.2f, 0.2f)) * spacing - length * 0.5f;
                lobes.Add(CreateLobe(x, spine, length, width, spacing, random));
            }
            int puffs = random.Next(2, 4);
            for (int i = 0; i < puffs; i++)
                lobes.Add(CreatePuff(Range(random, -0.3f, 0.3f) * length, spine, length, width, random));
            return CreateSpot(data, center, along, false, lobes, width, cellSize, random);
        }

        /// <summary>A cloud that is mirror-symmetric about the line through <paramref name="center"/>
        /// perpendicular to <paramref name="along"/>, so it is fair to both sides of that line on its own.</summary>
        public static HazardSpot CreateIsland(SpaceHazardData data, Vector3 center, Vector3 along,
            float length, float halfWidth, float cellSize, Random random)
        {
            float width = halfWidth * CORE_SHARE;
            float bow = Range(random, -ISLAND_BOW, ISLAND_BOW) * length;
            Func<float, float> spine = x => bow * (1f - 4f * x * x / (length * length));
            int count = Mathf.Clamp(Mathf.RoundToInt(length / (width * LOBE_SPACING * 2f)) + random.Next(2),
                MIN_HALF_LOBES, MAX_HALF_LOBES);
            float spacing = length / (count * 2 + 1);
            // The middle lobe lies on the mirror line, aligned with the spine there, so it is its own reflection.
            LocalLobe middle = CreateLobe(0f, spine, length, width, spacing, random);
            List<LocalLobe> lobes = new List<LocalLobe> { middle.WithAngle(0f) };
            List<LocalLobe> half = new List<LocalLobe>();
            for (int i = 0; i < count; i++)
                half.Add(CreateLobe((i + 1 + Range(random, -0.15f, 0.15f)) * spacing, spine, length, width, spacing, random));
            half.Add(CreatePuff(Range(random, 0.1f, 0.3f) * length, spine, length, width, random));
            foreach (LocalLobe lobe in half)
            {
                lobes.Add(lobe);
                lobes.Add(lobe.Reflect());
            }
            return CreateSpot(data, center, along, true, lobes, width, cellSize, random);
        }

        private static LocalLobe CreateLobe(float x, Func<float, float> spine, float length, float width,
            float spacing, Random random)
        {
            float across = width * Profile(x, length) * Range(random, 0.75f, 1.15f);
            // Lobes are spaced evenly along the axis; on a steep bend the spine between them is longer.
            float slope = Slope(spine, x);
            float reach = Mathf.Max(spacing * LOBE_REACH / Mathf.Cos(slope), across * Range(random, 1.1f, 1.6f));
            across = Mathf.Max(across, reach / MAX_ASPECT);
            return new LocalLobe(x, spine(x) + Range(random, -0.2f, 0.2f) * across,
                slope + Range(random, -0.25f, 0.25f), reach, across);
        }

        // Side puffs break the outline up with bulges and short arms. Each one reaches back past the spine
        // so it always joins the main body instead of floating free between two lobes.
        private static LocalLobe CreatePuff(float x, Func<float, float> spine, float length, float width, Random random)
        {
            float profile = Profile(x, length);
            float side = random.Next(2) == 0 ? -1f : 1f;
            float across = width * profile * Range(random, 0.45f, 0.6f);
            return new LocalLobe(x, spine(x) + side * across * Range(random, 0.7f, 0.95f),
                Slope(spine, x) + Range(random, -0.6f, 0.6f), across * Range(random, 1.1f, 1.5f), across);
        }

        private static HazardSpot CreateSpot(SpaceHazardData data, Vector3 center, Vector3 along, bool isFolded,
            List<LocalLobe> local, float width, float cellSize, Random random)
        {
            Vector2 noiseOffset = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * NOISE_OFFSET_RANGE;
            HazardFrame frame = new HazardFrame(center, along, isFolded, noiseOffset);
            HazardLobe[] lobes = new HazardLobe[local.Count];
            for (int i = 0; i < lobes.Length; i++)
            {
                LocalLobe lobe = local[i];
                lobes[i] = new HazardLobe(center + frame.ToWorldDirection(new Vector2(lobe.X, lobe.Y)),
                    frame.ToWorldDirection(new Vector2(Mathf.Cos(lobe.Angle), Mathf.Sin(lobe.Angle))),
                    Mathf.Max(lobe.Reach, lobe.Across), lobe.Across);
            }
            return new HazardSpot(data, lobes, frame, width, cellSize);
        }

        // Rounded ends: the tips keep almost half the middle width instead of tapering to a point.
        private static float Profile(float x, float length)
        {
            return 0.45f + 0.55f * Mathf.Sin(Mathf.Clamp01(x / length + 0.5f) * Mathf.PI);
        }

        private static float Slope(Func<float, float> spine, float x)
        {
            return Mathf.Atan((spine(x + 1f) - spine(x - 1f)) * 0.5f);
        }

        private static float SampleBend(IReadOnlyList<float> controls, float t)
        {
            float progress = Mathf.Clamp01(t) * (controls.Count - 1);
            int segment = Mathf.Min(Mathf.FloorToInt(progress), controls.Count - 2);
            float u = progress - segment;
            float a = controls[segment];
            float b = controls[segment + 1];
            float before = segment == 0 ? a * 2f - b : controls[segment - 1];
            float after = segment + 2 == controls.Count ? b * 2f - a : controls[segment + 2];
            return 0.5f * (2f * a + (-before + b) * u + (2f * before - 5f * a + 4f * b - after) * u * u +
                (-before + 3f * a - 3f * b + after) * u * u * u);
        }

        private static float Range(Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        /// <summary>A lobe in spine space: x along, y across, angle from the along axis in radians.</summary>
        private readonly struct LocalLobe
        {
            public float X { get; }
            public float Y { get; }
            public float Angle { get; }
            public float Reach { get; }
            public float Across { get; }

            public LocalLobe(float x, float y, float angle, float reach, float across)
            {
                X = x;
                Y = y;
                Angle = angle;
                Reach = reach;
                Across = across;
            }

            public LocalLobe WithAngle(float angle) => new LocalLobe(X, Y, angle, Reach, Across);

            // Mirror across the line x = 0: the ellipse axis (cos a, sin a) becomes (-cos a, sin a), i.e. angle π - a.
            public LocalLobe Reflect() => new LocalLobe(-X, Y, Mathf.PI - Angle, Reach, Across);
        }
    }
}
