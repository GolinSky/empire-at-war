using System.Collections.Generic;
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
        private string _savedJson;

        public SettingsData Saved { get; private set; }
        public SettingsData Draft { get; private set; }
        public bool IsDirty => Draft.ToJson() != _savedJson;
        public bool IsAwaitingDisplayConfirmation { get; private set; }
        public CameraSettingsData Camera => Saved.Camera;

        /// <param name="appliers">Applied in binding order, so display changes run before quality and input.</param>
        public SettingsService(ISettingsRepository repository, List<ISettingsApplier> appliers)
        {
            _repository = repository;
            _appliers = appliers;
        }

        public void Initialize()
        {
            Application.backgroundLoadingPriority = ThreadPriority.High;

            if (!_repository.TryLoad(out SettingsData loaded))
            {
                loaded = ImportLegacySettings();
                _repository.Save(loaded);
            }

            SetSaved(loaded);
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

            Commit();
            return SettingsApplyResult.Committed;
        }

        public void KeepDisplay()
        {
            IsAwaitingDisplayConfirmation = false;
            Commit();
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

        private void Commit()
        {
            // Save before promoting, so a failed write never reports the draft as saved.
            _repository.Save(Draft);
            SetSaved(Draft.Clone());
        }

        private void SetSaved(SettingsData saved)
        {
            Saved = saved;
            _savedJson = saved.ToJson();
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
