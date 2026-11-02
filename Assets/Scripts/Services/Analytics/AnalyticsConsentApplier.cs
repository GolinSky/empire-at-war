using EmpireAtWar.Services.Settings;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>Pushes the saved usage-data answer into the analytics SDK; an unanswered consent stays unspecified.</summary>
    public sealed class AnalyticsConsentApplier : ISettingsApplier
    {
        private readonly IAnalyticsService _analytics;

        public AnalyticsConsentApplier(IAnalyticsService analytics)
        {
            _analytics = analytics;
        }

        public void Apply(SettingsData settings)
        {
            if (settings.Privacy.HasAnsweredConsent) _analytics.SetConsent(settings.Privacy.ShareUsageData);
        }
    }
}
