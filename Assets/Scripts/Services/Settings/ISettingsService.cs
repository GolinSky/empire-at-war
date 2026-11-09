using System;
using System.Threading.Tasks;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.Settings
{

    //todo: check if saving operation run async
    /// <summary>
    /// Owns the saved settings and the editable draft. Knows no consumer: engine state is pushed through
    /// <see cref="ISettingsApplier"/> implementations, gameplay reads narrow interfaces such as <see cref="ICameraPreferences"/>.
    /// </summary>
    public interface ISettingsService : IService
    {
        event Action SavedChanged;

        SettingsData Saved { get; } // todo: don't share data - share behaviour
        SettingsData Draft { get; } // todo: don't share data - share behaviour
        bool IsDirty { get; }
        bool IsAwaitingDisplayConfirmation { get; }

        /// <summary>Applies the draft. A display change must then be kept or reverted before anything is saved.</summary>
        Task<SettingsApplyResult> ApplyAsync();
        Task PreviewMonitorAsync(int index);
        Task Initialization { get; }

        /// <summary>Returns <see cref="SettingsApplyResult.Committed"/> or <see cref="SettingsApplyResult.SaveFailed"/>.</summary>
        SettingsApplyResult KeepDisplay();

        /// <summary>Restores the saved display and aborts this Apply; other draft edits stay pending.</summary>
        Task RevertDisplayAsync();

        /// <summary>Replaces the draft with the saved settings and restores their engine state.</summary>
        Task DiscardAsync();

        /// <summary>Replaces the draft with defaults; nothing is applied until <see cref="ApplyAsync"/>.</summary>
        void ResetDraftToDefaults();

        /// <summary>Saves and applies the usage-data consent answer without touching other pending draft edits.</summary>
        SettingsApplyResult AnswerUsageDataConsent(bool isShared);

        /// <summary>Saves the show-hints flag at once (in-match "Turn off hints") without touching other draft edits.</summary>
        SettingsApplyResult SetShowHints(bool isOn);
    }
}
