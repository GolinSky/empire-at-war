using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.ViewComponents.Health;

public static class ReplaceTectorVisuals
{
    private const string OUTPUT = "F:/CodexArtifacts/tector-preview/UnityReplacement/";
    private const string VISUAL = "Assets/Prefabs/Models/Ships/Empire/Tector.prefab";
    private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/TectorShipView.prefab";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab";
    private const string WRECK = "Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Tector replacement requires Edit Mode.");
        var manifest = JObject.Parse(File.ReadAllText(OUTPUT + "ArtManifest.json"));
        var baseline = JObject.Parse(File.ReadAllText(OUTPUT + "UnityBaseline.json"));
        float length = (float)baseline[VISUAL]["boundsSize"][2];
        var root = PrefabUtility.LoadPrefabContents(VISUAL);
        Bounds hullBounds;
        try
        {
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>((string)manifest["model_path"]), geometry.transform);
            model.name = "Tector_Remake";
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                var definition = manifest["meshes"][renderer.name];
                string shader = (string)definition["shader"];
                bool helper = shader.Contains("Shadow") || shader.Contains("Collision") || shader.Contains("Shield") || renderer.name.StartsWith("Engine_Glow");
                if (helper) UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            }
            foreach (var lod in model.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(lod);
            var raw = BoundsOf(model.GetComponentsInChildren<MeshRenderer>(true));
            float scale = length / raw.size.z;
            geometry.transform.localScale = Vector3.one * scale;
            geometry.transform.localPosition = -raw.center * scale;
            hullBounds = BoundsOf(model.GetComponentsInChildren<MeshRenderer>(true));
            PrefabUtility.SaveAsPrefabAsset(root, VISUAL);
            File.WriteAllText(OUTPUT + "ArtBounds.json", new JObject { ["size"] = new JArray(hullBounds.size.x, hullBounds.size.y, hullBounds.size.z), ["scale"] = scale, ["offset"] = new JArray(geometry.transform.localPosition.x, geometry.transform.localPosition.y, geometry.transform.localPosition.z) }.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            var movement = Component(root, "ShipMoveComponent");
            var body = (Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            var visual = body.Cast<Transform>().Single(t => t.name == "Tector").gameObject;
            var hull = visual.GetComponentsInChildren<MeshRenderer>(true);
            Assign(Component(root, "Ship"), "explosionHullRenderers", hull.Where(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray());
            Assign(Component(root, "TeamColorView"), "meshRenderers", hull);
            Assign(Component(root, "FogVisibilityComponent"), "renderers", root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray());
            var engines = visual.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("pe_imperial_star_destroyer_large")).OrderBy(t => t.position.x).ToArray();
            if (engines.Length != 3) throw new InvalidOperationException("Expected the source model's three large engine anchors.");
            string[] effects = { "EngineL2", "EngineL", "EngineR2" };
            for (int i = 0; i < effects.Length; i++) body.Cast<Transform>().Single(t => t.name == effects[i]).position = engines[i].position;
            ShieldHullBaker.Bake(root.transform, (Shield)Component(root, "Shield"));
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            var renderers = model.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled = renderers.Contains(renderer);
                if (renderer.enabled) renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
            }
            Assign(Component(root, "UnitSpawnView"), "meshRenderers", renderers);
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        root = PrefabUtility.LoadPrefabContents(WRECK);
        try
        {
            var template = root.GetComponentsInChildren<MeshRenderer>(true).First().sharedMaterial;
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
            var sources = view.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var renderers = new MeshRenderer[sources.Length]; var filters = new MeshFilter[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var part = new GameObject(source.name); part.transform.SetParent(root.transform, false);
                Matrix4x4 matrix = view.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                part.transform.localPosition = matrix.GetColumn(3); part.transform.localRotation = matrix.rotation; part.transform.localScale = matrix.lossyScale;
                filters[i] = part.AddComponent<MeshFilter>(); filters[i].sharedMesh = source.GetComponents<MeshFilter>().Single().sharedMesh;
                renderers[i] = part.AddComponent<MeshRenderer>();
                renderers[i].sharedMaterials = source.sharedMaterials.Select(material =>
                {
                    string path = "Assets/Art/Materials/Wrecks/Tector/" + material.name + "_Wreck.mat";
                    var wreckMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (wreckMaterial == null) { wreckMaterial = new Material(template); AssetDatabase.CreateAsset(wreckMaterial, path); }
                    foreach (string property in new[] { "_BaseMap", "_BumpMap", "_TeamMaskMap" }) wreckMaterial.SetTexture(property, material.GetTexture(property));
                    foreach (string property in new[] { "_Metallic", "_Smoothness", "_TeamLiveryStrength", "_TeamMaskStrength", "_TeamRimStrength" }) wreckMaterial.SetFloat(property, material.GetFloat(property));
                    wreckMaterial.SetColor("_BaseColor", material.GetColor("_BaseColor"));
                    EditorUtility.SetDirty(wreckMaterial);
                    return wreckMaterial;
                }).ToArray();
            }
            Assign(Component(root, "UnitWreckView"), "meshRenderers", renderers);
            Assign(Component(root, "UnitWreckView"), "meshFilters", filters);
            PrefabUtility.SaveAsPrefabAsset(root, WRECK);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return "Replaced Tector hull/ship/placement/wreck artwork in place; turret meshes unused; existing gameplay components and all data assets retained. Hull size: " + hullBounds.size;
    }

    private static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == name);
    private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(target); var array = serialized.FindProperty(property); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static Bounds BoundsOf(MeshRenderer[] renderers)
    {
        var bounds = renderers[0].bounds; foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
    }
}
