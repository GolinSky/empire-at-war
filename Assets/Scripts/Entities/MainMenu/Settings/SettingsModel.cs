using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public class SettingsModel : PureModel, ISettingsModelObserver
    {
        public IReadOnlyList<string> QualityPresets { get; private set; }
        public int SelectedIndex { get; private set; }

        public void Configure(IReadOnlyList<string> qualityPresets, int selectedIndex)
        {
            QualityPresets = qualityPresets;
            SelectedIndex = selectedIndex;
        }

        public void SelectQualityPreset(int index)
        {
            SelectedIndex = index;
        }
    }
}
