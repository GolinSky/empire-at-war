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
            string processor,
            string sourceRevision,
            string sourceState,
            string buildGuid,
            string scenario,
            int targetThreshold,
            int targetBatchSize,
            int dueThreshold,
            int dueBatchSize)
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
            SourceRevision = sourceRevision;
            SourceState = sourceState;
            BuildGuid = buildGuid;
            Scenario = scenario;
            TargetThreshold = targetThreshold;
            TargetBatchSize = targetBatchSize;
            DueThreshold = dueThreshold;
            DueBatchSize = dueBatchSize;
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
        public string SourceRevision { get; }
        public string SourceState { get; }
        public string BuildGuid { get; }
        public string Scenario { get; }
        public int TargetThreshold { get; }
        public int TargetBatchSize { get; }
        public int DueThreshold { get; }
        public int DueBatchSize { get; }
    }
}
