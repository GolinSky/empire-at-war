namespace EmpireAtWar.Services.Analytics
{
    public sealed class PerformanceSampleData
    {
        public string MatchId { get; }
        public float AverageFps { get; }
        /// <summary>5th-percentile frame rate over the sample window.</summary>
        public float LowFps { get; }
        public int MemoryMb { get; }
        public int UnitCount { get; }
        public int QualityLevel { get; }

        public PerformanceSampleData(string matchId, float averageFps, float lowFps, int memoryMb, int unitCount,
            int qualityLevel)
        {
            MatchId = matchId;
            AverageFps = averageFps;
            LowFps = lowFps;
            MemoryMb = memoryMb;
            UnitCount = unitCount;
            QualityLevel = qualityLevel;
        }
    }
}
