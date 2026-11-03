using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>Registered in the Editor (unless ANALYTICS_EDITOR is defined) so Play Mode sessions never reach the dashboards.</summary>
    public sealed class DisabledAnalyticsService : Service, IAnalyticsService
    {
        public void RecordMatchStarted(MatchStartedData data)
        {
        }

        public void RecordMatchCompleted(MatchCompletedData data)
        {
        }

        public void RecordMatchAbandoned(MatchAbandonedData data)
        {
        }

        public void RecordSurvivalWaveCompleted(SurvivalWaveCompletedData data)
        {
        }

        public void RecordPerformanceSample(PerformanceSampleData data)
        {
        }

        public void Flush()
        {
        }

        public void SetConsent(bool isGranted)
        {
        }
    }
}
