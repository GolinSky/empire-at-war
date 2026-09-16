namespace EmpireAtWar.Services.Timing
{
    internal sealed class BattlePerformanceCaptureMetadata
    {
        public BattlePerformanceCaptureMetadata(
            string unityVersion,
            string runtime,
            string quality,
            string resolution,
            float timeScale,
            int vSyncCount,
            int targetFrameRate,
            string graphicsDevice,
            string processor)
        {
            UnityVersion = unityVersion;
            Runtime = runtime;
            Quality = quality;
            Resolution = resolution;
            TimeScale = timeScale;
            VSyncCount = vSyncCount;
            TargetFrameRate = targetFrameRate;
            GraphicsDevice = graphicsDevice;
            Processor = processor;
        }

        public string UnityVersion { get; }
        public string Runtime { get; }
        public string Quality { get; }
        public string Resolution { get; }
        public float TimeScale { get; }
        public int VSyncCount { get; }
        public int TargetFrameRate { get; }
        public string GraphicsDevice { get; }
        public string Processor { get; }
    }
}
