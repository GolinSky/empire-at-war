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
        public List<string> BulkKeys = new List<string>();
        public string Faction = "All Factions";
        public string Kind = "Ship";
        public string Class = "All Classes";
        public string Search = "";
        public string Group = "All Groups";
        public string Tab = "Units";
        public string UnitTab = "Stats";
        public string UnitGroup = "All Groups";
        public string CombatTab = "Weapons";
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
