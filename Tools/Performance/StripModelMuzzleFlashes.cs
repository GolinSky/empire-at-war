using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.FogOfWar;
using UnityEditor;
using UnityEngine;

public static class StripModelMuzzleFlashes
{
    public static object Inspect()
    {
        return new
        {
            models = ModelPaths().Select(path => new
                {
                    path, flashes = FlashNames(AssetDatabase.LoadAssetAtPath<GameObject>(path)),
                    flashMeshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>()
                        .Where(m => m.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) >= 0).Select(m => m.name).ToArray()
                }).Where(row => row.flashes.Length > 0 || row.flashMeshes.Length > 0).ToArray(),
            prefabs = PrefabPaths().Select(path => new { path, flashes = FlashNames(AssetDatabase.LoadAssetAtPath<GameObject>(path)) })
                .Where(row => row.flashes.Length > 0).ToArray()
        };
    }

    public static object ApplyPrefabs(string[] affectedPrefabs)
    {
        var changed = new List<string>();
        int removed = 0;
        // Pass the baseline's affected prefab paths after models have reimported.
        // Inherited nodes disappear at import; only authored copies and stale fog entries need writes.
        foreach (string path in affectedPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var flashes = root.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                var renderers = new HashSet<Renderer>(flashes.SelectMany(t => t.GetComponents<Renderer>()));
                foreach (FogVisibilityComponent fog in root.GetComponentsInChildren<FogVisibilityComponent>(true))
                {
                    var serialized = new SerializedObject(fog);
                    SerializedProperty list = serialized.FindProperty("renderers");
                    for (int i = list.arraySize - 1; i >= 0; i--)
                    {
                        var renderer = list.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;
                        if (renderer != null && !renderers.Contains(renderer)) continue;
                        list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                        list.DeleteArrayElementAtIndex(i);
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                removed += Strip(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed.Add(path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.ForceReserializeAssets(changed);
        AssetDatabase.SaveAssets();
        return new { removedPrefabObjects = removed, changed, remaining = Inspect() };
    }

    public static object VerifyReferences(string[] affectedPrefabs)
    {
        var missing = new List<string>();
        int muzzles = 0;
        foreach (string path in affectedPrefabs)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (node.name.StartsWith("Muzzle", StringComparison.OrdinalIgnoreCase)) muzzles++;
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) { missing.Add(path + ": missing script"); continue; }
                SerializedProperty property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None)
                        missing.Add(path + ": " + component.GetType().Name + "." + property.propertyPath);
                if (!(component is FogVisibilityComponent)) continue;
                SerializedProperty renderers = new SerializedObject(component).FindProperty("renderers");
                for (int i = 0; i < renderers.arraySize; i++)
                    if (renderers.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        missing.Add(path + ": null fog renderer " + i);
            }
        }
        return new { prefabs = affectedPrefabs.Length, survivingMuzzles = muzzles, missing };
    }

    private static IEnumerable<string> ModelPaths() => AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models" })
        .Select(AssetDatabase.GUIDToAssetPath).Where(p => AssetImporter.GetAtPath(p) is ModelImporter).OrderBy(p => p);

    private static IEnumerable<string> PrefabPaths() => AssetDatabase.FindAssets("t:Prefab",
            new[] { "Assets/Prefabs/Models", "Assets/Prefabs/View", "Assets/Prefabs/Ui/Reinforcement" })
        .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p);

    private static string[] FlashNames(GameObject root) => root.GetComponentsInChildren<Transform>(true)
        .Where(t => t.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) >= 0).Select(t => t.name).ToArray();

    private static int Strip(Transform root)
    {
        int removed = 0;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            removed += Strip(child);
            if (child.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) < 0) continue;
            while (child.childCount > 0) child.GetChild(0).SetParent(root, true);
            UnityEngine.Object.DestroyImmediate(child.gameObject);
            removed++;
        }
        return removed;
    }
}
