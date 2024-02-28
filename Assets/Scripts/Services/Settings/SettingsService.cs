using UnityEngine;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Settings
{
    public interface ISettingsService: IService
    {
    }

    public class SettingsService : Service, ISettingsService, IInitializable
    {
        public void Initialize()
        {
            QualitySettings.SetQualityLevel(QualitySettings.count-1);
            Application.backgroundLoadingPriority = ThreadPriority.High;
#if UNITY_EDITOR
            Application.targetFrameRate = -1;
#else 
            Application.targetFrameRate = 60;
#endif
        }
    }
}