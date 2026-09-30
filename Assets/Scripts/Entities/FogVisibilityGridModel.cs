using System;
using System.Collections.Generic;

namespace EmpireAtWar.Models.FogOfWar
{
    public sealed class FogVisibilityGridModel
    {
        private const float HISTORIC_VISIBILITY = 0.35f;

        private readonly float[] _current;
        private readonly float[] _target;
        private readonly Dictionary<(int, float), float[]> _falloffs =
            new Dictionary<(int, float), float[]>();

        public int Resolution { get; }

        public FogVisibilityGridModel(int resolution)
        {
            Resolution = resolution;
            _current = new float[resolution * resolution];
            _target = new float[resolution * resolution];
        }

        public float GetVisibility(int index) => _current[index];

        public float GetVisibility(int x, int y)
        {
            int clampedX = Math.Min(Math.Max(x, 0), Resolution - 1);
            int clampedY = Math.Min(Math.Max(y, 0), Resolution - 1);
            return _current[clampedY * Resolution + clampedX];
        }

        public void ResetTargets(bool keepHistory)
        {
            float baseVisibility = keepHistory ? HISTORIC_VISIBILITY : 0f;
            for (int i = 0; i < _target.Length; i++)
            {
                _target[i] = _current[i] > 0f ? baseVisibility : 0f;
            }
        }

        public void Reveal(int centerX, int centerY, int radius, float edgeSoftness, float intensity)
        {
            int outerRadius = (int)Math.Ceiling(radius * (1f + edgeSoftness));
            int minX = Math.Min(Math.Max(centerX - outerRadius, 0), Resolution - 1);
            int maxX = Math.Min(Math.Max(centerX + outerRadius, 0), Resolution - 1);
            int minY = Math.Min(Math.Max(centerY - outerRadius, 0), Resolution - 1);
            int maxY = Math.Min(Math.Max(centerY + outerRadius, 0), Resolution - 1);
            int sqrOuterRadius = outerRadius * outerRadius;
            float[] falloff = GetFalloff(radius, edgeSoftness, sqrOuterRadius);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - centerY;
                int rowOffset = y * Resolution;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    int distSqr = dx * dx + dy * dy;
                    if (distSqr > sqrOuterRadius) continue;

                    float visibility = falloff[distSqr] * intensity;
                    int index = rowOffset + x;
                    if (visibility > _target[index]) _target[index] = visibility;
                }
            }
        }

        // Pixel distances are integers, so the soft falloff only ever takes one value per
        // squared distance. Caching it per radius replaces a sqrt and a falloff evaluation
        // per pixel with an array read; the result is identical.
        private float[] GetFalloff(int radius, float edgeSoftness, int sqrOuterRadius)
        {
            if (_falloffs.TryGetValue((radius, edgeSoftness), out float[] falloff))
            {
                return falloff;
            }

            // The registered radius is the middle of the
            // feather, so its border is exactly half visible.
            falloff = new float[sqrOuterRadius + 1];
            for (int distSqr = 0; distSqr <= sqrOuterRadius; distSqr++)
            {
                falloff[distSqr] = FogVisibilityModel.CalculateSoftVisibility(
                    (float)Math.Sqrt(distSqr), radius, edgeSoftness);
            }

            _falloffs.Add((radius, edgeSoftness), falloff);
            return falloff;
        }

        /// <summary>Moves current visibility toward the targets; returns true when any cell changed.</summary>
        public bool Fade(float maxDelta)
        {
            bool changed = false;
            for (int i = 0; i < _current.Length; i++)
            {
                float delta = _target[i] - _current[i];
                if (delta == 0f) continue;

                _current[i] = Math.Abs(delta) <= maxDelta
                    ? _target[i]
                    : _current[i] + Math.Sign(delta) * maxDelta;
                changed = true;
            }

            return changed;
        }
    }
}
