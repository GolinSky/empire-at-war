using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class ISDIRemakeBaseline
{
    public static string Main()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDIShipView.prefab");
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        var data = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Ship/ISDIShipData.asset");
        string[] paths = {"Assets/Prefabs/Models/Ships/ISDI.prefab", "Assets/Prefabs/Models/Ships/ISDIShipView.prefab", "Assets/Prefabs/Models/Wrecks/ISDIWreckView.prefab", "Assets/Prefabs/Ui/Reinforcement/ISDIReinforcementView.prefab", "Assets/Settings/Data/Ship/ISDIShipData.asset", "Assets/Settings/Data/Ship/Wreck/ISDIWreckData.asset", "Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIcon.png", "Assets/Art/Textures/Ui/Icons/ShipIcon/ISDISilhouette.png"};
        var report = new JObject { ["dataJson"] = EditorJsonUtility.ToJson(data), ["length"] = bounds.size.z, ["bounds"] = bounds.ToString(), ["guids"] = JObject.FromObject(paths.ToDictionary(p => p, AssetDatabase.AssetPathToGUID)) };
        File.WriteAllText("Temp/ISDIRemakeImport/Baseline.json", report.ToString());
        return report.ToString();
    }
}
