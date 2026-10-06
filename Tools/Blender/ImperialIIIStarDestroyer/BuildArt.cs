using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildImperialIIIStarDestroyerArt
{
    const string TASK = "Temp/ImperialIIIStarDestroyerImport/";
    const string PREFABS = "Assets/Prefabs/Models/Ships/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Imperial III art construction requires Edit Mode.");
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
                }
                if (material.HasProperty("_TeamMaskStrength")) material.SetFloat("_TeamMaskStrength", name == "ImperialIIIStarDestroyer" && sourceName == "ISDII_diff Material" ? 1 : 0);
                if (material.HasProperty("_TeamRimStrength")) material.SetFloat("_TeamRimStrength", 0);
                if (material.HasProperty("_TeamLiveryStrength")) material.SetFloat("_TeamLiveryStrength", 0);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .15f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .25f);
                if (shader.Contains("Alpha"))
                {
                    material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .5f);
                    material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = 2450;
                }
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
                }
                if (name == "ImperialIIIStarDestroyer")
                {
                    var bounds = BoundsOf(visual);
                    float scale = 190 / bounds.size.z;
                    model.transform.localScale *= scale;
                    model.transform.localPosition = -bounds.center * scale;
                }
                PrefabUtility.SaveAsPrefabAsset(visual, PREFABS + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(visual); }
        }
        var hull = PrefabUtility.LoadPrefabContents(PREFABS + "ImperialIIIStarDestroyer.prefab");
        try
        {
            var audit = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
            foreach (var hp in audit["hardpoints"].Where(h => h["Model_To_Attach"] != null))
            {
                string name = (string)hp["Attachment_Bone"];
                var bone = hull.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
                string source = (string)hp["Model_To_Attach"];
                string turretName = "ImperialIIIStarDestroyer" + (source.StartsWith("EV_ISD3_TURRET_") ? "HeavyDualTurret" : source.StartsWith("EV_ISD1_Triple_") ? "MediumTripleTurret" : source.StartsWith("Empire_Imperial_SD_ICQ_") ? "IonTurret" : "CompositeTurret");
                var turret = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + turretName + ".prefab"), bone);
                turret.name = name + "_Turret";
                turret.transform.localPosition = Vector3.zero;
                turret.transform.localRotation = Quaternion.Inverse(turret.transform.GetChild(0).localRotation);
                turret.transform.localScale = Vector3.one / turret.transform.GetChild(0).localScale.x;
            }
            PrefabUtility.SaveAsPrefabAsset(hull, PREFABS + "ImperialIIIStarDestroyer.prefab");
            var bounds = BoundsOf(hull);
            File.WriteAllText(TASK + "ArtBounds.json", new JObject {
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z)
            }.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(hull); }
        AssetDatabase.SaveAssets();
        return "Saved Imperial III hull, four turret families and eighteen mounted turrets with source textures, normals and team masks.";
    }

    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
