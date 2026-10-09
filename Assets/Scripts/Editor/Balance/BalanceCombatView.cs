using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCombatView
    {
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceWindowState state,
            Action<BalanceField, string> edit, Action<BalanceField> select, Action refresh)
        {
            Label title = new Label("Combat data"); title.AddToClassList("balance-unit-name"); panel.Add(title);
            panel.Add(new HelpBox("Base DPS is an estimate, not measured combat performance. Target-class accuracy, damage and shield modifiers are editable in Damage matrix. TTK and battle simulations are not implemented.", HelpBoxMessageType.Info));
            BalanceUnit unit = registry.Units.FirstOrDefault(entry => entry.Id == state.SelectedUnit);
            if (unit != null)
            {
                panel.Add(new Label(unit.Caption));
                BalanceUnitView.Summary(panel, unit, registry, state, edit);
            }
            Toolbar tabs = new Toolbar(); tabs.AddToClassList("balance-subtabs"); panel.Add(tabs);
            foreach (string tab in new[] { "Weapons", "Damage matrix" })
            {
                ToolbarButton button = new ToolbarButton(() => { state.CombatTab = tab; refresh(); }) { text = tab };
                button.EnableInClassList("balance-tab-active", state.CombatTab == tab); tabs.Add(button);
            }
            TextField search = new TextField("Search combat data") { value = state.CombatSearch, isDelayed = true };
            search.RegisterValueChangedCallback(evt => { state.CombatSearch = evt.newValue; refresh(); }); panel.Add(search);
            IEnumerable<BalanceField> fields = registry.Fields.Values.Where(field => state.CombatTab == "Weapons" ? field.Group == "Weapons"
                : field.Stat.StartsWith("matrix/") || field.Stat == "missSpread")
                .Where(field => (field.Context + " " + field.Label).IndexOf(state.CombatSearch, StringComparison.OrdinalIgnoreCase) >= 0);
            BalanceUnitView.Fields(panel, fields, state, edit, select, "combat-" + state.CombatTab, new BalanceDraftUsage(registry, state.Draft));
        }
    }
}
