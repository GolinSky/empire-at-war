using Unity.Services.Analytics;

namespace EmpireAtWar.Services.Analytics
{
    /// <summary>Custom UGS event filled by name; parameter names must match the dashboard schema.</summary>
    public sealed class UgsAnalyticsEvent : Event
    {
        public UgsAnalyticsEvent(string name) : base(name)
        {
        }

        public void Set(string name, string value) => SetParameter(name, value);
        public void Set(string name, int value) => SetParameter(name, value);
        public void Set(string name, float value) => SetParameter(name, value);
        public void Set(string name, bool value) => SetParameter(name, value);
    }
}
