using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace EmpireAtWar.Editor.BattleSimulator
{
    public static class BattleExport
    {
        public const string LIMITATIONS = "Stationary, all in range; no abilities, hangar launches, missile interception or attack-run restrictions. Level unlocks only; no research multipliers. Arc factor rounds the number of bearing mounts. TTK excludes timeouts. Detailed results show the first seed.";
        private static string N(double number) => double.IsNaN(number) ? "n/a" : number.ToString("0.###", CultureInfo.InvariantCulture);
        private static string Cell(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
        private static string Md(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        public static string Markdown(BattleSummary summary, BattleSetup setup)
        {
            var text = new StringBuilder();
            text.AppendLine("# Battle Simulator");
            text.AppendLine($"\nData: {(setup.UseDraft ? "Balance draft snapshot" : "Live asset snapshot")}; seed {setup.Seed}; completed {summary.Completed}/{setup.Runs}; cancelled {summary.Cancelled}.");
            text.AppendLine($"\nWins A/B/draw: {summary.WinsA}/{summary.WinsB}/{summary.Draws}. " + Rates(summary));
            text.AppendLine($"\nTTK seconds (resolved battles): mean {N(summary.Mean)}, median {N(summary.Median)}, p10 {N(summary.P10)}, p90 {N(summary.P90)}.");
            text.AppendLine($"\nLimit {N(setup.TimeLimit)} s; distance {N(setup.Distance)} (-1 = shooter range); alpha strike {setup.AlphaStrike}; targets {setup.PolicyA}/{setup.PolicyB}; subsystem {setup.HardPointFocus}.");
            foreach (var side in new[] { ("A", setup.SideA), ("B", setup.SideB) })
                foreach (BattleFleetEntry entry in side.Item2)
                    text.AppendLine($"- {side.Item1}: {Md(entry.UnitId)} × {entry.Quantity}; level {entry.Level}; bearing {N(entry.ArcFactor * 100)}%.");
            text.AppendLine("\n" + LIMITATIONS);
            if (summary.FirstResult == null) return text.ToString();
            BattleResult result = summary.FirstResult;
            text.AppendLine($"\nFirst seed {result.Seed}: {(result.Winner < 0 ? "Draw" : result.Winner == 0 ? "Side A wins" : "Side B wins")}, {result.Reason}, {N(result.Duration)} s.");
            text.AppendLine("\n| Side / unit | Hull | Shields | Destroyed (s) | Dealt | Taken shield / hull / subsystem | Hits / shots | Realized / base DPS | Wasted raw damage |\n|---|---:|---:|---:|---:|---|---:|---:|---:|");
            foreach (UnitCombatStats unit in result.Units)
                text.AppendLine($"| {(unit.Side == 0 ? "A" : "B")} #{unit.Index + 1} {Md(unit.Name)} | {N(unit.Hull)} / {N(unit.MaxHull)} | {N(unit.Shields)} / {N(unit.MaxShields)} | {(unit.DestroyedAt < 0 ? "survived" : N(unit.DestroyedAt))} | {N(unit.DamageDealt)} | {N(unit.ShieldDamage)} / {N(unit.HullDamage)} / {N(unit.HardPointDamage)} | {unit.Hits} / {unit.Shots} | {N(RealizedDps(unit, result))} / {N(unit.BaseDps)} | {N(unit.Overkill)} |");
            text.AppendLine("\nWasted raw damage counts queued impacts on already destroyed targets/members. Subsystem damage overlaps hull damage; do not sum all three taken columns. Realized DPS uses total battle duration.");
            text.AppendLine("\n## Events");
            foreach (BattleEvent entry in result.Events)
                text.AppendLine($"- {N(entry.Time)} s · #{entry.UnitIndex + 1} · {entry.Kind} · {Md(entry.Detail)}");
            return text.ToString();
        }

        public static double RealizedDps(UnitCombatStats unit, BattleResult result) => result.Duration <= 0 ? double.NaN : unit.DamageDealt / result.Duration;

        public static string Rates(BattleSummary summary) => summary.Completed == 0 ? "No completed runs." :
            $"Win rates A {N(100.0 * summary.WinsA / summary.Completed)}%, B {N(100.0 * summary.WinsB / summary.Completed)}%, draw {N(100.0 * summary.Draws / summary.Completed)}%.";

        public static string UnitsCsv(BattleResult result)
        {
            var text = new StringBuilder("seed,side,index,unit_id,name,hull,max_hull,shields,max_shields,destroyed_at,damage_dealt,shield_taken,hull_taken,hardpoint_taken,hits,shots,realized_dps,base_dps,wasted_raw_damage\n");
            foreach (UnitCombatStats unit in result.Units)
                text.AppendLine(string.Join(",", result.Seed, unit.Side == 0 ? "A" : "B", unit.Index + 1, Cell(unit.UnitId), Cell(unit.Name),
                    N(unit.Hull), N(unit.MaxHull), N(unit.Shields), N(unit.MaxShields), N(unit.DestroyedAt), N(unit.DamageDealt),
                    N(unit.ShieldDamage), N(unit.HullDamage), N(unit.HardPointDamage), unit.Hits, unit.Shots,
                    N(RealizedDps(unit, result)), N(unit.BaseDps), N(unit.Overkill)));
            return text.ToString();
        }

        public static string Write(BattleSummary summary, BattleSetup setup)
        {
            string directory = Path.GetFullPath(Path.Combine("Library/BattleSimulator", DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6)));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "results.md"), Markdown(summary, setup));
            File.WriteAllText(Path.Combine(directory, "summary.csv"), "completed,cancelled,wins_a,wins_b,draws,mean_ttk,median_ttk,p10_ttk,p90_ttk\n" +
                string.Join(",", summary.Completed, summary.Cancelled, summary.WinsA, summary.WinsB, summary.Draws, N(summary.Mean), N(summary.Median), N(summary.P10), N(summary.P90)) + "\n");
            if (summary.FirstResult == null) return directory;
            BattleResult result = summary.FirstResult;
            File.WriteAllText(Path.Combine(directory, "units.csv"), UnitsCsv(result));
            File.WriteAllLines(Path.Combine(directory, "timeline.csv"), new[] { "time,hull_a,shields_a,hull_b,shields_b" }.Concat(
                result.Timeline.Select(s => string.Join(",", N(s.Time), N(s.HullA), N(s.ShieldsA), N(s.HullB), N(s.ShieldsB)))));
            File.WriteAllLines(Path.Combine(directory, "events.csv"), new[] { "time,unit_index,kind,detail" }.Concat(
                result.Events.Select(e => string.Join(",", N(e.Time), e.UnitIndex + 1, Cell(e.Kind), Cell(e.Detail)))));
            return directory;
        }
    }
}
