using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmpireAtWar.Services.Graphics;
using EmpireAtWar.Mvc;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Settings
{
    public class SettingsService : Service, ISettingsService, ICameraPreferences, IInitializable
    {
        // Keys written by the PlayerPrefs storage this service replaced; imported once when no settings file exists.
        private const string LEGACY_QUALITY_PRESET_KEY = "QualityPreset";
        private const string LEGACY_INPUT_BINDINGS_KEY = "InputBindingOverrides";

        private readonly ISettingsRepository _repository;

        private readonly IReadOnlyList<ISettingsApplier> _appliers;
        private DisplaySettingsData _displayBeforePreview;
        public Task Initialization { get; private set; } = Task.CompletedTask;

        public event Action SavedChanged;

        public SettingsData Saved { get; private set; }
        public SettingsData Draft { get; private set; }
        public bool IsDirty => !Draft.Matches(Saved);
        public bool IsAwaitingDisplayConfirmation { get; private set; }

        public float PanSpeedMultiplier => Saved.Camera.PanSpeedMultiplier;
        public float ZoomSpeedMultiplier => Saved.Camera.ZoomSpeedMultiplier;
        public bool EdgeScrolling => Saved.Camera.EdgeScrolling;
        public bool InvertZoom => Saved.Camera.InvertZoom;

        /// <param name="appliers">Applied in binding order, so display changes run before quality and input.</param>
        public SettingsService(ISettingsRepository repository, IReadOnlyList<ISettingsApplier> appliers)
        {
            _repository = repository;
            _appliers = appliers;
        }

        public void Initialize()
        {
            SettingsLoadStatus loadStatus = _repository.Load(out SettingsData loaded);
            switch (loadStatus)
            {
                case SettingsLoadStatus.Missing:
                    loaded = ImportLegacySettings();
                    TrySave(loaded);
                    break;
                case SettingsLoadStatus.Unreadable:
                    // Run on defaults but leave the file alone; only an explicit Apply overwrites it.
                    loaded = new SettingsData();
                    break;
            }

            Saved = loaded;
            Draft = Saved.Clone();
            Initialization = InitializeAppliedAsync(loadStatus);
            ObserveInitialization();
        }

        private async Task InitializeAppliedAsync(SettingsLoadStatus loadStatus)
        {
            GraphicsUpscaler requestedUpscaler = Saved.Graphics.Upscaler;
            await ApplyAllAsync(Saved);
            if (requestedUpscaler != Saved.Graphics.Upscaler && loadStatus != SettingsLoadStatus.Unreadable)
                TrySave(Saved);
            Draft = Saved.Clone();
        }

        private async void ObserveInitialization()
        {
            try { await Initialization; }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public async Task<SettingsApplyResult> ApplyAsync()
        {
            await Initialization;
            bool displayChanged = !Draft.Display.Matches(Saved.Display);
            CaptureDisplayBeforePreview();
            await ApplyAllAsync(Draft);
            if (displayChanged)
            {
                IsAwaitingDisplayConfirmation = true;
                return SettingsApplyResult.AwaitingDisplayConfirmation;
            }

            return Commit();
        }

        public SettingsApplyResult KeepDisplay()
        {
            IsAwaitingDisplayConfirmation = false;
            SettingsApplyResult result = Commit();
            if (result == SettingsApplyResult.Committed) _displayBeforePreview = null;
            return result;
        }

        public async Task RevertDisplayAsync()
        {
            await Initialization;
            IsAwaitingDisplayConfirmation = false;
            Draft.SetDisplay(Saved.Display.Clone());
            await RestoreDisplayAsync(Draft);
        }

        public async Task DiscardAsync()
        {
            await Initialization;
            IsAwaitingDisplayConfirmation = false;
            Draft = Saved.Clone();
            await RestoreDisplayAsync(Saved);
        }

        public async Task PreviewMonitorAsync(int index)
        {
            await Initialization;
            CaptureDisplayBeforePreview();
            foreach (ISettingsApplier applier in _appliers)
            {
                if (applier is IDisplayOptions display)
                {
                    DisplayInfo monitor = display.Monitors[index];
                    Draft.Display.SetMonitor(monitor.name, index);
                    await display.MoveToMonitorAsync(Draft.Display);
                    if (!Draft.Display.UsesDesktopResolution)
                    {
                        Vector2Int size = new Vector2Int(Draft.Display.Width, Draft.Display.Height);
                        bool supported = false;
                        foreach (Vector2Int available in display.Resolutions)
                            supported |= available == size;
                        if (!supported) Draft.Display.SetResolution(0, 0);
                    }
                    break;
                }
            }
            await ApplyAllAsync(Draft);
            IsAwaitingDisplayConfirmation = true;
        }

        private void CaptureDisplayBeforePreview()
        {
            if (_displayBeforePreview != null) return;
            foreach (ISettingsApplier applier in _appliers)
                if (applier is IDisplayOptions display)
                {
                    _displayBeforePreview = display.CaptureCurrentDisplay();
                    return;
                }
        }

        private async Task RestoreDisplayAsync(SettingsData settings)
        {
            SettingsData restored = settings;
            if (_displayBeforePreview != null)
            {
                restored = settings.Clone();
                restored.SetDisplay(_displayBeforePreview);
            }
            await ApplyAllAsync(restored);
            _displayBeforePreview = null;
        }

        public void ResetDraftToDefaults()
        {
            Draft = new SettingsData();
        }

        public SettingsApplyResult AnswerUsageDataConsent(bool isShared)
        {
            SettingsData answered = Saved.Clone();
            answered.Privacy.Answer(isShared);
            if (!TrySave(answered))
            {
                return SettingsApplyResult.SaveFailed;
            }

            Saved = answered;
            Draft.Privacy.Answer(isShared);
            foreach (ISettingsApplier applier in _appliers)
                if (!(applier is IDisplayOptions)) applier.Apply(Saved);
            SavedChanged?.Invoke();
            return SettingsApplyResult.Committed;
        }

        // Save before promoting, so a failed write never reports the draft as saved.
        private SettingsApplyResult Commit()
        {
            if (!TrySave(Draft))
            {
                return SettingsApplyResult.SaveFailed;
            }

            Saved = Draft.Clone();
            _displayBeforePreview = null;
            SavedChanged?.Invoke();
            return SettingsApplyResult.Committed;
        }

        private bool TrySave(SettingsData settings)
        {
            try
            {
                _repository.Save(settings);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"Could not save settings: {exception.Message}");
                return false;
            }
        }

        private async Task ApplyAllAsync(SettingsData settings)
        {
            foreach (ISettingsApplier applier in _appliers)
            {
                if (applier is IDisplayOptions display)
                    await display.ApplyDisplayAsync(settings.Display);
                else
                    applier.Apply(settings);
            }
        }

        private static SettingsData ImportLegacySettings()
        {
            SettingsData data = new SettingsData();
            data.Graphics.QualityPreset = PlayerPrefs.GetString(LEGACY_QUALITY_PRESET_KEY, string.Empty);
            data.Input.BindingOverridesJson = PlayerPrefs.GetString(LEGACY_INPUT_BINDINGS_KEY, string.Empty);
            return data;
        }
    }
}
