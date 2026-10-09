using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class ArchiveISDIVisuals
{
    public static string Main()
    {
        var archived = new List<string>();
        foreach (string parent in new[] {"Assets/Art/Models/EmpireShips", "Assets/Art/Materials/Models/EmpireShips", "Assets/Art/Textures/Models/EmpireShips", "Assets/Art/Materials/Wrecks"})
        {
            string oldPath = parent + "/ISDI", newPath = parent + "/ISDIObsoleteAOTR";
            if (AssetDatabase.IsValidFolder(newPath)) continue;
            string error = AssetDatabase.MoveAsset(oldPath, newPath);
            if (error.Length != 0) throw new InvalidOperationException(error);
            foreach (string guid in AssetDatabase.FindAssets("", new[] {newPath})) Mark(AssetDatabase.GUIDToAssetPath(guid));
            Mark(newPath);
        }
        foreach (string path in new[] {"Assets/Prefabs/Models/Ships/ISDI.prefab", "Assets/Prefabs/Models/Ships/ISDIShipView.prefab", "Assets/Prefabs/Models/Wrecks/ISDIWreckView.prefab", "Assets/Prefabs/Ui/Reinforcement/ISDIReinforcementView.prefab", "Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIcon.png", "Assets/Art/Textures/Ui/Icons/ShipIcon/ISDISilhouette.png", "Assets/Art/Models/Shields/ISDIShipViewShield.asset"})
        {
            string copy = Path.GetDirectoryName(path).Replace('\\','/') + "/" + Path.GetFileNameWithoutExtension(path) + "_ObsoleteAOTR" + Path.GetExtension(path);
            if (!File.Exists(copy) && !AssetDatabase.CopyAsset(path, copy)) throw new InvalidOperationException("Cannot archive " + path);
            Mark(copy);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/ISDIRemakeImport/ObsoleteAssets.json", new JArray(archived).ToString());
        return "Archived/labeled " + archived.Count + " old ISD I visual assets; registration GUIDs retained. Shared ISD II stripe references retain their original GUIDs.";

        void Mark(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            AssetDatabase.SetLabels(asset, AssetDatabase.GetLabels(asset).Concat(new[] {"Obsolete", "ISDI", "SourceAOTR1397421866"}).Distinct().ToArray());
            archived.Add(path);
        }
    }
}
