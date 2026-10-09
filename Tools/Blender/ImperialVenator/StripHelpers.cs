using System.IO;
using System.Linq;
using UnityEditor;
using EmpireAtWar.Editor.Rendering;

public static class StripImperialVenatorHelpers
{
    public static string Main()
    {
        string[] paths = { "Assets/Prefabs/Models/Ships/ImperialVenator.prefab", "Assets/Prefabs/Models/Ships/ImperialVenatorDeath.prefab", "Assets/Prefabs/Models/Ships/ImperialVenatorShipView.prefab", "Assets/Prefabs/Models/Wrecks/ImperialVenatorWreckView.prefab", "Assets/Prefabs/Ui/Reinforcement/ImperialVenatorReinforcementView.prefab" };
        var locked = UnitHelperMeshStripper.FindUnitPrefabPaths().Except(paths).ToArray();
        var report = UnitHelperMeshStripper.Strip(paths, locked);
        AssetDatabase.SaveAssets(); File.WriteAllText("Temp/ImperialVenatorImport/HelperStrip.txt", report); return report;
    }
}
