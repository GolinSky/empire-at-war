using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildBulwarkArt
{
    const string TASK = "Temp/BulwarkImport/";
    const string MODELS = "Assets/Art/Models/SeparatistShips/Bulwark/";
    const string TEXTURES = "Assets/Art/Textures/Models/SeparatistShips/Bulwark/";
    const string MATERIALS = "Assets/Art/Materials/Models/SeparatistShips/Bulwark/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Bulwark.prefab";


    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Bulwark art construction requires Edit Mode.");
        AssetDatabase.Refresh();
        EnsureFolder(MATERIALS.TrimEnd('/'));
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var normals = report["materials"].Select(m => (string)m["normal"]).Where(n => n != null).Select(n => TEXTURES + n + ".png").Distinct().ToArray();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] {TEXTURES.TrimEnd('/')}))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            bool normal = normals.Contains(importer.assetPath);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !normal && !importer.assetPath.EndsWith("_TeamMask.png");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (normal) importer.flipGreenChannel = true;
            importer.SaveAndReimport();
        }
        foreach (var row in report["materials"])
        {
            string name = (string)row["name"], shader = (string)row["shader"];
            string baseMap = (string)row["base"], normalMap = (string)row["normal"];
            bool additive = shader.Contains("Additive");
            string path = MATERIALS + name + ".mat";
            var unityShader = Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : baseMap != null ? "EmpireAtWar/Ship Lit" : "Universal Render Pipeline/Unlit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(unityShader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = unityShader;
            if (baseMap != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + baseMap + ".png"));
            material.SetColor("_BaseColor", Color.white);
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + normalMap + ".png"));
                material.EnableKeyword("_NORMALMAP");
            }
            if (additive)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
                material.SetFloat("_SrcBlend", 1); material.SetFloat("_DstBlend", 1); material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                if (row["properties"]["Color"] is JArray tint) material.SetColor("_BaseColor", new Color((float)tint[0], (float)tint[1], (float)tint[2], 1));
            }
            if (material.HasProperty("_TeamLiveryStrength"))
            {
                // The source hull alpha marks paint strips; preserve yellow hangar markings.
                bool paintedHull = baseMap == "Bulwark_SeV_bulwark" && shader.Contains("Colorize");
                material.SetFloat("_TeamMaskStrength", paintedHull ? 1 : 0);
                material.SetFloat("_TeamLiveryStrength", 0);
                if (paintedHull) material.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + "Bulwark_SeV_bulwark_TeamMask.png"));
                material.SetFloat("_Metallic", .1f); material.SetFloat("_Smoothness", .3f);
            }
            EditorUtility.SetDirty(material);
        }
        foreach (var record in ((JObject)report["models"]).Properties())
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(MODELS + record.Name + ".fbx");
            importer.globalScale = .02f;
            importer.preserveHierarchy = true;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.weldVertices = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            foreach (var key in importer.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)).ToArray()) importer.RemoveRemap(key);
            foreach (var row in record.Value["materials"])
            {
                string name = (string)row["name"];
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + name + ".mat"));
            }
            importer.SaveAndReimport();
        }
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Bulwark");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = ImportModel("Bulwark", root.transform, report);
            var front = Transforms(model.transform).Single(t => t.name == "HP_F_MAIN");
            var engine = Transforms(model.transform).Single(t => t.name == "HP_Bulwark_Engines");
            if (front.position.z < engine.position.z) model.transform.localRotation = Quaternion.Euler(0, 180, 0) * model.transform.localRotation;
            var bounds = BoundsOf(root);
            float gameplayLength = BoundsOf(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab")).size.z;
            float scale = gameplayLength / bounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            bounds = BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            AssetDatabase.SaveAssets();
            File.WriteAllText(TASK + "ArtReport.json", new JObject {
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                ["gameplayLength"] = gameplayLength, ["modelScale"] = scale,
                ["scaleBasis"] = "Provisional Providence-sized capital; measured from the current saved Providence prefab.",
                ["visibleMeshes"] = Components<MeshRenderer>(root).Count(r => r.enabled),
                ["frontZ"] = front.position.z, ["engineZ"] = engine.position.z
            }.ToString());
            return "Saved Bulwark hull and materials; bounds " + bounds.size;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static GameObject ImportModel(string name, Transform parent, JObject report)
    {
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + name + ".fbx"), parent);
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var lod in Components<LODGroup>(model).ToArray()) UnityEngine.Object.DestroyImmediate(lod);
        foreach (var renderer in Components<Renderer>(model).ToArray())
        {
            var record = ((JObject)report["models"][name]["before"]["meshes"]).Properties().Single(row => (string)row.Value["exported_name"] == renderer.name);
            bool helper = renderer.sharedMaterials.Any(m => m.name.Contains("collision") || m.name.Contains("shadow"));
            if ((bool)record.Value["hidden"] || helper)
            {
                foreach (var filter in Components<MeshFilter>(renderer.gameObject).ToArray()) UnityEngine.Object.DestroyImmediate(filter);
                UnityEngine.Object.DestroyImmediate(renderer);
                continue;
            }
            renderer.name = record.Name;
            renderer.enabled = true;
            if (!(renderer is SkinnedMeshRenderer skinned)) continue;
            var mesh = new Mesh(); skinned.BakeMesh(mesh, false);
            string meshPath = MODELS + name + "_" + record.Name + "Mesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
            var owner = renderer.gameObject; var materials = renderer.sharedMaterials;
            UnityEngine.Object.DestroyImmediate(renderer);
            owner.AddComponent<MeshFilter>().sharedMesh = mesh;
            owner.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }
        return model;
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points = Components<MeshRenderer>(root).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit"))
            .SelectMany(r => Components<MeshFilter>(r.gameObject).Single().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        return bounds;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
    static IEnumerable<Transform> Transforms(Transform root)
    {
        yield return root;
        foreach (Transform child in root) foreach (var descendant in Transforms(child)) yield return descendant;
    }
    static IEnumerable<T> Components<T>(GameObject root) where T : UnityEngine.Object
    {
        foreach (var transform in Transforms(root.transform))
        {
            var components = new SerializedObject(transform.gameObject).FindProperty("m_Component");
            for (int i = 0; i < components.arraySize; i++)
                if (components.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue is T component) yield return component;
        }
    }
}
