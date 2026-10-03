using System;

namespace EmpireAtWar.Entities.Fps
{
    public sealed class FpsModel
    {
        private const float SAMPLE_INTERVAL_SECONDS = 0.5f;

        private float _elapsedSeconds;

        private int _frameCount;

        public event Action<int> OnFramesPerSecondChanged;

        public int FramesPerSecond { get; private set; }

        public void SampleFrame(float unscaledDeltaTime)
        {
            _elapsedSeconds += unscaledDeltaTime;
            _frameCount++;

            if (_elapsedSeconds < SAMPLE_INTERVAL_SECONDS)
            {
                return;
            }

            FramesPerSecond = (int)Math.Round(_frameCount / _elapsedSeconds);
            _elapsedSeconds = 0f;
            _frameCount = 0;
            OnFramesPerSecondChanged?.Invoke(FramesPerSecond);
        }
    }
}
