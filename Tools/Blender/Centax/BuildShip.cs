using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class BuildCentaxShip
{
    const string TASK = "Temp/CentaxImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/Centax.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/CentaxShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Republic/CentaxShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Republic/CentaxWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/CentaxReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Centax construction requires Edit Mode.");
        Directory.CreateDirectory(TASK);
        Copy("Assets/Prefabs/Models/Ships/Republic/CR70ShipView.prefab", VIEW);
        Copy("Assets/Settings/Data/Ship/Republic/ArquitensShipData.asset", DATA);
        Copy("Assets/Settings/Data/Models/Wrecks/Republic/CR70WreckData.asset", WRECK_DATA);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom, top, radius;
        string[] sockets = Enumerable.Range(1, 10).Select(i => "HP" + i.ToString("00")).Concat(new[] {"HP11_00 + HP11_01 midpoint", "HP_E", "HP_S"}).ToArray();
        try
        {
            root.name = "CentaxShipView";
            var body = (Transform)Config(root, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root, "HealthComponent");
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            var weaponComponent = Component(root, "WeaponComponent");
            var donorPoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
            var weapons = donorPoints.OfType<WeaponHardPoint>().ToList();
            while (weapons.Count < 11)
                weapons.Add(UnityEngine.Object.Instantiate(weapons[0], root.transform));
            var hardpoints = weapons.Cast<HardPoint>().Concat(donorPoints.Where(p => !(p is WeaponHardPoint))).ToArray();
            for (int i = 0; i < hardpoints.Length; i++)
            {
                var config = new SerializedObject(hardpoints[i]);
                config.FindProperty("<Id>k__BackingField").intValue = i;
                hardpoints[i].name = i < 8 ? "Laser" + (i + 1).ToString("00") : i < 10 ? "ProtonTorpedo" + (i - 7) : i == 10 ? "ForwardDualTurbolaser" : i == 11 ? "Engines" : "ShieldGenerator";
                if (i < 11)
                {
                    config.FindProperty("<WeaponType>k__BackingField").intValue = i < 8 ? 8 : i < 10 ? 6 : 5;
                    config.FindProperty("mainBattery").boolValue = i == 10;
                    var arc = config.FindProperty("yAxisRange");
                    arc.FindPropertyRelative("<Min>k__BackingField").floatValue = i < 4 ? -177.5f : i < 8 ? 2.5f : i < 10 ? -90 : -50;
                    arc.FindPropertyRelative("<Max>k__BackingField").floatValue = i < 4 ? -2.5f : i < 8 ? 177.5f : i < 10 ? 90 : 50;
                }
                config.ApplyModifiedPropertiesWithoutUndo();
            }
            // Keep the donor's explicit component/VFX dependencies; replace only its geometry.
            foreach (var point in hardpoints) point.transform.SetParent(root.transform, true);
            foreach (var component in new MonoBehaviour[] {shield, weaponComponent, Component(root, "AudioShipComponent")})
                component.transform.SetParent(root.transform, true);
            foreach (var child in body.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            var bones = visual.GetComponentsInChildren<Transform>(true).ToArray();
            // Eight side lasers and two fore launchers use authored sockets; the twin barrel uses their midpoint.
            for (int i = 0; i < hardpoints.Length; i++)
            {
                var point = hardpoints[i];
                point.transform.SetParent(body, false);
                point.transform.localScale = Vector3.one;
                point.transform.localRotation = Quaternion.identity;
                point.transform.position = i == 10
                    ? (bones.Single(t => t.name == "HP11_00").position + bones.Single(t => t.name == "HP11_01").position) * .5f
                    : bones.Single(t => t.name == sockets[i]).position;
            }
            weaponComponent.transform.SetParent(body, true);
            var visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var hull = visible.Where(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var points = hull.SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices
                .Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            foreach (var point in hardpoints)
            {
                var anchor = (Transform)new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue;
                anchor.position = point.transform.position;
                anchor.localScale = Vector3.one;
            }
            var engineAnchor = (Transform)new SerializedObject(hardpoints[11]).FindProperty("destroyedVfxAnchor").objectReferenceValue;
            var engineTarget = root.transform.InverseTransformPoint(hardpoints[11].transform.position);
            engineAnchor.position = root.transform.TransformPoint(points.OrderBy(p => (p - engineTarget).sqrMagnitude).First());
            var banked = new[] {-15f, 0f, 15f}.SelectMany(angle => points.Select(p => Quaternion.Euler(0, 0, angle) * p)).ToArray();
            bottom = banked.Min(p => p.y); top = banked.Max(p => p.y);
            radius = Mathf.Ceil(new Vector2(bounds.extents.x, bounds.extents.z).magnitude) + 2;
            Assign(health, "<ShipUnits>k__BackingField", hardpoints);
            Assign(weaponComponent, "hardPoints", hardpoints.OfType<WeaponHardPoint>().ToArray());
            Assign(Component(root, "EntityBindings"), "<Injectables>k__BackingField", hardpoints.Cast<UnityEngine.Object>().Concat(new[] {Component(root, "UnitRenderStateComponent")}).ToArray());
            Assign(Component(root, "Ship"), "explosionHullRenderers", hull);
            ShieldHullBaker.Bake(root.transform, shield);
            var shieldRenderer = shield.GetComponents<MeshRenderer>().Single();
            Assign(Component(root, "UnitRenderStateComponent"), "meshRenderers", visual.GetComponentsInChildren<MeshRenderer>(true).Concat(new[] {shieldRenderer}).ToArray());
            Assign(Component(root, "FogVisibilityComponent"), "renderers", visible.Cast<Renderer>().Concat(new Renderer[] {shieldRenderer, root.GetComponents<LineRenderer>().Single()}).ToArray());
            var healthConfig = new SerializedObject(health);
            healthConfig.FindProperty("ionFieldBounds").boundsValue = new Bounds(new Vector3(0, (bottom + top) * .5f, 0), new Vector3(bounds.size.x, top - bottom, bounds.size.z) * 1.05f);
            healthConfig.ApplyModifiedPropertiesWithoutUndo();
            var collider = root.GetComponents<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size;
            var selection = Config(root, "SelectionComponent"); selection.FindProperty("ringSize").vector2Value = Vector2.one * radius * 2; Save(selection);
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t => t.name == "SelectedImage")) rect.sizeDelta = Vector2.one * radius * 2;
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            File.WriteAllText(TASK + "HardpointMapping.json", JsonConvert.SerializeObject(hardpoints.Select(p => new {id = p.Id, name = p.name, type = (int)p.HardPointType, position = new[] {p.transform.position.x, p.transform.position.y, p.transform.position.z}, source = sockets[p.Id]}), Formatting.Indented));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var wreckView = ShipWreckBuilder.Build(VIEW);
        ReplaceWreckGeometry(AssetDatabase.GetAssetPath(wreckView));
        var wreck = Load(WRECK_DATA); wreck.targetObject.name = "CentaxWreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView)); Save(wreck);
        var data = Load(DATA); data.targetObject.name = "CentaxShipData";
        Set(data, "Hull", 2000); Set(data, "Shields", 2000); Set(data, "ShieldRegenerateValue", 62); Set(data, "ShieldRegenerateDelay", 1);
        Set(data, "Speed", 30); Set(data, "RotationSpeed", 45); Set(data, "TurnAcceleration", 45);
        Set(data, "BodyRotationMaxAngle", 15); Set(data, "Range", 500);
        data.FindProperty("<ShipClass>k__BackingField").intValue = 3;
        var healthTypes = data.FindProperty("hardPointHealth"); healthTypes.arraySize = 3;
        float[] healthValues = {350, 325, 475};
        for (int i = 0; i < 3; i++)
        {
            var row = healthTypes.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("hardPointType").intValue = i;
            row.FindPropertyRelative("health").floatValue = healthValues[i];
            row.FindPropertyRelative("hullDamageMultiplier").floatValue = 1;
        }
        Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;
        var abilities = data.FindProperty("abilities"); abilities.arraySize = 1; abilities.GetArrayElementAtIndex(0).intValue = 20;
        data.FindProperty("hangarBays").arraySize = 0;
        var loadout = data.FindProperty("weaponLoadout"); loadout.arraySize = 3;
        int[] types = {8, 6, 5}, counts = {8, 2, 1};
        for (int i = 0; i < types.Length; i++)
        {
            var row = loadout.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("weaponType").intValue = types[i];
            row.FindPropertyRelative("count").intValue = counts[i];
        }
        Save(data);
        BuildPreview(bounds);
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, bottom, top, radius}, Formatting.Indented));
        return "Centax saved: eight side lasers, two forward proton torpedoes, one dual turbolaser, engine and shield generator; own shield, damaged-model wreck and placement preview.";
    }

    static void ReplaceWreckGeometry(string wreckPath)
    {
        var root = PrefabUtility.LoadPrefabContents(wreckPath);
        try
        {
            var oldMaterials = root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Republic/CentaxWreckVisual.prefab"), root.transform);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var hull = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var wreckShader = oldMaterials[0].shader;
            foreach (var renderer in hull)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    string path = "Assets/Art/Materials/Wrecks/Republic/Centax/" + source.name + "_Wreck.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    var copy = new Material(source) {shader = wreckShader, name = source.name + "_Wreck"};
                    if (material == null) { material = copy; AssetDatabase.CreateAsset(material, path); }
                    else { EditorUtility.CopySerialized(copy, material); UnityEngine.Object.DestroyImmediate(copy); }
                    material.SetFloat("_TeamRimStrength", 0);
                    EditorUtility.SetDirty(material);
                    return material;
                }).ToArray();
            }
            var wreckView = Component(root, "UnitWreckView");
            Assign(wreckView, "meshRenderers", hull);
            Assign(wreckView, "meshFilters", hull.Select(r => r.GetComponents<MeshFilter>().Single()).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, wreckPath);
            var usedMaterials = hull.SelectMany(r => r.sharedMaterials).ToArray();
            foreach (var material in oldMaterials.Where(m => !usedMaterials.Contains(m))) AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(material));
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void BuildPreview(Bounds bounds)
    {
        Copy("Assets/Prefabs/Ui/Reinforcement/CorellianCorvetteReinforcementView.prefab", PREVIEW);
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "CentaxReinforcementView";
            foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            var hull = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = hull.Contains(renderer);
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach (var renderer in hull) renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
            var spawn = Component(root, "UnitSpawnView"); Assign(spawn, "meshRenderers", hull);
            var config = new SerializedObject(spawn); config.FindProperty("hologramMaterial").objectReferenceValue = hologram; Save(config);
            var collider = root.GetComponents<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size; collider.isTrigger = true;
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Copy(string source, string target) { if (!File.Exists(target) && !AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException(target); }
    static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name);
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(Component(root, name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Assign(MonoBehaviour component, string field, UnityEngine.Object[] objects)
    {
        var config = new SerializedObject(component); var array = config.FindProperty(field); array.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        config.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Set(SerializedObject data, string name, float value) => data.FindProperty("<" + name + ">k__BackingField").floatValue = value;
    static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
}
