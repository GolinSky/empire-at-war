using System;
using System.Threading.Tasks;
using EmpireAtWar.Services.Settings;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>In-memory settings for UI tests: saves succeed and nothing is applied.</summary>
    public sealed class FakeSettingsService : ISettingsService
    {
        public event Action SavedChanged;

        public string Id => nameof(FakeSettingsService);

        public SettingsData Saved { get; private set; } = new SettingsData();
        public SettingsData Draft { get; } = new SettingsData();
        public bool IsDirty => false;
        public bool IsAwaitingDisplayConfirmation => false;
        public Task Initialization => Task.CompletedTask;

        public Task<SettingsApplyResult> ApplyAsync() => Task.FromResult(SettingsApplyResult.Committed);

        public Task PreviewMonitorAsync(int index) => Task.CompletedTask;

        public SettingsApplyResult KeepDisplay() => SettingsApplyResult.Committed;

        public Task RevertDisplayAsync() => Task.CompletedTask;

        public Task DiscardAsync() => Task.CompletedTask;

        public void ResetDraftToDefaults()
        {
        }

        public SettingsApplyResult AnswerUsageDataConsent(bool isShared) => SettingsApplyResult.Committed;

        public SettingsApplyResult SetShowHints(bool isOn) => SettingsApplyResult.Committed;

        public SettingsApplyResult SetLobbyOccupant(int slotIndex, int occupant, bool online = false)
        {
            Saved.Lobby.SetOccupantChoice(slotIndex, occupant, online);
            SavedChanged?.Invoke();
            return SettingsApplyResult.Committed;
        }
    }
}
