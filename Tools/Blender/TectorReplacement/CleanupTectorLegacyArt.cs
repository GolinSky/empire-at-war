using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;

public static class CleanupTectorLegacyArt
{
    private const string OUTPUT = "F:/CodexArtifacts/tector-preview/UnityReplacement/LegacyCleanup.json";
    private static readonly string[] _folders = {
        "Assets/Art/Models/EmpireShips/Tector/",
        "Assets/Art/Materials/Models/EmpireShips/Tector/",
        "Assets/Art/Materials/Wrecks/Tector/",
        "Assets/Art/Textures/Models/EmpireShips/Tector/"
    };

    public static string Main(bool apply = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Legacy Tector cleanup requires an idle Editor in Edit Mode.");
        var candidates = AssetDatabase.GetAllAssetPaths().Where(path => _folders.Any(path.StartsWith)
            && !AssetDatabase.IsValidFolder(path) && !Path.GetFileName(path).StartsWith("Tector_Remake", StringComparison.Ordinal)
            && new[] { ".fbx", ".mat", ".png", ".dds", ".tga", ".jpg" }.Contains(Path.GetExtension(path).ToLowerInvariant())).OrderBy(path => path).ToArray();
        var candidateSet = candidates.ToHashSet();
        var outside = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path) && !candidateSet.Contains(path)).ToArray();
        var required = AssetDatabase.GetDependencies(outside, true).Where(candidateSet.Contains).Distinct().ToArray();
        var references = new JArray();
        if (required.Length > 0)
            foreach (string path in outside)
            {
                var dependencies = AssetDatabase.GetDependencies(path, false).Where(candidateSet.Contains).ToArray();
                if (dependencies.Length > 0) references.Add(new JObject { ["consumer"] = path, ["dependencies"] = new JArray(dependencies) });
            }
        var retained = outside.Where(path => _folders.Any(path.StartsWith) || path.StartsWith("Assets/Settings/") || path.StartsWith("Assets/AddressableAssetsData/")
            || new[] { "Assets/Prefabs/Models/Ships/Tector.prefab", "Assets/Prefabs/Models/Ships/TectorShipView.prefab", "Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab", "Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab", "Assets/Art/Models/Shields/TectorShipViewShield.asset", "Assets/Art/Textures/Ui/Icons/ShipIcon/TectorIcon.png", "Assets/Art/Textures/Ui/Icons/ShipIcon/TectorSilhouette.png" }.Contains(path))
            .SelectMany(path => new[] { path, path + ".meta" }).Where(File.Exists).Distinct().ToDictionary(path => path, Hash);
        var report = new JObject {
            ["assets"] = new JArray(candidates.Select(path => new JObject { ["path"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path), ["sha256"] = Hash(path), ["bytes"] = new FileInfo(path).Length })),
            ["references"] = references, ["retainedHashes"] = JObject.FromObject(retained), ["removed"] = false
        };
        File.WriteAllText(OUTPUT, report.ToString());
        if (apply)
        {
            if (references.Count > 0) throw new InvalidOperationException("Legacy Tector assets have retained consumers: " + references);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in candidates)
                    if (!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("Could not delete " + path);
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            foreach (string path in candidates)
                if (File.Exists(path) || File.Exists(path + ".meta")) throw new InvalidOperationException("Deleted asset still exists: " + path);
            foreach (var entry in retained)
                if (!File.Exists(entry.Key) || Hash(entry.Key) != entry.Value) throw new InvalidOperationException("Retained file changed: " + entry.Key);
            report["removed"] = true;
            File.WriteAllText(OUTPUT, report.ToString());
        }
        return new JObject { ["candidateAssets"] = candidates.Length, ["types"] = JObject.FromObject(candidates.GroupBy(Path.GetExtension).ToDictionary(g => g.Key, g => g.Count())),
            ["bytes"] = candidates.Sum(path => (long)report["assets"].Single(a => (string)a["path"] == path)["bytes"]), ["retainedConsumers"] = references,
            ["retainedFiles"] = retained.Count, ["removed"] = apply }.ToString();
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
}
