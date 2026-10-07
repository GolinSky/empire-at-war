using System.Linq;
using System.IO;
using System.Security.Cryptography;
using EmpireAtWar.ViewComponents.Health;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class CaptureCisStationBaseline
{
    public static string Main()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Stations/SeparatistSpaceStationView.prefab");
        var hardpoints = new JArray(root.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).Select(h =>
        {
            h.TryGetWeaponType(out var weapon);
            return new JObject { ["id"] = h.Id, ["name"] = h.name, ["level"] = h.UnlockLevel, ["type"] = h.HardPointType.ToString(), ["weapon"] = weapon.ToString() };
        }));
        var mapping = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/AssetMappingData.asset")).FindProperty("assetMappings.keyValue");
        var routes = new JObject();
        for (int i = 0; i < mapping.arraySize; i++)
        {
            var row = mapping.GetArrayElementAtIndex(i);
            string key = row.FindPropertyRelative("key").stringValue;
            if (key.EndsWith("SpaceStationView")) routes[key] = row.FindPropertyRelative("value.m_AssetGUID").stringValue;
        }
        var hashes = new JObject();
        foreach (string path in new[] {"Assets/Settings/Data/Factions/Shared/StationLevelData.asset", "Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset"}
            .Concat(new[] {"Republic", "Rebellion", "Empire"}.Select(f => "Assets/Prefabs/Models/Stations/" + f + "SpaceStationView.prefab")))
            using (var hash = SHA256.Create()) hashes[path] = System.BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)));
        var renderers = new SerializedObject(root.GetComponent<EmpireAtWar.Entities.SpaceStation.SpaceStation>()).FindProperty("explosionHullRenderers");
        Bounds bounds = ((Renderer)renderers.GetArrayElementAtIndex(0).objectReferenceValue).bounds;
        for (int i = 1; i < renderers.arraySize; i++) bounds.Encapsulate(((Renderer)renderers.GetArrayElementAtIndex(i).objectReferenceValue).bounds);
        var snapshot = new JObject { ["hardpoints"] = hardpoints, ["routes"] = routes, ["hashes"] = hashes,
            ["targetDiameter"] = Mathf.Max(bounds.size.x, bounds.size.z) };
        File.WriteAllText("Temp/CisStationImport/Baseline.json", snapshot.ToString());
        return snapshot.ToString();
    }
}
