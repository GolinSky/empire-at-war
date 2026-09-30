using System;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsRouteController : UiController, ISettingsRoute, ISettingsRouteNavigation, ITickable, ILateDisposable
    {
        private const float DISPLAY_CONFIRMATION_SECONDS = 15f;
        private const string APPLIED_MESSAGE = "Settings applied.";
        private const string DISCARDED_MESSAGE = "Changes discarded.";
        private const string DEFAULTS_MESSAGE = "Defaults restored. Apply to keep them.";
        private const string DISPLAY_REVERTED_MESSAGE = "Display change reverted.";
        private const string CONFLICTS_MESSAGE = "Resolve key binding conflicts before applying.";

        private static readonly SettingsPromptAction[] DISPLAY_ACTIONS =
            { SettingsPromptAction.Revert, SettingsPromptAction.Keep };

        private static readonly SettingsPromptAction[] UNSAVED_ACTIONS =
            { SettingsPromptAction.Apply, SettingsPromptAction.Discard, SettingsPromptAction.Stay };

        private readonly ISettingsService _settingsService;
        private readonly IInputBindings _bindings;
        private readonly SettingsDraftEditor _draftEditor;
        private readonly KeyBindingEditor _keyBindingEditor;
        private readonly SettingsModel _model;

        private ISettingsUi _ui;
        private float _displayRevertTime;
        private int _shownSecondsLeft;

        public SettingsRouteController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            ISettingsService settingsService,
            IInputBindings bindings,
            SettingsDraftEditor draftEditor,
            KeyBindingEditor keyBindingEditor,
            SettingsModel model) : base(uiService, cancelRouter)
        {
            _settingsService = settingsService;
            _bindings = bindings;
            _draftEditor = draftEditor;
            _keyBindingEditor = keyBindingEditor;
            _model = model;
        }

        public void Open()
        {
            _model.SetStatus(string.Empty);
            _model.SetPrompt(SettingsPrompt.None);
            _draftEditor.Refresh();
            _keyBindingEditor.Refresh();

            if (_ui == null)
            {
                BaseUi ui = UiService.CreateUi(UiType.Settings);
                _ui = ui as ISettingsUi
                    ?? throw new InvalidOperationException(
                        "The settings prefab does not implement ISettingsUi.");
                _ui.SetModel(_model);
                _ui.SetNavigation(this);
                _ui.Initialize();
            }
            else
            {
                _ui.Render();
            }

            _ui.Show();
            Focus();
        }

        /// <summary>Unsaved changes ask Apply / Discard / Stay instead of closing.</summary>
        public void Close()
        {
            if (_settingsService.IsDirty)
            {
                _model.SetPrompt(new SettingsPrompt(
                    SettingsPromptKind.UnsavedChanges, "You have unsaved changes.", UNSAVED_ACTIONS));
                return;
            }

            _ui.Hide();
            Unfocus();
        }

        // Escape answers the open prompt first; only a screen without a prompt closes.
        protected override bool HandleCancel()
        {
            switch (_model.Prompt.Kind)
            {
                case SettingsPromptKind.ListeningForKey:
                    break;
                case SettingsPromptKind.BindingConflict:
                    ChoosePromptAction(SettingsPromptAction.Cancel);
                    break;
                case SettingsPromptKind.DisplayConfirmation:
                    ChoosePromptAction(SettingsPromptAction.Revert);
                    break;
                case SettingsPromptKind.UnsavedChanges:
                    ChoosePromptAction(SettingsPromptAction.Stay);
                    break;
                default:
                    Close();
                    break;
            }

            return true;
        }

        public void ApplySettings()
        {
            if (_bindings.HasConflicts())
            {
                _model.SetStatus(CONFLICTS_MESSAGE);
                return;
            }

            if (_settingsService.Apply() == SettingsApplyResult.AwaitingDisplayConfirmation)
            {
                _displayRevertTime = Time.unscaledTime + DISPLAY_CONFIRMATION_SECONDS;
                _shownSecondsLeft = -1;
                UpdateDisplayCountdown();
                return;
            }

            ShowResult(APPLIED_MESSAGE);
        }

        public void DiscardChanges()
        {
            _settingsService.Discard();
            ShowResult(DISCARDED_MESSAGE);
        }

        public void ResetToDefaults()
        {
            _settingsService.ResetDraftToDefaults();
            _keyBindingEditor.ResetAll();
            ShowResult(DEFAULTS_MESSAGE);
        }

        public void SelectQualityPreset(int index) => _draftEditor.SelectQuality(index);
        public void SelectWindowMode(int index) => _draftEditor.SelectWindowMode(index);
        public void SelectResolution(int index) => _draftEditor.SelectResolution(index);
        public void SelectFrameRateLimit(int index) => _draftEditor.SelectFrameRateLimit(index);
        public void SetVSync(bool isOn) => _draftEditor.SetVSync(isOn);
        public void SetPanSpeed(float multiplier) => _draftEditor.SetPanSpeed(multiplier);
        public void SetZoomSpeed(float multiplier) => _draftEditor.SetZoomSpeed(multiplier);
        public void SetEdgeScrolling(bool isOn) => _draftEditor.SetEdgeScrolling(isOn);
        public void SetInvertZoom(bool isOn) => _draftEditor.SetInvertZoom(isOn);
        public void StartRebind(int row) => _keyBindingEditor.StartRebind(row);
        public void ResetBinding(int row) => _keyBindingEditor.ResetBinding(row);

        public void ChoosePromptAction(SettingsPromptAction action)
        {
            switch (action)
            {
                case SettingsPromptAction.Keep:
                    _settingsService.KeepDisplay();
                    ShowResult(APPLIED_MESSAGE);
                    break;
                case SettingsPromptAction.Revert:
                    _settingsService.RevertDisplay();
                    ShowResult(DISPLAY_REVERTED_MESSAGE);
                    break;
                case SettingsPromptAction.Replace:
                    _keyBindingEditor.ResolveConflict(ConflictResolution.Replace);
                    break;
                case SettingsPromptAction.Swap:
                    _keyBindingEditor.ResolveConflict(ConflictResolution.Swap);
                    break;
                case SettingsPromptAction.Cancel:
                    _keyBindingEditor.ResolveConflict(ConflictResolution.Cancel);
                    break;
                case SettingsPromptAction.Apply:
                    _model.SetPrompt(SettingsPrompt.None);
                    ApplySettings();
                    CloseIfSaved();
                    break;
                case SettingsPromptAction.Discard:
                    DiscardChanges();
                    Close();
                    break;
                case SettingsPromptAction.Stay:
                    _model.SetPrompt(SettingsPrompt.None);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        public void Tick()
        {
            if (_settingsService.IsAwaitingDisplayConfirmation)
            {
                UpdateDisplayCountdown();
            }
        }

        public void LateDispose()
        {
            if (_ui != null)
            {
                _ui.Dispose();
            }
        }

        private void UpdateDisplayCountdown()
        {
            int secondsLeft = Mathf.CeilToInt(_displayRevertTime - Time.unscaledTime);
            if (secondsLeft <= 0)
            {
                ChoosePromptAction(SettingsPromptAction.Revert);
                return;
            }

            if (secondsLeft == _shownSecondsLeft)
            {
                return;
            }

            _shownSecondsLeft = secondsLeft;
            _model.SetPrompt(new SettingsPrompt(
                SettingsPromptKind.DisplayConfirmation,
                $"Keep these display settings?\nReverting in {secondsLeft} s.",
                DISPLAY_ACTIONS));
        }

        // Apply from the unsaved-changes prompt closes only once nothing is left pending.
        private void CloseIfSaved()
        {
            if (!_settingsService.IsDirty && !_settingsService.IsAwaitingDisplayConfirmation)
            {
                Close();
            }
        }

        private void ShowResult(string message)
        {
            _model.SetPrompt(SettingsPrompt.None);
            _draftEditor.Refresh();
            _keyBindingEditor.Refresh();
            _model.SetStatus(message);
        }
    }
}
