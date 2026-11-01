using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class BuildGladiatorShip
{
    const string TASK = "Temp/GladiatorImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Empire/Models/Gladiator.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/GladiatorShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Empire/GladiatorShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Empire/GladiatorWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/GladiatorReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Gladiator construction requires Edit Mode.");
        Directory.CreateDirectory(TASK);
        Copy("Assets/Prefabs/Models/Ships/Empire/View/ImperialVenatorShipView.prefab", VIEW);
        Copy("Assets/Settings/Data/Ship/Empire/ImperialVenatorShipData.asset", DATA);
        Copy("Assets/Settings/Data/Models/Wrecks/Empire/ImperialVenatorWreckData.asset", WRECK_DATA);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom, top, radius;
        string[] sockets = {"LC_01", "LC_02", "TL_01", "TL_02", "ML_01", "ML_02", "DTL_02", "DTL_05", "Shield_Gen", "Hangar"};
        int[] types = {5, 5, 5, 5, 13, 13, 14, 14};
        float[] widths = {175, 175, 175, 175, 160, 160, 360, 360};
        try
        {
            root.name = "GladiatorShipView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            var body = (Transform)Config(root, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root, "HealthComponent");
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            var weaponComponent = Component(root, "WeaponComponent");
            var donorPoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
            var weapons = donorPoints.OfType<WeaponHardPoint>().ToList();
            while (weapons.Count < types.Length)
                weapons.Add(UnityEngine.Object.Instantiate(weapons[0], root.transform));
            var hardpoints = weapons.Take(types.Length).Cast<HardPoint>().Concat(new[] {
                donorPoints.Single(p => (int)p.HardPointType == 2),
                donorPoints.Single(p => (int)p.HardPointType == 4)}).ToArray();
            for (int i = 0; i < hardpoints.Length; i++)
            {
                var config = new SerializedObject(hardpoints[i]);
                config.FindProperty("<Id>k__BackingField").intValue = i;
                config.FindProperty("healthOverride").floatValue = 0;
                config.FindProperty("healthWeight").floatValue = 1;
                hardpoints[i].name = sockets[i];
                if (i < types.Length)
                {
                    config.FindProperty("<WeaponType>k__BackingField").intValue = types[i];
                    config.FindProperty("<HardPointType>k__BackingField").intValue = types[i] == 14 ? 8 : 0;
                    config.FindProperty("mainBattery").boolValue = false;
                }
                config.ApplyModifiedPropertiesWithoutUndo();
            }
            // Keep the donor's explicit component/VFX dependencies; replace only its geometry.
            foreach (var point in hardpoints) point.transform.SetParent(root.transform, true);
            foreach (var point in donorPoints.Except(hardpoints)) UnityEngine.Object.DestroyImmediate(point.gameObject);
            var hangarComponent = Component(root, "HangarComponent");
            var hangarConfig = new SerializedObject(hangarComponent);
            hangarComponent.transform.SetParent(root.transform, true);
            var launch = (Transform)hangarConfig.FindProperty("launchPoint").objectReferenceValue;
            launch.SetParent(root.transform, true);
            foreach (var component in new MonoBehaviour[] {shield, weaponComponent, Component(root, "AudioShipComponent")})
                component.transform.SetParent(root.transform, true);
            foreach (var child in body.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            var bones = visual.GetComponentsInChildren<Transform>(true);
            // Weapons fire at source muzzle midpoints; system targets use their source attachment bones.
            for (int i = 0; i < hardpoints.Length; i++)
            {
                var point = hardpoints[i];
                point.transform.SetParent(body, false);
                point.transform.localScale = Vector3.one;
                point.transform.localRotation = Quaternion.identity;
                point.transform.position = i < 4 ? (bones.Single(t => t.name == sockets[i] + "_FP_01").position + bones.Single(t => t.name == sockets[i] + "_FP_02").position) * .5f : bones.Single(t => t.name == sockets[i]).position;
                if (i < types.Length)
                {
                    var direction = i < 4 ? body.InverseTransformVector(point.transform.position - bones.Single(t => t.name == sockets[i]).position) : Vector3.forward;
                    float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    var config = new SerializedObject(point);
                    config.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = yaw - widths[i] / 2;
                    config.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = yaw + widths[i] / 2;
                    config.ApplyModifiedPropertiesWithoutUndo();
                }
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
            var banked = new[] {-15f, 0f, 15f}.SelectMany(angle => points.Select(p => Quaternion.Euler(0, 0, angle) * p)).ToArray();
            bottom = banked.Min(p => p.y); top = banked.Max(p => p.y);
            radius = Mathf.Ceil(new Vector2(bounds.extents.x, bounds.extents.z).magnitude) + 2;
            launch.SetParent(body, false);
            launch.localPosition = new Vector3(0, bottom - 8, bounds.max.z + 8);
            hangarConfig.Update();
            hangarConfig.FindProperty("launchPoint").objectReferenceValue = launch;
            hangarConfig.FindProperty("hangarHardPoint").objectReferenceValue = hardpoints[types.Length + 1];
            hangarConfig.FindProperty("isDestroyable").boolValue = true;
            hangarConfig.ApplyModifiedPropertiesWithoutUndo();
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
        var wreck = Load(WRECK_DATA); wreck.targetObject.name = "GladiatorWreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView)); Save(wreck);
        var data = Load(DATA); data.targetObject.name = "GladiatorShipData";
        Set(data, "Hull", 6000); Set(data, "Shields", 5000);
        Set(data, "Speed", 25); Set(data, "RotationSpeed", 7.5f); Set(data, "TurnAcceleration", 7.5f);
        Set(data, "BodyRotationMaxAngle", 10);
        data.FindProperty("<ShipClass>k__BackingField").intValue = 4;
        data.FindProperty("<ShieldRegenTier>k__BackingField").intValue = 3;
        data.FindProperty("<HeightTier>k__BackingField").intValue = 2;
        Set(data, "Range", 600); Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;
        var abilities = data.FindProperty("abilities");
        abilities.arraySize = 1;
        abilities.GetArrayElementAtIndex(0).intValue = (int)EmpireAtWar.Services.ShipAbilities.ShipAbilityId.BoostShieldPower;
        var bays = data.FindProperty("hangarBays"); bays.arraySize = 6;
        for (int i = 0; i < 6; i++)
        {
            var bay = bays.GetArrayElementAtIndex(i);
            bay.FindPropertyRelative("squadronType").intValue = 200 + i;
            bay.FindPropertyRelative("reserve").intValue = 10;
            bay.FindPropertyRelative("maxActive").intValue = 1;
        }
        Set(data, "HangarInitialDelay", 4); Set(data, "HangarLaunchInterval", 8);
        var loadout = data.FindProperty("weaponLoadout");
        var groups = types.GroupBy(t => t).ToArray(); loadout.arraySize = groups.Length;
        for (int i = 0; i < groups.Length; i++)
        {
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue = groups[i].Key;
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = groups[i].Count();
        }
        Save(data);
        BuildPreview(bounds);
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, bottom, top, radius}, Formatting.Indented));
        return "Gladiator saved: four dual medium turbolasers, two missiles, two point defense lasers, shield generator and hangar; 6000 hull, 5000 shields, capital regeneration, six Empire squadron bays, one active and ten carried per type.";
    }

    static void BuildPreview(Bounds bounds)
    {
        Copy("Assets/Prefabs/Ui/Reinforcement/CorellianCorvetteReinforcementView.prefab", PREVIEW);
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "GladiatorReinforcementView";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
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
