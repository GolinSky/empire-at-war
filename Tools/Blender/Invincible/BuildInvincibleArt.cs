using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildInvincibleArt
{
    const string MODEL = "Assets/Art/Models/SeparatistShips/Invincible/Invincible.fbx";
    const string TEXTURES = "Assets/Art/Textures/Models/SeparatistShips/Invincible/";
    const string MATERIALS = "Assets/Art/Materials/Models/SeparatistShips/Invincible/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Invincible.prefab";
    const string PROVIDENCE = "Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab";
    const string TASK = "Temp/InvincibleImport/";
    const float SIZE_RATIO = 39f / 14f;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Invincible art construction requires Edit Mode.");
        AssetDatabase.Refresh();
        EnsureFolder(MATERIALS.TrimEnd('/'));
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var normalMaps = report["materials"].Select(m => (string)m["normal"]).Where(n => n != null).Select(n => TEXTURES + n + ".png").Distinct().ToArray();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] {TEXTURES.TrimEnd('/')}))
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
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(MODEL);
        modelImporter.globalScale = .02f;
        modelImporter.preserveHierarchy = true;
        modelImporter.importAnimation = false;
        modelImporter.isReadable = true;
        modelImporter.weldVertices = false;
        modelImporter.meshCompression = ModelImporterMeshCompression.Off;
        foreach (var key in modelImporter.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)).ToArray()) modelImporter.RemoveRemap(key);
        foreach (var source in report["materials"])
        {
            string name = (string)source["name"], sourceShader = (string)source["shader"];
            string baseMap = (string)source["base"], normalMap = (string)source["normal"], maskMap = (string)source["mask"];
            bool additive = sourceShader == "MeshAdditive.fx";
            var shader = Shader.Find(additive ? "Universal Render Pipeline/Particles/Unlit" : baseMap != null ? "EmpireAtWar/Ship Lit" : "Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Missing shader for " + name);
            string path = MATERIALS + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            if (baseMap != null) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + baseMap + ".png"));
            material.SetColor("_BaseColor", Color.white);
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + normalMap + ".png"));
                material.SetFloat("_BumpScale", 1);
                material.EnableKeyword("_NORMALMAP");
            }
            if (additive)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
                material.SetFloat("_SrcBlend", 1); material.SetFloat("_DstBlend", 1);
                material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }
            if (material.HasProperty("_TeamMaskStrength"))
            {
                material.SetTexture("_TeamMaskMap", maskMap != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURES + maskMap + ".png") : null);
                material.SetFloat("_TeamMaskStrength", maskMap != null ? 1 : 0);
                material.SetFloat("_TeamLiveryStrength", 0);
                material.SetFloat("_TeamRimStrength", 0);
                material.SetFloat("_Metallic", .15f); material.SetFloat("_Smoothness", .3f);
            }
            EditorUtility.SetDirty(material);
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }
        modelImporter.SaveAndReimport();
        float donorLength = BoundsOf(AssetDatabase.LoadAssetAtPath<GameObject>(PROVIDENCE)).size.z;
        float targetLength = donorLength * SIZE_RATIO;
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Invincible");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODEL), root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var lod in All<LODGroup>(model)) UnityEngine.Object.DestroyImmediate(lod);
            foreach (var animator in All<Animator>(model)) UnityEngine.Object.DestroyImmediate(animator);
            foreach (var renderer in All<Renderer>(model))
            {
                var source = ((JObject)report["before"]["meshes"]).Properties().Single(row => (string)row.Value["exported_name"] == renderer.name).Value;
                if ((bool)source["hidden"])
                {
                    foreach (var filter in Attached<MeshFilter>(renderer.gameObject)) UnityEngine.Object.DestroyImmediate(filter);
                    UnityEngine.Object.DestroyImmediate(renderer);
                    continue;
                }
                renderer.enabled = true;
                if (!(renderer is SkinnedMeshRenderer skinned)) continue;
                var mesh = new Mesh(); skinned.BakeMesh(mesh, false);
                string path = Path.GetDirectoryName(MODEL).Replace('\\', '/') + "/Invincible_" + renderer.name + "Mesh.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null) AssetDatabase.CreateAsset(mesh, path);
                else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
                var owner = renderer.gameObject; var materials = renderer.sharedMaterials;
                UnityEngine.Object.DestroyImmediate(renderer);
                owner.AddComponent<MeshFilter>().sharedMesh = mesh;
                owner.AddComponent<MeshRenderer>().sharedMaterials = materials;
            }
            var rawBounds = BoundsOf(root);
            float scale = targetLength / rawBounds.size.z;
            model.transform.localScale *= scale;
            model.transform.localPosition = -rawBounds.center * scale;
            var bounds = BoundsOf(root);
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            AssetDatabase.SaveAssets();
            var result = new JObject {
                ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z), ["center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                ["donorLength"] = donorLength, ["targetLength"] = targetLength, ["sizeRatio"] = SIZE_RATIO,
                ["modelScale"] = scale, ["sourceBoundsCenter"] = new JArray(rawBounds.center.x, rawBounds.center.y, rawBounds.center.z),
                ["visibleMeshes"] = All<MeshRenderer>(root).Count(r => r.enabled),
                ["teamMask"] = "Invincible_Hull_TeamMask", ["maskConvention"] = "source albedo alpha, 4.52621% painted stripes"
            };
            File.WriteAllText(TASK + "ArtReport.json", result.ToString());
            return result.ToString();
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static Bounds BoundsOf(GameObject root)
    {
        var points = All<MeshRenderer>(root).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit"))
            .SelectMany(r => Attached<MeshFilter>(r.gameObject).Single().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        return bounds;
    }

    static IEnumerable<Transform> Hierarchy(Transform root)
    {
        yield return root;
        foreach (Transform child in root)
            foreach (var descendant in Hierarchy(child)) yield return descendant;
    }

    static IEnumerable<T> Attached<T>(GameObject owner) where T : Component
    {
        var serialized = new SerializedObject(owner);
        var list = serialized.FindProperty("m_Component");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue is T component) yield return component;
    }

    static T[] All<T>(GameObject root) where T : Component => Hierarchy(root.transform).SelectMany(t => Attached<T>(t.gameObject)).ToArray();

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
