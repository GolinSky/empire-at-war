namespace EmpireAtWar.Services.Settings
{
    public enum SettingsLoadStatus
    {
        Loaded = 0,
        /// <summary>No settings file exists yet (first run).</summary>
        Missing = 1,
        /// <summary>A file exists but neither it nor its backup could be read; it must not be overwritten on load.</summary>
        Unreadable = 2,
    }
}
