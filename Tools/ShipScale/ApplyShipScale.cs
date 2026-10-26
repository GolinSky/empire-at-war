using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class ApplyShipScale
{
    public static string Main()
    {
        var before = JArray.Parse(File.ReadAllText("Logs/ShipScale/before.json"));
        var targets = JArray.Parse(File.ReadAllText("Tools/ShipScale/targets.json"));
        float reference = (float)before.Single(r => (string)r["id"] == "Venator")["length"];
        var prefabs = new Dictionary<string, float>();
        var data = new Dictionary<string, float>();
        var report = new JArray();
        foreach (var row in before)
        {
            var target = targets.Single(t => (string)t["id"] == (string)row["id"]);
            float factor = (bool)target["preserve"] ? 1f : reference * (float)target["meters"] / 1137f / (float)row["length"];
            if (Mathf.Abs(factor - 1f) < 0.000001f) factor = 1f;
            report.Add(new JObject { ["id"] = row["id"], ["factor"] = factor,
                ["before"] = row["length"], ["target"] = (float)row["length"] * factor });
            if (factor == 1f) continue;
            foreach (string key in new[] { "path", "wreckPath" })
            {
                string path = (string)row[key];
                if (prefabs.ContainsKey(path) && Mathf.Abs(prefabs[path] - factor) > 0.000001f)
                    throw new InvalidOperationException("Conflicting shared prefab scale: " + path);
                prefabs[path] = factor;
            }
            data[(string)row["dataPath"]] = factor;
        }
        // Fail before any writes if source assets changed after the snapshot.
        foreach (string path in prefabs.Keys.Concat(data.Keys))
            if (!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes("Logs/ShipScale/before-files/" + path)))
                throw new InvalidOperationException("Snapshot mismatch: " + path);
        File.WriteAllText("Logs/ShipScale/multipliers.json", report.ToString());
        foreach (var pair in prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(pair.Key);
            try
            {
                root.transform.localScale *= pair.Value;
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var serialized = new SerializedObject(component);
                    var effectScale = serialized.FindProperty("destroyedVfxScale");
                    if (effectScale == null) continue;
                    effectScale.floatValue *= pair.Value;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach (var particle in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (particle.main.scalingMode == ParticleSystemScalingMode.Hierarchy) continue;
                    if (particle.main.scalingMode != ParticleSystemScalingMode.Local || particle.transform.childCount != 0 ||
                        particle.GetComponents<Component>().Length != 3)
                        throw new InvalidOperationException("Unsupported particle hierarchy: " + pair.Key + "/" + particle.name);
                    particle.transform.localScale *= pair.Value;
                }
                PrefabUtility.SaveAsPrefabAsset(root, pair.Key, out bool success);
                if (!success) throw new InvalidOperationException("Prefab save failed: " + pair.Key);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
        foreach (var pair in data)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pair.Key);
            var serialized = new SerializedObject(asset);
            foreach (string field in new[] { "HullBottom", "HullTop", "NavigationRadius" })
                serialized.FindProperty("<" + field + ">k__BackingField").floatValue *= pair.Value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
        File.WriteAllText("Logs/ShipScale/changed-assets.json", new JArray(prefabs.Keys.Concat(data.Keys)).ToString());
        return "Saved " + prefabs.Count + " prefabs and " + data.Count + " geometry data assets.";
    }
}
