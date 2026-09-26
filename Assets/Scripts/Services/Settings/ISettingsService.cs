using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Settings
{
    public interface ISettingsService : IService
    {
        string[] GetQualityPresets();
        int GetCurrentQualityPresetIndex();
        void SetQualityPreset(int index);
    }
}
