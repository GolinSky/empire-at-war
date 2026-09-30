namespace EmpireAtWar.Services.Settings
{
    public interface ISettingsRepository
    {
        SettingsLoadStatus Load(out SettingsData data);

        /// <summary>Throws <see cref="System.IO.IOException"/> or <see cref="System.UnauthorizedAccessException"/> when the file cannot be written.</summary>
        void Save(SettingsData data);
    }
}
