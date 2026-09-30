using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Settings
{
    /// <summary>
    /// Owns the saved settings and the editable draft. Knows no consumer: engine state is pushed through
    /// <see cref="ISettingsApplier"/> implementations, gameplay reads narrow interfaces such as <see cref="ICameraPreferences"/>.
    /// </summary>
    public interface ISettingsService : IService
    {
        SettingsData Saved { get; }
        SettingsData Draft { get; }
        bool IsDirty { get; }
        bool IsAwaitingDisplayConfirmation { get; }

        /// <summary>Applies the draft. A display change must then be kept or reverted before anything is saved.</summary>
        SettingsApplyResult Apply();

        /// <summary>Returns <see cref="SettingsApplyResult.Committed"/> or <see cref="SettingsApplyResult.SaveFailed"/>.</summary>
        SettingsApplyResult KeepDisplay();

        /// <summary>Restores the saved display and aborts this Apply; other draft edits stay pending.</summary>
        void RevertDisplay();

        /// <summary>Replaces the draft with the saved settings and restores their engine state.</summary>
        void Discard();

        /// <summary>Replaces the draft with defaults; nothing is applied until <see cref="Apply"/>.</summary>
        void ResetDraftToDefaults();
    }
}
