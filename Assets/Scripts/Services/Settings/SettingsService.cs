using System;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Settings
{
    public class SettingsService : Service, ISettingsService, IInitializable
    {
        private const string QUALITY_PRESET_KEY = "QualityPreset";
        private const string INPUT_BINDINGS_KEY = "InputBindingOverrides";

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
            Application.targetFrameRate = -1;
        }

        public void SetQualityPreset(int index)
        {
            string presetName = QualitySettings.names[index];
            QualitySettings.SetQualityLevel(index);
            PlayerPrefs.SetString(QUALITY_PRESET_KEY, presetName);
            PlayerPrefs.Save();
        }

        public string[] GetQualityPresets()
        {
            return QualitySettings.names;
        }

        public int GetCurrentQualityPresetIndex()
        {
            return QualitySettings.GetQualityLevel();
        }

        public string LoadInputBindingOverrides()
        {
            return PlayerPrefs.GetString(INPUT_BINDINGS_KEY, string.Empty);
        }

        public void SaveInputBindingOverrides(string overridesJson)
        {
            PlayerPrefs.SetString(INPUT_BINDINGS_KEY, overridesJson);
            PlayerPrefs.Save();
        }
    }
}
