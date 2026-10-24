using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildPeltaArt
{
    const string MODELS = "Assets/Art/Models/RepublicShips/Pelta/";
    const string TEXTURES = "Assets/Art/Textures/Models/RepublicShips/Pelta/";
    const string MATERIALS = "Assets/Art/Materials/Models/RepublicShips/Pelta/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/Pelta.prefab";
    const string TASK = "Temp/PeltaImport/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pelta art construction requires Edit Mode.");
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
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(MODELS + "Pelta.fbx");
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
                material.SetFloat("_TeamMaskStrength", 0);
                material.SetFloat("_TeamLiveryStrength", baseMap == "Pelta_Hull_Albedo" ? 1 : 0);
                material.SetFloat("_TeamLiveryHue", 0);
                material.SetFloat("_TeamLiveryHueRange", .07f);
                material.SetFloat("_TeamLiveryMinSaturation", .3f);
                material.SetFloat("_Metallic", .1f); material.SetFloat("_Smoothness", .3f);
            }
            EditorUtility.SetDirty(material);
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        modelImporter.SaveAndReimport();
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Pelta");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + "Pelta.fbx"), root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var record = ((JObject)report["models"]["Pelta"]["before"]["meshes"]).Properties().Single(row => (string)row.Value["exported_name"] == renderer.name);
                string sourceName = record.Name;
                var source = record.Value;
                if ((bool)source["hidden"] || sourceName.ToLowerInvariant().Contains("shadow"))
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
                string meshPath = MODELS + "Pelta_" + sourceName + "Mesh.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
                else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
                var owner = renderer.gameObject; var materials = renderer.sharedMaterials;
                UnityEngine.Object.DestroyImmediate(renderer);
                owner.AddComponent<MeshFilter>().sharedMesh = mesh;
                owner.AddComponent<MeshRenderer>().sharedMaterials = materials;
            }
            var bounds = BoundsOf(root);
            float length = 45f;
            float scale = length / bounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -bounds.center * scale;
            bounds = BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            AssetDatabase.SaveAssets();
            File.WriteAllText(TASK + "ArtReport.json", new JObject {
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                ["donorLength"] = length, ["modelScale"] = scale,
                ["visibleMeshes"] = root.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled)
            }.ToString());
            return "Saved Pelta visual, source-red team livery and materials; 45-unit hull bounds " + bounds.size;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points = root.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit"))
            .SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
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
