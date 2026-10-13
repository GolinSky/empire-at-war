using EmpireAtWar.Components.Ui.Tooltip;
using System.Collections.Generic;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsUi : BaseUi
    {
        private const string UNSAVED_CHANGES_MESSAGE = "Unsaved changes";
        private const string SETTINGS_SCOPE_MESSAGE = "Changes apply across all categories.";

        private ISettingsModelObserver _model;
        private ISettingsPresenter _presenter;

        [SerializeField] private Button closeButton;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private SettingsDropdownRow windowModeRow;
        [SerializeField] private SettingsDropdownRow resolutionRow;
        [SerializeField] private SettingsDropdownRow qualityRow;
        [SerializeField] private SettingsDropdownRow frameRateLimitRow;
        [SerializeField] private SettingsToggleRow vSyncRow;
        [SerializeField] private SettingsSliderRow masterVolumeRow;
        [SerializeField] private SettingsSliderRow musicVolumeRow;
        [SerializeField] private SettingsSliderRow voiceVolumeRow;
        [SerializeField] private SettingsSliderRow sfxVolumeRow;
        [SerializeField] private SettingsToggleRow muteWhenUnfocusedRow;
        [SerializeField] private SettingsSliderRow panSpeedRow;
        [SerializeField] private SettingsSliderRow zoomSpeedRow;
        [SerializeField] private SettingsToggleRow edgeScrollingRow;
        [SerializeField] private SettingsToggleRow invertZoomRow;
        [Tooltip("Inactive row cloned once per rebindable binding into its parent.")]
        [SerializeField] private KeyBindingRow keyBindingRowTemplate;
        [SerializeField] private SettingsPromptView promptView;
        private readonly List<KeyBindingRow> _keyBindingRows = new List<KeyBindingRow>();

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            closeButton.onClick.AddListener(_presenter.Close);
            applyButton.onClick.AddListener(_presenter.ApplySettings);
            discardButton.onClick.AddListener(_presenter.DiscardChanges);
            resetDefaultsButton.onClick.AddListener(_presenter.ResetToDefaults);

            InitializeRows();
            promptView.Initialize();
            promptView.ActionChosen += _presenter.ChoosePromptAction;
            _isInitialized = true;
            Render();
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            closeButton.onClick.RemoveListener(_presenter.Close);
            applyButton.onClick.RemoveListener(_presenter.ApplySettings);
            discardButton.onClick.RemoveListener(_presenter.DiscardChanges);
            resetDefaultsButton.onClick.RemoveListener(_presenter.ResetToDefaults);

            DisposeRows();
            promptView.ActionChosen -= _presenter.ChoosePromptAction;
            promptView.Dispose();
            _isInitialized = false;
        }

        public void SetModel(ISettingsModelObserver model)
        {
            _model = model;
        }

        public void AssignPresenter(ISettingsPresenter presenter)
        {
            if (_presenter != null)
            {
                throw new System.InvalidOperationException("Presenter is already assigned.");
            }
            _presenter = presenter;
        }

        public void Render()
        {
            windowModeRow.Render(_model.WindowMode);
            resolutionRow.Render(_model.Resolution);
            qualityRow.Render(_model.Quality);
            frameRateLimitRow.Render(_model.FrameRateLimit);
            vSyncRow.Render(_model.VSync);

            masterVolumeRow.Render(_model.MasterVolume);
            musicVolumeRow.Render(_model.MusicVolume);
            voiceVolumeRow.Render(_model.VoiceVolume);
            sfxVolumeRow.Render(_model.SfxVolume);
            muteWhenUnfocusedRow.Render(_model.MuteWhenUnfocused);

            panSpeedRow.Render(_model.PanSpeed);
            zoomSpeedRow.Render(_model.ZoomSpeed);
            edgeScrollingRow.Render(_model.EdgeScrolling);
            invertZoomRow.Render(_model.InvertZoom);

            RenderKeyBindings();

            applyButton.interactable = _model.IsDirty;
            discardButton.interactable = _model.IsDirty;
            statusText.text = _model.StatusMessage.Length != 0
                ? _model.StatusMessage
                : _model.IsDirty ? UNSAVED_CHANGES_MESSAGE : SETTINGS_SCOPE_MESSAGE;
            statusText.color = _model.IsDirty ? new Color32(231, 189, 105, 255) : new Color32(123, 156, 175, 255);
            promptView.Render(_model.Prompt);
        }

        private void InitializeRows()
        {
            windowModeRow.Initialize();
            windowModeRow.ValueChanged += _presenter.SelectWindowMode;
            resolutionRow.Initialize();
            resolutionRow.ValueChanged += _presenter.SelectResolution;
            qualityRow.Initialize();
            qualityRow.ValueChanged += _presenter.SelectQualityPreset;
            frameRateLimitRow.Initialize();
            frameRateLimitRow.ValueChanged += _presenter.SelectFrameRateLimit;
            vSyncRow.Initialize();
            vSyncRow.ValueChanged += _presenter.SetVSync;

            masterVolumeRow.Initialize();
            masterVolumeRow.ValueChanged += _presenter.SetMasterVolume;
            musicVolumeRow.Initialize();
            musicVolumeRow.ValueChanged += _presenter.SetMusicVolume;
            voiceVolumeRow.Initialize();
            voiceVolumeRow.ValueChanged += _presenter.SetVoiceVolume;
            sfxVolumeRow.Initialize();
            sfxVolumeRow.ValueChanged += _presenter.SetSfxVolume;
            muteWhenUnfocusedRow.Initialize();
            muteWhenUnfocusedRow.ValueChanged += _presenter.SetMuteWhenUnfocused;

            panSpeedRow.Initialize();
            panSpeedRow.ValueChanged += _presenter.SetPanSpeed;
            zoomSpeedRow.Initialize();
            zoomSpeedRow.ValueChanged += _presenter.SetZoomSpeed;
            edgeScrollingRow.Initialize();
            edgeScrollingRow.ValueChanged += _presenter.SetEdgeScrolling;
            invertZoomRow.Initialize();
            invertZoomRow.ValueChanged += _presenter.SetInvertZoom;
        }

        private void DisposeRows()
        {
            windowModeRow.ValueChanged -= _presenter.SelectWindowMode;
            windowModeRow.Dispose();
            resolutionRow.ValueChanged -= _presenter.SelectResolution;
            resolutionRow.Dispose();
            qualityRow.ValueChanged -= _presenter.SelectQualityPreset;
            qualityRow.Dispose();
            frameRateLimitRow.ValueChanged -= _presenter.SelectFrameRateLimit;
            frameRateLimitRow.Dispose();
            vSyncRow.ValueChanged -= _presenter.SetVSync;
            vSyncRow.Dispose();

            masterVolumeRow.ValueChanged -= _presenter.SetMasterVolume;
            masterVolumeRow.Dispose();
            musicVolumeRow.ValueChanged -= _presenter.SetMusicVolume;
            musicVolumeRow.Dispose();
            voiceVolumeRow.ValueChanged -= _presenter.SetVoiceVolume;
            voiceVolumeRow.Dispose();
            sfxVolumeRow.ValueChanged -= _presenter.SetSfxVolume;
            sfxVolumeRow.Dispose();
            muteWhenUnfocusedRow.ValueChanged -= _presenter.SetMuteWhenUnfocused;
            muteWhenUnfocusedRow.Dispose();

            panSpeedRow.ValueChanged -= _presenter.SetPanSpeed;
            panSpeedRow.Dispose();
            zoomSpeedRow.ValueChanged -= _presenter.SetZoomSpeed;
            zoomSpeedRow.Dispose();
            edgeScrollingRow.ValueChanged -= _presenter.SetEdgeScrolling;
            edgeScrollingRow.Dispose();
            invertZoomRow.ValueChanged -= _presenter.SetInvertZoom;
            invertZoomRow.Dispose();

            foreach (KeyBindingRow row in _keyBindingRows)
            {
                row.Dispose();
            }
        }

        private void RenderKeyBindings()
        {
            IReadOnlyList<KeyBindingRowState> bindings = _model.Bindings;
            while (_keyBindingRows.Count < bindings.Count)
            {
                KeyBindingRow row = Instantiate(keyBindingRowTemplate, keyBindingRowTemplate.transform.parent);
                row.gameObject.SetActive(true);
                row.Initialize(_keyBindingRows.Count, _presenter.StartRebind, _presenter.ResetBinding);
                row.RegisterTooltips(tooltipHover);
                _keyBindingRows.Add(row);
            }

            for (int i = 0; i < _keyBindingRows.Count; i++)
            {
                _keyBindingRows[i].Render(bindings[i]);
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
