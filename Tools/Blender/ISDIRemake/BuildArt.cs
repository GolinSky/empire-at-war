using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildISDIRemakeArt
{
    private const string TASK = "Temp/ISDIRemakeImport/";
    private const string MATERIALS = "Assets/Art/Materials/Models/EmpireShips/ISDI/";
    private const string TEXTURES = "Assets/Art/Textures/Models/EmpireShips/ISDI/";
    private static readonly Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>();

    public static string Main(bool importMaterials = true)
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory(MATERIALS);
        AssetDatabase.Refresh();
        var manifest = JObject.Parse(File.ReadAllText(TASK + "ArtManifest.json"));
        foreach (var entry in manifest.Properties())
        {
            if (!importMaterials)
            {
                _models[entry.Name] = AssetDatabase.LoadAssetAtPath<GameObject>((string)entry.Value["model_path"]);
                continue;
            }
            foreach (var row in entry.Value["materials"])
            {
                string shader = (string)row["shader"], stem = (string)row["base"], normal = (string)row["normal"];
                bool effect = shader.Contains("Additive") || shader.Contains("Shield");
                string path = MATERIALS + (string)row["name"] + "_" + (stem ?? "Helper") + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find(effect ? "Universal Render Pipeline/Particles/Unlit" : "EmpireAtWar/Ship Lit"));
                    AssetDatabase.CreateAsset(material, path);
                }
                if (stem != null)
                {
                    string map = TEXTURES + stem + (effect && !stem.EndsWith("NONE") ? "_Additive" : "") + ".png";
                    ConfigureTexture(map, false, false);
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(map));
                }
                if (normal != null)
                {
                    ConfigureTexture(TEXTURES + normal + ".png", true, true);
                    material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + normal + ".png"));
                    material.EnableKeyword("_NORMALMAP");
                }
                if (effect)
                {
                    material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
                    material.SetFloat("_SrcBlend", (float)BlendMode.One); material.SetFloat("_DstBlend", (float)BlendMode.One);
                    material.SetFloat("_SrcBlendAlpha", 1); material.SetFloat("_DstBlendAlpha", 10);
                    material.SetFloat("_ZWrite", 0); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
                }
                else
                {
                    material.SetFloat("_Smoothness", .25f); material.SetFloat("_Metallic", .1f);
                    material.SetFloat("_TeamLiveryStrength", 0); material.SetFloat("_TeamMaskStrength", 0); material.SetFloat("_TeamRimStrength", 0);
                }
                if (row["source_properties"] is JObject parameters)
                {
                    var tint = parameters[effect ? "Color" : "Diffuse"] as JArray;
                    if (tint != null) material.SetColor("_BaseColor", new Color((float)tint[0], (float)tint[1], (float)tint[2], effect ? Mathf.Max((float)tint[3], 0) : 1));
                }
                EditorUtility.SetDirty(material);
                row["material_path"] = path;
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath((string)entry.Value["model_path"]);
            importer.globalScale = .02f; importer.importAnimation = false; importer.preserveHierarchy = true;
            importer.isReadable = true; importer.meshCompression = ModelImporterMeshCompression.Off;
            foreach (var row in entry.Value["materials"])
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), (string)row["name"]), AssetDatabase.LoadAssetAtPath<Material>((string)row["material_path"]));
            importer.SaveAndReimport();
            _models[entry.Name] = AssetDatabase.LoadAssetAtPath<GameObject>((string)entry.Value["model_path"]);
        }
        File.WriteAllText(TASK + "ArtManifest.json", manifest.ToString());
        var audit = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
        var root = new GameObject("ISDI");
        float scale; Vector3 offset;
        try
        {
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(root.transform, false);
            var hull = AddModel("ISDI", geometry.transform, manifest);
            foreach (var attachment in audit["attachments"])
                AddModel((string)attachment["variant"], Bone(hull, (string)attachment["bone"]), manifest);
            var bounds = BoundsOf(root);
            scale = (float)JObject.Parse(File.ReadAllText(TASK + "Baseline.json"))["length"] / bounds.size.z;
            offset = -bounds.center * scale;
            geometry.transform.localScale = Vector3.one * scale; geometry.transform.localPosition = offset;
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Models/Ships/ISDI.prefab");
            var size = BoundsOf(root).size;
            File.WriteAllText(TASK + "ArtBounds.json", new JObject { ["size"] = new JArray(size.x,size.y,size.z), ["scale"] = scale, ["offset"] = new JArray(offset.x,offset.y,offset.z) }.ToString());
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        var wreck = new GameObject("ISDIView");
        try
        {
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(wreck.transform, false);
            var hull = AddModel("ISDIWreck", geometry.transform, manifest);
            AddModel("ISDIPart00", Bone(hull, "Decal_Attach_04"), manifest);
            geometry.transform.localScale = Vector3.one * scale; geometry.transform.localPosition = offset;
            PrefabUtility.SaveAsPrefabAsset(wreck, "Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(wreck); }
        AssetDatabase.SaveAssets();
        return "Saved full Remake ISD I with 14 source-bone attachments and dedicated DeathClone geometry.";
    }

    private static GameObject AddModel(string name, Transform parent, JObject manifest)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(_models[name], parent); obj.name = name;
        if (parent.name != "Geometry")
        {
            obj.transform.localScale = Vector3.one * .01f; obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        foreach (var lod in obj.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true)) renderer.enabled = !(bool)manifest[name]["after"]["meshes"][renderer.name]["hidden"];
        foreach (var skin in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.enabled).ToArray())
        {
            var mesh = new Mesh(); skin.BakeMesh(mesh, true); mesh.name = name + "_" + skin.name;
            string path = "Assets/Art/Models/EmpireShips/ISDI/" + mesh.name + "_Static.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; EditorUtility.SetDirty(saved); }
            var visual = new GameObject(skin.name + "_Static"); visual.transform.SetParent(skin.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            visual.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
            skin.enabled = false;
        }
        return obj;
    }

    private static Transform Bone(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(name, StringComparison.OrdinalIgnoreCase));
    private static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
    }
    private static void ConfigureTexture(string path, bool linear, bool normal)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !linear; importer.alphaIsTransparency = path.Contains("_Additive");
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (normal) importer.flipGreenChannel = true;
        importer.SaveAndReimport();
    }
}
