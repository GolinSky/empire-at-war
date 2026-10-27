using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.ViewComponents.Health;

public static class BuildRecalcitrantShip
{
    const string TASK = "Temp/RecalcitrantImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Recalcitrant.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/RecalcitrantShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/RecalcitrantShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Separatist/RecalcitrantWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/RecalcitrantReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Recalcitrant construction requires Edit Mode.");
        Directory.CreateDirectory(TASK);
        Copy("Assets/Prefabs/Models/Ships/Separatist/RecusantShipView.prefab", VIEW);
        Copy("Assets/Settings/Data/Ship/Separatist/RecusantShipData.asset", DATA);
        Copy("Assets/Settings/Data/Models/Wrecks/Separatist/RecusantWreckData.asset", WRECK_DATA);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom, top, radius;
        string[] sockets = Enumerable.Range(1, 8).Select(i => "TL_" + i.ToString("00"))
            .Concat(Enumerable.Range(1, 6).Select(i => "HP_" + i.ToString("00")))
            .Concat(new[] {"IC_01", "HP_E", "HP_S"}).ToArray();
        int[] types = Enumerable.Repeat(5, 8).Concat(Enumerable.Repeat(93, 6)).Concat(new[] {37}).ToArray();
        float[] widths = Enumerable.Repeat(160f, 8).Concat(Enumerable.Repeat(130f, 6)).Concat(new[] {35f}).ToArray();
        var facing = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(TASK + "ArtReport.json"))["weaponFacing"];
        try
        {
            root.name = "RecalcitrantShipView";
            var body = (Transform)Config(root, "ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root, "HealthComponent");
            var shield = (Shield)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            var weaponComponent = Component(root, "WeaponComponent");
            var donorPoints = root.GetComponentsInChildren<HardPoint>(true).OrderBy(p => p.Id).ToArray();
            var weapons = donorPoints.OfType<WeaponHardPoint>().ToList();
            while (weapons.Count < 15)
                weapons.Add(UnityEngine.Object.Instantiate(weapons[0], root.transform));
            var hardpoints = weapons.Cast<HardPoint>().Concat(new[] {
                donorPoints.Single(p => (int)p.HardPointType == 1),
                donorPoints.Single(p => (int)p.HardPointType == 2)}).ToArray();
            for (int i = 0; i < hardpoints.Length; i++)
            {
                var config = new SerializedObject(hardpoints[i]);
                config.FindProperty("<Id>k__BackingField").intValue = i;
                config.FindProperty("healthOverride").floatValue = i < 8 ? 270 : i < 14 ? 300 : i == 14 ? 575 : 350;
                hardpoints[i].name = sockets[i];
                if (i < 15)
                {
                    config.FindProperty("<WeaponType>k__BackingField").intValue = types[i];
                    config.FindProperty("<HardPointType>k__BackingField").intValue = 0;
                    float yaw = (float)facing[sockets[i]]["yaw"];
                    config.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = yaw - widths[i] / 2;
                    config.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = yaw + widths[i] / 2;
                    config.FindProperty("mainBattery").boolValue = false;
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
            var bones = visual.GetComponentsInChildren<Transform>(true);
            // Mount positions follow the source hardpoint attachment bones; muzzle art stays in the visual.
            for (int i = 0; i < hardpoints.Length; i++)
            {
                var point = hardpoints[i];
                point.transform.SetParent(body, false);
                point.transform.localScale = Vector3.one;
                point.transform.localRotation = Quaternion.identity;
                point.transform.position = bones.Single(t => t.name == sockets[i]).position;
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
            var engineAnchor = (Transform)new SerializedObject(hardpoints[15]).FindProperty("destroyedVfxAnchor").objectReferenceValue;
            var engineTarget = root.transform.InverseTransformPoint(hardpoints[15].transform.position);
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
        var wreck = Load(WRECK_DATA); wreck.targetObject.name = "RecalcitrantWreckData";
        wreck.FindProperty("prefab.m_AssetGUID").stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(wreckView)); Save(wreck);
        var data = Load(DATA); data.targetObject.name = "RecalcitrantShipData";
        Set(data, "Hull", 2800); Set(data, "Shields", 2500);
        Set(data, "Speed", 22); Set(data, "RotationSpeed", 70); Set(data, "TurnAcceleration", 70);
        Set(data, "BodyRotationMaxAngle", 10);
        data.FindProperty("<ShipClass>k__BackingField").intValue = 3;
        data.FindProperty("<ShieldRegenTier>k__BackingField").intValue = 2;
        data.FindProperty("<HeightTier>k__BackingField").intValue = 5;
        Set(data, "Range", 550); Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wreck.targetObject;
        var abilities = data.FindProperty("abilities");
        abilities.arraySize = 1;
        abilities.GetArrayElementAtIndex(0).intValue = (int)EmpireAtWar.Services.ShipAbilities.ShipAbilityId.TIEAvengerPowerToWeapons;
        data.FindProperty("hangarBays").arraySize = 0;
        var loadout = data.FindProperty("weaponLoadout");
        var groups = types.GroupBy(t => t).ToArray(); loadout.arraySize = groups.Length;
        for (int i = 0; i < groups.Length; i++)
        {
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue = groups[i].Key;
            loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = groups[i].Count();
        }
        var hp = data.FindProperty("hardPointHealth"); hp.arraySize = 3;
        int[] hpTypes = {0,1,2};
        for (int i = 0; i < hpTypes.Length; i++)
        {
            var row = hp.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("hardPointType").intValue = hpTypes[i];
            row.FindPropertyRelative("health").floatValue = i == 0 ? 270 : 350;
            row.FindPropertyRelative("hullDamageMultiplier").floatValue = 1;
        }
        Save(data);
        BuildPreview(bounds);
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, bottom, top, radius}, Formatting.Indented));
        return "Recalcitrant saved: fifteen medium weapon mounts, engine and shield generator; own shield, wreck and placement preview.";
    }

    static void BuildPreview(Bounds bounds)
    {
        Copy("Assets/Prefabs/Ui/Reinforcement/CorellianCorvetteReinforcementView.prefab", PREVIEW);
        var root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "RecalcitrantReinforcementView";
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
