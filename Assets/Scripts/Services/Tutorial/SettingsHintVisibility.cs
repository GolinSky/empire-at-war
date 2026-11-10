using EmpireAtWar.Services.Settings;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Hints follow the player's "Show tutorial hints" setting.</summary>
    public sealed class SettingsHintVisibility : IHintVisibility
    {
        private readonly ISettingsService _settings;

        public SettingsHintVisibility(ISettingsService settings)
        {
            _settings = settings;
        }

        public bool IsOn => _settings.Saved.Gameplay.ShowHints;
    }
}
