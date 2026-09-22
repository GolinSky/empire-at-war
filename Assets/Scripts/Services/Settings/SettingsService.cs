using System;
using EmpireAtWar.Mvc;
using UnityEngine;
using EmpireAtWar.Mvc;
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
        private const string QUALITY_PRESET_KEY = "QualityPreset";

        public void Initialize()
        {
            if (PlayerPrefs.HasKey(QUALITY_PRESET_KEY))
            {
                string savedPreset = PlayerPrefs.GetString(QUALITY_PRESET_KEY);
                int savedIndex = Array.IndexOf(QualitySettings.names, savedPreset);
                if (savedIndex >= 0)
                {
                    QualitySettings.SetQualityLevel(savedIndex);
                }
            }

            Application.backgroundLoadingPriority = ThreadPriority.High;
#if UNITY_EDITOR
            Application.targetFrameRate = 60;
#else
            Application.targetFrameRate = 60;
#endif
        }

        public void SetQualityPreset(int index)
        {
            string presetName = QualitySettings.names[index];
            QualitySettings.SetQualityLevel(index);
            PlayerPrefs.SetString(QUALITY_PRESET_KEY, presetName);
            PlayerPrefs.Save();
        }
    }
}
