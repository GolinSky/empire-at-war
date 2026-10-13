using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;

public static class AuditBindings
{
    public static object Run()
    {
        var registry = BalanceInventory.BuildCombat();
        using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
        {
            var rows = registry.Units.Select(unit =>
            {
                try
                {
                    var definition = factory.Build(unit);
                    return unit.Id + " | " + unit.Caption + " | points=" + definition.HardPoints.Count +
                        " mounts=" + definition.Mounts.Count + " | " + definition.Warning;
                }
                catch (Exception exception) { return "ERROR | " + unit.Id + " | " + exception.Message; }
            }).ToArray();
            Directory.CreateDirectory("Library/BattleSimulator");
            File.WriteAllLines("Library/BattleSimulator/binding-audit.txt", rows);
            return new { Total = rows.Length, Valid = rows.Count(row => !row.StartsWith("ERROR")), Errors = rows.Where(row => row.StartsWith("ERROR")).ToArray() };
        }
    }
}
