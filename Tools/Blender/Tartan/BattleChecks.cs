using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;

public static class TartanBattleChecks
{
    public static object Main()
    {
        var registry = BalanceInventory.BuildCombat();
        using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
        {
            var tartan = factory.Build(registry.Units.Single(u => u.Name == "Tartan-class Patrol Cruiser"));
            var results = new List<object>();
            foreach (var name in new[] {"Venator", "Victory II Star Destroyer", "Mon Calamari Star Cruiser / MC80"})
            {
                var enemyRow = registry.Units.Single(u => name == "Victory II Star Destroyer" ? u.Name.StartsWith(name) : u.Name.Trim() == name);
                var enemy = factory.Build(enemyRow);
                var definitions = new Dictionary<string, SimUnitDefinition> {{tartan.Id, tartan}, {enemy.Id, enemy}};
                var setup = new BattleSetup {Runs = 10,
                    SideA = new List<BattleFleetEntry> {new BattleFleetEntry {UnitId = tartan.Id}},
                    SideB = new List<BattleFleetEntry> {new BattleFleetEntry {UnitId = enemy.Id}}};
                var runner = new MonteCarloRunner(setup, definitions, factory.Matrix);
                while (!runner.IsComplete) runner.Advance(8);
                if (runner.Summary.Completed != 10) throw new InvalidOperationException("Incomplete Tartan comparison.");
                results.Add(new {Opponent = name, runner.Summary.Completed, runner.Summary.WinsA, runner.Summary.WinsB, runner.Summary.Draws, runner.Summary.Mean});
            }
            File.WriteAllText("Temp/TartanImport/BattleChecks.json", Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented));
            return results;
        }
    }
}
