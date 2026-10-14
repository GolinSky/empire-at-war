using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildMiningFacilityArt
{
    const string TASK = "Temp/MiningFacilitiesImport/";
    const string CATEGORY = "SpaceStations/VanillaMiningFacilities/";
    public const string VISUALS = "Assets/Prefabs/Models/MiningFacilities/Vanilla/";
    public static string Main()
    {
        var reports = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        foreach (var report in JArray.Parse(File.ReadAllText(TASK + "AttachmentConversionReport.json"))) reports.Add(report);
        var textures = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"))["textures"];
        string models = "Assets/Art/Models/" + CATEGORY;
        string maps = "Assets/Art/Textures/Models/" + CATEGORY;
        string materials = "Assets/Art/Materials/Models/" + CATEGORY;
        foreach (string folder in new[] {models, maps, materials, VISUALS}) Directory.CreateDirectory(folder);
        foreach (var row in reports) File.Copy(TASK + "Output/" + (string)row["name"] + ".fbx", models + (string)row["name"] + ".fbx", true);
        foreach (var property in ((JObject)textures).Properties()) File.Copy((string)property.Value["png"], maps + Path.GetFileName((string)property.Value["png"]), true);
        AssetDatabase.Refresh();
        foreach (var property in ((JObject)textures).Properties())
        {
            string path = maps + Path.GetFileName((string)property.Value["png"]);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = (bool)property.Value["normal"];
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !property.Name.Contains("gloss");
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (normal) importer.flipGreenChannel = true;
            importer.SaveAndReimport();
        }
        foreach (var report in reports)
        {
            string name = (string)report["name"], path = models + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = .02f;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.weldVertices = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            foreach (var row in report["materials"])
            {
                string source = (string)row["name"], shader = (string)row["shader"];
                bool additive = shader.Contains("Additive"), alpha = shader.Contains("Alpha");
                string materialPath = materials + source + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = additive ? new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Victory/Victory_Lights.mat"))
                        : new Material(Shader.Find(alpha ? "Universal Render Pipeline/Lit" : "EmpireAtWar/Ship Lit"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                var props = row["properties"];
                foreach (var binding in new[] {new[]{"BaseTexture", "_BaseMap"}, new[]{"NormalTexture", "_BumpMap"}})
                {
                    string reference = (string)props[binding[0]];
                    if (reference == null) continue;
                    string texture = maps + Path.GetFileName((string)textures[reference.ToLowerInvariant()]["png"]);
                    material.SetTexture(binding[1], AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
                    if (binding[0] == "NormalTexture") material.EnableKeyword("_NORMALMAP");
                }
                var tint = props["Color"];
                material.SetColor("_BaseColor", additive && tint != null ? new Color((float)tint[0], (float)tint[1], (float)tint[2], 1) : Color.white);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .05f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .2f);
                if (material.HasProperty("_TeamLiveryStrength")) material.SetFloat("_TeamLiveryStrength", 0);
                if (alpha)
                {
                    material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", (float)CullMode.Off);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = 3000; material.SetShaderPassEnabled("ShadowCaster", false);
                    string glossReference = (string)props["GlossTexture"];
                    if (glossReference != null)
                    {
                        var sourceMap = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                        sourceMap.LoadImage(File.ReadAllBytes((string)textures[glossReference.ToLowerInvariant()]["png"]));
                        var pixels = sourceMap.GetPixels();
                        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, pixels[i].r);
                        sourceMap.SetPixels(pixels); sourceMap.Apply();
                        string glossPath = maps + source + "_MetallicSmoothness.png";
                        File.WriteAllBytes(glossPath, sourceMap.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(sourceMap);
                        AssetDatabase.ImportAsset(glossPath);
                        var glossImporter = (TextureImporter)AssetImporter.GetAtPath(glossPath); glossImporter.sRGBTexture = false; glossImporter.SaveAndReimport();
                        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(glossPath));
                        material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Smoothness", 1);
                    }
                }
                EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source), material);
            }
            importer.SaveAndReimport();
            var root = new GameObject(name);
            try
            {
                var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), root.transform);
                model.name = name + "Model";
                foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var row = report["source"]["meshes"][renderer.name];
                    if (row == null) throw new InvalidOperationException("Missing source mesh " + name + "/" + renderer.name);
                    if ((bool)row["hidden"])
                    {
                        UnityEngine.Object.DestroyImmediate(renderer.GetComponent<MeshFilter>());
                        UnityEngine.Object.DestroyImmediate(renderer);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, VISUALS + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        foreach (string name in new[] {"MiningFacility", "BattleMiningFacility", "MiningConstruction"})
        {
            string path = VISUALS + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (name == "BattleMiningFacility")
                    foreach (var mount in JArray.Parse(File.ReadAllText(TASK + "Attachments.json")))
                    {
                        var bone = root.GetComponentsInChildren<Transform>(true).Single(t => string.Equals(t.name, (string)mount["bone"], StringComparison.OrdinalIgnoreCase));
                        var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUALS + (string)mount["model"] + ".prefab"), bone);
                        art.name = (string)mount["bone"] + "_Attachment";
                        art.transform.localRotation = Quaternion.Inverse(art.transform.GetChild(0).localRotation);
                        art.transform.localScale = Vector3.one / art.transform.GetChild(0).localScale.x;
                    }
                var bounds = BoundsOf(root);
                float scale = (name == "MiningConstruction" ? 65 : 120) / Mathf.Max(bounds.size.x, bounds.size.z);
                var model = root.transform.GetChild(0);
                model.localScale *= scale;
                model.localPosition = -bounds.center * scale;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return "Imported vanilla facility art and saved source-visible geometry prefabs.";
    }
    public static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
