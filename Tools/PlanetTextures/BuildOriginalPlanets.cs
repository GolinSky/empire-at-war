using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Services.VContainer;
using Object = UnityEngine.Object;

public static class BuildOriginalPlanets
{
    private const string PREFABS = "Assets/Prefabs/View/Planets/";
    private const string MATERIALS = "Assets/Art/Materials/Models/Planets/Original/";
    private const string MESHES = "Assets/Art/Models/Planets/Shared/";
    private static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    private static JObject _imports;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build planets in Edit Mode.");
        _imports = JObject.Parse(File.ReadAllText("Temp/PlanetImport/imported-textures.json"));
        if (!AssetDatabase.IsValidFolder(MATERIALS.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Art/Materials/Models/Planets", "Original");
        var sphere = AssetDatabase.LoadAssetAtPath<Mesh>(MESHES + "PlanetSphere.asset");
        var manifest = JObject.Parse(File.ReadAllText("Temp/PlanetImport/manifest.json"));
        foreach (JToken planet in manifest["planets"])
        {
            string name = (string)planet["name"];
            bool existing = (bool)planet["preserve_existing"];
            string path = PREFABS + name + "Planet.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(existing ? path : PREFABS + "CoruscantPlanet.prefab");
            try
            {
                root.name = name + "Planet";
                var scope = root.GetComponent<PlanetScope>();
                var view = new SerializedObject(scope.View);
                var surface = (MeshRenderer)view.FindProperty("planetRenderers").GetArrayElementAtIndex(0).objectReferenceValue;
                var filter = surface.GetComponent<MeshFilter>();
                // Retain authored planet size while replacing the 33k-vertex Kamino mesh too.
                float radius = filter.sharedMesh.bounds.extents.magnitude / Mathf.Sqrt(3) * surface.transform.localScale.x;
                if (name == "Kamino" && filter.sharedMesh != sphere)
                {
                    surface.transform.localScale = Vector3.one * radius * 2;
                    surface.transform.localRotation *= Quaternion.Euler(90, 0, 0);
                }
                filter.sharedMesh = sphere;
                if (!existing)
                {
                    // Keep the full rings below the battle plane, aligned with the planet's equator.
                    if (planet["rings"].Any())
                        surface.transform.localRotation = Quaternion.Inverse(root.transform.localRotation) * Quaternion.Euler(15, 0, 0);
                    surface.sharedMaterial = MaterialFor((string)planet["surface"], "Surface", false);
                    var clouds = view.FindProperty("cloudRenderers");
                    var cloud = (MeshRenderer)clouds.GetArrayElementAtIndex(0).objectReferenceValue;
                    for (int i = clouds.arraySize - 1; i > 0; i--)
                        Object.DestroyImmediate(((MeshRenderer)clouds.GetArrayElementAtIndex(i).objectReferenceValue).gameObject);
                    string texture = (string)planet["cloud"];
                    clouds.arraySize = texture.Length > 0 ? 1 : 0;
                    if (texture.Length == 0) Object.DestroyImmediate(cloud.gameObject);
                    else
                    {
                        cloud.sharedMaterial = MaterialFor(texture, "Clouds", true);
                        cloud.GetComponent<MeshFilter>().sharedMesh = sphere;
                        cloud.transform.localScale = Vector3.one * 201;
                        cloud.transform.localRotation = surface.transform.localRotation;
                    }
                    view.ApplyModifiedPropertiesWithoutUndo();
                    foreach (JToken ring in planet["rings"])
                    {
                        string part = (string)ring["mesh"];
                        var go = new GameObject(part, typeof(MeshFilter), typeof(MeshRenderer));
                        go.layer = surface.gameObject.layer;
                        go.transform.SetParent(root.transform, false);
                        go.transform.localScale = Vector3.one * 100;
                        go.transform.localRotation = surface.transform.localRotation;
                        go.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESHES + name + "_" + part + ".asset");
                        var renderer = go.GetComponent<MeshRenderer>();
                        renderer.sharedMaterial = MaterialFor((string)ring["texture"], "Rings", (string)ring["shader"] == "MeshAlpha.fx");
                        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        BindCatalog();
        AssetDatabase.SaveAssets();
        return "Saved " + manifest["planets"].Count() + " planet prefabs, shared artwork and same-level selection/preview mappings.";
    }

    private static Material MaterialFor(string source, string role, bool transparent)
    {
        string key = Path.GetFileNameWithoutExtension(source) + "_" + role;
        if (_materials.TryGetValue(key, out Material material)) return material;
        string path = MATERIALS + key + ".mat";
        bool unlitRing = role == "Rings" && transparent;
        Shader shader = Shader.Find(unlitRing ? "Universal Render Pipeline/Unlit" : role == "Clouds" ? "EmpireAtWar/Planet Clouds" : "EmpireAtWar/Planet Lit");
        material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = key };
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.shaderKeywords = Array.Empty<string>();
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>((string)_imports["textures"][source]));
        material.SetColor("_BaseColor", Color.white);
        if (!unlitRing)
        {
            material.SetFloat("_Smoothness", 0);
            material.SetFloat("_ReceiveShadows", 0); material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            if (role != "Clouds")
            {
                material.SetFloat("_EnvironmentReflections", 0); material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                material.SetFloat("_SpecularHighlights", 0); material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
        }
        material.SetShaderPassEnabled("ShadowCaster", false);
        if (transparent)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", role == "Rings" ? 0 : 2);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        _materials.Add(key, material);
        return material;
    }

    private static void BindCatalog()
    {
        var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
        var data = new SerializedObject(settings);
        var entries = data.FindProperty("planetPrefabs.keyValue");
        var values = (PlanetType[])Enum.GetValues(typeof(PlanetType));
        entries.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            var row = entries.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("key").intValue = (int)values[i];
            row.FindPropertyRelative("value").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + values[i] + "Planet.prefab").GetComponent<PlanetScope>();
        }
        data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings);
        const string UI = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
        File.Copy(UI, "Temp/PlanetImport/SkirmishUi.before-import.prefab", true);
        var root = PrefabUtility.LoadPrefabContents(UI);
        try
        {
            var view = new SerializedObject(root.GetComponent<SkirmishUi>());
            var previews = view.FindProperty("planetPreviews.keyValue");
            previews.arraySize = values.Length - 2;
            for (int i = 2; i < values.Length; i++)
            {
                var row = previews.GetArrayElementAtIndex(i - 2);
                row.FindPropertyRelative("key").intValue = (int)values[i];
                row.FindPropertyRelative("value").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>((string)_imports["previews"][values[i].ToString()]);
            }
            view.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, UI);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
