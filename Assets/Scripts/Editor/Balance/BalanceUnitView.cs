using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceUnitView
    {
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceWindowState state,
            Action<BalanceField, string> edit, Action<BalanceField> select, Action refresh)
        {
            if (!state.UnitDetailsOpen)
            {
                BalanceCompareView.Build(panel, registry, state, edit, refresh, selected =>
                {
                    state.SelectedUnit = selected.Id;
                    state.SelectedField = "";
                    state.ShowDetails = false;
                    state.UnitDetailsOpen = true;
                    refresh();
                });
                return;
            }
            Button back = new Button(() => { state.UnitDetailsOpen = false; state.ShowDetails = false; refresh(); })
                { text = "Back to units", name = "units-back" };
            back.AddToClassList("balance-units-back"); panel.Add(back);
            BalanceUnit unit = registry.Units.FirstOrDefault(entry => entry.Id == state.SelectedUnit);
            if (unit == null)
            {
                panel.Add(new HelpBox("Select a unit to see its stats, weapons, abilities and hardpoints.", HelpBoxMessageType.Info));
                return;
            }
            VisualElement heading = new VisualElement(); heading.AddToClassList("balance-unit-heading");
            if (unit.Icon != null)
            {
                Image icon = new Image { name = "unit-icon", sprite = unit.Icon, scaleMode = UnityEngine.ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("balance-unit-icon"); heading.Add(icon);
            }
            Label name = new Label(unit.Name); name.AddToClassList("balance-unit-name"); heading.Add(name);
            Label caption = new Label(unit.Faction + " · " + unit.Class + " · " + unit.Kind); caption.AddToClassList("balance-muted"); heading.Add(caption);
            panel.Add(heading);
            Summary(panel, unit, registry, state, edit);
            VisualElement tabs = new VisualElement(); tabs.AddToClassList("balance-subtabs"); panel.Add(tabs);
            foreach (string tab in new[] { "Stats", "Weapons", "Abilities", "Hardpoints" })
            {
                Button button = new Button(() => { state.UnitTab = tab; refresh(); }) { text = tab };
                button.EnableInClassList("balance-tab-active", state.UnitTab == tab); tabs.Add(button);
            }
            if (state.UnitTab == "Stats")
                BalanceRosterView.Filter(panel, "Category", new List<string> { "All Groups", "Combat", "Movement", "Economy", "Hangar", "Advanced" },
                    state.UnitGroup, value => state.UnitGroup = value, refresh);
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, state.Draft);
            var fields = registry.Fields.Values.Where(field => !field.SharedMountSource && usage.Users(field).Any(user => user.Id == unit.Id))
                .Where(field => state.UnitTab == "Stats" ? new[] { "Combat", "Movement", "Economy", "Hangar", "Advanced" }.Contains(field.Group)
                    && field.Stat != "Abilities" && (state.UnitGroup == "All Groups" || field.Group == state.UnitGroup)
                    : state.UnitTab == "Abilities" ? field.Group == "Abilities" || field.Stat == "Abilities" : field.Group == state.UnitTab).ToList();
            Fields(panel, fields, state, edit, select, "unit-" + unit.Id + "-" + state.UnitTab + "-" + state.UnitGroup, usage);
        }

        public static void Fields(VisualElement panel, IEnumerable<BalanceField> fields, BalanceWindowState state,
            Action<BalanceField, string> edit, Action<BalanceField> select, string viewKey, BalanceDraftUsage usage)
        {
            var sections = fields.OrderBy(field => field.Group).ThenBy(field => field.Context).ThenBy(field => field.Label)
                .GroupBy(field => (field.Group, field.Context)).Select(group => group.ToList()).ToList();
            if (sections.Count == 0)
            {
                panel.Add(new HelpBox("No matching fields in this view.", HelpBoxMessageType.Info));
                return;
            }
            ListView list = new ListView(sections, 180, () => new VisualElement(), (row, index) =>
            {
                row.Clear();
                List<BalanceField> section = sections[index];
                Foldout group = new Foldout { text = section[0].Group + " · " + section[0].Context,
                    value = true, viewDataKey = section[0].Key };
                group.AddToClassList("balance-field-section");
                VisualElement grid = new VisualElement(); grid.AddToClassList("balance-field-grid"); group.Add(grid);
                foreach (BalanceField field in section)
                    grid.Add(BalanceFieldView.Create(field, state, usage, value => edit(field, value), () => select(field)));
                row.Add(group);
            }) { virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight, selectionType = SelectionType.None, viewDataKey = viewKey };
            list.AddToClassList("balance-field-list"); panel.Add(list);
        }

        public static void Summary(VisualElement panel, BalanceUnit unit, BalanceRegistration registry, BalanceWindowState state, Action<BalanceField, string> edit)
        {
            VisualElement summary = new VisualElement(); summary.AddToClassList("balance-summary"); panel.Add(summary);
            EditableStat(summary, unit, registry, state, edit, unit.Kind == "Squadron" ? "Hull / member" : "Hull HP", "Hull", "MemberHull");
            EditableStat(summary, unit, registry, state, edit, unit.Kind == "Squadron" ? "Shield / member" : "Shield", "Shields", "MemberShields");
            Stat(summary, "Base DPS · estimate", new BalanceDpsEstimator(registry).Estimate(unit, state.Draft).ToString());
            Stat(summary, "Speed", Value(unit, registry, state, "Speed", "CruiseSpeed"));
        }

        private static void EditableStat(VisualElement panel, BalanceUnit unit, BalanceRegistration registry, BalanceWindowState state,
            Action<BalanceField, string> edit, string title, params string[] stats)
        {
            string stat = stats.FirstOrDefault(unit.Fields.ContainsKey);
            if (stat == null) { Stat(panel, title, "N/A"); return; }
            BalanceField field = registry.Fields[unit.Fields[stat]];
            VisualElement card = new VisualElement(); card.AddToClassList("balance-stat"); card.AddToClassList("balance-stat-adjustable");
            Label label = new Label(title); label.AddToClassList("balance-muted"); card.Add(label);
            card.Add(BalanceStatSlider.Create(field, state.Draft, value => edit(field, value), BalanceStatSlider.Maximum(field, state.Draft)));
            panel.Add(card);
        }

        private static string Value(BalanceUnit unit, BalanceRegistration registry, BalanceWindowState state, params string[] stats)
        {
            foreach (string stat in stats)
                if (unit.Fields.TryGetValue(stat, out string key))
                    return double.Parse(registry.Fields[key].DraftValue(state.Draft), CultureInfo.InvariantCulture).ToString("N2", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            return "N/A";
        }

        private static void Stat(VisualElement panel, string title, string value)
        {
            VisualElement card = new VisualElement(); card.AddToClassList("balance-stat");
            Label label = new Label(title); label.AddToClassList("balance-muted"); card.Add(label);
            Label number = new Label(value); number.AddToClassList("balance-stat-value"); card.Add(number);
            card.tooltip = "Read-only draft value. " + BalanceDpsEstimator.FORMULA + " Excludes accuracy, target modifiers, firing arcs, abilities, movement and projectile travel.";
            panel.Add(card);
        }
    }
}
