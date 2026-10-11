using System;
using Unity.Collections;
using Unity.Jobs;

namespace EmpireAtWar.Models.FogOfWar
{
    /// <summary>
    /// Fog visibility per cell. The per-cell passes run as Burst jobs: a 512² battle mask is 262,144 cells
    /// faded and written every frame the fog changes.
    /// </summary>
    public sealed class FogVisibilityGridModel : IDisposable
    {
        private const float HISTORIC_VISIBILITY = 0.35f;

        private NativeArray<float> _current;
        private NativeArray<float> _target;
        private NativeArray<int> _changed;

        public int Resolution { get; }

        public FogVisibilityGridModel(int resolution)
        {
            Resolution = resolution;
            _current = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
            _target = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
            _changed = new NativeArray<int>(1, Allocator.Persistent);
        }

        public void Dispose()
        {
            _current.Dispose();
            _target.Dispose();
            _changed.Dispose();
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
            new FogResetJob
            {
                Current = _current,
                Target = _target,
                BaseVisibility = keepHistory ? HISTORIC_VISIBILITY : 0f
            }.Run();
        }

        public void Reveal(int centerX, int centerY, int radius, float edgeSoftness, float intensity)
        {
            if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            if (edgeSoftness < 0f || edgeSoftness > 1f) throw new ArgumentOutOfRangeException(nameof(edgeSoftness));
            new FogRevealJob
            {
                Target = _target,
                Resolution = Resolution,
                CenterX = centerX,
                CenterY = centerY,
                Radius = radius,
                EdgeSoftness = edgeSoftness,
                Intensity = intensity
            }.Run();
        }

        /// <summary>Moves current visibility toward the targets; returns true when any cell changed.</summary>
        public bool Fade(float maxDelta)
        {
            new FogFadeJob
            {
                Current = _current,
                Target = _target,
                MaxDelta = maxDelta,
                Changed = _changed
            }.Run();
            return _changed[0] != 0;
        }

        /// <summary>Writes visibility as one byte per cell, e.g. into an R8 texture's pixel data.</summary>
        public void WriteBytes(NativeArray<byte> pixels)
        {
            new FogWriteJob { Current = _current, Pixels = pixels }.Run();
        }
    }
}
