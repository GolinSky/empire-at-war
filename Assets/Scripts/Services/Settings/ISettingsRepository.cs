namespace EmpireAtWar.Services.Settings
{
    public interface ISettingsRepository
    {
        /// <summary>Returns false when no settings file exists yet.</summary>
        bool TryLoad(out SettingsData data);

        void Save(SettingsData data);
    }
}
