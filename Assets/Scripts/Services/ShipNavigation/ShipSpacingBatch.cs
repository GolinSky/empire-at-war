using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace EmpireAtWar.Services.ShipNavigation
{
    /// <summary>One reusable numeric snapshot and asynchronous solve for the battle.</summary>
    internal sealed class ShipSpacingBatch : IDisposable
    {
        private static readonly ProfilerMarker _indexMarker = new ProfilerMarker("Battle.Spacing.Index");
        private NativeArray<ShipSpacingInput> _inputs;
        private NativeArray<ShipSpacingResult> _results;
        private NativeArray<int> _occupants;
        private NativeParallelHashMap<int2, int2> _ranges;
        private NativeParallelHashMap<int2, int> _writeOffsets;
        private NativeParallelHashMap<int2, uint> _occupancyHashes;
        private JobHandle _handle;

        public NativeArray<ShipSpacingInput> Inputs => _inputs;
        public NativeArray<ShipSpacingResult> Results => _results;
        public bool IsScheduled { get; private set; }
        public int Count { get; private set; }

        public void EnsureCapacity(int count)
        {
            if (_inputs.IsCreated && _inputs.Length >= count) return;
            Complete();
            ReleaseBuffers();
            int capacity = math.ceilpow2(math.max(16, count));
            _inputs = new NativeArray<ShipSpacingInput>(capacity, Allocator.Persistent);
            _results = new NativeArray<ShipSpacingResult>(capacity, Allocator.Persistent);
            _occupants = new NativeArray<int>(capacity, Allocator.Persistent);
            _ranges = new NativeParallelHashMap<int2, int2>(capacity, Allocator.Persistent);
            _writeOffsets = new NativeParallelHashMap<int2, int>(capacity, Allocator.Persistent);
            _occupancyHashes = new NativeParallelHashMap<int2, uint>(capacity, Allocator.Persistent);
        }

        public void Schedule(int count, float maxRadius)
        {
            if (IsScheduled) throw new InvalidOperationException("Consume the spacing batch before rescheduling it.");
            Count = count;
            if (count == 0) return;
            float cellSize = math.max(1f, maxRadius * 2f);
            using (_indexMarker.Auto())
            {
                _ranges.Clear();
                _writeOffsets.Clear();
                _occupancyHashes.Clear();
                for (int i = 0; i < count; i++)
                {
                    int2 cell = (int2)math.floor(_inputs[i].Position / cellSize);
                    _ranges.TryGetValue(cell, out int2 range);
                    _ranges[cell] = new int2(0, range.y + 1);
                    _occupancyHashes.TryGetValue(cell, out uint occupancyHash);
                    _occupancyHashes[cell] = occupancyHash ^ ShipSpacingJob.HashOccupant(_inputs[i]);
                }

                // Assign ranges in input order without enumerator mutation or sorting.
                int start = 0;
                for (int i = 0; i < count; i++)
                {
                    int2 cell = (int2)math.floor(_inputs[i].Position / cellSize);
                    if (_writeOffsets.ContainsKey(cell)) continue;
                    int length = _ranges[cell].y;
                    _ranges[cell] = new int2(start, length);
                    _writeOffsets.Add(cell, start);
                    start += length;
                }
                for (int i = 0; i < count; i++)
                {
                    int2 cell = (int2)math.floor(_inputs[i].Position / cellSize);
                    int offset = _writeOffsets[cell];
                    _occupants[offset] = i;
                    _writeOffsets[cell] = offset + 1;
                }
            }
            _handle = new ShipSpacingJob
            {
                Inputs = _inputs, Results = _results, Ranges = _ranges,
                Occupants = _occupants, OccupancyHashes = _occupancyHashes, CellSize = cellSize
            }.Schedule(count, 32);
            JobHandle.ScheduleBatchedJobs();
            IsScheduled = true;
        }

        public bool TryComplete()
        {
            if (!IsScheduled || !_handle.IsCompleted) return false;
            Complete();
            return true;
        }

        public void Dispose()
        {
            Complete();
            ReleaseBuffers();
        }

        private void Complete()
        {
            if (!IsScheduled) return;
            _handle.Complete();
            IsScheduled = false;
        }

        private void ReleaseBuffers()
        {
            if (!_inputs.IsCreated) return;
            _inputs.Dispose();
            _results.Dispose();
            _occupants.Dispose();
            _ranges.Dispose();
            _writeOffsets.Dispose();
            _occupancyHashes.Dispose();
        }
    }
}
