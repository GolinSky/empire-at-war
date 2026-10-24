using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildCentaxArt
{
    const string MODELS = "Assets/Art/Models/RepublicShips/Centax/";
    const string TEXTURES = "Assets/Art/Textures/Models/RepublicShips/Centax/";
    const string MATERIALS = "Assets/Art/Materials/Models/RepublicShips/Centax/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/Centax.prefab";
    const string TASK = "Temp/CentaxImport/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Centax art construction requires Edit Mode.");
        EnsureFolder(MATERIALS.TrimEnd('/'));
        AssetDatabase.Refresh();
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var normalMaps = report["materials"].Select(m => (string)m["normal"]).Where(name => name != null).Select(name => TEXTURES + name + ".png").Distinct().ToArray();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TEXTURES.TrimEnd('/') }))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            bool normal = normalMaps.Contains(importer.assetPath);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = !normal && !importer.assetPath.Contains("TeamMask");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (normal) importer.flipGreenChannel = true;
            importer.SaveAndReimport();
        }
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(MODELS + "Centax.fbx");
        modelImporter.globalScale = .02f;
        modelImporter.preserveHierarchy = true;
        modelImporter.importAnimation = false;
        modelImporter.isReadable = true;
        modelImporter.weldVertices = false;
        modelImporter.meshCompression = ModelImporterMeshCompression.Off;
        foreach (var remap in modelImporter.GetExternalObjectMap().Keys.Where(key => key.type == typeof(Material)).ToArray()) modelImporter.RemoveRemap(remap);
        foreach (var source in report["materials"])
        {
            string name = (string)source["name"], shader = (string)source["shader"];
            string path = MATERIALS + name + ".mat";
            bool additive = shader.Contains("Additive");
            string baseMap = (string)source["base"], normalMap = (string)source["normal"], maskMap = (string)source["mask"];
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var unityShader = Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : baseMap != null ? "EmpireAtWar/Ship Lit" : "Universal Render Pipeline/Unlit");
            if (material == null)
            {
                material = new Material(unityShader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = unityShader;
            if (baseMap != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + baseMap + ".png"));
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
            }
            if (source["properties"][additive ? "Color" : "Diffuse"] is JArray tint)
                material.SetColor("_BaseColor", new Color((float)tint[0], (float)tint[1], (float)tint[2], 1));
            if (material.HasProperty("_TeamMaskStrength"))
            {
                material.SetTexture("_TeamMaskMap", maskMap != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + maskMap + ".png") : null);
                material.SetFloat("_TeamMaskStrength", maskMap != null ? 1f : 0);
                material.SetFloat("_TeamLiveryStrength", 0);
                material.SetFloat("_Metallic", .1f); material.SetFloat("_Smoothness", .3f);
            }
            EditorUtility.SetDirty(material);
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        modelImporter.SaveAndReimport();
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Centax");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + "Centax.fbx"), root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var record = ((JObject)report["models"]["Centax"]["before"]["meshes"]).Properties().Single(row => (string)row.Value["exported_name"] == renderer.name);
                string sourceName = record.Name;
                var source = record.Value;
                if ((bool)source["hidden"] || new[] {"engines", "engine", "collision", "shield", "shadow"}.Contains(sourceName))
                {
                    // Keep attachment transforms; runtime prefabs do not render source helpers/effects.
                    foreach (var filter in renderer.GetComponents<MeshFilter>()) UnityEngine.Object.DestroyImmediate(filter);
                    UnityEngine.Object.DestroyImmediate(renderer);
                    continue;
                }
                renderer.name = sourceName;
                renderer.enabled = true;
                if (!(renderer is SkinnedMeshRenderer skinned)) continue;
                var mesh = new Mesh(); skinned.BakeMesh(mesh, false);
                string meshPath = MODELS + "Centax_" + sourceName + "Mesh.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
                else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
                var owner = renderer.gameObject; var materials = renderer.sharedMaterials;
                UnityEngine.Object.DestroyImmediate(renderer);
                owner.AddComponent<MeshFilter>().sharedMesh = mesh;
                owner.AddComponent<MeshRenderer>().sharedMaterials = materials;
            }
            var bounds = BoundsOf(root);
            float length = 28f; // Cruiser gameplay length, between the CR70 and Arquitens.
            float scale = length / bounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            bounds = BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            BuildWreckVisual(model.transform.localScale, model.transform.localPosition);
            AssetDatabase.SaveAssets();
            File.WriteAllText(TASK + "ArtReport.json", new JObject {
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                ["gameplayLength"] = length, ["modelScale"] = scale,
                ["visibleMeshes"] = root.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled)
            }.ToString());
            return "Saved Centax visual, source-alpha team mask and materials; bounds " + bounds.size;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static void BuildWreckVisual(Vector3 modelScale, Vector3 modelPosition)
    {
        var report = JObject.Parse(File.ReadAllText(TASK + "WreckConversionReport.json"));
        var importer = (ModelImporter)AssetImporter.GetAtPath(MODELS + "CentaxWreck.fbx");
        importer.globalScale = .02f;
        importer.preserveHierarchy = true;
        importer.importAnimation = false;
        importer.isReadable = true;
        importer.weldVertices = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        foreach (var source in report["materials"])
        {
            string name = (string)source["name"];
            var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + name + ".mat");
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + "Centax_centax_Slot0.mat"));
                AssetDatabase.CreateAsset(material, MATERIALS + name + ".mat");
            }
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        importer.SaveAndReimport();
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("CentaxWreckVisual");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + "CentaxWreck.fbx"), root.transform);
            model.transform.localScale = modelScale;
            model.transform.localPosition = modelPosition;
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Models/Ships/Republic/CentaxWreckVisual.prefab");
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit"))
            .SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        return bounds;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
