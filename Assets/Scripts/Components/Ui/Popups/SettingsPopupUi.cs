using System;
using EmpireAtWar.Entities.MenuUi.Popups;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Ui.Popups
{
    public class SettingsPopupUi : BaseUi, ISettingsPopupUi
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Dropdown qualitySettingsDropdown;
        [SerializeField] private Button applyButton;

        private ISettingsPopupModelObserver _model;
        private ISettingsPopupPresenter _presenter;
        private bool _isInitialized;

        public void SetModel(ISettingsPopupModelObserver model)
        {
            _model = model;
        }

        public void SetPresenter(ISettingsPopupPresenter presenter)
        {
            _presenter = presenter;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_model == null || _presenter == null)
            {
                throw new InvalidOperationException("Settings popup dependencies must be set before initialization.");
            }

            Render();
            closeButton.onClick.AddListener(_presenter.CloseSettings);
            qualitySettingsDropdown.onValueChanged.AddListener(_presenter.SelectQualityPreset);
            applyButton.onClick.AddListener(_presenter.ApplySettings);
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

            closeButton.onClick.RemoveListener(_presenter.CloseSettings);
            qualitySettingsDropdown.onValueChanged.RemoveListener(_presenter.SelectQualityPreset);
            applyButton.onClick.RemoveListener(_presenter.ApplySettings);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
