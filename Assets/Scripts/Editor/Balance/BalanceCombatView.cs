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
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceEditorController controller)
        {
            BalanceWindowState state = controller.State;
            VisualElement heading = new VisualElement();
            heading.AddToClassList("balance-compare-heading");
            panel.Add(heading);
            Label title = new Label("Combat data");
            title.AddToClassList("balance-compare-title");
            heading.Add(title);
            Toolbar tabs = new Toolbar();
            tabs.AddToClassList("balance-subtabs");
            panel.Add(tabs);
            foreach (BalanceCombatTab tab in Enum.GetValues(typeof(BalanceCombatTab)))
            {
                ToolbarButton button = new ToolbarButton(() => controller.ShowCombatTab(tab))
                {
                    text = ObjectNames.NicifyVariableName(tab.ToString()),
                    name = "combat-" + tab
                };
                button.EnableInClassList("balance-tab-active", state.CombatTab == tab);
                tabs.Add(button);
            }

            if (state.CombatTab == BalanceCombatTab.Hardpoints) Hardpoints(panel, heading, registry, controller);
            else Profiles(panel, registry, controller);

            Label assumptions = new Label("Draft values · Base DPS is an estimate before accuracy, target modifiers, firing arcs, abilities and movement. TTK and battle simulations are not implemented.");
            assumptions.AddToClassList("balance-compare-assumptions");
            panel.Add(assumptions);
        }

        private static void Hardpoints(VisualElement panel, VisualElement heading, BalanceRegistration registry, BalanceEditorController controller)
        {
            BalanceWindowState state = controller.State;
            List<BalanceUnit> units = state.Pins
                .Select(id => registry.Units.FirstOrDefault(unit => unit.Id == id))
                .Where(unit => unit != null)
                .ToList();
            Label count = new Label(units.Count + " units");
            count.AddToClassList("balance-muted");
            heading.Add(count);
            BalanceCompareView.AddPicker(panel, heading, registry, controller, units.Count == 0);
            ScrollView scroll = new ScrollView { viewDataKey = "balance-combat-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 0;
            panel.Add(scroll);
            if (units.Count == 0)
            {
                Label empty = new Label("Add units to inspect and edit their hardpoints.");
                empty.AddToClassList("balance-muted");
                scroll.Add(empty);
            }

            VisualElement grid = new VisualElement { name = "combat-grid" };
            grid.AddToClassList("balance-compare-grid");
            scroll.Add(grid);
            BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry);
            foreach (BalanceUnit unit in units)
                Card(grid, unit, estimator.Estimate(unit, state.Draft), registry, controller);
        }

        private static void Card(VisualElement grid, BalanceUnit unit, BalanceDpsEstimate dps, BalanceRegistration registry, BalanceEditorController controller)
        {
            VisualElement slot = new VisualElement { userData = unit.Id };
            slot.AddToClassList("balance-compare-slot");
            grid.Add(slot);
            VisualElement card = new VisualElement { userData = unit.Id };
            card.AddToClassList("balance-compare-card");
            card.AddToClassList("balance-combat-card");
            slot.Add(card);
            VisualElement header = new VisualElement();
            header.AddToClassList("balance-compare-card-heading");
            card.Add(header);
            Label name = new Label(unit.Name);
            name.AddToClassList("balance-compare-unit-name");
            header.Add(name);
            Button remove = new Button(() => controller.RemovePin(unit.Id))
            {
                text = "×",
                name = "combat-remove-unit",
                tooltip = "Remove " + unit.Name + " from selected units"
            };
            remove.AddToClassList("balance-compare-remove");
            header.Add(remove);
            Label caption = new Label(unit.Faction + " · " + unit.Class);
            caption.AddToClassList("balance-compare-caption");
            card.Add(caption);
            Label dpsLabel = new Label("Base DPS · " + dps);
            dpsLabel.AddToClassList("balance-muted");
            card.Add(dpsLabel);
            Label hardpoints = new Label("Hardpoints (" + unit.Mounts.Distinct().Count() + ")");
            hardpoints.AddToClassList("balance-compare-section");
            card.Add(hardpoints);
            ScrollView mounts = new ScrollView { viewDataKey = "balance-combat-hardpoints-" + unit.Id };
            mounts.AddToClassList("balance-combat-hardpoints");
            card.Add(mounts);
            BalanceCompareSummary.BuildHardpoints(mounts, unit, registry, controller.State.Draft, controller.Edit);
            if (unit.Mounts.Count != 0) return;
            Label empty = new Label("No hardpoints on this unit.");
            empty.AddToClassList("balance-muted");
            mounts.Add(empty);
        }

        private static void Profiles(VisualElement panel, BalanceRegistration registry, BalanceEditorController controller)
        {
            BalanceWindowState state = controller.State;
            TextField search = new TextField("Search combat data") { value = state.CombatSearch, isDelayed = true };
            search.RegisterValueChangedCallback(evt => controller.SearchCombat(evt.newValue));
            panel.Add(search);
            IEnumerable<BalanceField> fields = registry.Fields.Values
                .Where(field => state.CombatTab == BalanceCombatTab.Weapons
                    ? field.Group == BalanceFieldGroup.Weapons
                    : field.Stat.StartsWith("matrix/") || field.Stat == "missSpread")
                .Where(field => (field.Context + " " + field.Label).IndexOf(state.CombatSearch, StringComparison.OrdinalIgnoreCase) >= 0);
            BalanceUnitView.Fields(panel, fields, controller, "combat-" + state.CombatTab, new BalanceDraftUsage(registry, state.Draft));
        }
    }
}
