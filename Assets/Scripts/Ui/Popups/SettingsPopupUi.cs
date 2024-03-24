using EmpireAtWar.Services.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Ui.Popups
{
    public class SettingsPopupUi: PopupUi
    {
        [SerializeField] private TMP_Dropdown qualitySettingsDropdown;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button closeButton;
        
        [Inject]
        private ISettingsCommand SettingsCommand { get; }
        
        public override void Initialize()
        {
            base.Initialize();
            qualitySettingsDropdown.options.Clear();
            foreach (string qualityPreset in QualitySettings.names)
            {
                qualitySettingsDropdown.options.Add(new TMP_Dropdown.OptionData(qualityPreset));
            }
            applyButton.onClick.AddListener(ApplySettings);
            closeButton.onClick.AddListener(ClosePopup);
        }

        public override void LateDispose()
        {
            base.LateDispose();
            applyButton.onClick.RemoveListener(ApplySettings);
            closeButton.onClick.RemoveListener(ClosePopup);

        }

        private void ApplySettings()
        {
            SettingsCommand.SetQualityPreset(qualitySettingsDropdown.value);
        }
    }
}