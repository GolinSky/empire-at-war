namespace EmpireAtWar.Services.Analytics
{
    /// <summary>Gameplay statistics sink; gameplay code never calls an analytics SDK directly.</summary>
    public interface IAnalyticsService
    {
        void RecordMatchStarted(MatchStartedData data);
        void RecordMatchCompleted(MatchCompletedData data);
        void RecordMatchAbandoned(MatchAbandonedData data);
        void RecordSurvivalWaveCompleted(SurvivalWaveCompletedData data);
        void RecordPerformanceSample(PerformanceSampleData data);
        /// <summary>Uploads buffered events now instead of on the SDK's own schedule.</summary>
        void Flush();
        /// <summary>Granting starts collection; revoking a granted consent also requests deletion of uploaded data.</summary>
        void SetConsent(bool isGranted);
    }
}
