using UnityEditor;
using EmpireAtWar.Editor.Rendering;

public static class StripDauntlessHelpers
{
    public static string Main()
    {
        var result=UnitHelperMeshStripper.Strip(new[]{"Assets/Prefabs/Models/Ships/Rebellion/Dauntless.prefab","Assets/Prefabs/Models/Ships/Rebellion/DauntlessShipView.prefab","Assets/Prefabs/Models/Wrecks/DauntlessWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/DauntlessReinforcementView.prefab"},new string[0]);
        AssetDatabase.SaveAssets();return result;
    }
}
