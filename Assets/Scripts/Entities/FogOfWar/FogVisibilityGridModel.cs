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
        private const int REVEAL_ROWS_PER_BATCH = 16;

        private NativeArray<float> _current;
        private NativeArray<float> _target;
        private NativeArray<int> _changed;
        private NativeList<FogRevealCircle> _reveals;
        private JobHandle _pendingWrite;
        private bool _writePending;
        private bool _clearReveals;

        public int Resolution { get; }

        public FogVisibilityGridModel(int resolution)
        {
            Resolution = resolution;
            _current = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
            _target = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
            _changed = new NativeArray<int>(1, Allocator.Persistent);
            _reveals = new NativeList<FogRevealCircle>(Allocator.Persistent);
        }

        public void Dispose()
        {
            CompleteWrite();
            _current.Dispose();
            _target.Dispose();
            _changed.Dispose();
            _reveals.Dispose();
        }

        public float GetVisibility(int index)
        {
            CompleteWrite();
            return _current[index];
        }

        public float GetVisibility(int x, int y)
        {
            CompleteWrite();
            int clampedX = Math.Min(Math.Max(x, 0), Resolution - 1);
            int clampedY = Math.Min(Math.Max(y, 0), Resolution - 1);
            return _current[clampedY * Resolution + clampedX];
        }

        public void ResetTargets(bool keepHistory)
        {
            CompleteWrite();
            new FogResetJob
            {
                Current = _current,
                Target = _target,
                BaseVisibility = keepHistory ? HISTORIC_VISIBILITY : 0f
            }.Run();
        }

        public void Reveal(int centerX, int centerY, int radius, float edgeSoftness, float intensity)
        {
            QueueReveal(centerX, centerY, radius);
            ApplyReveals(edgeSoftness, intensity);
        }

        /// <summary>Queues one vision circle; <see cref="ApplyReveals"/> raises all queued circles in one job.</summary>
        public void QueueReveal(int centerX, int centerY, int radius)
        {
            CompleteWrite();
            if (radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            _reveals.Add(new FogRevealCircle { CenterX = centerX, CenterY = centerY, Radius = radius });
        }

        public void ApplyReveals(float edgeSoftness, float intensity)
        {
            CompleteWrite();
            if (edgeSoftness < 0f || edgeSoftness > 1f) throw new ArgumentOutOfRangeException(nameof(edgeSoftness));
            if (_reveals.Length == 0) return;
            new FogRevealJob
            {
                Target = _target,
                Circles = _reveals.AsArray(),
                Resolution = Resolution,
                EdgeSoftness = edgeSoftness,
                Intensity = intensity
            }.Schedule(Resolution, REVEAL_ROWS_PER_BATCH).Complete();
            _reveals.Clear();
        }

        /// <summary>Rebuilds optional targets, fades and writes the mask with one completion before upload.</summary>
        public bool UpdateAndWriteBytes(float maxDelta, NativeArray<byte> pixels,
            bool rebuildTargets, bool keepHistory, float edgeSoftness, float intensity)
        {
            ScheduleUpdateAndWriteBytes(maxDelta, pixels, rebuildTargets, keepHistory, edgeSoftness, intensity);
            return CompleteWrite();
        }

        public void ScheduleUpdateAndWriteBytes(float maxDelta, NativeArray<byte> pixels,
            bool rebuildTargets, bool keepHistory, float edgeSoftness, float intensity)
        {
            CompleteWrite();
            if (edgeSoftness < 0f || edgeSoftness > 1f) throw new ArgumentOutOfRangeException(nameof(edgeSoftness));
            JobHandle handle = default;
            if (rebuildTargets)
            {
                handle = new FogResetJob
                {
                    Current = _current,
                    Target = _target,
                    BaseVisibility = keepHistory ? HISTORIC_VISIBILITY : 0f
                }.Schedule();
                if (_reveals.Length != 0)
                {
                    handle = new FogRevealJob
                    {
                        Target = _target,
                        Circles = _reveals.AsArray(),
                        Resolution = Resolution,
                        EdgeSoftness = edgeSoftness,
                        Intensity = intensity
                    }.Schedule(Resolution, REVEAL_ROWS_PER_BATCH, handle);
                }
            }

            _pendingWrite = new FogFadeWriteJob
            {
                Current = _current,
                Target = _target,
                MaxDelta = maxDelta,
                Changed = _changed,
                Pixels = pixels
            }.Schedule(handle);
            _writePending = true;
            _clearReveals = rebuildTargets;
            JobHandle.ScheduleBatchedJobs();
        }

        public bool CompleteWrite()
        {
            if (_writePending)
            {
                _pendingWrite.Complete();
                _writePending = false;
                if (_clearReveals) _reveals.Clear();
            }
            return _changed[0] != 0;
        }

        /// <summary>Moves current visibility toward the targets; returns true when any cell changed.</summary>
        public bool Fade(float maxDelta)
        {
            CompleteWrite();
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
            CompleteWrite();
            new FogWriteJob { Current = _current, Pixels = pixels }.Run();
        }
    }
}
