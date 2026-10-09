using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    public static class BalanceCompareSummary
    {
        private static readonly string[][] DURABILITY = { new[] { "Hull", "MemberHull" }, new[] { "Shields", "MemberShields" } };

        public static void Build(VisualElement panel, List<BalanceUnit> units, BalanceRegistration registry,
            BalanceEditorController controller, bool unitsTab)
        {
            BalanceDraft draft = controller.State.Draft;
            VisualElement grid = new VisualElement { name = "compare-grid" };
            grid.AddToClassList("balance-compare-grid");
            panel.Add(grid);
            BalanceCompareSelection.Bind(grid, controller);

            BalanceDpsEstimator estimator = new BalanceDpsEstimator(registry);
            List<BalanceDpsEstimate> dps = units.Select(unit => estimator.Estimate(unit, draft)).ToList();
            double ceiling = dps.Where(estimate => estimate.HasValue).Select(estimate => estimate.Value).Append(1).Max();
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, draft);
            float[] maxima = DURABILITY
                .Select(stats => units.Select(unit => Field(unit, registry, stats))
                    .Where(field => field != null)
                    .Select(field => BalanceStatSlider.Maximum(field, draft))
                    .DefaultIfEmpty(100)
                    .Max())
                .ToArray();
            for (int i = 0; i < units.Count; i++)
                Card(grid, units[i], dps[i], ceiling, maxima, registry, controller, usage, unitsTab);

            BalanceCompareSelection.Update(grid, controller.State);
            Label assumptions = Text(panel, "Draft values · DPS is an AI estimate before accuracy, target modifiers, firing arcs and abilities.", "balance-compare-assumptions");
            assumptions.tooltip = BalanceDpsEstimator.FORMULA + " This estimate does not simulate projectile travel, movement, interception or target switching.";
        }

        private static void Card(VisualElement grid, BalanceUnit unit, BalanceDpsEstimate dps, double ceiling, float[] maxima,
            BalanceRegistration registry, BalanceEditorController controller, BalanceDraftUsage usage, bool unitsTab)
        {
            BalanceDraft draft = controller.State.Draft;
            Action<BalanceField, string> edit = controller.Edit;
            VisualElement slot = new VisualElement { userData = unit.Id };
            slot.AddToClassList("balance-compare-slot");
            grid.Add(slot);
            VisualElement card = new VisualElement { userData = unit.Id, focusable = true };
            card.AddToClassList("balance-compare-card");
            slot.Add(card);
            card.tooltip = "Click to select; Ctrl+click to toggle; Shift+click to select a range. Ctrl+A selects all; Delete removes selected units. Drag the background to reorder.";
            BalanceCompareDrag.Bind(card, slot, grid, controller,
                (additive, range) => BalanceCompareSelection.Select(grid, controller, unit.Id, additive, range));
            Heading(card, unit, controller);
            Text(card, unit.Faction + " · " + unit.Class, "balance-compare-caption");

            Section(card, "Durability");
            for (int stat = 0; stat < DURABILITY.Length; stat++)
            {
                BalanceField field = Field(unit, registry, DURABILITY[stat]);
                VisualElement row = new VisualElement();
                row.AddToClassList("balance-compare-health");
                card.Add(row);
                string label = stat == 0 ? "Health" : "Shield";
                Text(row, label + (unit.Kind == BalanceUnitKind.Squadron ? " / member · HP" : " · HP"), "balance-muted");
                if (field == null)
                {
                    Text(row, "N/A", "balance-compare-value");
                    continue;
                }
                row.Add(BalanceStatSlider.Create(field, draft, value => edit(field, value), maxima[stat]));
                if (field.Shared) Text(row, field.DescribeScope(field.Users), "balance-scope");
            }

            Section(card, "Mobility & economy");
            Editable(card, "Speed (u/s)", Field(unit, registry, "Speed", "CruiseSpeed"), draft, usage, edit);
            Editable(card, "Height level", Field(unit, registry, "HeightTier", "Height"), draft, usage, edit);
            Editable(card, "Cost (credits)", Field(unit, registry, "Price"), draft, usage, edit);
            Editable(card, "Build time (s)", Field(unit, registry, "BuildTime"), draft, usage, edit);

            Section(card, "Firepower");
            Editable(card, "Weapon range", Field(unit, registry, unit.Kind == BalanceUnitKind.Ship ? "Range" : "WeaponRange"), draft, usage, edit);
            Metric(card, "Base DPS", dps.ToString(), "dmg/s");
            if (dps.HasValue)
            {
                ProgressBar bar = new ProgressBar { lowValue = 0, highValue = (float)ceiling, value = (float)dps.Value, title = "" };
                bar.AddToClassList("balance-dps-meter");
                card.Add(bar);
            }

            int count = unit.Mounts.Distinct().Count();
            Button hardpoints = new Button { text = "View hardpoints (" + count + ")", name = "compare-view-hardpoints" };
            hardpoints.clicked += () => UnityEditor.PopupWindow.Show(hardpoints.worldBound, new BalanceHardpointsPopup(unit, registry, controller));
            hardpoints.SetEnabled(count > 0);
            card.Add(hardpoints);
            if (!unitsTab) return;
            Button details = new Button(() => controller.OpenUnit(unit.Id)) { text = "Edit unit", name = "units-edit-unit" };
            details.AddToClassList("balance-unit-edit");
            card.Add(details);
        }

        private static void Heading(VisualElement card, BalanceUnit unit, BalanceEditorController controller)
        {
            VisualElement heading = new VisualElement();
            heading.AddToClassList("balance-compare-card-heading");
            card.Add(heading);
            heading.Add(new Label("↕") { name = "compare-drag-handle", tooltip = "Drag to reorder; Escape cancels", focusable = true });
            if (unit.Icon != null)
            {
                Image icon = new Image { name = "unit-icon", sprite = unit.Icon, scaleMode = UnityEngine.ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                icon.AddToClassList("balance-unit-icon");
                heading.Add(icon);
            }

            Text(heading, unit.Name, "balance-compare-unit-name");
            Button remove = new Button(() => controller.RemovePin(unit.Id)) { text = "×", name = "compare-remove-unit", tooltip = "Remove " + unit.Name };
            remove.AddToClassList("balance-compare-remove");
            heading.Add(remove);
        }

        public static void BuildHardpoints(VisualElement panel, BalanceUnit unit, BalanceRegistration registry, BalanceDraft draft,
            Action<BalanceField, string> edit)
        {
            BalanceDraftUsage usage = new BalanceDraftUsage(registry, draft);
            foreach (UnityEngine.Object mount in unit.Mounts.Distinct())
            {
                VisualElement hardpoint = new VisualElement();
                hardpoint.AddToClassList("balance-compare-hardpoint");
                panel.Add(hardpoint);
                Text(hardpoint, mount.name, "balance-compare-hardpoint-name");
                BalanceField weapon = registry.FieldsFor(mount).FirstOrDefault(field => !field.SharedMountSource && field.Stat == "WeaponType");
                if (weapon != null) Editable(hardpoint, "Weapon type", weapon, draft, usage, edit);
                else Text(hardpoint, ((EmpireAtWar.ViewComponents.Health.HardPoint)mount).HardPointType.ToString(), "balance-muted");
            }
        }

        private static BalanceField Field(BalanceUnit unit, BalanceRegistration registry, params string[] stats)
        {
            foreach (string stat in stats)
            {
                if (unit.Fields.TryGetValue(stat, out string key)) return registry.Fields[key];
            }
            return null;
        }

        private static void Editable(VisualElement panel, string title, BalanceField field, BalanceDraft draft,
            BalanceDraftUsage usage, Action<BalanceField, string> edit)
        {
            if (field == null)
            {
                Metric(panel, title, "N/A", "");
                return;
            }

            VisualElement container = new VisualElement { userData = field.Key };
            container.AddToClassList("balance-compare-edit");
            panel.Add(container);
            VisualElement row = new VisualElement();
            row.AddToClassList("balance-compare-stat");
            container.Add(row);
            Text(row, title, "balance-compare-label");
            string value = field.DraftValue(draft);
            string saved = field.Read();
            VisualElement control = BalanceFieldView.Control(field, value, usage, next => edit(field, next));
            control.AddToClassList("balance-compare-input");
            control.tooltip = field.Context + " · saved: " + BalanceFieldView.Display(field, saved);
            row.Add(control);
            bool sourceStaged = field.InheritsSource && draft.Changes.Any(change => change.Key == field.SourceKey);
            control.SetEnabled(!sourceStaged);
            if (sourceStaged) Text(container, "Linked to staged shared source", "balance-scope");
            if (field.Shared) Text(container, field.DescribeScope(usage.Users(field)), "balance-scope");
            row.EnableInClassList("balance-changed", value != saved);
            string error = usage.Validate(field, value);
            if (error.Length != 0) container.Add(new HelpBox(error, HelpBoxMessageType.Error));
        }

        private static double? Number(string value) =>
            double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double number)
            && double.IsFinite(number) ? number : null;

        private static void Metric(VisualElement panel, string title, string value, string unit)
        {
            VisualElement row = new VisualElement { userData = title };
            row.AddToClassList("balance-compare-stat");
            panel.Add(row);
            Text(row, title, "balance-compare-label");
            string text = Number(value) is double number
                ? number.ToString("N2", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.') + " " + unit
                : value;
            Text(row, text, "balance-compare-value");
        }

        private static void Section(VisualElement panel, string title) => Text(panel, title, "balance-compare-section");

        private static Label Text(VisualElement panel, string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            panel.Add(label);
            return label;
        }
    }
}
