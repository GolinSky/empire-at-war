namespace EmpireAtWar.Services.Settings
{
    /// <summary>
    /// Pushes one settings section into engine state. Implementations live with the system they drive
    /// and must be idempotent: applying values that are already active changes nothing.
    /// </summary>
    public interface ISettingsApplier
    {
        void Apply(SettingsData settings);
    }
}
