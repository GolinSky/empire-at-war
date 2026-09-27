using System;

namespace EmpireAtWar.Models.FogOfWar
{
    public sealed class FogVisibilityGridModel
    {
        private const float HISTORIC_VISIBILITY = 0.35f;

        private readonly float[] _current;
        private readonly float[] _target;

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
            float sqrOuterRadius = outerRadius * outerRadius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distSqr = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    if (distSqr > sqrOuterRadius) continue;

                    // The registered radius is the middle of the
                    // feather, so its border is exactly half visible.
                    float visibility = FogVisibilityModel.CalculateSoftVisibility(
                        (float)Math.Sqrt(distSqr), radius, edgeSoftness) * intensity;
                    int index = y * Resolution + x;
                    _target[index] = Math.Max(_target[index], visibility);
                }
            }
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
