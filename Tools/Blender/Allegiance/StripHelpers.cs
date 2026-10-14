using System;
using System.IO;
using System.Linq;
using EmpireAtWar.Editor.Rendering;
using UnityEditor;

public static class StripAllegianceHelpers
{
    public static string Main()
    {
        var paths = Enumerable.Range(1, 53)
            .Select(i => $"Assets/Prefabs/Models/Cannons/Empire/AllegianceTurret{i:00}.prefab")
            .Concat(new[]
            {
                "Assets/Prefabs/Models/Ships/Empire/Allegiance.prefab",
                "Assets/Prefabs/Models/Ships/Empire/AllegianceShipView.prefab",
                "Assets/Prefabs/Models/Wrecks/AllegianceWreckView.prefab",
                "Assets/Prefabs/Ui/Reinforcement/AllegianceReinforcementView.prefab"
            }).ToArray();
        var report = UnitHelperMeshStripper.Strip(paths, Array.Empty<string>());
        if (UnitHelperMeshStripper.FindHelpers(paths, Array.Empty<string>()).Any(h => h.BlockedBy == null))
            throw new InvalidOperationException("Allegiance still contains unused helper meshes.");
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/AllegianceImport/HelperStrip.txt", report);
        return report;
    }
}
