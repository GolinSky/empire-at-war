using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class VerifyCisStationMapping
{
    private const string TASK = "Temp/CisStationImport/";
    private const string VIEW = "Assets/Prefabs/Models/Stations/SeparatistSpaceStationView.prefab";

    public static string Main()
    {
        var baseline = JObject.Parse(File.ReadAllText(TASK + "Baseline.json"));
        foreach (var row in ((JObject)baseline["hashes"]).Properties().Where(p => p.Name.StartsWith("Assets/Settings/")))
            using (var hash = SHA256.Create())
                if ((string)row.Value != BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(row.Name))))
                    throw new InvalidOperationException("Shared station gameplay data changed: " + row.Name);
        var mapping = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/AssetMappingData.asset")).FindProperty("assetMappings.keyValue");
        var routes = new JObject();
        for (int i = 0; i < mapping.arraySize; i++)
        {
            var row = mapping.GetArrayElementAtIndex(i);
            string key = row.FindPropertyRelative("key").stringValue;
            if (key.EndsWith("SpaceStationView")) routes[key] = row.FindPropertyRelative("value.m_AssetGUID").stringValue;
        }
        if (!JToken.DeepEquals(baseline["routes"], routes)) throw new InvalidOperationException("Station faction routing changed.");
        string guid = AssetDatabase.AssetPathToGUID(VIEW);
        if ((string)routes["SeparatistSpaceStationView"] != guid || routes.Properties().Any(p => p.Name != "SeparatistSpaceStationView" && (string)p.Value == guid))
            throw new InvalidOperationException("CIS station is not isolated to Separatist.");
        if (AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address != "SeparatistSpaceStationView")
            throw new InvalidOperationException("CIS station Addressables address changed.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var hardpoints = new JArray(prefab.GetComponentsInChildren<HardPoint>(true).OrderBy(h => h.Id).Select(h =>
        {
            h.TryGetWeaponType(out var weapon);
            return new JObject { ["id"] = h.Id, ["name"] = h.name, ["level"] = h.UnlockLevel, ["type"] = h.HardPointType.ToString(), ["weapon"] = weapon.ToString() };
        }));
        if (!JToken.DeepEquals(baseline["hardpoints"], hardpoints)) throw new InvalidOperationException("Existing CIS gameplay hardpoints changed.");
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        var results = new JArray();
        try
        {
            var view = root.GetComponent<StationLevelView>();
            var models = root.GetComponentsInChildren<StationLevelModel>(true);
            if (root.transform.localScale != Vector3.one || root.transform.localRotation != Quaternion.identity || models.Length != 5) throw new InvalidOperationException("CIS station model/root setup is incomplete.");
            foreach (int level in new[] {1, 5, 2, 3, 4, 5, 1})
            {
                view.ApplyLevel(level);
                var current = view.CurrentModel;
                if (current.name != "CisSpaceStationLevel" + level || models.Count(m => m.gameObject.activeSelf) != 1)
                    throw new InvalidOperationException("Incorrect CIS model at level " + level);
                var bounds = current.HullBounds;
                if (Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x), Mathf.Abs(bounds.min.z), Mathf.Abs(bounds.max.z)) > 396f)
                    throw new InvalidOperationException("CIS station exceeds the project station clearance.");
                results.Add(new JObject { ["level"] = level, ["model"] = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(current.gameObject),
                    ["mounts"] = current.Mounts.Length, ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                    ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z) });
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        File.WriteAllText(TASK + "VerifiedMapping.json", results.ToString());
        return "Seven level transitions passed; CIS-only routing, original prefab GUID, 17 gameplay hardpoints and shared combat/economy data preserved.\n" + results;
    }
}
