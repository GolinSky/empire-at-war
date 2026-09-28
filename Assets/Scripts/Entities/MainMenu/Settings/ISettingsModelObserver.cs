using System.Collections.Generic;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public interface ISettingsModelObserver
    {
        IReadOnlyList<string> QualityPresets { get; }
        int SelectedIndex { get; }
    }
}
