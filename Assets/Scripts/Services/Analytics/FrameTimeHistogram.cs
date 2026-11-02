using System;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>Fixed-size frame-time histogram; adding a frame never allocates.</summary>
    public sealed class FrameTimeHistogram
    {
        private const float BUCKET_SECONDS = 0.00025f;
        private const int BUCKET_COUNT = 400;
        private const float LOW_PERCENTILE = 0.95f;

        // The last bucket collects every frame slower than 100 ms.
        private readonly int[] _buckets = new int[BUCKET_COUNT + 1];
        private double _totalSeconds;
        private float _slowestSeconds;

        public int Count { get; private set; }
        public float TotalSeconds => (float)_totalSeconds;
        public float AverageFps => (float)(Count / _totalSeconds);

        public void Add(float frameSeconds)
        {
            _buckets[Math.Min((int)(frameSeconds / BUCKET_SECONDS), BUCKET_COUNT)]++;
            _totalSeconds += frameSeconds;
            _slowestSeconds = Math.Max(_slowestSeconds, frameSeconds);
            Count++;
        }

        /// <summary>Frame rate of the 95th-percentile frame time ("5% low"); bucket upper bounds keep it conservative.</summary>
        public float GetLowFps()
        {
            int target = (int)Math.Ceiling(Count * LOW_PERCENTILE);
            int seen = 0;
            for (int i = 0; i < BUCKET_COUNT; i++)
            {
                seen += _buckets[i];
                if (seen >= target) return 1f / ((i + 1) * BUCKET_SECONDS);
            }

            return 1f / _slowestSeconds;
        }

        public void Reset()
        {
            Array.Clear(_buckets, 0, _buckets.Length);
            _totalSeconds = 0;
            _slowestSeconds = 0f;
            Count = 0;
        }
    }
}
