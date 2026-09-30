using System.Collections.Generic;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    /// <summary>Render state of a choice row: its option labels, the selected index, and whether it can change.</summary>
    public sealed class SettingsChoice
    {
        public static readonly SettingsChoice Empty = new SettingsChoice(new string[0], 0, false);

        public IReadOnlyList<string> Options { get; }
        public int Index { get; }
        public bool Interactable { get; }

        public SettingsChoice(IReadOnlyList<string> options, int index, bool interactable)
        {
            Options = options;
            Index = index;
            Interactable = interactable;
        }
    }
}
