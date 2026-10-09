using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceUnitView
    {
        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceEditorController controller)
        {
            BalanceWindowState state = controller.State;
            if (!state.UnitDetailsOpen)
            {
                BalanceCompareView.Build(panel, registry, controller, true);
                return;
            }

            Button back = new Button(controller.CloseUnit) { text = "Back to units", name = "units-back" };
            back.AddToClassList("balance-units-back");
            panel.Add(back);
            BalanceUnit unit = registry.Units.FirstOrDefault(entry => entry.Id == state.SelectedUnit);
            if (unit == null)
            {
                panel.Add(new HelpBox("Select a unit to see its stats, weapons, abilities and hardpoints.", HelpBoxMessageType.Info));
                return;
            }

            Heading(panel, unit);
            Summary(panel, unit, registry, controller);
            Tabs(panel, controller);
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, state.Draft);
            var fields = registry.Fields.Values
                .Where(field => !field.SharedMountSource && usage.Users(field).Any(user => user.Id == unit.Id))
                .Where(field => InTab(field, state.UnitTab, state.UnitGroup))
                .ToList();
            Fields(panel, fields, controller, "unit-" + unit.Id + "-" + state.UnitTab + "-" + state.UnitGroup, usage);
        }

        private static void Heading(VisualElement panel, BalanceUnit unit)
        {
            VisualElement heading = new VisualElement();
            heading.AddToClassList("balance-unit-heading");
            if (unit.Icon != null)
            {
                Image icon = new Image { name = "unit-icon", sprite = unit.Icon, scaleMode = UnityEngine.ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("balance-unit-icon");
                heading.Add(icon);
            }

            Label name = new Label(unit.Name);
            name.AddToClassList("balance-unit-name");
            heading.Add(name);
            Label caption = new Label(unit.Faction + " · " + unit.Class + " · " + unit.Kind);
            caption.AddToClassList("balance-muted");
            heading.Add(caption);
            panel.Add(heading);
        }

        private static void Tabs(VisualElement panel, BalanceEditorController controller)
        {
            VisualElement tabs = new VisualElement();
            tabs.AddToClassList("balance-subtabs");
            panel.Add(tabs);
            foreach (BalanceUnitTab tab in Enum.GetValues(typeof(BalanceUnitTab)))
            {
                Button button = new Button(() => controller.ShowUnitTab(tab)) { text = tab.ToString() };
                button.EnableInClassList("balance-tab-active", controller.State.UnitTab == tab);
                tabs.Add(button);
            }

            if (controller.State.UnitTab != BalanceUnitTab.Stats) return;
            EnumField category = new EnumField("Category", controller.State.UnitGroup);
            category.RegisterValueChangedCallback(evt => controller.ShowStatsCategory((BalanceStatsCategory)evt.newValue));
            panel.Add(category);
        }

        private static bool InTab(BalanceField field, BalanceUnitTab tab, BalanceStatsCategory category) => tab switch
        {
            BalanceUnitTab.Stats => field.Stat != "Abilities" && InCategory(field.Group, category),
            BalanceUnitTab.Weapons => field.Group == BalanceFieldGroup.Weapons,
            BalanceUnitTab.Abilities => field.Group == BalanceFieldGroup.Abilities || field.Stat == "Abilities",
            BalanceUnitTab.Hardpoints => field.Group == BalanceFieldGroup.Hardpoints,
            _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null)
        };

        private static bool InCategory(BalanceFieldGroup group, BalanceStatsCategory category) => category switch
        {
            BalanceStatsCategory.All => group == BalanceFieldGroup.Combat || group == BalanceFieldGroup.Movement
                || group == BalanceFieldGroup.Economy || group == BalanceFieldGroup.Hangar || group == BalanceFieldGroup.Advanced,
            BalanceStatsCategory.Combat => group == BalanceFieldGroup.Combat,
            BalanceStatsCategory.Movement => group == BalanceFieldGroup.Movement,
            BalanceStatsCategory.Economy => group == BalanceFieldGroup.Economy,
            BalanceStatsCategory.Hangar => group == BalanceFieldGroup.Hangar,
            BalanceStatsCategory.Advanced => group == BalanceFieldGroup.Advanced,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };

        public static void Fields(VisualElement panel, IEnumerable<BalanceField> fields, BalanceEditorController controller,
            string viewKey, BalanceDraftUsage usage)
        {
            var sections = fields.OrderBy(field => field.Group).ThenBy(field => field.Context).ThenBy(field => field.Label)
                .GroupBy(field => (field.Group, field.Context))
                .Select(group => group.ToList())
                .ToList();
            if (sections.Count == 0)
            {
                panel.Add(new HelpBox("No matching fields in this view.", HelpBoxMessageType.Info));
                return;
            }

            ListView list = new ListView(sections, 180, () => new VisualElement(), (row, index) =>
            {
                row.Clear();
                List<BalanceField> section = sections[index];
                Foldout group = new Foldout { text = section[0].GroupLabel + " · " + section[0].Context, value = true, viewDataKey = section[0].Key };
                group.AddToClassList("balance-field-section");
                VisualElement grid = new VisualElement();
                grid.AddToClassList("balance-field-grid");
                group.Add(grid);
                foreach (BalanceField field in section)
                    grid.Add(BalanceFieldView.Create(field, controller.State.Draft, usage, value => controller.Edit(field, value), () => controller.ShowField(field)));
                row.Add(group);
            })
            {
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                selectionType = SelectionType.None,
                viewDataKey = viewKey
            };
            list.AddToClassList("balance-field-list");
            panel.Add(list);
        }

        public static void Summary(VisualElement panel, BalanceUnit unit, BalanceRegistration registry, BalanceEditorController controller)
        {
            BalanceDraft draft = controller.State.Draft;
            bool squadron = unit.Kind == BalanceUnitKind.Squadron;
            VisualElement summary = new VisualElement();
            summary.AddToClassList("balance-summary");
            panel.Add(summary);
            EditableStat(summary, unit, registry, controller, squadron ? "Hull / member" : "Hull HP", "Hull", "MemberHull");
            EditableStat(summary, unit, registry, controller, squadron ? "Shield / member" : "Shield", "Shields", "MemberShields");
            Stat(summary, "Base DPS · estimate", new BalanceDpsEstimator(registry).Estimate(unit, draft).ToString());
            Stat(summary, "Speed", Value(unit, registry, draft, "Speed", "CruiseSpeed"));
        }

        private static void EditableStat(VisualElement panel, BalanceUnit unit, BalanceRegistration registry,
            BalanceEditorController controller, string title, params string[] stats)
        {
            string stat = stats.FirstOrDefault(unit.Fields.ContainsKey);
            if (stat == null)
            {
                Stat(panel, title, "N/A");
                return;
            }

            BalanceField field = registry.Fields[unit.Fields[stat]];
            BalanceDraft draft = controller.State.Draft;
            VisualElement card = new VisualElement();
            card.AddToClassList("balance-stat");
            card.AddToClassList("balance-stat-adjustable");
            Label label = new Label(title);
            label.AddToClassList("balance-muted");
            card.Add(label);
            card.Add(BalanceStatSlider.Create(field, draft, value => controller.Edit(field, value), BalanceStatSlider.Maximum(field, draft)));
            panel.Add(card);
        }

        private static string Value(BalanceUnit unit, BalanceRegistration registry, BalanceDraft draft, params string[] stats)
        {
            foreach (string stat in stats)
            {
                if (!unit.Fields.TryGetValue(stat, out string key)) continue;
                double value = double.Parse(registry.Fields[key].DraftValue(draft), CultureInfo.InvariantCulture);
                return value.ToString("N2", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            }
            return "N/A";
        }

        private static void Stat(VisualElement panel, string title, string value)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("balance-stat");
            Label label = new Label(title);
            label.AddToClassList("balance-muted");
            card.Add(label);
            Label number = new Label(value);
            number.AddToClassList("balance-stat-value");
            card.Add(number);
            card.tooltip = "Read-only draft value. " + BalanceDpsEstimator.FORMULA
                + " Excludes accuracy, target modifiers, firing arcs, abilities, movement and projectile travel.";
            panel.Add(card);
        }
    }
}
