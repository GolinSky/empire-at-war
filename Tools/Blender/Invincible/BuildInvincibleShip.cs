using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class BuildInvincibleShip
{
    const string TASK = "Temp/InvincibleImport/";
    const string DONOR_VIEW = "Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab";
    const string DONOR_DATA = "Assets/Settings/Data/Ship/Separatist/ProvidenceShipData.asset";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Invincible.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/InvincibleShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/InvincibleWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/InvincibleReinforcementView.prefab";
    const float SIZE_RATIO = 39f / 14f;

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build Invincible in Edit Mode.");
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
            root.name = "InvincibleShipView";
            foreach (var child in root.transform.Cast<Transform>().ToArray())
                if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                    PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var movement = Config(root, "ShipMoveComponent");
            var oldBody = (Transform)movement.FindProperty("bodyTransform").objectReferenceValue;
            var oldPoints = HullPoints(root);
            var oldBounds = BoundsOf(oldPoints);
            var health = Component(root, "HealthComponent");
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            shield.transform.SetParent(root.transform, true);
            var weapons = Components<WeaponHardPoint>(root).OrderBy(p => p.Id).ToArray();
            var systems = Components<HardPoint>(root).Where(p => !(p is WeaponHardPoint)).OrderBy(p => p.Id).ToArray();
            if (weapons.Length != 14 || systems.Length != 3)
                throw new InvalidOperationException("Invincible requires the unmodified Providence donor with 14 weapons and 3 systems.");
            var donorPositions = weapons.Select(p => root.transform.InverseTransformPoint(p.transform.position)).ToArray();
            var donorSides = weapons.Select((p, i) => p.MinYaw >= 0 ? 1f : p.MaxYaw <= 0 ? -1f : Mathf.Sign(donorPositions[i].x)).ToArray();
            // Providence's two full-arc type4 mounts both sit on the left. Preserve paired port/starboard placement in this new ship.
            foreach (var group in weapons.Select((point, index) => new {point, index})
                .Where(row => row.point.MinYaw < 0 && row.point.MaxYaw > 0).GroupBy(row => row.point.WeaponType))
            {
                var pair = group.OrderBy(row => donorPositions[row.index].x).ThenBy(row => row.index).ToArray();
                for (int i = 0; i < pair.Length; i++) donorSides[pair[i].index] = i < pair.Length / 2 ? -1f : 1f;
            }
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
            if (Mathf.Abs(bounds.size.z / oldBounds.size.z - SIZE_RATIO) > .002f)
                throw new InvalidOperationException("Invincible visual must be 2.785714 times Providence's visible length.");
            var weaponComponent = Component(root, "WeaponComponent");
            var weaponRoot = weaponComponent.transform;
            weaponRoot.SetParent(body, false);
            Reset(weaponRoot);
            var mounts = visualTransforms.Where(t => Regex.IsMatch(t.name, "^HP_(TBL|LC)[0-9]{2}$")).ToList();
            if (mounts.Count != 28) throw new InvalidOperationException("The Invincible model must preserve all 28 authored TBL/LC sockets.");
            var allWeapons = new List<WeaponHardPoint>();
            var mapping = new List<object>();
            // Keep each Providence weapon's profile and firing arc; use distinct authored sockets on the matching side.
            for (int copy = 0; copy < 2; copy++)
            for (int i = 0; i < weapons.Length; i++)
            {
                var point = copy == 0 ? weapons[i] : Components<WeaponHardPoint>(UnityEngine.Object.Instantiate(weapons[i].gameObject, weaponRoot)).Single();
                int id = copy * weapons.Length + i;
                Vector3 target = (donorPositions[i] - oldBounds.center) * SIZE_RATIO;
                target.z += bounds.size.z * (copy == 0 ? -.018f : .018f);
                float side = donorSides[i];
                target.x = Mathf.Abs(target.x) * side;
                var mount = mounts.Where(t => Mathf.Sign(root.transform.InverseTransformPoint(t.position).x) == side)
                    .OrderBy(t => (root.transform.InverseTransformPoint(t.position) - target).sqrMagnitude).First();
                mounts.Remove(mount);
                point.transform.SetParent(weaponRoot, false);
                point.transform.localScale = Vector3.one;
                point.transform.localRotation = Quaternion.identity;
                point.transform.position = mount.position;
                point.name = point.WeaponType + "_" + id.ToString("00");
                var config = new SerializedObject(point);
                config.FindProperty("<Id>k__BackingField").intValue = id;
                Save(config);
                FitAnchor(point, root.transform, points);
                allWeapons.Add(point);
                mapping.Add(new {id, name = point.name, weapon = (int)point.WeaponType, source = mount.name,
                    position = Values(root.transform.InverseTransformPoint(point.transform.position)),
                    yaw = new[] {point.MinYaw, point.MaxYaw}, donorPosition = Values(donorPositions[i]),
                    side, mirroredDonorTarget = Mathf.Sign(donorPositions[i].x) != side});
            }
            string[] systemSockets = {"HP_Shield", "HP_Engine", "Spawn_00"};
            for (int i = 0; i < systems.Length; i++)
            {
                var point = systems[i];
                point.transform.SetParent(body, false);
                point.transform.localScale = Vector3.one;
                point.transform.localRotation = Quaternion.identity;
                var mount = visualTransforms.Single(t => t.name == systemSockets[i]);
                point.transform.position = mount.position;
                var config = new SerializedObject(point);
                config.FindProperty("<Id>k__BackingField").intValue = 28 + i;
                Save(config);
                FitAnchor(point, root.transform, points);
                mapping.Add(new {id = 28 + i, name = point.name, source = mount.name,
                    position = Values(root.transform.InverseTransformPoint(point.transform.position))});
            }
            var hangar = Config(root, "HangarComponent");
            var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
            var hangarRoot = ((MonoBehaviour)hangar.targetObject).transform;
            hangarRoot.SetParent(body, false);
            Reset(hangarRoot);
            launch.localPosition = new Vector3(0, bounds.min.y - 4, bounds.max.z + 10);
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
        wreck.targetObject.name = "InvincibleWreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView));
        Save(wreck);
        var data = Load(DATA); data.targetObject.name = "InvincibleShipData";
        var donor = Load(DONOR_DATA);
        Set(data, "Hull", donor.FindProperty("<Hull>k__BackingField").floatValue * 2);
        Set(data, "Shields", donor.FindProperty("<Shields>k__BackingField").floatValue * 2);
        Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;
        var loadout = data.FindProperty("weaponLoadout"); var donorLoadout = donor.FindProperty("weaponLoadout");
        for (int i = 0; i < loadout.arraySize; i++)
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = donorLoadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue * 2;
        Save(data);
        BuildPreview(bounds);
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = Values(bounds.size), center = Values(bounds.center), bottom, top, radius, ratio = SIZE_RATIO}, Formatting.Indented));
        return "Saved Invincible: 10000 hull, 6000 shields, 28 weapons plus shield/engine/hangar; own fitted shield, wreck and placement preview.";
    }

    static void BuildPreview(Bounds bounds)
    {
        Copy("Assets/Prefabs/Ui/Reinforcement/ProvidenceReinforcementView.prefab", PREVIEW);
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "InvincibleReinforcementView";
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
