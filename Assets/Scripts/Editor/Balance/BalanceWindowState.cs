using System;
using System.Collections.Generic;

namespace EmpireAtWar.Editor.Balance
{
    [Serializable]
    public sealed class BalanceWindowState
    {
        public BalanceDraft Draft = new BalanceDraft();
        public string SelectedUnit = "";
        public string SelectedField = "";
        public List<string> Pins = new List<string>();
        public List<string> CompareSelection = new List<string>();
        public string CompareSelectionAnchor = "";
        [NonSerialized] public bool CompareFocusPending;
        public string Faction = "All Factions";
        public BalanceUnitKind Kind = BalanceUnitKind.Ship;
        public string Class = "All Classes";
        public string Search = "";
        public string Group = "All Groups";
        public BalanceTab Tab = BalanceTab.Units;
        public BalanceUnitTab UnitTab = BalanceUnitTab.Stats;
        public bool UnitDetailsOpen;
        public BalanceStatsCategory UnitGroup = BalanceStatsCategory.All;
        public BalanceCombatTab CombatTab = BalanceCombatTab.Hardpoints;
        public string CombatSearch = "";
        public string CompareSearch = "";
        public string CompareFaction = "";
        public BalanceCompareSort CompareSort;
        public bool ComparePickerOpen;
        public bool ShowDetails;
        public List<BalanceScrollPosition> ScrollPositions = new List<BalanceScrollPosition>();
        public float LeftWidth = 255;
        public float RightWidth = 330;
    }
}
