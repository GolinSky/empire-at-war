using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareSummary
    {
        public static void Build(VisualElement panel, List<BalanceUnit> units, BalanceRegistration registry, BalanceWindowState state, Action<BalanceField, string> edit, Action refresh, Action<BalanceUnit> openUnit = null)
        {
            VisualElement grid = new VisualElement { name = "compare-grid" }; grid.AddToClassList("balance-compare-grid"); panel.Add(grid);
            BalanceCompareSelection.Bind(grid, state, refresh);
            List<string> dps = units.Select(unit => BalanceUnitView.BaseDps(unit, registry, state.Draft)).ToList();
            double ceiling = dps.Select(value => Number(value) ?? 0).Append(1).Max();
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, state.Draft);
            string[][] durability = { new[] { "Hull", "MemberHull" }, new[] { "Shields", "MemberShields" } };
            float[] maxima = durability.Select(stats => units.Select(unit => Field(unit, registry, stats)).Where(field => field != null)
                .Select(field => BalanceStatSlider.Maximum(field, state.Draft)).DefaultIfEmpty(100).Max()).ToArray();
            for (int i = 0; i < units.Count; i++)
            {
                BalanceUnit unit = units[i];
                VisualElement slot = new VisualElement { userData = unit.Id }; slot.AddToClassList("balance-compare-slot"); grid.Add(slot);
                VisualElement card = new VisualElement { userData = unit.Id, focusable = true }; card.AddToClassList("balance-compare-card"); slot.Add(card);
                VisualElement heading = new VisualElement(); heading.AddToClassList("balance-compare-card-heading"); card.Add(heading);
                Label drag = new Label("↕") { name = "compare-drag-handle", tooltip = "Drag to reorder; Escape cancels", focusable = true };
                heading.Add(drag);
                BalanceCompareDrag.Bind(card, slot, grid, state, refresh, additive => BalanceCompareSelection.Select(grid, state, unit.Id, additive));
                card.tooltip = "Click to select; Ctrl+click to toggle. Ctrl+A selects all; Delete removes selected units. Drag the background to reorder.";
                Text(heading, unit.Name, "balance-compare-unit-name");
                Button remove = new Button(() => { state.Pins.Remove(unit.Id); refresh(); })
                    { text = "×", name = "compare-remove-unit", tooltip = "Remove " + unit.Name };
                remove.AddToClassList("balance-compare-remove"); heading.Add(remove);
                Text(card, unit.Faction + " · " + unit.Class, "balance-compare-caption");
                Section(card, "Durability");
                for (int stat = 0; stat < durability.Length; stat++)
                {
                    BalanceField field = Field(unit, registry, durability[stat]);
                    VisualElement row = new VisualElement(); row.AddToClassList("balance-compare-health"); card.Add(row);
                    string label = stat == 0 ? "Health" : "Shield";
                    Text(row, label + (unit.Kind == "Squadron" ? " / member · HP" : " · HP"), "balance-muted");
                    if (field == null) Text(row, "N/A", "balance-compare-value");
                    else
                    {
                        row.Add(BalanceStatSlider.Create(field, state.Draft, value => edit(field, value), maxima[stat]));
                        if (field.Shared) Text(row, field.DescribeScope(field.Users), "balance-scope");
                    }
                }
                Section(card, "Mobility & economy");
                Editable(card, "Speed (u/s)", Field(unit, registry, "Speed", "CruiseSpeed"), state, usage, edit);
                Editable(card, "Height level", Field(unit, registry, "HeightTier", "Height"), state, usage, edit);
                Editable(card, "Cost (credits)", Field(unit, registry, "Price"), state, usage, edit);
                Editable(card, "Build time (s)", Field(unit, registry, "BuildTime"), state, usage, edit);
                Section(card, "Firepower");
                Editable(card, "Weapon range", Field(unit, registry, unit.Kind == "Ship" ? "Range" : "WeaponRange"), state, usage, edit);
                Metric(card, "Base DPS", dps[i], "dmg/s");
                if (Number(dps[i]) is double value)
                {
                    ProgressBar bar = new ProgressBar { lowValue = 0, highValue = (float)ceiling, value = (float)value, title = "" };
                    bar.AddToClassList("balance-dps-meter"); card.Add(bar);
                }
                int count = unit.Mounts.Distinct().Count();
                Button hardpoints = new Button { text = "View hardpoints (" + count + ")", name = "compare-view-hardpoints" };
                hardpoints.clicked += () => UnityEditor.PopupWindow.Show(hardpoints.worldBound, new BalanceHardpointsPopup(unit, registry, state, edit));
                hardpoints.SetEnabled(count > 0);
                card.Add(hardpoints);
                if (openUnit != null)
                {
                    Button details = new Button(() => openUnit(unit)) { text = "Edit unit", name = "units-edit-unit" };
                    details.AddToClassList("balance-unit-edit"); card.Add(details);
                }
            }
            BalanceCompareSelection.Update(grid, state);
            Label assumptions = Text(panel, "Draft values · DPS is an AI estimate before accuracy, target modifiers, firing arcs and abilities.", "balance-compare-assumptions");
            assumptions.tooltip = "Base DPS = mounts × damage × shots per salvo / reload. This estimate does not simulate full firing-sequence timing, movement, interception or target switching.";
        }

        public static void BuildHardpoints(VisualElement panel, BalanceUnit unit, BalanceRegistration registry, BalanceWindowState state, Action<BalanceField, string> edit)
        {
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, state.Draft);
            foreach (var mount in unit.Mounts.Distinct())
            {
                VisualElement hardpoint = new VisualElement(); hardpoint.AddToClassList("balance-compare-hardpoint"); panel.Add(hardpoint);
                Text(hardpoint, mount.name, "balance-compare-hardpoint-name");
                BalanceField weapon = registry.Fields.Values.FirstOrDefault(field => field.Target == mount && !field.SharedMountSource && field.Stat == "WeaponType");
                if (weapon != null) Editable(hardpoint, "Weapon type", weapon, state, usage, edit);
                else Text(hardpoint, ((EmpireAtWar.ViewComponents.Health.HardPoint)mount).HardPointType.ToString(), "balance-muted");
            }
        }

        private static BalanceField Field(BalanceUnit unit, BalanceRegistration registry, params string[] stats)
        {
            foreach (string stat in stats)
                if (unit.Fields.TryGetValue(stat, out string key)) return registry.Fields[key];
            return null;
        }

        private static void Editable(VisualElement panel, string title, BalanceField field, BalanceWindowState state, BalanceDraftUsage usage, Action<BalanceField, string> edit)
        {
            if (field == null) { Metric(panel, title, "N/A", ""); return; }
            VisualElement container = new VisualElement { userData = field.Key }; container.AddToClassList("balance-compare-edit"); panel.Add(container);
            VisualElement row = new VisualElement(); row.AddToClassList("balance-compare-stat"); container.Add(row);
            Text(row, title, "balance-compare-label");
            string value = field.DraftValue(state.Draft);
            VisualElement control = BalanceFieldView.Control(field, value, usage, next => edit(field, next));
            control.AddToClassList("balance-compare-input"); control.tooltip = field.Context + " · saved: " + BalanceFieldView.Display(field, field.Read()); row.Add(control);
            bool sourceStaged = field.InheritsSource && state.Draft.Changes.Any(change => change.Key == field.SourceKey);
            control.SetEnabled(!sourceStaged);
            if (sourceStaged) Text(container, "Linked to staged shared source", "balance-scope");
            if (field.Shared) Text(container, field.DescribeScope(usage.Users(field)), "balance-scope");
            row.EnableInClassList("balance-changed", value != field.Read());
            string error = usage.Validate(field, value);
            if (error.Length != 0) container.Add(new HelpBox(error, HelpBoxMessageType.Error));
        }

        private static double? Number(string value)
            => double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double number)
                && double.IsFinite(number) ? number : null;

        private static void Metric(VisualElement panel, string title, string value, string unit)
        {
            VisualElement row = new VisualElement { userData = title }; row.AddToClassList("balance-compare-stat"); panel.Add(row);
            Text(row, title, "balance-compare-label");
            Text(row, Number(value) is double number ? number.ToString("N2", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.') + " " + unit : value, "balance-compare-value");
        }

        private static void Section(VisualElement panel, string title) => Text(panel, title, "balance-compare-section");

        private static Label Text(VisualElement panel, string text, string className)
        {
            Label label = new Label(text); label.AddToClassList(className); panel.Add(label); return label;
        }
    }
}
