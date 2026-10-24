using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class ReplaceHeavyDreadnought
{
    const string TASK = "Temp/HeavyDreadnoughtImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/HeavyDreadnought.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/HeavyDreadnoughtShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Republic/HeavyDreadnoughtShipData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/HeavyDreadnoughtReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Replace the ship in Edit Mode.");
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom, top, radius;
        try
        {
            var movement = Config(root, "ShipMoveComponent");
            var oldBody = (Transform)movement.FindProperty("bodyTransform").objectReferenceValue;
            if (PrefabUtility.IsPartOfPrefabInstance(oldBody))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(oldBody.gameObject), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var shield = (Shield)Config(root, "HealthComponent").FindProperty("shieldView").objectReferenceValue;
            shield.transform.SetParent(root.transform, true);
            var hardpoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
            foreach (var point in hardpoints) point.transform.SetParent(root.transform, true);
            var oldBounds = root.GetComponents<BoxCollider>().Single().bounds;
            var exhaust = root.GetComponentsInChildren<ParticleSystem>(true).Where(p => p.name.StartsWith("Engine")).ToArray();
            var exhaustScales = exhaust.Select(p => p.transform.lossyScale).ToArray();
            var engineRoot = exhaust[0].transform.parent;
            engineRoot.SetParent(root.transform, true);
            UnityEngine.Object.DestroyImmediate(oldBody.gameObject);
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            movement.FindProperty("bodyTransform").objectReferenceValue = body;
            Save(movement);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            var bones = visual.GetComponentsInChildren<Transform>(true);
            var hull = visual.GetComponentsInChildren<MeshRenderer>(true);
            var points = hull.SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            // Six existing logical weapons keep their profiles/arcs/counts; each fires at an authored muzzle.
            int[] mounts = {6, 7, 4, 5, 0, 1};
            var mapping = hardpoints.Select(point => new {
                id = point.Id, type = (int)point.HardPointType,
                source = point is WeaponHardPoint ? "Turret" + mounts[point.Id].ToString("00") + " muzzle" : point.Id == 7 ? "b_engines" : "nearest upper hull surface"
            }).ToArray();
            foreach (var point in hardpoints)
            {
                point.transform.SetParent(body, false);
                point.transform.localScale = Vector3.one;
                point.transform.localRotation = Quaternion.identity;
                if (point is WeaponHardPoint)
                {
                    int mount = mounts[point.Id];
                    point.transform.position = bones.Single(t => t.name.StartsWith("Turret" + mount.ToString("00") + "__") && t.name.ToLowerInvariant().Contains("muzzlea_")).position;
                }
                else if (point.Id == 7) point.transform.position = bones.Single(t => t.name == "b_engines").position;
                else point.transform.localPosition = points.OrderBy(p => (p - new Vector3(0, bounds.max.y, -bounds.size.z * .15f)).sqrMagnitude).First();
                var anchor = (Transform)new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue;
                var target = root.transform.InverseTransformPoint(point.transform.position);
                anchor.position = root.transform.TransformPoint(points.OrderBy(p => (p - target).sqrMagnitude).First());
            }
            engineRoot.SetParent(body, false);
            engineRoot.localPosition = Vector3.zero; engineRoot.localRotation = Quaternion.identity; engineRoot.localScale = Vector3.one;
            var exhaustSockets = bones.Where(t => t.name.StartsWith("pe_dreadnought_")).ToArray();
            for (int i = 0; i < exhaustSockets.Length; i++)
            {
                var effect = i < exhaust.Length ? exhaust[i] : UnityEngine.Object.Instantiate(exhaust[0], engineRoot);
                effect.name = "EngineExhaust" + i.ToString("00");
                effect.transform.position = exhaustSockets[i].position;
                effect.transform.localRotation = Quaternion.Euler(0, 180, 0);
                effect.transform.localScale = exhaustScales[i % exhaustScales.Length] * (bounds.size.x / oldBounds.size.x) * (exhaustSockets[i].name.Contains("sml") ? .5f : 1f);
            }
            var bank = Load(DATA).FindProperty("<BodyRotationMaxAngle>k__BackingField").floatValue;
            var banked = new[] {-bank, 0f, bank}.SelectMany(angle => points.Select(p => Quaternion.Euler(0, 0, angle) * p)).ToArray();
            bottom = banked.Min(p => p.y); top = banked.Max(p => p.y);
            radius = Mathf.Ceil(new Vector2(bounds.extents.x, bounds.extents.z).magnitude) + 2;
            Assign(Component(root, "Ship"), "explosionHullRenderers", hull);
            ShieldHullBaker.Bake(root.transform, shield);
            Assign(Component(root, "UnitRenderStateComponent"), "meshRenderers", root.GetComponentsInChildren<MeshRenderer>(true));
            Assign(Component(root, "FogVisibilityComponent"), "renderers", root.GetComponentsInChildren<Renderer>(true).Where(r => !(r is SpriteRenderer)).ToArray());
            var health = Config(root, "HealthComponent");
            health.FindProperty("ionFieldBounds").boundsValue = new Bounds(new Vector3(0, (bottom + top) * .5f, 0), new Vector3(bounds.size.x, top - bottom, bounds.size.z) * 1.05f);
            Save(health);
            var collider = root.GetComponents<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size;
            var selection = Config(root, "SelectionComponent"); selection.FindProperty("ringSize").vector2Value = Vector2.one * radius * 2; Save(selection);
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            File.WriteAllText(TASK + "HardpointMapping.json", JsonConvert.SerializeObject(mapping, Formatting.Indented));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var data = Load(DATA);
        data.FindProperty("<HullBottom>k__BackingField").floatValue = bottom;
        data.FindProperty("<HullTop>k__BackingField").floatValue = top;
        data.FindProperty("<NavigationRadius>k__BackingField").floatValue = radius;
        Save(data);
        var wreck = ShipWreckBuilder.Build(VIEW);
        foreach (var material in wreck.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
        {
            material.SetFloat("_TeamRimStrength", 0);
            EditorUtility.SetDirty(material);
        }
        BuildPreview(bounds);
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, bottom, top, radius}, Formatting.Indented));
        return "Replaced Dreadnaught visual, fitted shield/collision/banking bounds, preserved six weapons and rebuilt the current procedural wreck and placement preview.";
    }

    static void BuildPreview(Bounds bounds)
    {
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach (var renderer in renderers) renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
            var spawn = Component(root, "UnitSpawnView"); Assign(spawn, "meshRenderers", renderers);
            var config = new SerializedObject(spawn); config.FindProperty("hologramMaterial").objectReferenceValue = hologram; Save(config);
            var collider = root.GetComponents<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name);
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(Component(root, name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Assign(MonoBehaviour component, string field, UnityEngine.Object[] values)
    {
        var config = new SerializedObject(component); var array = config.FindProperty(field); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        Save(config);
    }
    static void Save(SerializedObject config) { config.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(config.targetObject); }
}
