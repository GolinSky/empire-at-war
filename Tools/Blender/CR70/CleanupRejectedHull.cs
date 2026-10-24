using System;
using System.IO;
using System.Linq;
using UnityEditor;

public static class CleanupCR70RejectedHull
{
    public static string Main()
    {
        const string MATERIALS = "Assets/Art/Materials/Models/RepublicShips/CR70/";
        const string TEXTURES = "Assets/Art/Textures/Models/RepublicShips/CR70/";
        string[] obsolete = {
            MATERIALS + "COLLISION.001.mat",
            MATERIALS + "RV_Corvette Material.001.mat",
            MATERIALS + "RV_Corvette Material1.001.mat",
            MATERIALS + "SHADOW.001.mat",
            MATERIALS + "W_Laser_Small Material.001.mat",
            MATERIALS + "W_Laser_Small Material1.001.mat",
            TEXTURES + "CR70_Flash_Albedo.png",
            TEXTURES + "CR70_Hull_Albedo.png",
            TEXTURES + "CR70_Hull_Normal.png",
            TEXTURES + "CR70_Hull_TeamMask.png",
            "Assets/Art/Materials/Wrecks/CR70/RV_Corvette Material.001_Wreck.mat"
        };
        string[] roots = {
            "Assets/Art/Models/RepublicShips/CR70/CR70.fbx",
            "Assets/Prefabs/Models/Ships/Republic/CR70.prefab",
            "Assets/Prefabs/Models/Ships/Republic/CR70ShipView.prefab",
            "Assets/Prefabs/Models/Wrecks/CR70WreckView.prefab",
            "Assets/Prefabs/Ui/Reinforcement/CR70ReinforcementView.prefab",
            "Assets/Settings/Data/Ship/Republic/CR70ShipData.asset"
        };
        var dependencies = AssetDatabase.GetDependencies(roots, true);
        var retained = obsolete.Intersect(dependencies).ToArray();
        if (retained.Length != 0) throw new InvalidOperationException("Rejected CR90 assets remain referenced: " + string.Join(", ", retained));
        var deleted = obsolete.Where(File.Exists).ToArray();
        foreach (string path in deleted)
            if (!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("Could not remove obsolete CR70 import asset: " + path);
        AssetDatabase.SaveAssets();
        return "Removed " + deleted.Length + " unused assets from the rejected CR90 import; current Charger dependencies preserved.";
    }
}
