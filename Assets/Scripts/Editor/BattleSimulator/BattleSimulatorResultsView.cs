using System.Linq;
using UnityEngine.UIElements;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public sealed class BattleSimulatorResultsView : VisualElement
    {
        public BattleSimulatorResultsView(BattleSummary summary)
        {
            AddToClassList("sim-results");
            Add(new Label($"{summary.Completed} completed runs{(summary.Cancelled ? " · Cancelled (partial results)" : "")}") { name = "sim-summary" });
            Add(new Label(BattleExport.Rates(summary)));
            Add(new Label($"TTK: mean {summary.Mean:0.##} s · median {summary.Median:0.##} s · p10 {summary.P10:0.##} s · p90 {summary.P90:0.##} s (timeouts excluded; NaN = no resolved battle)"));
            BattleResult result = summary.FirstResult;
            if (result == null) return;
            Add(new Label($"Seed {result.Seed} · {(result.Winner < 0 ? "Draw" : result.Winner == 0 ? "Side A wins" : "Side B wins")} · {result.Reason} · {result.Duration:0.##} s"));
            foreach (int side in new[] { 0, 1 })
            {
                var units = result.Units.Where(u => u.Side == side).ToArray();
                float hull = units.Sum(u => u.Hull), maxHull = units.Sum(u => u.MaxHull);
                float shields = units.Sum(u => u.Shields), maxShields = units.Sum(u => u.MaxShields);
                Add(new Label($"Side {(side == 0 ? "A" : "B")}: {units.Count(u => u.DestroyedAt < 0)}/{units.Length} survive · fleet hull {100 * hull / maxHull:0.#}% · shields {(maxShields == 0 ? 0 : 100 * shields / maxShields):0.#}%"));
            }
            Add(new Label("Health over time · blue A / orange B · red marks kills"));
            Add(new BattleSimulatorTimelineChart(result));
            Add(new Label($"0 s ───────────────────────────────── {result.Duration:0.##} s"));
            var table = new ScrollView(ScrollViewMode.Horizontal); Add(table);
            Row(table, "Side / unit", "Destroyed", "Dealt", "Taken: shield / hull / subsystem", "Hit rate", "DPS: realized / base", "Hull / shields");
            foreach (UnitCombatStats unit in result.Units)
                Row(table, $"{(unit.Side == 0 ? "A" : "B")} #{unit.Index + 1} {unit.Name}",
                    unit.DestroyedAt < 0 ? "Survived" : unit.DestroyedAt.ToString("0.##") + " s", $"{unit.DamageDealt:0.#}",
                    $"{unit.ShieldDamage:0.#} / {unit.HullDamage:0.#} / {unit.HardPointDamage:0.#}",
                    $"{(unit.Shots == 0 ? 0 : 100.0 * unit.Hits / unit.Shots):0.#}% ({unit.Hits}/{unit.Shots})",
                    $"{BattleExport.RealizedDps(unit, result):0.#} / {unit.BaseDps:0.#}", $"{unit.Hull:0.#} / {unit.Shields:0.#}");
            var log = new Foldout { text = $"Event log ({result.Events.Count})", value = false }; Add(log);
            foreach (BattleEvent entry in result.Events)
                log.Add(new Label($"{entry.Time:0.##} s · #{entry.UnitIndex + 1} {result.Units[entry.UnitIndex].Name} · {entry.Kind} · {entry.Detail}"));
        }

        private static void Row(VisualElement table, params string[] values)
        {
            var row = new VisualElement(); row.AddToClassList("sim-result-row"); table.Add(row);
            for (int i = 0; i < values.Length; i++)
            {
                var label = new Label(values[i]) { tooltip = values[i] };
                label.AddToClassList("sim-result-cell");
                if (i == 0 || i == 3) label.AddToClassList("sim-result-wide");
                row.Add(label);
            }
        }
    }
}
