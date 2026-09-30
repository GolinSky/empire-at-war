namespace EmpireAtWar.Services.Settings
{
    /// <summary>Read access to the saved camera preferences. Read on use so an Apply takes effect immediately.</summary>
    public interface ICameraPreferences
    {
        CameraSettingsData Camera { get; }
    }
}
