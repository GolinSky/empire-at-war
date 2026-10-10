using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class CleanupISDIObsoleteVisuals
{
    private const string TASK = "Temp/ISDIRemakeImport/";

    public static string Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorUtility.scriptCompilationFailed, "Editor idle in Edit Mode");
        var plan = JObject.Parse(File.ReadAllText(TASK + "CleanupPlan.json"));
        var originals = plan["originalArchiveRecords"].Values<string>().ToArray();
        var moves = (JArray)plan["movedAssets"];
        var deleted = plan["deletedAssets"].Values<string>().ToArray();
        var shared = moves.Select(m => (string)m["oldPath"]).ToArray();
        var outside = AssetDatabase.GetAllAssetPaths().Where(p => !originals.Contains(p) && new[] { ".prefab", ".unity", ".asset", ".mat", ".fbx", ".controller", ".anim" }.Contains(Path.GetExtension(p))).ToArray();
        var required = AssetDatabase.GetDependencies(outside, true).Intersect(originals).OrderBy(p => p).ToArray();
        Check(required.SequenceEqual(shared.OrderBy(p => p)), "Only the recorded Tector dependencies are externally referenced");
        foreach (var move in moves)
        {
            string oldPath = (string)move["oldPath"], newPath = (string)move["newPath"];
            Check(originals.Contains(oldPath) && oldPath.Contains("/ISDIObsoleteAOTR/"), "Scoped source " + oldPath);
            Check(newPath.Contains("/Tector/Tector_AOTR") && !File.Exists(newPath), "Scoped destination " + newPath);
            Check(AssetDatabase.AssetPathToGUID(oldPath) == (string)move["guid"] && ContentHash(oldPath) == (string)move["contentSha256"], "Unchanged shared source " + oldPath);
            if (oldPath.EndsWith(".png")) Check(File.ReadAllBytes(oldPath).Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }), "Decoded PNG, not an LFS pointer " + oldPath);
            Check(AssetDatabase.IsValidFolder(Path.GetDirectoryName(newPath).Replace('\\', '/')), "Existing Tector folder " + newPath);
        }
        foreach (string path in deleted)
            Check(originals.Contains(path) && (File.Exists(path) || AssetDatabase.IsValidFolder(path)), "Scoped deletion " + path);

        var consumers = outside.Where(p => p.Contains("/Tector/") || Path.GetFileName(p).StartsWith("Tector")).ToDictionary(p => p, p => ContentHash(p));
        var meshes = shared.Where(p => p.EndsWith(".fbx")).ToDictionary(p => AssetDatabase.AssetPathToGUID(p), p => MeshSignature(p));
        var folders = deleted.Where(AssetDatabase.IsValidFolder).ToArray();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var move in moves)
            {
                string error = AssetDatabase.MoveAsset((string)move["oldPath"], (string)move["newPath"]);
                Check(error.Length == 0, error);
            }
            foreach (string path in deleted.Except(folders))
                Check(AssetDatabase.DeleteAsset(path), "Delete unused visual " + path);
            foreach (string folder in folders)
            {
                Check(Directory.GetFileSystemEntries(folder).Length == 0, "Archive folder is empty " + folder);
                Check(AssetDatabase.DeleteAsset(folder), "Delete empty archive folder " + folder);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.Refresh();
        foreach (var move in moves)
        {
            string path = (string)move["newPath"];
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            AssetDatabase.SetLabels(asset, AssetDatabase.GetLabels(asset).Except(new[] { "Obsolete", "ISDI" }).Concat(new[] { "Tector" }).Distinct().ToArray());
            Check(AssetDatabase.AssetPathToGUID(path) == (string)move["guid"] && ContentHash(path) == (string)move["contentSha256"], "Preserved content and GUID " + path);
            if (path.EndsWith(".fbx")) Check(meshes[(string)move["guid"]] == MeshSignature(path), "Preserved imported mesh file IDs and triangles " + path);
        }
        AssetDatabase.SaveAssets();
        foreach (string path in originals) Check(!File.Exists(path) && !Directory.Exists(path) && !File.Exists(path + ".meta"), "No old ISD I asset " + path);
        foreach (var pair in consumers) Check(pair.Value == ContentHash(pair.Key), "Unchanged Tector consumer " + pair.Key);
        foreach (string path in new[] { "Assets/Prefabs/Models/Ships/Tector.prefab", "Assets/Prefabs/Models/Ships/TectorShipView.prefab", "Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
            {
                Check(component != null, "Tector component " + path);
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        Check(iterator.objectReferenceValue != null || iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)), "Tector saved reference " + path + "/" + iterator.propertyPath);
            }
            Check(AssetDatabase.GetDependencies(path, true).All(p => !p.Contains("ISDIObsoleteAOTR") && !AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(p)).Contains("Obsolete")), "No obsolete Tector dependency " + path);
        }
        plan["completed"] = "2026-10-10";
        plan["sharedAssetsMoved"] = moves.Count;
        plan["unusedRecordsDeleted"] = deleted.Length;
        plan["tectorConsumerFilesUnchanged"] = consumers.Count;
        plan["tectorReferencesVerified"] = true;
        plan["importedMeshIdsPreserved"] = true;
        plan["oldVisualPathsAbsent"] = true;
        File.WriteAllText(TASK + "Cleanup.json", plan.ToString(Formatting.Indented));
        return "Moved " + moves.Count + " shared assets to Tector; deleted " + deleted.Length + " unused records. Tector geometry, GUIDs and saved references preserved.";
    }

    private static string ContentHash(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (path.EndsWith(".mat")) bytes = Encoding.UTF8.GetBytes(Regex.Replace(Encoding.UTF8.GetString(bytes), @"(?m)^  m_Name:.*\r?\n", ""));
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static string MeshSignature(string path)
    {
        return string.Join(";", AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Select(mesh =>
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id);
            return id + ":" + mesh.vertexCount + ":" + mesh.triangles.Length;
        }).OrderBy(value => value));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
