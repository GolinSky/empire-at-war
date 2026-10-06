using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.Editor;

public static class BuildArquitensImperialCruiserShip
{
    const string TASK = "Temp/ArquitensImperialCruiserImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/ArquitensImperialCruiser.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/ArquitensImperialCruiserShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/ArquitensImperialCruiserShipData.asset";
    const string WRECK = "Assets/Settings/Data/Ship/Wreck/ArquitensImperialCruiserWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/ArquitensImperialCruiserReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Arquitens prefab construction requires Edit Mode.");
        Copy("Assets/Prefabs/Models/Ships/C9979ShipView.prefab", VIEW);
        Copy("Assets/Settings/Data/Ship/ArquitensShipData.asset", DATA);
        Copy("Assets/Settings/Data/Ship/Wreck/CorellianCorvetteWreckData.asset", WRECK);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom = float.MaxValue, top = float.MinValue, radius;
        try
        {
            root.name = "ArquitensImperialCruiserShipView";
            var body = (Transform)new SerializedObject(Component(root, "ShipMoveComponent")).FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root, "HealthComponent");
            var healthConfig = new SerializedObject(health);
            var shield = (MonoBehaviour)healthConfig.FindProperty("shieldView").objectReferenceValue;
            var weaponsComponent = Component(root, "WeaponComponent");
            int weaponLayer = root.GetComponentsInChildren<WeaponHardPoint>(true).Select(p => p.gameObject.layer).Distinct().Single();
            shield.transform.SetParent(root.transform, true);
            weaponsComponent.transform.SetParent(root.transform, true);
            foreach (var child in body.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), body);
            bounds = BoundsOf(visual);
            var points = visual.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.GetComponents<MeshRenderer>().Single().enabled)
                .SelectMany(f => f.sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
            foreach (float bank in new[] {-15f, 0f, 15f})
                foreach (var point in points)
                {
                    float y = (Quaternion.Euler(0, 0, bank) * point).y;
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            radius = Mathf.Ceil(new Vector2(bounds.extents.x, bounds.extents.z).magnitude) + 2;
            var weapons = new List<WeaponHardPoint>();
            var mappings = new List<object>();
            var audit = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
            foreach (var hp in audit["hardpoints"].Where(h => h["Model_To_Attach"] != null))
            {
                string attachment = (string)hp["Attachment_Bone"];
                var turret = visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == attachment + "_Turret");
                var transforms = turret.GetComponentsInChildren<Transform>(true);
                var muzzleA = transforms.Single(t => t.name == (string)hp["Fire_Bone_A"]);
                var muzzleB = transforms.Single(t => t.name == (string)hp["Fire_Bone_B"]);
                var barrel = transforms.Single(t => t.childCount > 0 && string.Equals(t.name, (string)hp["Barrel_Bone_Name"], StringComparison.OrdinalIgnoreCase));
                var position = (muzzleA.position + muzzleB.position) * .5f;
                var forward = position - barrel.position;
                forward.y = 0;
                if (forward.sqrMagnitude < .00001f) throw new InvalidOperationException("Cannot derive source yaw: " + attachment);
                var weapon = new GameObject((string)hp["name"]).AddComponent<WeaponHardPoint>();
                weapon.gameObject.layer = weaponLayer;
                weapon.transform.SetParent(body, false);
                weapon.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
                var config = new SerializedObject(weapon);
                config.FindProperty("<HardPointType>k__BackingField").intValue = 0;
                config.FindProperty("<Id>k__BackingField").intValue = weapons.Count;
                config.FindProperty("<WeaponType>k__BackingField").intValue = ((string)hp["name"]).Contains("Quad") ? 39 : 40;
                float halfWidth = float.Parse((string)hp["Fire_Cone_Width"], System.Globalization.CultureInfo.InvariantCulture) * .5f;
                config.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = -halfWidth;
                config.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = halfWidth;
                config.ApplyModifiedPropertiesWithoutUndo();
                weapons.Add(weapon);
                mappings.Add(new {attachment, id = weapon.Id, weaponType = (int)weapon.WeaponType,
                    position = new[] {position.x, position.y, position.z}, yaw = weapon.transform.eulerAngles.y,
                    muzzleA = muzzleA.name, muzzleB = muzzleB.name, targetable = false});
            }
            Assign(health, "<ShipUnits>k__BackingField", new UnityEngine.Object[0]);
            weaponsComponent.transform.SetParent(body, false);
            Assign(weaponsComponent, "hardPoints", weapons.ToArray());
            shield.transform.SetParent(body, true);
            ShieldHullBaker.Bake(root.transform, (Shield)shield);
            healthConfig.Update();
            healthConfig.FindProperty("ionFieldBounds").boundsValue = new Bounds(new Vector3(0, (bottom + top) * .5f, 0), new Vector3(bounds.size.x, top - bottom, bounds.size.z));
            healthConfig.ApplyModifiedPropertiesWithoutUndo();
            foreach (var collider in root.GetComponents<BoxCollider>()) { collider.center = bounds.center; collider.size = bounds.size; }
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t => t.name == "SelectedImage")) rect.sizeDelta = Vector2.one * radius * 2;
            var visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            Assign(Component(root, "Ship"), "explosionHullRenderers", visible.Where(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray());
            var shieldRenderer = shield.GetComponents<MeshRenderer>().Single();
            Assign(Component(root, "FogVisibilityComponent"), "renderers", visible.Cast<Renderer>().Concat(new Renderer[] {shieldRenderer, root.GetComponents<LineRenderer>().Single()}).ToArray());
            Assign(Component(root, "FogVisibilityComponent"), "hardPoints", weapons.ToArray());
            Assign(Component(root, "TeamColorView"), "meshRenderers", visual.GetComponentsInChildren<MeshRenderer>(true).Concat(new[] {shieldRenderer}).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            File.WriteAllText(TASK + "HardpointMapping.json", JsonConvert.SerializeObject(mappings, Formatting.Indented));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<ShipData>(DATA));
        data.targetObject.name = "ArquitensImperialCruiserShipData";
        Set(data, "Hull", 1300); Set(data, "Shields", 1200); Set(data, "Speed", 30);
        Set(data, "ShieldRegenerateValue", 4); Set(data, "ShieldRegenerateDelay", 1);
        Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        Set(data, "Range", 562.5f); Set(data, "BodyRotationMaxAngle", 15);
        data.FindProperty("hardPointHealth").arraySize = 0;
        data.FindProperty("hangarBays").arraySize = 0;
        var abilities = data.FindProperty("abilities"); abilities.arraySize = 1; abilities.GetArrayElementAtIndex(0).intValue = 23;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK);
        Save(data);
        var wreckView = ShipWreckBuilder.Build(VIEW);
        var wreck = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK));
        wreck.targetObject.name = "ArquitensImperialCruiserWreckData";
        wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue = wreckView;
        Save(wreck);
        Copy("Assets/Prefabs/Ui/Reinforcement/C9979ReinforcementView.prefab", PREVIEW);
        root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "ArquitensImperialCruiserReinforcementView";
            foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL), root.transform);
            foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                renderer.enabled = renderer.enabled && renderer.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit");
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(m => hologram).ToArray();
            Assign(Component(root, "UnitSpawnView"), "meshRenderers", renderers);
            foreach (var collider in root.GetComponents<BoxCollider>()) { collider.center = bounds.center; collider.size = bounds.size; }
            PrefabUtility.SaveAsPrefabAsset(root, PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "GameplayBounds.json", JsonConvert.SerializeObject(new {size = new[] {bounds.size.x, bounds.size.y, bounds.size.z}, bottom, top, radius}, Formatting.Indented));
        return "Saved eight non-targetable weapons, hull-only health, shield/fog/team/collision bindings, ship data, wreck and placement preview.";
    }
    static void Copy(string source, string target) { if (!File.Exists(target) && !AssetDatabase.CopyAsset(source, target)) throw new InvalidOperationException(target); }
    static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(m => m.GetType().Name == name);
    static void Assign(MonoBehaviour component, string field, UnityEngine.Object[] objects)
    {
        var config = new SerializedObject(component); var array = config.FindProperty(field); array.arraySize = objects.Length;
        for (int i = 0; i < objects.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        config.ApplyModifiedPropertiesWithoutUndo();
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray(); var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds); return bounds;
    }
    static void Set(SerializedObject data, string name, float value) => data.FindProperty("<" + name + ">k__BackingField").floatValue = value;
    static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
}
