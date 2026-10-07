using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Rendering;
using UnityEditor;

public static class StripExecutorHelpers
{
    public static string Main()
    {
        var paths = Enumerable.Range(1, 10)
            .Select(i => $"Assets/Prefabs/Models/Ships/ExecutorTurret{i:00}.prefab")
            .Concat(new[]
            {
                "Assets/Prefabs/Models/Ships/Executor.prefab",
                "Assets/Prefabs/Models/Ships/ExecutorShipView.prefab",
                "Assets/Prefabs/Models/Wrecks/ExecutorWreckView.prefab",
                "Assets/Prefabs/Ui/Reinforcement/ExecutorReinforcementView.prefab"
            }).ToArray();
        var report = UnitHelperMeshStripper.Strip(paths, Array.Empty<string>());
        if (UnitHelperMeshStripper.FindHelpers(paths, Array.Empty<string>()).Any(h => h.BlockedBy == null))
            throw new InvalidOperationException("Executor still contains unused helper meshes.");
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/ExecutorImport/HelperStrip.txt", report);
        return report;
    }
}
