using System.IO;
using System.Linq;
using UnityEditor;
using EmpireAtWar.Editor.Rendering;

public static class StripTectorHelpers
{
    public static string Main()
    {
        string[] paths={"Assets/Prefabs/Models/Ships/Tector.prefab","Assets/Prefabs/Models/Ships/TectorShipView.prefab","Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab"};
        var locked=UnitHelperMeshStripper.FindUnitPrefabPaths().Except(paths).ToArray();
        var report=UnitHelperMeshStripper.Strip(paths,locked);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/TectorImport/HelperStrip.txt",report);
        return report;
    }
}
