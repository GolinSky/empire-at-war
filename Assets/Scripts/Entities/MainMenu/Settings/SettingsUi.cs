using EmpireAtWar.Components.Ui.Tooltip;
using System.Collections.Generic;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsUi : BaseUi, ISettingsUi, ITooltipHoverView
    {
        private const string UNSAVED_CHANGES_MESSAGE = "Unsaved changes";
        private const string SETTINGS_SCOPE_MESSAGE = "Changes apply across all categories.";

        [SerializeField] private Button closeButton;
        [SerializeField] private TooltipHoverView tooltipHover;
        public TooltipHoverView TooltipHover => tooltipHover;
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

            closeButton.onClick.AddListener(_navigation.Close);
            applyButton.onClick.AddListener(_navigation.ApplySettings);
            discardButton.onClick.AddListener(_navigation.DiscardChanges);
            resetDefaultsButton.onClick.AddListener(_navigation.ResetToDefaults);

            InitializeRows();
            promptView.Initialize();
            promptView.ActionChosen += _navigation.ChoosePromptAction;
            _model.Changed += Render;
            _isInitialized = true;
            Render();
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

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            closeButton.onClick.RemoveListener(_navigation.Close);
            applyButton.onClick.RemoveListener(_navigation.ApplySettings);
            discardButton.onClick.RemoveListener(_navigation.DiscardChanges);
            resetDefaultsButton.onClick.RemoveListener(_navigation.ResetToDefaults);

            DisposeRows();
            promptView.ActionChosen -= _navigation.ChoosePromptAction;
            promptView.Dispose();
            _model.Changed -= Render;
            _isInitialized = false;
        }

        private void InitializeRows()
        {
            windowModeRow.Initialize();
            windowModeRow.ValueChanged += _navigation.SelectWindowMode;
            resolutionRow.Initialize();
            resolutionRow.ValueChanged += _navigation.SelectResolution;
            qualityRow.Initialize();
            qualityRow.ValueChanged += _navigation.SelectQualityPreset;
            frameRateLimitRow.Initialize();
            frameRateLimitRow.ValueChanged += _navigation.SelectFrameRateLimit;
            vSyncRow.Initialize();
            vSyncRow.ValueChanged += _navigation.SetVSync;

            masterVolumeRow.Initialize();
            masterVolumeRow.ValueChanged += _navigation.SetMasterVolume;
            musicVolumeRow.Initialize();
            musicVolumeRow.ValueChanged += _navigation.SetMusicVolume;
            voiceVolumeRow.Initialize();
            voiceVolumeRow.ValueChanged += _navigation.SetVoiceVolume;
            sfxVolumeRow.Initialize();
            sfxVolumeRow.ValueChanged += _navigation.SetSfxVolume;
            muteWhenUnfocusedRow.Initialize();
            muteWhenUnfocusedRow.ValueChanged += _navigation.SetMuteWhenUnfocused;

            panSpeedRow.Initialize();
            panSpeedRow.ValueChanged += _navigation.SetPanSpeed;
            zoomSpeedRow.Initialize();
            zoomSpeedRow.ValueChanged += _navigation.SetZoomSpeed;
            edgeScrollingRow.Initialize();
            edgeScrollingRow.ValueChanged += _navigation.SetEdgeScrolling;
            invertZoomRow.Initialize();
            invertZoomRow.ValueChanged += _navigation.SetInvertZoom;
        }

        private void DisposeRows()
        {
            windowModeRow.ValueChanged -= _navigation.SelectWindowMode;
            windowModeRow.Dispose();
            resolutionRow.ValueChanged -= _navigation.SelectResolution;
            resolutionRow.Dispose();
            qualityRow.ValueChanged -= _navigation.SelectQualityPreset;
            qualityRow.Dispose();
            frameRateLimitRow.ValueChanged -= _navigation.SelectFrameRateLimit;
            frameRateLimitRow.Dispose();
            vSyncRow.ValueChanged -= _navigation.SetVSync;
            vSyncRow.Dispose();

            masterVolumeRow.ValueChanged -= _navigation.SetMasterVolume;
            masterVolumeRow.Dispose();
            musicVolumeRow.ValueChanged -= _navigation.SetMusicVolume;
            musicVolumeRow.Dispose();
            voiceVolumeRow.ValueChanged -= _navigation.SetVoiceVolume;
            voiceVolumeRow.Dispose();
            sfxVolumeRow.ValueChanged -= _navigation.SetSfxVolume;
            sfxVolumeRow.Dispose();
            muteWhenUnfocusedRow.ValueChanged -= _navigation.SetMuteWhenUnfocused;
            muteWhenUnfocusedRow.Dispose();

            panSpeedRow.ValueChanged -= _navigation.SetPanSpeed;
            panSpeedRow.Dispose();
            zoomSpeedRow.ValueChanged -= _navigation.SetZoomSpeed;
            zoomSpeedRow.Dispose();
            edgeScrollingRow.ValueChanged -= _navigation.SetEdgeScrolling;
            edgeScrollingRow.Dispose();
            invertZoomRow.ValueChanged -= _navigation.SetInvertZoom;
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
                row.Initialize(_keyBindingRows.Count, _navigation.StartRebind, _navigation.ResetBinding);
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
