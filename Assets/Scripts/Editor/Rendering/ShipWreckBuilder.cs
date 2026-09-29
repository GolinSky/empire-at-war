using System.Collections.Generic;
using System.IO;
using EmpireAtWar.ViewComponents.Wreck;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Builds the wreck prefab of a ship view prefab for the EmpireAtWar/Ship Wreck shader.
    /// Every Ship Lit renderer is copied with its original mesh; only the materials are Ship Wreck
    /// copies (same textures and colors). The shader cuts the mesh into parts at runtime. Safe to re-run.
    /// </summary>
    public static class ShipWreckBuilder
    {
        private const string MENU_PATH = "Tools/Rendering/Build Ship Wreck From Selected View";
        private const string SHIP_LIT_SHADER_NAME = "EmpireAtWar/Ship Lit";
        private const string SHIP_WRECK_SHADER_PATH = "Assets/Art/Shaders/Units/ShipWreck.shader";
        private const string VIEW_SUFFIX = "ShipView";
        private const string MATERIAL_FOLDER = "Assets/Art/Materials/Wrecks";
        private const string PREFAB_FOLDER = "Assets/Prefabs/Models/Wrecks";

        [MenuItem(MENU_PATH)]
        private static void BuildSelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            Debug.Log($"[ShipWreck] Built {AssetDatabase.GetAssetPath(Build(path))}");
        }

        [MenuItem(MENU_PATH, true)]
        private static bool CanBuildSelected()
        {
            return Selection.activeObject is GameObject &&
                   AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(VIEW_SUFFIX + ".prefab");
        }

        public static UnitWreckView Build(string viewPrefabPath)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPrefabPath);
            string shipName = view.name.Replace(VIEW_SUFFIX, string.Empty);
            Shader wreckShader = AssetDatabase.LoadAssetAtPath<Shader>(SHIP_WRECK_SHADER_PATH);
            string materialFolder = EnsureFolder($"{MATERIAL_FOLDER}/{shipName}");
            EnsureFolder(PREFAB_FOLDER);

            GameObject root = new GameObject(shipName + "WreckView");
            try
            {
                List<MeshRenderer> renderers = new List<MeshRenderer>();
                List<MeshFilter> filters = new List<MeshFilter>();
                Matrix4x4 viewToLocal = view.transform.worldToLocalMatrix;
                foreach (MeshRenderer source in view.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!UsesOnlyShipLit(source)) continue;

                    GameObject part = new GameObject(source.name);
                    part.transform.SetParent(root.transform, false);
                    Matrix4x4 local = viewToLocal * source.transform.localToWorldMatrix;
                    part.transform.localPosition = local.GetColumn(3);
                    part.transform.localRotation = local.rotation;
                    part.transform.localScale = local.lossyScale;

                    MeshFilter filter = part.AddComponent<MeshFilter>();
                    filter.sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                    MeshRenderer meshRenderer = part.AddComponent<MeshRenderer>();
                    meshRenderer.shadowCastingMode = ShadowCastingMode.On;
                    meshRenderer.sharedMaterials = CreateWreckMaterials(source.sharedMaterials, wreckShader, materialFolder);
                    renderers.Add(meshRenderer);
                    filters.Add(filter);
                }

                UnitWreckView wreckView = root.AddComponent<UnitWreckView>();
                SerializedObject serializedView = new SerializedObject(wreckView);
                SetArray(serializedView.FindProperty("meshRenderers"), renderers);
                SetArray(serializedView.FindProperty("meshFilters"), filters);
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PREFAB_FOLDER}/{root.name}.prefab");
                AssetDatabase.SaveAssets();
                return prefab.GetComponent<UnitWreckView>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool UsesOnlyShipLit(MeshRenderer meshRenderer)
        {
            foreach (Material material in meshRenderer.sharedMaterials)
            {
                if (material == null || material.shader.name != SHIP_LIT_SHADER_NAME) return false;
            }

            return meshRenderer.sharedMaterials.Length > 0;
        }

        private static Material[] CreateWreckMaterials(Material[] sources, Shader wreckShader, string folder)
        {
            Material[] materials = new Material[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                // Same property names as Ship Lit, so textures, colors and livery settings carry over.
                Material material = new Material(sources[i]) { shader = wreckShader, name = sources[i].name + "_Wreck" };
                materials[i] = SaveAsset(material, $"{folder}/{material.name}.mat");
            }

            return materials;
        }

        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            // Overwrite in place so prefabs keep their references (and GUIDs stay stable).
            EditorUtility.CopySerialized(asset, existing);
            Object.DestroyImmediate(asset);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void SetArray<T>(SerializedProperty property, List<T> values) where T : Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static string EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return path;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            return path;
        }
    }
}
