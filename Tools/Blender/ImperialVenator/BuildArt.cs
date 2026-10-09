using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildImperialVenatorArt
{
    const string ROOT = "Temp/ImperialVenatorImport/";
    const string MODEL_FOLDER = "Assets/Art/Models/EmpireShips/ImperialVenator/";
    const string TEXTURE_FOLDER = "Assets/Art/Textures/Models/EmpireShips/ImperialVenator/";
    const string MATERIAL_FOLDER = "Assets/Art/Materials/Models/EmpireShips/ImperialVenator/";
    static readonly Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>();
    public static string Main()
    {
        AssetDatabase.Refresh();
        var manifest = JObject.Parse(File.ReadAllText(ROOT + "ArtManifest.json"));
        var audit = JObject.Parse(File.ReadAllText(ROOT + "SourceAudit.json"));
        foreach (var row in manifest.Properties())
        {
            string path = (string)row.Value["model_path"];
            var remaps = new Dictionary<string, Material>();
            foreach (var definition in row.Value["materials"])
            {
                string shader = (string)definition["shader"], stem = (string)definition["base"], normal = (string)definition["normal"];
                bool effect = shader.Contains("Additive") || shader == "MeshShield.fx";
                bool engine = ((string)definition["name"]).Contains("Engine_");
                string materialPath = MATERIAL_FOLDER + row.Name + "_" + ((string)definition["name"]).Replace(row.Name + "_", "") + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(Shader.Find(effect ? "Universal Render Pipeline/Unlit" : "EmpireAtWar/Ship Lit")); AssetDatabase.CreateAsset(material, materialPath); }
                material.shader = Shader.Find(effect ? "Universal Render Pipeline/Unlit" : "EmpireAtWar/Ship Lit");
                material.shaderKeywords = new string[0];
                if (stem != null)
                {
                    string texturePath = TEXTURE_FOLDER + stem + (effect ? "_Additive" : "") + ".png";
                    Texture(texturePath, false, false, effect);
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                    if (!effect && shader.Contains("Colorize"))
                    {
                        Texture(TEXTURE_FOLDER + stem + "_TeamMask.png", true, false, false);
                        material.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_FOLDER + stem + "_TeamMask.png"));
                        material.SetFloat("_TeamMaskStrength", .2f);
                    }
                }
                if (normal != null)
                {
                    Texture(TEXTURE_FOLDER + normal + ".png", true, true, false);
                    material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_FOLDER + normal + ".png"));
                }
                if (effect)
                {
                    material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
                    material.SetFloat("_SrcBlend", (float)BlendMode.One); material.SetFloat("_DstBlend", (float)BlendMode.One);
                    material.SetFloat("_SrcBlendAlpha", 1); material.SetFloat("_DstBlendAlpha", 10);
                    material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                    material.SetFloat("_Cull", engine ? 0 : 2);
                    material.SetColor("_BaseColor", engine && shader == "MeshShield.fx" ? new Color(2, 2, 2, 1) : Color.white);
                    material.SetOverrideTag("RenderType", "Transparent");
                }
                else { material.SetFloat("_Smoothness", .25f); material.SetFloat("_Metallic", .1f); material.SetFloat("_TeamLiveryStrength", 0); }
                EditorUtility.SetDirty(material); remaps.Add((string)definition["name"], material);
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            foreach (var remap in remaps) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), remap.Key), remap.Value);
            importer.globalScale = .02f; importer.importAnimation = false; importer.preserveHierarchy = true;
            importer.weldVertices = false; importer.meshCompression = ModelImporterMeshCompression.Off; importer.isReadable = true; importer.SaveAndReimport();
            _models[row.Name] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        var root = new GameObject("ImperialVenator");
        try
        {
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(root.transform, false);
            var hull = AddModel("ImperialVenator", geometry.transform, manifest);
            foreach (var hp in audit["hardpoints"].Where(h => !string.IsNullOrEmpty((string)h["resolved"]["Model_To_Attach"])))
            {
                string name = "ImperialVenator_" + ((string)hp["name"]).Replace("HP_Venator_", "");
                AddModel(name, Bone(hull, (string)hp["resolved"]["Attachment_Bone"]), manifest);
            }
            var bounds = BoundsOf(Points(root));
            float scale = 120 / bounds.size.z;
            geometry.transform.localScale = Vector3.one * scale; geometry.transform.localPosition = -bounds.center * scale;
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Models/Ships/ImperialVenator.prefab");
            var size = BoundsOf(Points(root));
            File.WriteAllText(ROOT + "ArtBounds.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { scale, center = new[] { bounds.center.x, bounds.center.y, bounds.center.z }, size = new[] { size.size.x, size.size.y, size.size.z } }));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        // Keep the inherited death clone as a separately verified source-art variant.
        var death = new GameObject("ImperialVenatorDeath");
        try
        {
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(death.transform, false);
            AddModel("ImperialVenatorDeath", geometry.transform, manifest);
            var bounds = JObject.Parse(File.ReadAllText(ROOT + "ArtBounds.json"));
            geometry.transform.localScale = Vector3.one * (float)bounds["scale"];
            var center = bounds["center"]; geometry.transform.localPosition = -new Vector3((float)center[0], (float)center[1], (float)center[2]) * (float)bounds["scale"];
            PrefabUtility.SaveAsPrefabAsset(death, "Assets/Prefabs/Models/Ships/ImperialVenatorDeath.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(death); }
        AssetDatabase.SaveAssets();
        return "Imperial Venator visual: exact Imperial hull + ten XML attachments; 120 units; source death-clone variant saved.";
    }
    static GameObject AddModel(string name, Transform parent, JObject manifest)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(_models[name], parent); obj.name = name;
        if (parent.name != "Geometry") { obj.transform.localScale = Vector3.one * .01f; obj.transform.localRotation = Quaternion.Euler(90, 0, 0); }
        var report = manifest[name];
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true)) renderer.enabled = !(bool)report["after"]["meshes"][renderer.name]["hidden"];
        foreach (var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.enabled).ToArray())
        {
            var mesh = new Mesh(); skin.BakeMesh(mesh, true); mesh.name = name + "_" + skin.name;
            string path = MODEL_FOLDER + mesh.name + "_Static.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; EditorUtility.SetDirty(mesh); }
            var part = new GameObject(skin.name + "_Static"); part.transform.SetParent(skin.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh; part.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials; skin.enabled = false;
        }
        return obj;
    }
    static Transform Bone(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(name, StringComparison.OrdinalIgnoreCase));
    static Vector3[] Points(GameObject root) => root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.GetComponents<MeshRenderer>().Single().enabled).SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToArray();
    static Bounds BoundsOf(Vector3[] points) { var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point); return bounds; }
    static void Texture(string path, bool linear, bool normal, bool transparent)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !linear; importer.alphaIsTransparency = transparent; importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (normal) importer.flipGreenChannel = true;
        importer.SaveAndReimport();
    }
}
