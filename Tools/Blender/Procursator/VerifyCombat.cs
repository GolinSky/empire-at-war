using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;
using Newtonsoft.Json;

public static class VerifyProcursatorCombat
{
    public static object Main()
    {
        var registry = BalanceInventory.BuildCombat();
        var reports = new List<object>();
        using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
        {
            var procursator = factory.Build(registry.Units.Single(u => u.Data.name == "ProcursatorShipData"));
            if (procursator.Mounts.Count != 15 || procursator.HardPoints.Count != 17)
                throw new InvalidOperationException("Procursator runtime snapshot does not contain all mounts and systems.");
            foreach (var name in new[] { "Venator", "VictoryII", "MonCalCruiser" })
            {
                var enemy = factory.Build(registry.Units.Single(u => u.Data.name == name + "ShipData"));
                var definitions = new Dictionary<string, SimUnitDefinition> { { procursator.Id, procursator }, { enemy.Id, enemy } };
                var setup = new BattleSetup { Runs = 10,
                    SideA = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = procursator.Id } },
                    SideB = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = enemy.Id } } };
                var runner = new MonteCarloRunner(setup, definitions, factory.Matrix);
                while (!runner.IsComplete) runner.Advance(8);
                if (runner.Summary.Completed != 10) throw new InvalidOperationException("Incomplete match: " + name);
                reports.Add(new { Opponent = name, Runs = runner.Summary.Completed, runner.Summary.WinsA,
                    runner.Summary.WinsB, runner.Summary.Draws, runner.Summary.Mean, runner.Summary.Median });
            }
        }
        var report = new { Scenarios = reports, Limits = "Read-only stationary battle simulator; no movement, arcs, abilities or hangar launches." };
        File.WriteAllText("Temp/ProcursatorImport/CombatVerification.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }
}
