using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>Quarter-millisecond buckets below 100 ms; keep the slow tail at its measured precision.</summary>
    public sealed class FrameTimeHistogram
    {
        private const float BUCKET_SECONDS = 0.00025f;
        private const int BUCKET_COUNT = 400;
        internal const float HITCH_SECONDS = 0.05f;
        // At most 600 frames >=100 ms in a minute, plus the frame crossing the boundary.
        private const int SLOW_FRAME_CAPACITY = 601;

        private readonly int[] _buckets = new int[BUCKET_COUNT];
        private readonly List<float> _slowFrames = new List<float>(SLOW_FRAME_CAPACITY);
        private double _totalSeconds;

        public int Count { get; private set; }
        public float TotalSeconds => (float)_totalSeconds;
        public float AverageFps => (float)(Count / _totalSeconds);
        public float WorstSeconds { get; private set; }
        public float WorstEndSeconds { get; private set; }
        public int HitchCount { get; private set; }

        public void Add(float frameSeconds)
        {
            int bucket = (int)(frameSeconds / BUCKET_SECONDS);
            if (bucket < BUCKET_COUNT) _buckets[bucket]++;
            else _slowFrames.Add(frameSeconds);
            _totalSeconds += frameSeconds;
            if (frameSeconds > WorstSeconds)
            {
                WorstSeconds = frameSeconds;
                WorstEndSeconds = TotalSeconds;
            }
            if (frameSeconds > HITCH_SECONDS) HitchCount++;
            Count++;
        }

        /// <summary>Frame rate of the 95th-percentile frame time ("5% low"); bucket upper bounds keep it conservative.</summary>
        public float GetLowFps() => 1f / PercentileSeconds(0.95);

        public float PercentileSeconds(double percentile)
        {
            int target = (int)Math.Ceiling(Count * percentile);
            int seen = 0;
            for (int i = 0; i < BUCKET_COUNT; i++)
            {
                seen += _buckets[i];
                if (seen >= target) return (i + 1) * BUCKET_SECONDS;
            }

            _slowFrames.Sort();
            return _slowFrames[target - seen - 1];
        }

        public void Reset()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _totalSeconds = 0;
            _slowFrames.Clear();
            WorstSeconds = WorstEndSeconds = 0f;
            HitchCount = 0;
            Count = 0;
        }
    }
}
