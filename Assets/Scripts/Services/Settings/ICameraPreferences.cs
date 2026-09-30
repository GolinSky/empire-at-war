namespace EmpireAtWar.Services.Settings
{
    /// <summary>Read-only saved camera preferences. Read on use so an Apply takes effect immediately.</summary>
    public interface ICameraPreferences
    {
        float PanSpeedMultiplier { get; }
        float ZoomSpeedMultiplier { get; }
        bool EdgeScrolling { get; }
        bool InvertZoom { get; }
    }
}
