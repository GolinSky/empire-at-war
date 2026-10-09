using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareView
    {
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceWindowState state, Action<BalanceField, string> edit, Action refresh)
        {
            BalanceCompareOrder.Sort(registry, state);
            state.CompareSelection.RemoveAll(id => !state.Pins.Contains(id));
            List<BalanceUnit> units = state.Pins.Select(id => registry.Units.FirstOrDefault(unit => unit.Id == id)).Where(unit => unit != null).ToList();
            VisualElement heading = new VisualElement(); heading.AddToClassList("balance-compare-heading"); panel.Add(heading);
            Label title = new Label("Unit comparison"); title.AddToClassList("balance-compare-title"); heading.Add(title);
            Label count = new Label(units.Count + " units"); count.AddToClassList("balance-muted"); heading.Add(count);
            EnumField order = new EnumField("Order by", state.CompareSort) { name = "compare-order", tooltip = "Ascending order using draft values. Drag cards to switch to Manual." };
            order.RegisterValueChangedCallback(evt => { state.CompareSort = (BalanceCompareSort)evt.newValue; refresh(); });
            heading.Add(order);
            AddPicker(panel, heading, registry, state, units.Count == 0, refresh);
            ScrollView scroll = new ScrollView { viewDataKey = "balance-compare-scroll" };
            scroll.style.flexGrow = 1; scroll.style.minHeight = 0; panel.Add(scroll);
            if (units.Count == 0)
            {
                Label empty = new Label("Add units to compare their stats."); empty.AddToClassList("balance-muted"); scroll.Add(empty);
            }
            else BalanceCompareSummary.Build(scroll, units, registry, state, edit, refresh);
        }

        public static void AddPicker(VisualElement panel, VisualElement heading, BalanceRegistration registry, BalanceWindowState state, bool empty, Action refresh)
        {
            VisualElement picker = Picker(registry, state, refresh);
            if (empty) state.ComparePickerOpen = true;
            Button add = new Button { text = state.ComparePickerOpen ? "Close picker" : "+ Add units", name = "compare-add-unit" };
            add.clicked += () =>
            {
                state.ComparePickerOpen = !state.ComparePickerOpen;
                picker.style.display = state.ComparePickerOpen ? DisplayStyle.Flex : DisplayStyle.None;
                add.text = state.ComparePickerOpen ? "Close picker" : "+ Add units";
                if (state.ComparePickerOpen) picker.Q<TextField>("compare-search").Focus();
            };
            add.AddToClassList("balance-primary"); heading.Add(add);
            panel.Add(picker);
            picker.style.display = state.ComparePickerOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (empty && state.CompareFocusPending)
            {
                TextField search = picker.Q<TextField>("compare-search");
                search.schedule.Execute(search.Focus);
                state.CompareFocusPending = false;
            }
        }

        private static VisualElement Picker(BalanceRegistration registry, BalanceWindowState state, Action refresh)
        {
            VisualElement picker = new VisualElement { name = "compare-picker" }; picker.AddToClassList("balance-compare-picker");
            VisualElement filters = new VisualElement(); filters.AddToClassList("balance-picker-filters"); picker.Add(filters);
            TextField search = new TextField { name = "compare-search", value = state.CompareSearch, tooltip = "Search by unit name, faction or class" };
            search.textEdition.placeholder = "Search units, factions or classes"; filters.Add(search);
            List<string> factions = new[] { "All factions" }.Concat(registry.Units.Select(unit => unit.Faction).Distinct().OrderBy(name => name)).ToList();
            DropdownField faction = new DropdownField("Faction", factions, state.CompareFaction.Length == 0 ? 0 : factions.IndexOf(state.CompareFaction))
                { name = "compare-faction" };
            filters.Add(faction);
            List<BalanceUnit> units = new List<BalanceUnit>();
            HashSet<string> selected = new HashSet<string>();
            VisualElement actions = new VisualElement(); actions.AddToClassList("balance-picker-actions"); picker.Add(actions);
            Button selectAll = new Button { name = "compare-select-all", text = "Select all", tooltip = "Select all matching units" }; actions.Add(selectAll);
            Button unselectAll = new Button { name = "compare-unselect-all", text = "Unselect all", tooltip = "Unselect all matching units" }; actions.Add(unselectAll);
            Button addSelected = new Button { name = "compare-add-selected" }; addSelected.AddToClassList("balance-primary"); actions.Add(addSelected);
            void UpdateActions()
            {
                selectAll.SetEnabled(units.Any(unit => !selected.Contains(unit.Id)));
                unselectAll.SetEnabled(units.Any(unit => selected.Contains(unit.Id)));
                addSelected.text = "Add selected (" + selected.Count + ")";
                addSelected.SetEnabled(selected.Count > 0);
            }
            Label empty = new Label("No matching units"); empty.AddToClassList("balance-muted");
            ListView list = new ListView { itemsSource = units, fixedItemHeight = 32, selectionType = SelectionType.None, viewDataKey = "balance-compare-picker-list" };
            list.makeItem = () =>
            {
                VisualElement row = new VisualElement(); row.AddToClassList("balance-picker-row");
                Toggle selection = new Toggle { name = "picker-select", tooltip = "Select unit to add" }; row.Add(selection);
                selection.RegisterValueChangedCallback(evt =>
                {
                    string id = ((BalanceUnit)row.userData).Id;
                    if (evt.newValue) selected.Add(id); else selected.Remove(id);
                    UpdateActions();
                });
                Label name = new Label { name = "picker-unit-name", pickingMode = PickingMode.Ignore };
                name.AddToClassList("balance-picker-name"); row.Add(name);
                Button add = new Button { name = "picker-add", text = "+ Add" }; row.Add(add);
                add.clicked += () =>
                {
                    string id = ((BalanceUnit)row.userData).Id;
                    if (!state.Pins.Contains(id)) state.Pins.Add(id);
                    Filter(); refresh();
                };
                return row;
            };
            list.bindItem = (row, index) =>
            {
                BalanceUnit unit = units[index]; row.userData = unit;
                row.Q<Toggle>("picker-select").SetValueWithoutNotify(selected.Contains(unit.Id));
                row.Q<Label>("picker-unit-name").text = unit.Caption; row.tooltip = unit.Caption;
            };
            list.AddToClassList("balance-picker-list"); picker.Add(list);
            picker.Add(empty);
            void Filter()
            {
                selected.ExceptWith(state.Pins);
                units.Clear();
                units.AddRange(registry.Units.Where(unit => !state.Pins.Contains(unit.Id)
                    && (state.CompareFaction.Length == 0 || unit.Faction == state.CompareFaction)
                    && unit.Caption.IndexOf(search.value, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(unit => unit.Faction).ThenBy(unit => unit.Name));
                list.RefreshItems();
                empty.style.display = units.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                UpdateActions();
            }
            selectAll.clicked += () => { selected.UnionWith(units.Select(unit => unit.Id)); list.RefreshItems(); UpdateActions(); };
            unselectAll.clicked += () => { selected.ExceptWith(units.Select(unit => unit.Id)); list.RefreshItems(); UpdateActions(); };
            addSelected.clicked += () =>
            {
                state.Pins.AddRange(registry.Units.Where(unit => selected.Contains(unit.Id) && !state.Pins.Contains(unit.Id)).Select(unit => unit.Id));
                selected.Clear();
                Filter(); refresh();
            };
            search.RegisterValueChangedCallback(evt => { state.CompareSearch = evt.newValue; Filter(); });
            faction.RegisterValueChangedCallback(evt => { state.CompareFaction = faction.index == 0 ? "" : evt.newValue; Filter(); });
            Filter();
            return picker;
        }
    }
}
