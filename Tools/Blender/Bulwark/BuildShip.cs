using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class BuildBulwarkShip
{
    const string TASK = "Temp/BulwarkImport/";
    const string DONOR_VIEW = "Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab";
    const string DONOR_DATA = "Assets/Settings/Data/Ship/Separatist/ProvidenceShipData.asset";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Bulwark.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/BulwarkShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/BulwarkShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/BulwarkWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/BulwarkReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build Bulwark in Edit Mode.");
        Directory.CreateDirectory(TASK);
        var donorRoot = PrefabUtility.LoadPrefabContents(DONOR_VIEW);
        try { PrefabUtility.SaveAsPrefabAsset(donorRoot, VIEW); }
        finally { PrefabUtility.UnloadPrefabContents(donorRoot); }
        Copy(DONOR_DATA, DATA);
        Copy("Assets/Settings/Data/Models/Wrecks/Separatist/ProvidenceWreckData.asset", WRECK_DATA);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom, top, radius;
        try
        {
            root.name = "BulwarkShipView";
            foreach (var child in root.transform.Cast<Transform>().ToArray())
                if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                    PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var movement = Config(root, "ShipMoveComponent");
            var oldBody = (Transform)movement.FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root, "HealthComponent");
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            shield.transform.SetParent(root.transform, true);
            var weapons = Components<WeaponHardPoint>(root).OrderBy(p => p.Id).ToArray();
            var systems = Components<HardPoint>(root).Where(p => !(p is WeaponHardPoint)).OrderBy(p => p.Id).ToArray();
            // Preserve explicit donor dependencies before replacing the visual body.
            var weaponComponent = Component(root, "WeaponComponent");
            var hangar = Config(root, "HangarComponent");
            foreach (var point in weapons.Cast<HardPoint>().Concat(systems)) point.transform.SetParent(root.transform, true);
            weaponComponent.transform.SetParent(root.transform, true);
            ((MonoBehaviour)hangar.targetObject).transform.SetParent(root.transform, true);
            Component(root, "AudioShipComponent").transform.SetParent(root.transform, true);
            UnityEngine.Object.DestroyImmediate(oldBody.gameObject);
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            movement.FindProperty("bodyTransform").objectReferenceValue = body;
            Save(movement);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            var visualTransforms = Transforms(visual.transform).ToArray();
            var visible = Components<MeshRenderer>(visual).Where(r => r.enabled).ToArray();
            var hull = visible.Where(IsHull).ToArray();
            var points = HullPoints(visual);
            bounds = BoundsOf(points);
            var weaponRoot = weaponComponent.transform;
            weaponRoot.SetParent(body, false);
            Reset(weaponRoot);
            string[] sockets = {"HP_Bulwark_F1", "HP_Bulwark_F0", "HP_F_MAIN", "HP_Bulwark_L1", "HP_Bulwark_R1",
                "HP_Bulwark_L2", "HP_Bulwark_R2", "HP_Bulwark_L3", "HP_Bulwark_R3", "HP_Bulwark_L4", "HP_Bulwark_R4", "HP_Bulwark_L5", "HP_Bulwark_R5"};
            int[] types = {90,90,90,48,48,4,4,4,4,4,4,4,4};
            var allWeapons = new List<WeaponHardPoint>();
            var mapping = new List<object>();
            for (int i = 0; i < sockets.Length; i++)
            {
                var point = weapons[i];
                point.transform.SetParent(weaponRoot, false);
                Reset(point.transform);
                point.transform.position = visualTransforms.Single(t => t.name == sockets[i]).position;
                point.name = sockets[i];
                var config = new SerializedObject(point);
                config.FindProperty("<Id>k__BackingField").intValue = i;
                config.FindProperty("<WeaponType>k__BackingField").intValue = types[i];
                config.FindProperty("<HardPointType>k__BackingField").intValue = 0;
                float yaw = i < 3 ? 0 : sockets[i].Contains("_L") ? -90 : 90;
                config.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = yaw - 100;
                config.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = yaw + 100;
                config.FindProperty("mainBattery").boolValue = false;
                Save(config);
                FitAnchor(point, root.transform, points);
                allWeapons.Add(point);
                mapping.Add(new {id=i, source=sockets[i], weapon=types[i], position=Values(root.transform.InverseTransformPoint(point.transform.position))});
            }
            foreach (var unused in weapons.Skip(sockets.Length)) UnityEngine.Object.DestroyImmediate(unused.gameObject);
            string[] systemSockets = {"HP_Bulwark_Shield_Generator", "HP_Bulwark_Engines", "SPAWN_00"};
            int[] systemTypes = {2,1,4};
            var selectedSystems = systemTypes.Select(type => systems.First(p => (int)p.HardPointType == type)).ToArray();
            foreach (var unused in systems.Except(selectedSystems)) UnityEngine.Object.DestroyImmediate(unused.gameObject);
            systems = selectedSystems;
            for (int i = 0; i < systems.Length; i++)
            {
                var point = systems[i];
                point.transform.SetParent(body, false);
                Reset(point.transform);
                point.transform.position = visualTransforms.Single(t => t.name == systemSockets[i]).position;
                point.name = systemSockets[i];
                var config = new SerializedObject(point);
                config.FindProperty("<Id>k__BackingField").intValue = 13 + i;
                Save(config);
                FitAnchor(point, root.transform, points);
                mapping.Add(new {id=13+i, source=systemSockets[i], position=Values(root.transform.InverseTransformPoint(point.transform.position))});
            }
            var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
            var hangarRoot = ((MonoBehaviour)hangar.targetObject).transform;
            hangarRoot.SetParent(body, false);
            Reset(hangarRoot);
            launch.position = visualTransforms.Single(t => t.name == "SPAWN_00").position;
            var launchLocal = body.InverseTransformPoint(launch.position);
            launchLocal.y = Mathf.Min(launchLocal.y, bounds.min.y - 4);
            launch.localPosition = launchLocal;
            hangar.FindProperty("hangarHardPoint").objectReferenceValue = systems[2];
            hangar.FindProperty("bayHardPoints").arraySize = 0;
            hangar.FindProperty("bayLaunchPoints").arraySize = 0;
            hangar.FindProperty("isDestroyable").boolValue = true;
            Save(hangar);
            launch.localRotation = Quaternion.identity;
            launch.localScale = Vector3.one;
            var allPoints = allWeapons.Cast<HardPoint>().Concat(systems).OrderBy(p => p.Id).ToArray();
            Assign(health, "<ShipUnits>k__BackingField", allPoints);
            Assign(weaponComponent, "hardPoints", allWeapons.ToArray());
            var renderState = Component(root, "UnitRenderStateComponent");
            Assign(Component(root, "EntityBindings"), "<Injectables>k__BackingField", allPoints.Cast<UnityEngine.Object>().Concat(new[] {renderState}).ToArray());
            Assign(Component(root, "Ship"), "explosionHullRenderers", hull);
            ShieldHullBaker.Bake(root.transform, shield);
            var shieldRenderer = Components<MeshRenderer>(shield.gameObject).Single();
            Assign(renderState, "meshRenderers", visible.Concat(new[] {shieldRenderer}).ToArray());
            var fog = Component(root, "FogVisibilityComponent");
            Assign(fog, "renderers", Components<Renderer>(root).Where(r => !(r is SpriteRenderer)).ToArray());
            float bank = Load(DONOR_DATA).FindProperty("<BodyRotationMaxAngle>k__BackingField").floatValue;
            var banked = new[] {-bank, 0f, bank}.SelectMany(angle => points.Select(p => Quaternion.Euler(0, 0, angle) * p)).ToArray();
            var bankedBounds = BoundsOf(banked);
            bottom = bankedBounds.min.y; top = bankedBounds.max.y;
            radius = Mathf.Ceil(banked.Max(p => new Vector2(p.x, p.z).magnitude)) + 2;
            var healthConfig = new SerializedObject(health);
            healthConfig.FindProperty("ionFieldBounds").boundsValue = new Bounds(bankedBounds.center, bankedBounds.size * 1.05f);
            Save(healthConfig);
            var collider = Components<BoxCollider>(root).Single(c => c.gameObject == root);
            collider.center = bounds.center; collider.size = bounds.size;
            var selection = Config(root, "SelectionComponent");
            selection.FindProperty("ringSize").vector2Value = Vector2.one * radius * 2;
            Save(selection);
            var selectionRoot = (Transform)selection.FindProperty("selectionRoot").objectReferenceValue;
            selectionRoot.localPosition = new Vector3(bounds.center.x, bottom - 2, bounds.center.z);
            selectionRoot.localScale = Vector3.one;
            var selected = (SpriteRenderer)selection.FindProperty("selectedRenderer").objectReferenceValue;
            selected.transform.localPosition = Vector3.zero;
            var spriteSize = selected.sprite.bounds.size;
            selected.transform.localScale = new Vector3(radius * 2 / spriteSize.x, radius * 2 / spriteSize.y, 1);
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            File.WriteAllText(TASK + "HardpointMapping.json", JsonConvert.SerializeObject(mapping, Formatting.Indented));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var wreckView = ShipWreckBuilder.Build(VIEW);
        var wreck = Load(WRECK_DATA);
        wreck.targetObject.name = "BulwarkWreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView));
        Save(wreck);
        var data = Load(DATA); data.targetObject.name = "BulwarkShipData";
        Set(data, "Hull", 8500); Set(data, "Shields", 2500);
        Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;
        var abilities = data.FindProperty("abilities");
        abilities.arraySize = 2;
        abilities.GetArrayElementAtIndex(0).intValue = (int)EmpireAtWar.Services.ShipAbilities.ShipAbilityId.BoostShieldPower;
        abilities.GetArrayElementAtIndex(1).intValue = (int)EmpireAtWar.Services.ShipAbilities.ShipAbilityId.BoostWeaponPower;
        var bays = data.FindProperty("hangarBays"); bays.arraySize = 2;
        int[] squadrons = {102,101}, reserves = {4,2}, active = {2,1};
        for (int i = 0; i < 2; i++)
        {
            var bay = bays.GetArrayElementAtIndex(i);
            bay.FindPropertyRelative("squadronType").intValue = squadrons[i];
            bay.FindPropertyRelative("reserve").intValue = reserves[i];
            bay.FindPropertyRelative("maxActive").intValue = active[i];
        }
        var loadout = data.FindProperty("weaponLoadout"); loadout.arraySize = 3;
        int[] weaponTypes = {90,48,4}, counts = {3,2,8};
        for (int i = 0; i < 3; i++)
        {
            var row = loadout.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("weaponType").intValue = weaponTypes[i];
            row.FindPropertyRelative("count").intValue = counts[i];
        }
        Save(data);
        BuildPreview(bounds);
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = Values(bounds.size), center = Values(bounds.center), bottom, top, radius}, Formatting.Indented));
        return "Saved Bulwark: 8500 hull, 2500 shields, 13 medium weapons plus shield/engine/hangar; own fitted shield, wreck and placement preview.";
    }

    static void BuildPreview(Bounds bounds)
    {
        Copy("Assets/Prefabs/Ui/Reinforcement/ProvidenceReinforcementView.prefab", PREVIEW);
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "BulwarkReinforcementView";
            foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            var hull = Components<MeshRenderer>(visual).Where(r => r.enabled && IsHull(r)).ToArray();
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach (var renderer in Components<MeshRenderer>(visual))
            {
                renderer.enabled = hull.Contains(renderer);
                if (renderer.enabled) renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
            }
            var spawn = Component(root, "UnitSpawnView");
            Assign(spawn, "meshRenderers", hull);
            var config = new SerializedObject(spawn); config.FindProperty("hologramMaterial").objectReferenceValue = hologram; Save(config);
            var collider = Components<BoxCollider>(root).Single(c => c.gameObject == root);
            collider.center = bounds.center; collider.size = bounds.size; collider.isTrigger = true;
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void FitAnchor(HardPoint point, Transform root, Vector3[] hull)
    {
        var anchor = (Transform)new SerializedObject(point).FindProperty("destroyedVfxAnchor").objectReferenceValue;
        var target = root.InverseTransformPoint(point.transform.position);
        anchor.position = root.TransformPoint(hull.OrderBy(p => (p - target).sqrMagnitude).First());
        anchor.localScale = Vector3.one;
    }
    static void Reset(Transform transform) { transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity; transform.localScale = Vector3.one; }
    static Vector3[] HullPoints(GameObject root) => Components<MeshRenderer>(root).Where(r => r.enabled && IsHull(r)).SelectMany(r => Components<MeshFilter>(r.gameObject).Single().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
    static bool IsHull(MeshRenderer renderer) => renderer.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit");
    static Bounds BoundsOf(Vector3[] points) { var bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point); return bounds; }
    static float[] Values(Vector3 value) => new[] {value.x, value.y, value.z};
    static IEnumerable<Transform> Transforms(Transform root) { yield return root; foreach (Transform child in root) foreach (var descendant in Transforms(child)) yield return descendant; }
    static IEnumerable<T> Components<T>(GameObject root) where T : UnityEngine.Object
    {
        foreach (var transform in Transforms(root.transform))
        {
            var components = new SerializedObject(transform.gameObject).FindProperty("m_Component");
            for (int i = 0; i < components.arraySize; i++)
                if (components.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue is T component) yield return component;
        }
    }
    static MonoBehaviour Component(GameObject root, string name) => Components<MonoBehaviour>(root).Single(m => m.GetType().Name == name);
    static SerializedObject Config(GameObject root, string name) => new SerializedObject(Component(root, name));
    static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Copy(string source, string target) { if (!File.Exists(target) && !AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException(target); }
    static void Assign(MonoBehaviour component, string field, UnityEngine.Object[] objects)
    {
        var config = new SerializedObject(component); var array = config.FindProperty(field); array.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        Save(config);
    }
    static void Set(SerializedObject data, string name, float value) => data.FindProperty("<" + name + ">k__BackingField").floatValue = value;
    static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
}
