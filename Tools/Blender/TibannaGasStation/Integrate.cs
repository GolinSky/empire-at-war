using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class IntegrateTibanna
{
    const string VISUAL = "Assets/Prefabs/Models/MiningFacilities/TibannaGasStation.prefab";
    const string VIEW = "Assets/Prefabs/Models/MiningFacilities/TibannaGasStationView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/TibannaGasStationReinforcementView.prefab";
    const string WRECK = "Assets/Settings/Data/Models/Wrecks/Shared/TibannaGasStationWreckData.asset";
    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Tibanna integration requires Edit Mode.");
        if (!File.Exists(VIEW)) AssetDatabase.CopyAsset("Assets/Prefabs/Models/MiningFacilities/MiningFacilityView.prefab", VIEW);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        try
        {
            root.name = "TibannaGasStationView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); root.transform.localScale = Vector3.one;
            var old = root.transform.Cast<Transform>().Single(t => t.name == "Sphere" || t.name == "TibannaGasStation");
            UnityEngine.Object.DestroyImmediate(old.gameObject);
            var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            var hull = art.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            foreach (var t in art.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = root.layer;
            bounds = hull[0].bounds; foreach (var renderer in hull.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var collider = root.GetComponents<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size;
            var health = new SerializedObject(Component(root, "HealthComponent")); health.FindProperty("ionFieldBounds").boundsValue = bounds; Save(health);
            var shield = (Shield)health.FindProperty("shieldView").objectReferenceValue;
            ShieldHullBaker.Bake(root.transform, shield);
            var shieldRenderer = shield.GetComponents<MeshRenderer>().Single();
            SetArray(Component(root, "FogVisibilityComponent"), "renderers", hull.Cast<UnityEngine.Object>().Append(shieldRenderer).ToArray());
            SetArray(Component(root, "UnitRenderStateComponent"), "meshRenderers", hull.Cast<UnityEngine.Object>().Append(shieldRenderer).ToArray());
            SetArray(Component(root, "MiningFacility"), "explosionHullRenderers", hull.Where(r => r.sharedMaterials.Any(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray());
            var selection = root.transform.Cast<Transform>().Single(t => t.name == "SelectedCanvas"); selection.localScale = Vector3.one; selection.localPosition = new Vector3(0, bounds.min.y - 2, 0);
            var select = new SerializedObject(Component(root, "SelectionComponent")); select.FindProperty("ringSize").vector2Value = Vector2.one * Mathf.Max(bounds.size.x, bounds.size.z) * 1.12f; Save(select);
            root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "ShieldGenerator").position = new Vector3(0, bounds.max.y, 0);
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var wreck = ShipWreckBuilder.Build(VIEW);
        if (!File.Exists(WRECK)) AssetDatabase.CopyAsset("Assets/Settings/Data/Models/Wrecks/Shared/MiningFacilityWreckData.asset", WRECK);
        var wreckData = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK));
        wreckData.targetObject.name = "TibannaGasStationWreckData";
        wreckData.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreck)); Save(wreckData);
        if (!File.Exists(PREVIEW)) AssetDatabase.CopyAsset("Assets/Prefabs/Ui/Reinforcement/CommonMiningReinforcementView.prefab", PREVIEW);
        root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "TibannaGasStationReinforcementView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); root.transform.localScale = Vector3.one;
            foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var art = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            var renderers = art.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => hologram).ToArray();
            SetArray(Component(root, "UnitSpawnView"), "meshRenderers", renderers);
            var config = new SerializedObject(Component(root, "UnitSpawnView")); config.FindProperty("hologramMaterial").objectReferenceValue = hologram; Save(config);
            var collider = root.GetComponents<SphereCollider>().Single(); collider.center = bounds.center; collider.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return "Saved Tibanna gameplay, wreck/data and holographic placement prefab; bounds " + bounds;
    }
    static UnityEngine.Component Component(GameObject root, string name) => root.GetComponentsInChildren<UnityEngine.Component>(true).Single(c => c != null && (c.GetType().Name == name || name == "UnitRenderStateComponent" && c.GetType().Name == "TeamColorView"));
    static void Save(SerializedObject value) { value.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(value.targetObject); }
    static void SetArray(UnityEngine.Component component, string name, UnityEngine.Object[] items)
    {
        var config = new SerializedObject(component); var array = config.FindProperty(name); array.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i]; Save(config);
    }
}
