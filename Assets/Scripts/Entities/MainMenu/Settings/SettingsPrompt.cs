using System.Collections.Generic;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Modal question shown over the settings screen; it blocks every row until answered.</summary>
    public sealed class SettingsPrompt
    {
        public static readonly SettingsPrompt None =
            new SettingsPrompt(SettingsPromptKind.None, string.Empty, new SettingsPromptAction[0]);

        public SettingsPromptKind Kind { get; }
        public string Message { get; }
        public IReadOnlyList<SettingsPromptAction> Actions { get; }
        public bool IsVisible => Kind != SettingsPromptKind.None;

        public SettingsPrompt(SettingsPromptKind kind, string message, IReadOnlyList<SettingsPromptAction> actions)
        {
            Kind = kind;
            Message = message;
            Actions = actions;
        }
    }
}
