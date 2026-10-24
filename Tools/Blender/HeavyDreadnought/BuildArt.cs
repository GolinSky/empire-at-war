using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildHeavyDreadnoughtArt
{
    const string MODEL = "Assets/Art/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought.fbx";
    const string TEXTURES = "Assets/Art/Textures/Models/RepublicShips/HeavyDreadnought/";
    const string MATERIALS = "Assets/Art/Materials/Models/RepublicShips/HeavyDreadnought/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/HeavyDreadnought.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/HeavyDreadnoughtShipView.prefab";
    const string TASK = "Temp/HeavyDreadnoughtImport/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Replace art in Edit Mode.");
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        // Retain the existing ship's actual visible length, rather than choosing new balance.
        var old = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var oldBounds = BoundsOf(old);
        AssetDatabase.Refresh();
        foreach (string name in report["materials"].SelectMany(m => new[] {(string)m["base"], (string)m["normal"]}).Where(n => n != null).Distinct())
        {
            var texture = (TextureImporter)AssetImporter.GetAtPath(TEXTURES + name + ".png");
            bool normal = report["materials"].Any(m => (string)m["normal"] == name);
            texture.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            texture.sRGBTexture = !normal;
            texture.alphaIsTransparency = false;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            if (normal) texture.flipGreenChannel = true;
            texture.SaveAndReimport();
        }
        var importer = (ModelImporter)AssetImporter.GetAtPath(MODEL);
        importer.globalScale = .02f;
        importer.preserveHierarchy = true;
        importer.importAnimation = false;
        importer.isReadable = true;
        importer.weldVertices = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        foreach (var row in report["materials"])
        {
            string name = (string)row["name"];
            bool hull = (string)row["shader"] == "MeshBumpColorize.fx";
            string path = MATERIALS + name + ".mat";
            var shader = Shader.Find(hull ? "EmpireAtWar/Ship Lit" : "Universal Render Pipeline/Unlit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            if ((string)row["base"] != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + (string)row["base"] + ".png"));
            if (hull)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + (string)row["normal"] + ".png"));
                material.SetFloat("_BumpScale", 1);
                material.SetFloat("_Metallic", .1f);
                material.SetFloat("_Smoothness", .3f);
                material.SetFloat("_TeamMaskStrength", 0);
                material.SetFloat("_TeamLiveryHue", 0);
                material.SetFloat("_TeamLiveryHueRange", .07f);
                material.SetFloat("_TeamLiveryMinSaturation", .4f);
                material.SetFloat("_TeamLiveryStrength", 1);
            }
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        importer.SaveAndReimport();
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("HeavyDreadnought");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL), root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                var source = ((JObject)report["before"]["meshes"]).Properties().Single(p => (string)p.Value["exported_name"] == renderer.name);
                if ((string)source.Value["shader"] != "MeshBumpColorize.fx" || (bool)source.Value["hidden"])
                {
                    UnityEngine.Object.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                    UnityEngine.Object.DestroyImmediate(renderer);
                }
                else renderer.enabled = true;
            }
            var bounds = BoundsOf(root);
            float scale = oldBounds.size.z / bounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            bounds = BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            AssetDatabase.SaveAssets();
            File.WriteAllText(TASK + "ArtReport.json", new JObject {
                ["oldSize"] = new JArray(oldBounds.size.x, oldBounds.size.y, oldBounds.size.z),
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                ["modelScale"] = scale, ["visibleMeshes"] = root.GetComponentsInChildren<MeshRenderer>(true).Length
            }.ToString());
            return "Saved RaW hull with twelve attached turrets, red livery and original gameplay length: " + bounds.size;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.enabled && r.name != "ShieldSurface" && r.GetComponent<MeshFilter>() != null)
            .SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        return bounds;
    }
}
