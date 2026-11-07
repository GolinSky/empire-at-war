using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Threading.Tasks;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsRouteUiController : UiController, ISettingsRoute, ISettingsPresenter, ITickable, ILateDisposable
    {
        private const string APPLIED_MESSAGE = "Settings applied.";
        private const string SAVE_FAILED_MESSAGE = "Settings are active but could not be saved. Apply again to retry.";
        private const string DISCARDED_MESSAGE = "Changes discarded.";
        private const string DEFAULTS_MESSAGE = "Defaults restored. Apply to keep them.";
        private const string DISPLAY_REVERTED_MESSAGE = "Display change reverted.";
        private const string CONFLICTS_MESSAGE = "Resolve key binding conflicts before applying.";

        private readonly ISettingsService _settingsService;
        private readonly IInputBindings _bindings;
        private SettingsUi _ui;

        private static readonly SettingsPromptAction[] UNSAVED_ACTIONS =
            { SettingsPromptAction.Apply, SettingsPromptAction.Discard, SettingsPromptAction.Stay };
        private readonly SettingsDraftEditor _draftEditor;
        private readonly KeyBindingEditor _keyBindingEditor;
        private readonly SettingsModel _model;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;
        private readonly DisplayConfirmationCountdown _displayCountdown = new DisplayConfirmationCountdown();

        private bool _isOpen;
        private bool _isDisposed;
        private Task _activeOperation = Task.CompletedTask;

        public SettingsRouteUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            ISettingsService settingsService,
            IInputBindings bindings,
            ITooltipService tooltipService,
            SettingsDraftEditor draftEditor,
            KeyBindingEditor keyBindingEditor, SettingsModel model) : base(uiService, cancelRouter)
        {
            _settingsService = settingsService;
            _bindings = bindings;
            _draftEditor = draftEditor;
            _keyBindingEditor = keyBindingEditor;
            _model = model;
            _tooltips = new TooltipRequests(tooltipService);
            GraphicsUpscalerSupport.DlssDisabled += OnDlssDisabled;
        }

        private void OnDlssDisabled()
        {
            if (!_isOpen) return;
            _draftEditor.RefreshUpscalerOptions();
            _model.SetStatus("DLSS stopped producing frames. Using Bilinear for this session.");
        }

        public void LateDispose()
        {
            _isDisposed = true;
            GraphicsUpscalerSupport.DlssDisabled -= OnDlssDisabled;
            if (_settingsService.IsAwaitingDisplayConfirmation || _model.IsBusy) RestoreDisplayOnDispose();
            _model.Changed -= Render;
            if (_tooltipHover != null) _tooltipHover.Dispose();
            if (_ui != null)
            {
                _ui.Dispose();
            }
            _tooltipHover = null;
            _ui = null;
            Unfocus();
        }

        private async void RestoreDisplayOnDispose()
        {
            try
            {
                await _activeOperation;
                await _settingsService.RevertDisplayAsync();
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public async void Open()
        {
            try { await _settingsService.Initialization; }
            catch (Exception exception) { Debug.LogException(exception); return; }
            if (_isDisposed) return;
            _model.SetStatus(string.Empty);
            _model.SetPrompt(SettingsPrompt.None);
            _draftEditor.Open();
            _keyBindingEditor.Refresh();

            if (_ui == null)
            {
                _ui = CreateUi<SettingsUi>();
                _ui.SetModel(_model);
                _ui.AssignPresenter(this);
                _ui.Initialize();
                _model.Changed += Render;
                _tooltipHover = new TooltipHoverSubscription(
                    _ui.TooltipHover, HandleTooltipHover, _tooltips);
            }
            else
            {
                _ui.Render();
            }

            _ui.Show();
            _isOpen = true;
            Focus();
        }

        /// <summary>Unsaved changes ask Apply / Discard / Stay instead of closing.</summary>
        public void Close()
        {
            if (_model.IsBusy || _settingsService.IsAwaitingDisplayConfirmation) return;
            if (_settingsService.IsDirty)
            {
                _model.SetPrompt(new SettingsPrompt(
                    kind: SettingsPromptKind.UnsavedChanges, message: "You have unsaved changes.", actions: UNSAVED_ACTIONS));
                return;
            }

            _tooltips.HideAll();
            _isOpen = false;
            _ui.Hide();
            Unfocus();
        }

        // Escape answers the open prompt first; only a screen without a prompt closes.
        protected override bool HandleCancel()
        {
            if (_model.IsBusy) return true;
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

        public async void ApplySettings() => await RunOperationAsync(ApplySettingsAsync);

        private async Task ApplySettingsAsync()
        {
            if (_bindings.HasConflicts())
            {
                _model.SetStatus(CONFLICTS_MESSAGE);
                return;
            }

            SettingsApplyResult result = await _settingsService.ApplyAsync();
            _draftEditor.Open();
            if (result == SettingsApplyResult.AwaitingDisplayConfirmation)
            {
                StartDisplayCountdown();
                return;
            }

            ShowApplyResult(result);
        }

        public async void DiscardChanges() => await RunOperationAsync(DiscardChangesAsync);

        private async Task DiscardChangesAsync()
        {
            await _settingsService.DiscardAsync();
            _draftEditor.Open();
            ShowResult(DISCARDED_MESSAGE);
        }

        public async void SelectMonitor(int index)
        {
            await RunOperationAsync(async () =>
            {
                if (_bindings.HasConflicts())
                {
                    _model.SetStatus(CONFLICTS_MESSAGE);
                    return;
                }
                await _settingsService.PreviewMonitorAsync(index);
                _draftEditor.Open();
                StartDisplayCountdown();
            });
        }

        public void SelectRefreshRate(int index) => _draftEditor.SelectRefreshRate(index);

        private void StartDisplayCountdown()
        {
            _displayCountdown.Start(Time.unscaledTime,
                $"{Screen.mainWindowDisplayInfo.name} · {Screen.width} x {Screen.height} · " +
                $"{Screen.currentResolution.refreshRateRatio.value:0.##} Hz · {Screen.fullScreenMode}");
            UpdateDisplayCountdown();
        }

        private async Task RunOperationAsync(Func<Task> operation)
        {
            if (_model.IsBusy || _isDisposed) return;
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
            _activeOperation = completion.Task;
            _model.SetBusy(true);
            try
            {
                await operation();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                try
                {
                    await _settingsService.RevertDisplayAsync();
                    _draftEditor.Open();
                }
                catch (Exception restoreException) { Debug.LogException(restoreException); }
                _model.SetPrompt(SettingsPrompt.None);
                _model.SetStatus("Could not change display settings. See the log for details.");
            }
            finally
            {
                _model.SetBusy(false);
                completion.SetResult(true);
            }
        }

        public void ResetToDefaults()
        {
            _settingsService.ResetDraftToDefaults();
            _draftEditor.Open();
            _draftEditor.PreviewAudio();
            _keyBindingEditor.ResetAll();
            ShowResult(DEFAULTS_MESSAGE);
        }

        public void SelectQualityPreset(int index) => _draftEditor.SelectQuality(index);

        public void SelectWindowMode(int index) => _draftEditor.SelectWindowMode(index);

        public void SelectResolution(int index) => _draftEditor.SelectResolution(index);

        public void SelectFrameRateLimit(int index) => _draftEditor.SelectFrameRateLimit(index);

        public void SetNebulaEnabled(bool isOn) => _draftEditor.SetNebulaEnabled(isOn);

        public void SetRenderResolution(float resolution) => _draftEditor.SetRenderResolution(resolution);

        public void SelectUpscaler(int index) => _draftEditor.SelectUpscaler(index);

        public void SetVSync(bool isOn) => _draftEditor.SetVSync(isOn);

        public void SetMasterVolume(float volume) => _draftEditor.SetMasterVolume(volume);

        public void SetMusicVolume(float volume) => _draftEditor.SetMusicVolume(volume);

        public void SetVoiceVolume(float volume) => _draftEditor.SetVoiceVolume(volume);

        public void SetSfxVolume(float volume) => _draftEditor.SetSfxVolume(volume);

        public void SetMuteWhenUnfocused(bool isOn) => _draftEditor.SetMuteWhenUnfocused(isOn);

        public void SetPanSpeed(float multiplier) => _draftEditor.SetPanSpeed(multiplier);

        public void SetZoomSpeed(float multiplier) => _draftEditor.SetZoomSpeed(multiplier);

        public void SetEdgeScrolling(bool isOn) => _draftEditor.SetEdgeScrolling(isOn);

        public void SetInvertZoom(bool isOn) => _draftEditor.SetInvertZoom(isOn);

        public void SetAutoPowerAbilities(bool isOn) => _draftEditor.SetAutoPowerAbilities(isOn);

        public void SetShareUsageData(bool isOn) => _draftEditor.SetShareUsageData(isOn);

        public void StartRebind(int row) => _keyBindingEditor.StartRebind(row);

        public void ResetBinding(int row) => _keyBindingEditor.ResetBinding(row);

        public async void ChoosePromptAction(SettingsPromptAction action)
        {
            if (_model.IsBusy) return;
            switch (action)
            {
                case SettingsPromptAction.Keep:
                    ShowApplyResult(_settingsService.KeepDisplay());
                    break;
                case SettingsPromptAction.Revert:
                    await RunOperationAsync(async () =>
                    {
                        await _settingsService.RevertDisplayAsync();
                        _draftEditor.Open();
                        ShowResult(DISPLAY_REVERTED_MESSAGE);
                    });
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
                    await RunOperationAsync(ApplySettingsAsync);
                    CloseIfSaved();
                    break;
                case SettingsPromptAction.Discard:
                    await RunOperationAsync(DiscardChangesAsync);
                    CloseIfSaved();
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
            if (_settingsService.IsAwaitingDisplayConfirmation && !_model.IsBusy)
            {
                UpdateDisplayCountdown();
            }
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) {
            if (key is ValueTuple<int, bool> binding)
            {
                _tooltips.Show(source, key, anchor, () => _isOpen, () =>
                {
                    KeyBindingRowState row = _model.Bindings[binding.Item1];
                    return new TooltipContent(title: row.ActionLabel,
                        description: binding.Item2 ? "Restore this binding to its default." : "Click, then press a key or button to rebind. Escape cancels.",
                        shortcut: row.BindingLabel);
                });
                return;
            }
            _tooltips.Show(source, key, anchor, () => _isOpen, () =>
                new TooltipContent(title: (string)key, description: (string)key switch
                {
                    "Apply" => "Apply and save changes. Display changes require confirmation.",
                    "Discard" => "Discard changes and restore the saved settings.",
                    "Defaults" => "Restore default settings in the current draft. Apply to save them.",
                    "Close" => "Close settings. Unsaved changes require a choice.",
                    "Pan speed" => $"Camera pan speed: {_model.PanSpeed:0.#}.",
                    "Zoom speed" => $"Camera zoom speed: {_model.ZoomSpeed:0.#}.",
                    "Edge scrolling" => $"Move the camera at screen edges. Currently {(_model.EdgeScrolling ? "On" : "Off")}.",
                    "Ships use power abilities automatically" => "Automatically use eligible ship power abilities. Individual selection pauses new automatic starts; running abilities keep their ordinary duration and cooldown.",
                    "Share gameplay statistics" => $"Send gameplay statistics such as match results and performance. Crash diagnostics are collected separately. Currently {(_model.ShareUsageData ? "On" : "Off")}.",
                    "Invert zoom" => $"Reverse camera zoom input. Currently {(_model.InvertZoom ? "On" : "Off")}.",
                    "Master volume" => "Overall game volume. Changes are heard immediately.",
                    "Music volume" => "Volume of the soundtrack.",
                    "Voice volume" => "Volume of unit acknowledgements and alerts.",
                    "Effects volume" => "Volume of weapons, engines, and explosions.",
                    "Mute when unfocused" => $"Silence the game while its window is in the background. Currently {(_model.MuteWhenUnfocused ? "On" : "Off")}.",
                    "VSync" => $"Synchronize presentation with the display refresh rate. Currently {(_model.VSync ? "On" : "Off")}.",
                    "Window mode" => "Choose fullscreen or windowed display mode.",
                    "Resolution" => "Choose the output resolution. Borderless scales to fill the monitor and adds letterbox bars when aspect ratios differ.",
                    "Monitor" => "Move the game to another monitor. Keep the change within 15 seconds or the previous display is restored.",
                    "Refresh Rate" => "Choose refresh rate in exclusive fullscreen on Windows. Other modes use the monitor refresh rate.",
                    "Quality" => "Choose the graphics quality preset.",
                    "Nebula" => $"Render volumetric nebula clouds. Currently {(_model.NebulaEnabled ? "On" : "Off")}.",
                    "Render resolution" => $"Render the 3D scene at {_model.RenderResolution:P0} of the display resolution; the interface stays sharp.",
                    "Upscaler" => "Choose how a reduced render resolution is upscaled to the display.",
                    "Frame rate" => "Set the frame rate limit. VSync may override it.",
                    _ => throw new ArgumentOutOfRangeException(nameof(key))
                }));
        }

        private void UpdateDisplayCountdown()
        {
            float now = Time.unscaledTime;
            if (_displayCountdown.IsExpired(now))
            {
                ChoosePromptAction(SettingsPromptAction.Revert);
                return;
            }

            if (_displayCountdown.TryUpdatePrompt(now, out SettingsPrompt prompt))
            {
                _model.SetPrompt(prompt);
            }
        }

        private void ShowApplyResult(SettingsApplyResult result)
        {
            ShowResult(result == SettingsApplyResult.SaveFailed ? SAVE_FAILED_MESSAGE : APPLIED_MESSAGE);
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

        private void Render()
        {
            if (_ui != null) _ui.Render();
        }
    }
}
