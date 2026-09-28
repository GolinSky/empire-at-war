using System;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsUi : BaseUi, ISettingsUi
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Dropdown qualitySettingsDropdown;
        [SerializeField] private Button applyButton;

        private ISettingsModelObserver _model;
        private ISettingsRouteNavigation _navigation;
        private bool _isInitialized;

        public void SetModel(ISettingsModelObserver model)
        {
            _model = model;
        }

        public void SetNavigation(ISettingsRouteNavigation navigation)
        {
            _navigation = navigation;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_model == null || _navigation == null)
            {
                throw new InvalidOperationException("Settings UI dependencies must be set before initialization.");
            }

            Render();
            closeButton.onClick.AddListener(_navigation.Close);
            qualitySettingsDropdown.onValueChanged.AddListener(_navigation.SelectQualityPreset);
            applyButton.onClick.AddListener(_navigation.ApplySettings);
            _isInitialized = true;
        }

        public void Render()
        {
            qualitySettingsDropdown.options.Clear();
            foreach (string qualityPreset in _model.QualityPresets)
            {
                qualitySettingsDropdown.options.Add(new TMP_Dropdown.OptionData(qualityPreset));
            }

            qualitySettingsDropdown.SetValueWithoutNotify(_model.SelectedIndex);
            qualitySettingsDropdown.RefreshShownValue();
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            closeButton.onClick.RemoveListener(_navigation.Close);
            qualitySettingsDropdown.onValueChanged.RemoveListener(_navigation.SelectQualityPreset);
            applyButton.onClick.RemoveListener(_navigation.ApplySettings);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
