using System.Globalization;
using System.Linq;
using EmpireAtWar.Entities.Ship.Data;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.Balance
{
    // Global data: every shield regen tier table side by side (rows = tiers, columns = tables) so factions can be compared and edited.
    public static class BalanceShieldRegenTiersView
    {
        private static readonly ShieldRegenTier[] TIERS =
        {
            ShieldRegenTier.Corvette, ShieldRegenTier.Frigate, ShieldRegenTier.Cruiser, ShieldRegenTier.HeavyCruiser,
            ShieldRegenTier.Capital, ShieldRegenTier.BattleCruiser, ShieldRegenTier.Dreadnought
        };

        public static bool Contains(BalanceField field) => field.Stat.StartsWith(BalanceUnitAdapter.SHIELD_REGEN_TIER_VALUE);

        public static void Build(VisualElement panel, BalanceRegistration registry, BalanceEditorController controller, BalanceDraftUsage usage)
        {
            var tables = registry.Fields.Values.Where(Contains)
                .GroupBy(field => field.Target)
                .OrderBy(group => group.Key.name)
                .Select(group => group.ToDictionary(field => field.Stat))
                .ToList();
            if (tables.Count == 0) return;

            Label title = new Label("SHIELD REGEN PER TIER (HP / TICK)");
            title.AddToClassList("balance-section-title");
            panel.Add(title);
            VisualElement grid = new VisualElement { name = "shield-regen-tiers" };
            grid.AddToClassList("balance-regen-grid");
            panel.Add(grid);

            VisualElement header = Row(grid);
            Cell(header, "Tier", "balance-regen-tier");
            foreach (var table in tables)
            {
                string name = table.Values.First().Target.name.Replace("ShipShieldRegenTiersData", "").Replace("ShieldRegenTiersData", "");
                Cell(header, name, "balance-regen-head").tooltip = table.Values.First().AssetPath;
            }

            foreach (ShieldRegenTier tier in TIERS)
            {
                string stat = BalanceUnitAdapter.SHIELD_REGEN_TIER_VALUE + (int)tier;
                VisualElement row = Row(grid);
                Cell(row, tier.ToString(), "balance-regen-tier");
                var fields = tables.Select(table => table[stat]).ToList();
                double average = fields.Average(field => Value(field, controller.State.Draft));
                foreach (BalanceField field in fields)
                {
                    VisualElement cell = new VisualElement();
                    cell.AddToClassList("balance-regen-cell");
                    row.Add(cell);
                    string saved = field.Read();
                    string value = field.DraftValue(controller.State.Draft, saved);
                    VisualElement control = BalanceFieldView.Control(field, value, usage, next => controller.Edit(field, next));
                    control.AddToClassList("balance-compare-input");
                    control.tooltip = field.Context + " · saved: " + saved;
                    cell.Add(control);
                    double delta = average > 0 ? (Value(field, controller.State.Draft) / average - 1) * 100 : 0;
                    Label versus = new Label(delta.ToString("+0;-0;0", CultureInfo.InvariantCulture) + "% vs avg");
                    versus.AddToClassList("balance-muted");
                    cell.Add(versus);
                }
            }
        }

        private static double Value(BalanceField field, BalanceDraft draft) =>
            double.Parse(field.DraftValue(draft), CultureInfo.InvariantCulture);

        private static VisualElement Row(VisualElement grid)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("balance-regen-row");
            grid.Add(row);
            return row;
        }

        private static Label Cell(VisualElement row, string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            row.Add(label);
            return label;
        }
    }
}
