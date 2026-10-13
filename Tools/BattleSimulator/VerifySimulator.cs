using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;

public static class VerifySimulator
{
    public static object Run()
    {
        var registry = BalanceInventory.BuildCombat();
        using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
        {
            var roster = registry.Units.First(u => u.Faction == "Republic" && u.Class == ShipClass.Corvette.ToString());
            var definition = factory.Build(roster);
            var definitions = new Dictionary<string, SimUnitDefinition> { { definition.Id, definition } };
            var setup = new BattleSetup { Runs = 1000,
                SideA = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = definition.Id } },
                SideB = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = definition.Id } } };
            var runner = new MonteCarloRunner(setup, definitions, factory.Matrix);
            var watch = Stopwatch.StartNew();
            while (!runner.IsComplete) runner.Advance(8);
            watch.Stop();
            if (runner.Summary.Completed != 1000) throw new InvalidOperationException("Batch incomplete.");
            string export = BattleExport.Write(runner.Summary, setup);
            if (Directory.GetFiles(export).Length != 5) throw new InvalidOperationException("Missing export files.");
            var cancelled = new MonteCarloRunner(setup, definitions, factory.Matrix);
            cancelled.Advance(1); cancelled.Cancel(); int completed = cancelled.Summary.Completed;
            cancelled.Advance();
            if (!cancelled.IsComplete || cancelled.Summary.Completed != completed) throw new InvalidOperationException("Cancel continued running.");
            var report = new { Unit = definition.Name, Runs = runner.Summary.Completed, Milliseconds = watch.ElapsedMilliseconds,
                UnderFiveSeconds = watch.ElapsedMilliseconds < 5000, runner.Summary.WinsA, runner.Summary.WinsB,
                runner.Summary.Draws, runner.Summary.Mean, runner.Summary.Median, runner.Summary.P10, runner.Summary.P90,
                Cancellation = "passed", Export = export };
            File.WriteAllText("Library/BattleSimulator/verification.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return report;
        }
    }
}
