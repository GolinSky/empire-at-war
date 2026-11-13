using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildConstructionPodArt
{
    const string INPUT = "Temp/ConstructionPodImport/";
    const string MODEL = "Assets/Art/Models/SpaceStations/ConstructionPod/ConstructionPod.fbx";
    const string TEXTURES = "Assets/Art/Textures/Models/SpaceStations/ConstructionPod/";
    const string MATERIALS = "Assets/Art/Materials/Models/SpaceStations/ConstructionPod/";
    const string PREFAB = "Assets/Prefabs/Models/SpaceStations/ConstructionPod.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Import requires Edit Mode.");
        var spec = JObject.Parse(File.ReadAllText(INPUT + "Spec.json"));
        var report = JObject.Parse(File.ReadAllText(INPUT + "ConversionReport.json"));
        foreach (string directory in new[] {Path.GetDirectoryName(MODEL), TEXTURES, MATERIALS, Path.GetDirectoryName(PREFAB)})
            Directory.CreateDirectory(directory);
        File.Copy(INPUT + "ConstructionPod.fbx", MODEL, true);
        foreach (var texture in ((JObject)spec["textures"]).Properties())
            File.Copy((string)texture.Value["path"], TEXTURES + Path.GetFileName((string)texture.Value["path"]), true);
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(TEXTURES, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = path.Contains("_Normal");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.flipGreenChannel = normal;
            importer.alphaIsTransparency = path.Contains("Girder");
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
        var hull = Material("ConstructionPod_Hull", "EmpireAtWar/Ship Lit");
        hull.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + "ConstructionPod_Hull_Albedo.png"));
        hull.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + "ConstructionPod_Hull_Normal.png"));
        var girders = Material("ConstructionPod_Girders", "Universal Render Pipeline/Lit");
        girders.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + "ConstructionPod_Girder_Albedo.png"));
        girders.SetFloat("_Surface", 1);
        girders.SetFloat("_Blend", 0);
        girders.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        girders.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        girders.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        girders.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        girders.SetFloat("_ZWrite", 0);
        girders.SetFloat("_Cull", (float)CullMode.Off);
        girders.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        girders.SetOverrideTag("RenderType", "Transparent");
        girders.renderQueue = 3000;
        girders.SetShaderPassEnabled("ShadowCaster", false);
        var helper = Material("ConstructionPod_Helper", "EmpireAtWar/Ship Lit");
        foreach (var material in new[] {hull, girders, helper}) EditorUtility.SetDirty(material);
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(MODEL);
        modelImporter.globalScale = 1;
        modelImporter.importAnimation = false;
        modelImporter.importNormals = ModelImporterNormals.Import;
        modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
        modelImporter.meshCompression = ModelImporterMeshCompression.Off;
        modelImporter.weldVertices = false;
        foreach (var mesh in report["sourceMap"]["meshes"])
            foreach (var slot in mesh["slots"])
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), (string)slot["exportedMaterialName"]),
                    (int)mesh["index"] == 0 ? hull : (int)mesh["index"] <= 2 ? girders : helper);
        modelImporter.SaveAndReimport();
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL), scene);
            root.name = "ConstructionPod";
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                var row = report["sourceMap"]["meshes"].Single(m => (string)m["exportedName"] == renderer.name);
                renderer.enabled = (int)row["index"] <= 2;
                if ((int)row["index"] == 4 || (int)row["index"] == 5)
                {
                    UnityEngine.Object.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                    UnityEngine.Object.DestroyImmediate(renderer);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        AssetDatabase.SaveAssets();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
        var visible = prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        if (visible.Length != 3 || visible.Any(r => r.sharedMaterials.Any(m => m == null)))
            throw new InvalidOperationException("Saved visual renderer/material inventory differs from source.");
        int triangles = visible.Sum(r => r.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3);
        if (triangles != 4508) throw new InvalidOperationException("Visible triangle count changed.");
        var bounds = visible[0].bounds;
        foreach (var renderer in visible.Skip(1)) bounds.Encapsulate(renderer.bounds);
        string evidence = new JObject { ["prefab"] = PREFAB, ["visibleMeshes"] = visible.Length,
            ["visibleTriangles"] = triangles, ["boundsSize"] = bounds.size.ToString("F4"),
            ["missingDependencies"] = new JArray(AssetDatabase.GetDependencies(PREFAB).Where(p => !File.Exists(p))) }.ToString();
        File.WriteAllText(INPUT + "UnityReport.json", evidence);
        return evidence;
    }

    static Material Material(string name, string shader)
    {
        string path = MATERIALS + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", .05f);
        material.SetFloat("_Smoothness", .2f);
        return material;
    }
}
