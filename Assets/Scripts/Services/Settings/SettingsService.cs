using LightWeightFramework.Command;
using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Settings
{
    public interface ISettingsService: IService
    {
        
    }

    public interface ISettingsCommand:ICommand
    {
        void SetQualityPreset(int index);
    }

    public class SettingsService : Service, ISettingsService, IInitializable, ISettingsCommand
    {
        public void Initialize()
        {
            QualitySettings.SetQualityLevel(QualitySettings.count-1);
            Application.backgroundLoadingPriority = ThreadPriority.High;
#if UNITY_EDITOR
            Application.targetFrameRate = 60;
#else
            Application.targetFrameRate = 60;
#endif
        }

        public void SetQualityPreset(int index)
        {
            QualitySettings.SetQualityLevel(index);
        }
    }
}