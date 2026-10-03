using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Settings
{
    public class SettingsService : Service, ISettingsService, ICameraPreferences, IInitializable
    {
        // Keys written by the PlayerPrefs storage this service replaced; imported once when no settings file exists.
        private const string LEGACY_QUALITY_PRESET_KEY = "QualityPreset";
        private const string LEGACY_INPUT_BINDINGS_KEY = "InputBindingOverrides";

        private readonly ISettingsRepository _repository;

        private readonly List<ISettingsApplier> _appliers;

        public SettingsData Saved { get; private set; }
        public SettingsData Draft { get; private set; }
        public bool IsDirty => !Draft.Matches(Saved);
        public bool IsAwaitingDisplayConfirmation { get; private set; }

        public float PanSpeedMultiplier => Saved.Camera.PanSpeedMultiplier;
        public float ZoomSpeedMultiplier => Saved.Camera.ZoomSpeedMultiplier;
        public bool EdgeScrolling => Saved.Camera.EdgeScrolling;
        public bool InvertZoom => Saved.Camera.InvertZoom;

        /// <param name="appliers">Applied in binding order, so display changes run before quality and input.</param>
        public SettingsService(ISettingsRepository repository, List<ISettingsApplier> appliers)
        {
            _repository = repository;
            _appliers = appliers;
        }

        public void Initialize()
        {
            switch (_repository.Load(out SettingsData loaded))
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
            Draft = loaded.Clone();
            ApplyAll(Saved);
        }

        public SettingsApplyResult Apply()
        {
            bool displayChanged = !Draft.Display.Matches(Saved.Display);
            ApplyAll(Draft);
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
            return Commit();
        }

        public void RevertDisplay()
        {
            IsAwaitingDisplayConfirmation = false;
            Draft.SetDisplay(Saved.Display.Clone());
            ApplyAll(Draft);
        }

        public void Discard()
        {
            IsAwaitingDisplayConfirmation = false;
            Draft = Saved.Clone();
            ApplyAll(Saved);
        }

        public void ResetDraftToDefaults()
        {
            Draft = new SettingsData();
        }

        // Save before promoting, so a failed write never reports the draft as saved.
        private SettingsApplyResult Commit()
        {
            if (!TrySave(Draft))
            {
                return SettingsApplyResult.SaveFailed;
            }

            Saved = Draft.Clone();
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

        private void ApplyAll(SettingsData settings)
        {
            foreach (ISettingsApplier applier in _appliers)
            {
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
