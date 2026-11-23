using System;

namespace EmpireAtWar.Entities.PerformanceOverlay
{
    public sealed class PerformanceOverlayModel
    {
        private const float SAMPLE_INTERVAL_SECONDS = 0.5f;

        private float _elapsedSeconds;

        private int _frameCount;

        public event Action<int> OnFramesPerSecondChanged;

        public event Action<int> OnPingChanged;

        public int FramesPerSecond { get; private set; }

        public int PingMs { get; private set; }

        /// <summary>Returns true when a sample window closed and <see cref="FramesPerSecond"/> was published.</summary>
        public bool SampleFrame(float unscaledDeltaTime)
        {
            _elapsedSeconds += unscaledDeltaTime;
            _frameCount++;

            if (_elapsedSeconds < SAMPLE_INTERVAL_SECONDS)
            {
                return false;
            }

            FramesPerSecond = (int)Math.Round(_frameCount / _elapsedSeconds);
            _elapsedSeconds = 0f;
            _frameCount = 0;
            OnFramesPerSecondChanged?.Invoke(FramesPerSecond);
            return true;
        }

        public void SetPing(int pingMs)
        {
            PingMs = pingMs;
            OnPingChanged?.Invoke(PingMs);
        }
    }
}
