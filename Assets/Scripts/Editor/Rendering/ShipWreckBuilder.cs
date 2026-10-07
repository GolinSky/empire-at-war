using System.Collections.Generic;
using System.IO;
using EmpireAtWar.ViewComponents.Wreck;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EmpireAtWar.Editor.Rendering
{
    /// <summary>
    /// Builds the wreck prefab of a unit view prefab (ship, station, platform, facility) for the EmpireAtWar/Ship Wreck shader.
    /// Every Ship Lit renderer is copied with its original mesh; only the materials are Ship Wreck
    /// copies (same textures and colors). The shader cuts the mesh into parts at runtime. Safe to re-run.
    /// </summary>
    public static class ShipWreckBuilder
    {
        private const string BUILD_MENU_PATH = "Tools/Rendering/Build Wreck From Selected View";
        private const string SYNC_MENU_PATH = "Tools/Rendering/Sync Wreck Materials";
        private const string SHIP_LIT_SHADER_NAME = "EmpireAtWar/Ship Lit";
        private const string SHIP_WRECK_SHADER_PATH = "Assets/Art/Shaders/Units/ShipWreck.shader";
        // Ship views drop "ShipView" (VenatorShipView -> Venator); other unit views drop "View".
        private const string SHIP_VIEW_SUFFIX = "ShipView";
        private const string VIEW_SUFFIX = "View";
        private const string VIEW_FOLDER = "Assets/Prefabs/Models";
        private const string MATERIAL_FOLDER = "Assets/Art/Materials/Wrecks";
        public const string PREFAB_FOLDER = "Assets/Prefabs/Models/Wrecks";

        [MenuItem(BUILD_MENU_PATH)]
        private static void BuildSelected()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            Debug.Log($"[ShipWreck] Built {AssetDatabase.GetAssetPath(Build(path))}");
        }

        [MenuItem(BUILD_MENU_PATH, true)]
        private static bool CanBuildSelected()
        {
            return Selection.activeObject is GameObject &&
                   AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(VIEW_SUFFIX + ".prefab");
        }

        /// <summary>
        /// Copies every source Ship Lit material onto its wreck material again (textures, colors, metallic,
        /// smoothness, ...). Run after editing a unit's materials; prefabs keep their material references.
        /// </summary>
        [MenuItem(SYNC_MENU_PATH)]
        public static void SyncAllMaterials()
        {
            Shader wreckShader = AssetDatabase.LoadAssetAtPath<Shader>(SHIP_WRECK_SHADER_PATH);
            List<string> viewPaths = FindWreckedViewPaths();
            foreach (string viewPath in viewPaths)
            {
                GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
                string materialFolder = $"{MATERIAL_FOLDER}/{GetUnitName(view)}";
                foreach (MeshRenderer source in GetSourceRenderers(view))
                {
                    CreateWreckMaterials(source.sharedMaterials, wreckShader, materialFolder);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ShipWreck] Synced wreck materials of {viewPaths.Count} units.");
        }

        /// <summary>Unit view prefabs that have a wreck prefab.</summary>
        public static List<string> FindWreckedViewPaths()
        {
            List<string> paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { VIEW_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(PREFAB_FOLDER) || !path.EndsWith(VIEW_SUFFIX + ".prefab")) continue;

                GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (AssetDatabase.LoadAssetAtPath<UnitWreckView>(GetWreckPrefabPath(view)) != null)
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        public static string GetWreckPrefabPath(GameObject view)
        {
            return $"{PREFAB_FOLDER}/{GetUnitName(view)}WreckView.prefab";
        }

        /// <summary>The renderers a wreck copies, in the order its prefab lists them.</summary>
        public static List<MeshRenderer> GetSourceRenderers(GameObject view)
        {
            var levelView = view.GetComponent<EmpireAtWar.ViewComponents.Station.StationLevelView>();
            Renderer[] sources;
            if (levelView != null)
            {
                var levels = new SerializedObject(levelView).FindProperty("levelModels");
                var model = (EmpireAtWar.ViewComponents.Station.StationLevelModel)levels.GetArrayElementAtIndex(levels.arraySize - 1).objectReferenceValue;
                sources = model.HullRenderers;
            }
            else
            {
                sources = view.GetComponentsInChildren<MeshRenderer>(true);
            }

            List<MeshRenderer> renderers = new List<MeshRenderer>();
            foreach (Renderer source in sources)
            {
                if (source is MeshRenderer meshRenderer && meshRenderer.enabled && UsesOnlyShipLit(meshRenderer)) renderers.Add(meshRenderer);
            }

            return renderers;
        }

        private static string GetUnitName(GameObject view)
        {
            string suffix = view.name.EndsWith(SHIP_VIEW_SUFFIX) ? SHIP_VIEW_SUFFIX : VIEW_SUFFIX;
            return view.name.Substring(0, view.name.Length - suffix.Length);
        }

        public static UnitWreckView Build(string viewPrefabPath)
        {
            // In Play Mode AddComponent runs UnitWreckView.Awake before its renderers are assigned.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Exit Play Mode (including test runs) before building wrecks.");

            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPrefabPath);
            string shipName = GetUnitName(view);
            Shader wreckShader = AssetDatabase.LoadAssetAtPath<Shader>(SHIP_WRECK_SHADER_PATH);
            string materialFolder = EnsureFolder($"{MATERIAL_FOLDER}/{shipName}");
            EnsureFolder(PREFAB_FOLDER);

            GameObject root = new GameObject(shipName + "WreckView");
            // The spawn sets position and rotation only, so the view's own root scale must be kept here.
            root.transform.localScale = view.transform.localScale;
            try
            {
                List<MeshRenderer> renderers = new List<MeshRenderer>();
                List<MeshFilter> filters = new List<MeshFilter>();
                Matrix4x4 viewToLocal = view.transform.worldToLocalMatrix;
                foreach (MeshRenderer source in GetSourceRenderers(view))
                {
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
