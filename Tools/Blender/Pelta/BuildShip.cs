using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class BuildPeltaShip
{
    const string TASK = "Temp/PeltaImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/Pelta.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Republic/PeltaShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Republic/PeltaShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Republic/PeltaWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/PeltaReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pelta construction requires Edit Mode.");
        Directory.CreateDirectory(TASK);
        Copy("Assets/Prefabs/Models/Ships/Republic/CR70ShipView.prefab", VIEW);
        Copy("Assets/Settings/Data/Ship/Republic/ArquitensShipData.asset", DATA);
        Copy("Assets/Settings/Data/Models/Wrecks/Republic/CR70WreckData.asset", WRECK_DATA);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom, top, radius;
        string[][] sockets = {new[] {"HP01", "HP02"}, new[] {"HP06", "HP07"}, new[] {"HP03", "HP04"}, new[] {"HP08", "HP09"}, new[] {"HP11"}, new[] {"HP12"}, new[] {"HP13"}, new[] {"HP14"}, new[] {"HP05"}, new[] {"HP10"}, new[] {"derived stern hull surface"}, new[] {"Hull_Fixed: derived dorsal surface"}};
        int[] profiles = {1, 1, 1, 1, 8, 8, 8, 8, 10, 10};
        try
        {
            root.name = "PeltaShipView";
            var body = (Transform)Config(root, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root, "HealthComponent");
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            var weaponComponent = Component(root, "WeaponComponent");
            var hardpoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
            if (hardpoints.Length != 12 || hardpoints.OfType<WeaponHardPoint>().Count() != 10)
                throw new InvalidOperationException("Pelta donor must contain ten weapons, engine and shield generator.");
            foreach (var point in hardpoints) point.transform.SetParent(root.transform, true);
            foreach (var component in new MonoBehaviour[] {shield, weaponComponent, Component(root, "AudioShipComponent")})
                component.transform.SetParent(root.transform, true);
            foreach (var child in body.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            var bones = visual.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponents<MeshFilter>().Length == 0).ToArray();
            for (int i = 0; i < hardpoints.Length; i++)
            {
                var point = hardpoints[i];
                point.transform.SetParent(body, false);
                point.transform.localScale = Vector3.one;
                point.transform.localRotation = Quaternion.identity;
                if (i >= 10) continue;
                // The game has one emitter per logical weapon; paired source muzzles use their midpoint.
                var positions = sockets[i].Select(name => bones.Single(t => t.name == name).position).ToArray();
                point.transform.position = positions.Aggregate(Vector3.zero, (sum, p) => sum + p) / positions.Length;
                point.name = (i < 4 ? "Turbolaser_" : i < 8 ? "Laser_" : "IonCannon_") + (i + 1);
                var config = new SerializedObject(point);
                config.FindProperty("<WeaponType>k__BackingField").intValue = profiles[i];
                config.FindProperty("<HardPointType>k__BackingField").intValue = 0;
                config.FindProperty("mainBattery").boolValue = i < 4;
                Save(config);
            }
            weaponComponent.transform.SetParent(body, true);
            var visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var hull = visible.Where(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var points = hull.SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices
                .Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            var engineTarget = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z);
            var generatorTarget = root.transform.InverseTransformPoint(bones.Single(t => t.name == "Hull_Fixed").position);
            generatorTarget.x = bounds.center.x; generatorTarget.y = bounds.max.y;
            hardpoints[10].name = "Engines";
            hardpoints[11].name = "ShieldGenerator";
            hardpoints[10].transform.position = root.transform.TransformPoint(points.OrderBy(p => (p - engineTarget).sqrMagnitude).First());
            hardpoints[11].transform.position = root.transform.TransformPoint(points.OrderBy(p => (p - generatorTarget).sqrMagnitude).First());
            foreach (var point in hardpoints)
            {
                var anchor = (Transform)new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue;
                anchor.position = point.transform.position;
                anchor.localScale = Vector3.one;
            }
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
            var fog = Config(root, "FogVisibilityComponent"); fog.FindProperty("revealRadius").floatValue = radius; Save(fog);
            Assign(Component(root, "FogVisibilityComponent"), "renderers", visible.Cast<Renderer>().Concat(new Renderer[] {shieldRenderer, root.GetComponents<LineRenderer>().Single()}).ToArray());
            var healthConfig = new SerializedObject(health);
            healthConfig.FindProperty("ionFieldBounds").boundsValue = new Bounds(new Vector3(0, (bottom + top) * .5f, 0), new Vector3(bounds.size.x, top - bottom, bounds.size.z) * 1.05f);
            Save(healthConfig);
            var collider = root.GetComponents<BoxCollider>().Single(); collider.center = bounds.center; collider.size = bounds.size;
            var selection = Config(root, "SelectionComponent"); selection.FindProperty("ringSize").vector2Value = Vector2.one * radius * 2; Save(selection);
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t => t.name == "SelectedImage")) rect.sizeDelta = Vector2.one * radius * 2;
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            File.WriteAllText(TASK + "HardpointMapping.json", JsonConvert.SerializeObject(hardpoints.Select(p => new {id = p.Id, name = p.name, type = (int)p.HardPointType, weapon = p.Id < 10 ? profiles[p.Id] : 0, position = new[] {p.transform.position.x, p.transform.position.y, p.transform.position.z}, source = sockets[p.Id], pairedEmitterMidpoint = p.Id < 4}), Formatting.Indented));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var wreckView = ShipWreckBuilder.Build(VIEW);
        foreach (var material in wreckView.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
        {
            material.SetFloat("_TeamRimStrength", 0);
            EditorUtility.SetDirty(material);
        }
        var wreck = Load(WRECK_DATA); wreck.targetObject.name = "PeltaWreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView)); Save(wreck);
        var data = Load(DATA); data.targetObject.name = "PeltaShipData";
        Set(data, "Hull", 2200); Set(data, "Shields", 1000); Set(data, "ShieldRegenerateValue", 1000f / 120f);
        Set(data, "Speed", 30); Set(data, "RotationSpeed", 45); Set(data, "TurnAcceleration", 45);
        Set(data, "BodyRotationMaxAngle", 15); Set(data, "Range", 500);
        Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        data.FindProperty("<ShipClass>k__BackingField").intValue = 3;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;
        data.FindProperty("abilities").arraySize = 0;
        data.FindProperty("hangarBays").arraySize = 0;
        var healthTypes = data.FindProperty("hardPointHealth"); healthTypes.arraySize = 3;
        float[] healthValues = {250, 400, 400};
        for (int i = 0; i < 3; i++) { var row = healthTypes.GetArrayElementAtIndex(i); row.FindPropertyRelative("hardPointType").intValue = i; row.FindPropertyRelative("health").floatValue = healthValues[i]; row.FindPropertyRelative("hullDamageMultiplier").floatValue = 1; }
        var loadout = data.FindProperty("weaponLoadout"); loadout.arraySize = 3;
        int[] types = {1, 8, 10}; int[] counts = {4, 4, 2};
        for (int i = 0; i < 3; i++) { var row = loadout.GetArrayElementAtIndex(i); row.FindPropertyRelative("weaponType").intValue = types[i]; row.FindPropertyRelative("count").intValue = counts[i]; }
        Save(data);
        BuildPreview(bounds);
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, bottom, top, radius}, Formatting.Indented));
        return "Pelta saved: four turbolasers, four lasers, two ions, engine and generator; own shield, wreck and placement preview.";
    }

    static void BuildPreview(Bounds bounds)
    {
        Copy("Assets/Prefabs/Ui/Reinforcement/CR70ReinforcementView.prefab", PREVIEW);
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "PeltaReinforcementView";
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
