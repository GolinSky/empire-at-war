using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceRosterView
    {
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceEditorController controller)
        {
            BalanceWindowState state = controller.State;
            panel.AddToClassList("balance-roster");
            Label title = new Label("ROSTER");
            title.AddToClassList("balance-section-title");
            panel.Add(title);
            TextField search = new TextField { value = state.Search, name = "roster-search", tooltip = "Search unit name, faction or class" };
            search.textEdition.placeholder = "Search units";
            panel.Add(search);
            VisualElement filters = new VisualElement();
            filters.AddToClassList("balance-roster-filters");
            panel.Add(filters);
            Label count = new Label();
            count.AddToClassList("balance-muted");
            panel.Add(count);

            List<BalanceUnit> units = new List<BalanceUnit>();
            ListView list = new ListView(units, 32, () => MakeRow(controller), (row, index) => BindRow(row, units[index], state))
            {
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                viewDataKey = "balance-roster-list"
            };
            list.AddToClassList("balance-roster-list");

            void FilterUnits()
            {
                units.Clear();
                units.AddRange(registry.Units
                    .Where(unit => unit.Kind == state.Kind
                        && (state.Faction == "All Factions" || unit.Faction == state.Faction)
                        && (state.Class == "All Classes" || unit.Class == state.Class)
                        && unit.Caption.IndexOf(state.Search, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(unit => unit.Faction)
                    .ThenBy(unit => unit.Name));
                list.RefreshItems();
                int selected = units.FindIndex(unit => unit.Id == state.SelectedUnit);
                list.SetSelectionWithoutNotify(selected < 0 ? Array.Empty<int>() : new[] { selected });
                count.text = units.Count + " visible · " + state.Pins.Count + " in comparison";
            }

            List<string> factions = new[] { "All Factions" }.Concat(registry.Units.Select(unit => unit.Faction).Distinct().OrderBy(name => name)).ToList();
            Filter(filters, "Faction", factions, state.Faction, controller.FilterRosterFaction, FilterUnits);
            EnumField kindFilter = new EnumField("Kind", state.Kind);
            kindFilter.RegisterValueChangedCallback(evt =>
            {
                controller.FilterRosterKind((BalanceUnitKind)evt.newValue);
                FilterUnits();
            });
            filters.Add(kindFilter);
            List<string> classes = new[] { "All Classes" }.Concat(registry.Units.Select(unit => unit.Class).Distinct().OrderBy(name => name)).ToList();
            Filter(filters, "Class", classes, state.Class, controller.FilterRosterClass, FilterUnits);
            search.RegisterValueChangedCallback(evt =>
            {
                controller.SearchRoster(evt.newValue);
                FilterUnits();
            });
            list.selectionChanged += selected =>
            {
                BalanceUnit unit = selected.Cast<BalanceUnit>().FirstOrDefault();
                if (unit == null || unit.Id == state.SelectedUnit) return;
                controller.SelectUnit(unit.Id);
            };
            panel.Add(list);
            FilterUnits();
        }

        private static VisualElement MakeRow(BalanceEditorController controller)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("balance-roster-row");
            Image icon = new Image { name = "unit-icon", scaleMode = UnityEngine.ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("balance-unit-icon-small");
            row.Add(icon);
            Label name = new Label { name = "unit-name" };
            name.AddToClassList("balance-roster-name");
            row.Add(name);
            Label kind = new Label { name = "unit-class" };
            kind.AddToClassList("balance-roster-class");
            row.Add(kind);
            Button pin = new Button(() => controller.TogglePin(((BalanceUnit)row.userData).Id)) { name = "unit-pin" };
            pin.AddToClassList("balance-pin");
            pin.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            row.Add(pin);
            return row;
        }

        private static void BindRow(VisualElement row, BalanceUnit unit, BalanceWindowState state)
        {
            row.userData = unit;
            row.tooltip = unit.Caption;
            Image icon = row.Q<Image>("unit-icon");
            icon.sprite = unit.Icon;
            icon.style.display = unit.Icon == null ? DisplayStyle.None : DisplayStyle.Flex;
            row.Q<Label>("unit-name").text = unit.Name;
            row.Q<Label>("unit-class").text = unit.Class;
            Button pin = row.Q<Button>("unit-pin");
            bool pinned = state.Pins.Contains(unit.Id);
            pin.text = pinned ? "P" : "+";
            pin.tooltip = pinned ? "Remove from comparison" : "Add to comparison";
            pin.EnableInClassList("balance-pinned", pinned);
        }

        public static void Filter(VisualElement panel, string label, List<string> choices, string current, Action<string> set, Action refresh)
        {
            DropdownField field = new DropdownField(label, choices, choices.IndexOf(current));
            field.RegisterValueChangedCallback(evt =>
            {
                set(evt.newValue);
                refresh();
            });
            panel.Add(field);
        }
    }
}
