using System.Collections.Generic;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Modal question shown over the settings screen; it blocks every row until answered.</summary>
    public sealed class SettingsPrompt
    {
        public static readonly SettingsPrompt None =
            new SettingsPrompt(kind: SettingsPromptKind.None, message: string.Empty, actions: new SettingsPromptAction[0]);

        public SettingsPromptKind Kind { get; }
        public string Message { get; }
        public IReadOnlyList<SettingsPromptAction> Actions { get; }
        public bool IsVisible => Kind != SettingsPromptKind.None;

        public SettingsPrompt(IReadOnlyList<SettingsPromptAction> actions, string message, SettingsPromptKind kind)
        {
            Kind = kind;
            Message = message;
            Actions = actions;
        }
    }
}
