using System.Collections.Generic;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISettingsPopupModelObserver
    {
        IReadOnlyList<string> QualityPresets { get; }
        int SelectedIndex { get; }
    }
}
