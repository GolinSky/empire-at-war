using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildRaiderCorvetteArt
{
    const string TASK = "Temp/RaiderCorvetteImport/";
    const string PREFABS = "Assets/Prefabs/Models/Ships/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Raider art construction requires Edit Mode.");
        var reports = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        foreach (var report in reports)
        {
            string name = (string)report["name"];
            string modelPath = "Assets/Art/Models/EmpireShips/" + name + "/" + name + ".fbx";
            string textureFolder = "Assets/Art/Textures/Models/EmpireShips/" + name;
            string materialFolder = "Assets/Art/Materials/Models/EmpireShips/" + name;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] {textureFolder}))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = false;
                importer.sRGBTexture = !importer.assetPath.Contains("Normal") && !importer.assetPath.Contains("TeamMask");
                if (importer.assetPath.Contains("Normal"))
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.flipGreenChannel = true;
                }
                importer.SaveAndReimport();
            }
            var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            modelImporter.globalScale = .02f;
            modelImporter.importAnimation = false;
            modelImporter.isReadable = true;
            modelImporter.weldVertices = false;
            modelImporter.meshCompression = ModelImporterMeshCompression.Off;
            foreach (var row in report["materials"])
            {
                string sourceName = (string)row["name"], shader = (string)row["shader"];
                string path = materialFolder + "/" + name + "_" + sourceName.Replace(" ", "_").Replace(".", "_") + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool additive = shader.Contains("Additive");
                if (material == null)
                {
                    material = additive
                        ? new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Victory/Victory_Lights.mat"))
                        : new Material(Shader.Find(row["baseMap"] != null ? "EmpireAtWar/Ship Lit" : "Universal Render Pipeline/Unlit"));
                    AssetDatabase.CreateAsset(material, path);
                }
                if (row["baseMap"] != null)
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>((string)row["baseMap"]));
                if (row["normalMap"] != null)
                {
                    material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>((string)row["normalMap"]));
                    material.EnableKeyword("_NORMALMAP");
                }
                if (row["maskMap"] != null)
                {
                    material.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>((string)row["maskMap"]));
                    material.SetFloat("_TeamMaskStrength", 1);
                }
                if (material.HasProperty("_TeamLiveryStrength")) material.SetFloat("_TeamLiveryStrength", 0);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .15f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .25f);
                EditorUtility.SetDirty(material);
                modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName), material);
            }
            modelImporter.SaveAndReimport();
            var visual = new GameObject(name);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), visual.transform);
                foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    var row = report["source"]["meshes"][renderer.name];
                    if (row == null) throw new InvalidOperationException("No source visibility for " + renderer.name);
                    renderer.enabled = !(bool)row["hidden"];
                    if ((bool)row["hidden"])
                    {
                        // Keep source helpers in FBX/blend and preserve their attachment transforms.
                        // Runtime prefabs do not need EaW collision/shadow rendering components.
                        var filter = renderer.GetComponents<MeshFilter>().Single();
                        UnityEngine.Object.DestroyImmediate(renderer);
                        UnityEngine.Object.DestroyImmediate(filter);
                    }
                }
                if (name == "RaiderCorvette")
                {
                    var bounds = BoundsOf(visual);
                    float scale = 24 / bounds.size.z;
                    model.transform.localScale *= scale;
                    model.transform.localPosition = -bounds.center * scale;
                }
                PrefabUtility.SaveAsPrefabAsset(visual, PREFABS + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(visual); }
        }
        var hull = PrefabUtility.LoadPrefabContents(PREFABS + "RaiderCorvette.prefab");
        try
        {
            var bounds = BoundsOf(hull);
            File.WriteAllText(TASK + "ArtBounds.json", new JObject {
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z)
            }.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(hull); }
        AssetDatabase.SaveAssets();
        return "Saved Raider hull, authored hidden helpers, materials, normal map and alpha team mask.";
    }

    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
