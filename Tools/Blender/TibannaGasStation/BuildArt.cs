using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildTibannaArt
{
    const string TASK = "output/TibannaGasStationImport/";
    const string CATEGORY = "SpaceStations/TibannaGasStation/";
    const string PREFAB = "Assets/Prefabs/Models/MiningFacilities/TibannaGasStation.prefab";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Tibanna integration requires Edit Mode.");
        var report = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json")).Single();
        var textures = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"))["textures"];
        string models = "Assets/Art/Models/" + CATEGORY, maps = "Assets/Art/Textures/Models/" + CATEGORY, materials = "Assets/Art/Materials/Models/" + CATEGORY;
        foreach (string folder in new[] {models, maps, materials}) Directory.CreateDirectory(folder);
        File.Copy(TASK + "Output/TibannaGasStation.fbx", models + "TibannaGasStation.fbx", true);
        foreach (string path in Directory.GetFiles(TASK + "Output/Textures", "*.png")) File.Copy(path, maps + Path.GetFileName(path), true);
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(maps, "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            bool normal = textures.Children<JProperty>().Any(t => (bool)t.Value["normal"] && Path.GetFileName(path) == Path.GetFileName((string)t.Value["png"]));
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !path.Contains("TeamMask");
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (normal) importer.flipGreenChannel = true;
            importer.SaveAndReimport();
        }
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(models + "TibannaGasStation.fbx");
        modelImporter.globalScale = .02f; modelImporter.importAnimation = false; modelImporter.isReadable = true;
        modelImporter.weldVertices = false; modelImporter.meshCompression = ModelImporterMeshCompression.Off;
        modelImporter.importNormals = ModelImporterNormals.Import; modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
        foreach (var row in report["materials"])
        {
            string source = (string)row["name"], shader = (string)row["shader"], path = materials + source + ".mat";
            bool additive = shader.Contains("Additive");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = additive ? new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Victory/Victory_Lights.mat")) : new Material(Shader.Find("EmpireAtWar/Ship Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            var props = row["properties"];
            foreach (var binding in new[] {new[]{"BaseTexture", "_BaseMap"}, new[]{"NormalTexture", "_BumpMap"}})
            {
                string reference = (string)props[binding[0]];
                if (reference == null) continue;
                string texture = maps + Path.GetFileName((string)textures[reference.ToLowerInvariant()]["png"]);
                material.SetTexture(binding[1], AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
                if (binding[0] == "NormalTexture") material.EnableKeyword("_NORMALMAP");
                if (binding[0] == "BaseTexture" && shader.Contains("Colorize"))
                {
                    string mask = maps + Path.GetFileNameWithoutExtension(texture) + "_TeamMask.png";
                    material.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(mask));
                }
            }
            var tint = props[additive ? "Color" : "Diffuse"];
            material.SetColor("_BaseColor", tint == null ? Color.white : new Color((float)tint[0], (float)tint[1], (float)tint[2], 1));
            if (!additive)
            {
                material.SetFloat("_Metallic", .05f); material.SetFloat("_Smoothness", .2f);
                material.SetFloat("_TeamLiveryStrength", 0); material.SetFloat("_TeamMaskStrength", shader.Contains("Colorize") ? 1 : 0);
            }
            EditorUtility.SetDirty(material);
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source), material);
        }
        modelImporter.SaveAndReimport();
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var root = new GameObject("TibannaGasStation");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(models + "TibannaGasStation.fbx"), root.transform);
            model.name = "TibannaGasStationModel";
            foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                string sourceName = System.Text.RegularExpressions.Regex.Replace(renderer.name, @"\.\d{3}$", "");
                var row = report["source"]["meshes"][sourceName];
                renderer.name = sourceName;
                if (row == null) throw new InvalidOperationException("Missing audited mesh " + renderer.name);
                if ((bool)row["hidden"]) { UnityEngine.Object.DestroyImmediate(renderer.GetComponents<MeshFilter>().Single()); UnityEngine.Object.DestroyImmediate(renderer); }
            }
            var donor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/MiningFacilities/MiningFacilityView.prefab");
            var donorHull = new SerializedObject(donor.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "MiningFacility")).FindProperty("explosionHullRenderers");
            var donorBounds = ((MeshRenderer)donorHull.GetArrayElementAtIndex(0).objectReferenceValue).bounds;
            var bounds = BoundsOf(root);
            float size = Mathf.Max(donorBounds.size.x, donorBounds.size.z);
            float scale = size / Mathf.Max(bounds.size.x, bounds.size.z);
            model.transform.localScale *= scale; model.transform.localPosition = -bounds.center * scale;
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
            File.WriteAllText(TASK + "ArtBounds.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { donorSize = size, size = BoundsOf(root).size.ToString(), scale }, Newtonsoft.Json.Formatting.Indented));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        AssetDatabase.SaveAssets();
        return File.ReadAllText(TASK + "ArtBounds.json");
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
    }
}
