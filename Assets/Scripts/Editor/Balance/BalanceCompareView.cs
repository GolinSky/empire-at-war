using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareView
    {
        // unitsTab: the Units tab reuses the comparison as its overview, with "Edit unit" buttons on the cards.
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceEditorController controller, bool unitsTab)
        {
            controller.PrepareCompare(registry);
            BalanceWindowState state = controller.State;
            List<BalanceUnit> units = state.Pins
                .Select(id => registry.Units.FirstOrDefault(unit => unit.Id == id))
                .Where(unit => unit != null)
                .ToList();

            VisualElement heading = new VisualElement();
            heading.AddToClassList("balance-compare-heading");
            panel.Add(heading);
            Label title = new Label(unitsTab ? "Units" : "Unit comparison");
            title.AddToClassList("balance-compare-title");
            heading.Add(title);
            Label count = new Label(units.Count + " units");
            count.AddToClassList("balance-muted");
            heading.Add(count);
            EnumField order = new EnumField("Order by", state.CompareSort)
            {
                name = "compare-order",
                tooltip = "Ascending order using draft values. Drag cards to switch to Manual."
            };
            order.RegisterValueChangedCallback(evt => controller.SortCompare((BalanceCompareSort)evt.newValue));
            heading.Add(order);
            AddPicker(panel, heading, registry, controller, units.Count == 0);

            ScrollView scroll = new ScrollView { viewDataKey = "balance-compare-scroll" };
            scroll.style.flexGrow = 1;
            scroll.style.minHeight = 0;
            panel.Add(scroll);
            if (units.Count == 0)
            {
                Label empty = new Label(unitsTab ? "Add units to edit their stats, weapons, abilities and hardpoints." : "Add units to compare their stats.");
                empty.AddToClassList("balance-muted");
                scroll.Add(empty);
                return;
            }

            BalanceCompareSummary.Build(scroll, units, registry, controller, unitsTab);
        }

        public static void AddPicker(VisualElement panel, VisualElement heading, BalanceRegistration registry,
            BalanceEditorController controller, bool empty)
        {
            VisualElement picker = Picker(registry, controller);
            if (empty) controller.SetComparePickerOpen(true);
            bool open = controller.State.ComparePickerOpen;
            Button add = new Button { text = open ? "Close picker" : "+ Add units", name = "compare-add-unit" };
            void SetOpen(bool nowOpen)
            {
                controller.SetComparePickerOpen(nowOpen);
                picker.style.display = nowOpen ? DisplayStyle.Flex : DisplayStyle.None;
                add.text = nowOpen ? "Close picker" : "+ Add units";
                if (nowOpen) picker.Q<TextField>("compare-search").Focus();
            }
            add.clicked += () => SetOpen(!controller.State.ComparePickerOpen);
            void CloseOutside(PointerDownEvent evt)
            {
                VisualElement target = (VisualElement)evt.target;
                if (evt.button == 0 && controller.State.ComparePickerOpen
                    && target != picker && !picker.Contains(target)
                    && target != add && !add.Contains(target))
                    SetOpen(false);
            }
            picker.RegisterCallback<AttachToPanelEvent>(evt =>
                evt.destinationPanel.visualTree.RegisterCallback<PointerDownEvent>(CloseOutside, TrickleDown.TrickleDown));
            picker.RegisterCallback<DetachFromPanelEvent>(evt =>
                evt.originPanel.visualTree.UnregisterCallback<PointerDownEvent>(CloseOutside, TrickleDown.TrickleDown));
            add.AddToClassList("balance-primary");
            heading.Add(add);
            panel.Add(picker);
            picker.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (empty && controller.TakeCompareFocus())
            {
                TextField search = picker.Q<TextField>("compare-search");
                search.schedule.Execute(search.Focus);
            }
        }

        private static VisualElement Picker(BalanceRegistration registry, BalanceEditorController controller)
        {
            BalanceWindowState state = controller.State;
            VisualElement picker = new VisualElement { name = "compare-picker" };
            picker.AddToClassList("balance-compare-picker");
            VisualElement filters = new VisualElement();
            filters.AddToClassList("balance-picker-filters");
            picker.Add(filters);
            TextField search = new TextField { name = "compare-search", value = state.CompareSearch, tooltip = "Search by unit name, faction or class" };
            search.textEdition.placeholder = "Search units, factions or classes";
            filters.Add(search);
            List<string> factions = new[] { "All factions" }
                .Concat(registry.Units.Select(unit => unit.Faction).Distinct().OrderBy(name => name))
                .ToList();
            int factionIndex = state.CompareFaction.Length == 0 ? 0 : factions.IndexOf(state.CompareFaction);
            DropdownField faction = new DropdownField("Faction", factions, factionIndex) { name = "compare-faction" };
            filters.Add(faction);

            List<BalanceUnit> units = new List<BalanceUnit>();
            HashSet<string> selected = new HashSet<string>();
            VisualElement actions = new VisualElement();
            actions.AddToClassList("balance-picker-actions");
            picker.Add(actions);
            Button selectAll = new Button { name = "compare-select-all", text = "Select all", tooltip = "Select all matching units" };
            actions.Add(selectAll);
            Button unselectAll = new Button { name = "compare-unselect-all", text = "Unselect all", tooltip = "Unselect all matching units" };
            actions.Add(unselectAll);
            Button addSelected = new Button { name = "compare-add-selected" };
            addSelected.AddToClassList("balance-primary");
            actions.Add(addSelected);

            void UpdateActions()
            {
                selectAll.SetEnabled(units.Any(unit => !selected.Contains(unit.Id)));
                unselectAll.SetEnabled(units.Any(unit => selected.Contains(unit.Id)));
                addSelected.text = "Add selected (" + selected.Count + ")";
                addSelected.SetEnabled(selected.Count > 0);
            }

            Label empty = new Label("No matching units");
            empty.AddToClassList("balance-muted");
            ListView list = new ListView { itemsSource = units, fixedItemHeight = 32, selectionType = SelectionType.None, viewDataKey = "balance-compare-picker-list" };

            void Filter()
            {
                selected.ExceptWith(state.Pins);
                units.Clear();
                units.AddRange(registry.Units
                    .Where(unit => !state.Pins.Contains(unit.Id)
                        && (state.CompareFaction.Length == 0 || unit.Faction == state.CompareFaction)
                        && unit.Caption.IndexOf(search.value, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(unit => unit.Faction)
                    .ThenBy(unit => unit.Name));
                list.RefreshItems();
                empty.style.display = units.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                UpdateActions();
            }

            list.makeItem = () =>
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("balance-picker-row");
                Toggle selection = new Toggle { name = "picker-select", tooltip = "Select unit to add" };
                row.Add(selection);
                selection.RegisterValueChangedCallback(evt =>
                {
                    string id = ((BalanceUnit)row.userData).Id;
                    if (evt.newValue) selected.Add(id);
                    else selected.Remove(id);
                    UpdateActions();
                });
                Label name = new Label { name = "picker-unit-name", pickingMode = PickingMode.Ignore };
                Image icon = new Image { name = "unit-icon", scaleMode = UnityEngine.ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("balance-unit-icon-small");
                row.Add(icon);
                name.AddToClassList("balance-picker-name");
                row.Add(name);
                Button add = new Button { name = "picker-add", text = "+ Add" };
                row.Add(add);
                row.RegisterCallback<PointerDownEvent>(evt =>
                {
                    VisualElement target = (VisualElement)evt.target;
                    if (evt.button == 0 && target != selection && !selection.Contains(target)
                        && target != add && !add.Contains(target))
                        selection.value = !selection.value;
                });
                add.clicked += () =>
                {
                    controller.AddPins(new[] { ((BalanceUnit)row.userData).Id });
                    Filter();
                };
                return row;
            };
            list.bindItem = (row, index) =>
            {
                BalanceUnit unit = units[index];
                row.userData = unit;
                Image icon = row.Q<Image>("unit-icon");
                icon.sprite = unit.Icon;
                icon.style.display = unit.Icon == null ? DisplayStyle.None : DisplayStyle.Flex;
                row.Q<Toggle>("picker-select").SetValueWithoutNotify(selected.Contains(unit.Id));
                row.Q<Label>("picker-unit-name").text = unit.Caption;
                row.tooltip = unit.Caption;
            };
            list.AddToClassList("balance-picker-list");
            picker.Add(list);
            picker.Add(empty);

            selectAll.clicked += () =>
            {
                selected.UnionWith(units.Select(unit => unit.Id));
                list.RefreshItems();
                UpdateActions();
            };
            unselectAll.clicked += () =>
            {
                selected.ExceptWith(units.Select(unit => unit.Id));
                list.RefreshItems();
                UpdateActions();
            };
            addSelected.clicked += () =>
            {
                List<string> ids = registry.Units.Where(unit => selected.Contains(unit.Id)).Select(unit => unit.Id).ToList();
                selected.Clear();
                controller.AddPins(ids);
                Filter();
            };
            search.RegisterValueChangedCallback(evt =>
            {
                controller.SearchCompare(evt.newValue);
                Filter();
            });
            faction.RegisterValueChangedCallback(evt =>
            {
                controller.FilterCompareFaction(faction.index == 0 ? "" : evt.newValue);
                Filter();
            });
            Filter();
            return picker;
        }
    }
}
