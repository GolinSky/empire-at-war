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

public static class BuildTartanShip
{
    const string TASK = "Temp/TartanImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Empire/Tartan.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/Empire/TartanShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Empire/TartanShipData.asset";
    const string WRECK = "Assets/Settings/Data/Models/Wrecks/Empire/TartanWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/TartanReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Tartan prefab construction requires Edit Mode.");
        Copy("Assets/Prefabs/Models/Ships/Separatist/C9979ShipView.prefab", VIEW);
        Copy("Assets/Settings/Data/Ship/Rebellion/CorellianCorvetteShipData.asset", DATA);
        Copy("Assets/Settings/Data/Models/Wrecks/Rebellion/CorellianCorvetteWreckData.asset", WRECK);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom = float.MaxValue, top = float.MinValue, radius;
        try
        {
            root.name = "TartanShipView";
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
            var sourceMounts = ((JArray)audit["hardpoints"]).Select(h => (JObject)h).ToList();
            foreach (var row in new[] { new {bone="HP_01", yaw=0f}, new {bone="HP_02", yaw=-90f}, new {bone="HP_05", yaw=90f}, new {bone="HP_06", yaw=180f} })
                sourceMounts.Add(new JObject {["name"]="Tartan_PD_" + row.bone, ["Fire_Bone_A"]=row.bone, ["weaponType"]=14, ["yaw"]=row.yaw, ["Fire_Cone_Width"]="180"});
            foreach (var hp in sourceMounts)
            {
                string attachment = (string)hp["Fire_Bone_A"];
                var muzzle = visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == attachment && t.GetComponents<MeshFilter>().Length == 0);
                var position = muzzle.position;
                bool heavy = ((string)hp["name"]) == "HP_Tartan_HL";
                bool pointDefense = hp["weaponType"] != null;
                int type = pointDefense ? 14 : heavy ? 28 : 90;
                var forward = heavy ? Vector3.forward : Quaternion.Euler(0, pointDefense ? (float)hp["yaw"] : 0, 0) * Vector3.forward;
                if (!heavy && !pointDefense)
                {
                    var matrix = audit["models"]["Tartan"]["bones"].Single(b => (string)b["name"] == attachment)["matrix"];
                    forward = new Vector3(-(float)matrix[0], 0, -(float)matrix[4]);
                }
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                var weapon = new GameObject((string)hp["name"]).AddComponent<WeaponHardPoint>();
                weapon.gameObject.layer = weaponLayer;
                weapon.transform.SetParent(body, false);
                weapon.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
                var config = new SerializedObject(weapon);
                config.FindProperty("<HardPointType>k__BackingField").intValue = 0;
                config.FindProperty("<Id>k__BackingField").intValue = weapons.Count;
                config.FindProperty("<WeaponType>k__BackingField").intValue = type;
                float halfWidth = float.Parse((string)hp["Fire_Cone_Width"], System.Globalization.CultureInfo.InvariantCulture) * .5f;
                config.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = yaw - halfWidth;
                config.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = yaw + halfWidth;
                config.ApplyModifiedPropertiesWithoutUndo();
                weapons.Add(weapon);
                mappings.Add(new {attachment, id = weapon.Id, weaponType = (int)weapon.WeaponType,
                    position = new[] {position.x, position.y, position.z}, yaw = weapon.transform.eulerAngles.y,
                    muzzle = muzzle.name, targetable = true});
            }
            Assign(health, "<ShipUnits>k__BackingField", weapons.ToArray());
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
            Assign(Component(root, "TeamColorView"), "meshRenderers", visual.GetComponentsInChildren<MeshRenderer>(true).Concat(new[] {shieldRenderer}).ToArray());
            Assign(Component(root, "EntityBindings"), "<Injectables>k__BackingField", weapons.Cast<UnityEngine.Object>().Concat(new[] {Component(root, "TeamColorView")}).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            File.WriteAllText(TASK + "HardpointMapping.json", JsonConvert.SerializeObject(mappings, Formatting.Indented));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<ShipData>(DATA));
        data.targetObject.name = "TartanShipData";
        Set(data, "Hull", 650); Set(data, "Shields", 850); Set(data, "Speed", 70);
        Set(data, "ShieldRegenerateValue", 8); Set(data, "ShieldRegenerateDelay", 1);
        Set(data, "HullBottom", bottom); Set(data, "HullTop", top); Set(data, "NavigationRadius", radius);
        Set(data, "Range", 300); Set(data, "BodyRotationMaxAngle", 15);
        var hardpointHealth = data.FindProperty("hardPointHealth"); hardpointHealth.arraySize = 1;
        var weaponHealth = hardpointHealth.GetArrayElementAtIndex(0);
        weaponHealth.FindPropertyRelative("hardPointType").intValue = 0;
        weaponHealth.FindPropertyRelative("health").floatValue = 70;
        weaponHealth.FindPropertyRelative("hullDamageMultiplier").floatValue = 1;
        var loadout = data.FindProperty("weaponLoadout"); loadout.arraySize = 3;
        int[] types = {90, 28, 14}, counts = {4, 1, 4};
        for (int i = 0; i < 3; i++) { loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue = types[i]; loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = counts[i]; }
        data.FindProperty("hangarBays").arraySize = 0;
        var abilities = data.FindProperty("abilities"); abilities.arraySize = 1; abilities.GetArrayElementAtIndex(0).intValue = 20;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK);
        Save(data);
        var wreckView = ShipWreckBuilder.Build(VIEW);
        var wreck = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK));
        wreck.targetObject.name = "TartanWreckData";
        wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue = wreckView;
        Save(wreck);
        Copy("Assets/Prefabs/Ui/Reinforcement/C9979ReinforcementView.prefab", PREVIEW);
        root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "TartanReinforcementView";
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
        return "Saved nine targetable weapons, shield/fog/team/collision bindings, ship data, wreck and placement preview.";
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
