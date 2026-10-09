using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;

public static class BuildImperialVenatorShip
{
    const string VIEW = "Assets/Prefabs/Models/Ships/ImperialVenatorShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/ImperialVenatorShipData.asset";
    public static string Main()
    {
        Copy("Assets/Prefabs/Models/Ships/VenatorShipView.prefab", VIEW);
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Models/Ships/VenatorShipView.prefab");
        Bounds bounds; float bottom, top;
        var mappings = new List<object>();
        try
        {
            root.name = "ImperialVenatorShipView";
            var body = (Transform)new SerializedObject(Component(root, "ShipMoveComponent")).FindProperty("bodyTransform").objectReferenceValue;
            body.name = "Body";
            var shield = (Shield)Component(root, "Shield");
            var shieldRenderer = (Renderer)new SerializedObject(shield).FindProperty("shieldRenderer").objectReferenceValue;
            var weapons = root.GetComponentsInChildren<WeaponHardPoint>(true);
            var targets = root.GetComponentsInChildren<HardPoint>(true);
            foreach (var hardpoint in targets) hardpoint.transform.SetParent(body, true);
            Component(root, "WeaponComponent").transform.SetParent(body, true);
            shieldRenderer.transform.SetParent(body, true);
            var oldModel = body.Cast<Transform>().Single(t => t.name == "rep_venator_body0_model0");
            UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r => r != shieldRenderer && (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray())
                UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            body.localPosition = Vector3.zero; body.localRotation = Quaternion.identity; body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialVenator.prefab"), body);
            var map = new Dictionary<string, string>
            {
                { "DualProjectileL1", "ImperialVenator_Heavy_TL_08/FP_01" },
                { "DualProjectileL2", "ImperialVenator_Heavy_TL_05/FP_01" },
                { "DualProjectileL3", "ImperialVenator_Heavy_TL_06/FP_01" },
                { "DualProjectileL4", "ImperialVenator_Heavy_TL_07/FP_01" },
                { "DualProjectileR4", "ImperialVenator_Heavy_TL_01/FP_01" },
                { "DualProjectileR1", "ImperialVenator_Heavy_TL_02/FP_01" },
                { "DualProjectileR2", "ImperialVenator_Heavy_TL_03/FP_01" },
                { "DualProjectileR3", "ImperialVenator_Heavy_TL_04/FP_01" },
                { "DualTurboLaserDualProjectileR", "ImperialVenator_Medium_TL_01/FP_01" },
                { "DualTurboLaserDualProjectileL", "ImperialVenator_Medium_TL_02/FP_01" },
                { "IonProjectileR", "Laser_Hardpoint_02_Fire" },
                { "IonProjectileL", "Laser_Hardpoint_01_Fire" },
                { "TurboLaserProjectileR", "Laser_01" },
                { "TurboLaserProjectileL", "Laser_08" },
                { "LaserBeam", "SPHAT" },
                { "ShieldGenerator", "Shield_Generator" },
                { "HangarUnit", "SPAWN_00" }
            };
            for (int i = 0; i < targets.Length; i++)
            {
                var hp = targets[i]; var so = new SerializedObject(hp);
                so.FindProperty("<Id>k__BackingField").intValue = i;
                if (hp is WeaponHardPoint weapon && weapon.WeaponType == EmpireAtWar.Components.AttackComponent.WeaponType.DualHeavyTurboLaserDby827)
                    so.FindProperty("<WeaponType>k__BackingField").intValue = 43;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (map.ContainsKey(hp.name)) hp.transform.position = Bone(map[hp.name]).position;
                else hp.transform.position = visual.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Pe_Venator_")).Select(t => t.position).Aggregate(Vector3.zero, (a, p) => a + p) / 10;
                hp.transform.rotation = root.transform.rotation;
                if (hp.GetComponents<Collider>().Length == 0) hp.gameObject.AddComponent<SphereCollider>().radius = 2.5f;
                mappings.Add(new { hp.name, hp.Id, bone = map.ContainsKey(hp.name) ? map[hp.name] : "mean of ten authored engine emitters", position = new[] { hp.transform.position.x, hp.transform.position.y, hp.transform.position.z } });
            }
            Assign(Component(root, "WeaponComponent"), "hardPoints", weapons);
            Assign(Component(root, "HealthComponent"), "<ShipUnits>k__BackingField", targets);
            var hull = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            var solid = hull.Where(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var points = solid.SelectMany(r => r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v => r.transform.TransformPoint(v))).ToArray();
            bounds = new Bounds(points[0], Vector3.zero); foreach (var p in points) bounds.Encapsulate(p);
            var box = root.GetComponents<BoxCollider>().Single(); box.center = bounds.center; box.size = bounds.size;
            var banked = points.SelectMany(p => new[] { p, Quaternion.Euler(0, 0, 15) * p, Quaternion.Euler(0, 0, -15) * p }).ToArray(); bottom = banked.Min(p => p.y); top = banked.Max(p => p.y);
            var health = new SerializedObject(Component(root, "HealthComponent")); health.FindProperty("ionFieldBounds").boundsValue = bounds; health.ApplyModifiedPropertiesWithoutUndo();
            var hangar = new SerializedObject(Component(root, "HangarComponent"));
            var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue; launch.SetParent(body, false);
            launch.position = new Vector3(Bone("SPAWN_00").position.x, bounds.min.y - 8, Bone("SPAWN_00").position.z); launch.rotation = root.transform.rotation;
            hangar.FindProperty("hangarHardPoint").objectReferenceValue = targets.Single(h => h.name == "HangarUnit"); hangar.ApplyModifiedPropertiesWithoutUndo();
            var emitters = root.GetComponentsInChildren<ParticleSystem>(true).ToArray();
            var engineBones = visual.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Pe_Venator_L")).ToArray();
            for (int i = 0; i < emitters.Length; i++) { emitters[i].transform.SetParent(body, true); emitters[i].transform.position = engineBones[i % engineBones.Length].position; }
            Assign(Component(root, "FogVisibilityComponent"), "hardPoints", targets);
            Assign(Component(root, "FogVisibilityComponent"), "renderers", root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray());
            Assign(Component(root, "TeamColorView"), "meshRenderers", hull.Concat(new[] { shieldRenderer }).ToArray());
            Assign(Component(root, "Ship"), "explosionHullRenderers", solid);
            var fog = new SerializedObject(Component(root, "FogVisibilityComponent")); fog.FindProperty("revealRadius").floatValue = 72; fog.ApplyModifiedPropertiesWithoutUndo();
            var selection = new SerializedObject(Component(root, "SelectionComponent"));
            var selectionCanvas = (Canvas)selection.FindProperty("selectedCanvas").objectReferenceValue;
            selectionCanvas.transform.localScale = Vector3.one;
            selectionCanvas.transform.localPosition = new Vector3(0, bottom - 1, 0);
            ((UnityEngine.UI.Image)selection.FindProperty("selectedImage").objectReferenceValue).rectTransform.sizeDelta = new Vector2(144, 144);
            ShieldHullBaker.Bake(root.transform, shield);
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
            Transform Bone(string path)
            {
                var parts = path.Split('/');
                var parent = parts.Length == 1 ? visual.transform : visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == parts[0]);
                return parent.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(parts.Last(), StringComparison.OrdinalIgnoreCase) && t != parent);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Copy("Assets/Settings/Data/Ship/VenatorShipData.asset", DATA);
        var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA)); data.targetObject.name = "ImperialVenatorShipData";
        Float(data, "Hull", 15000); Float(data, "Shields", 15000); Float(data, "HullBottom", bottom); Float(data, "HullTop", top);
        var abilities = data.FindProperty("abilities"); abilities.arraySize = 1; abilities.GetArrayElementAtIndex(0).intValue = 35;
        var bays = data.FindProperty("hangarBays"); bays.arraySize = 2;
        for (int i = 0; i < 2; i++) { var bay = bays.GetArrayElementAtIndex(i); bay.FindPropertyRelative("squadronType").intValue = 200 + i; bay.FindPropertyRelative("reserve").intValue = i == 0 ? 3 : 6; bay.FindPropertyRelative("maxActive").intValue = i == 0 ? 1 : 2; }
        var loadout = data.FindProperty("weaponLoadout");
        for (int i = 0; i < loadout.arraySize; i++) if (loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue == 9) loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue = 43;
        var wreck = ShipWreckBuilder.Build(VIEW);
        const string WRECK_DATA = "Assets/Settings/Data/Ship/Wreck/ImperialVenatorWreckData.asset";
        Copy("Assets/Settings/Data/Ship/Wreck/VenatorWreckData.asset", WRECK_DATA);
        var wd = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(WRECK_DATA)); wd.targetObject.name = "ImperialVenatorWreckData";
        wd.FindProperty("<Prefab>k__BackingField").objectReferenceValue = wreck; Save(wd);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = wd.targetObject; Save(data);
        File.WriteAllText("Temp/ImperialVenatorImport/GameplayMounts.json", Newtonsoft.Json.JsonConvert.SerializeObject(mappings, Newtonsoft.Json.Formatting.Indented));
        AssetDatabase.SaveAssets();
        return "Imperial Venator: copied 15 weapons/18 targetable hardpoints, ten green heavy dual turbolasers, TIE Fighter/Bomber garrison, own shield and wreck. Bounds " + bounds;
    }
    static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == name);
    static void Copy(string source, string target) { if (!File.Exists(target)) AssetDatabase.CopyAsset(source, target); }
    static void Assign(UnityEngine.Object obj, string field, UnityEngine.Object[] values) { var so = new SerializedObject(obj); var array = so.FindProperty(field); array.arraySize = values.Length; for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Float(SerializedObject so, string field, float value) => so.FindProperty("<" + field + ">k__BackingField").floatValue = value;
    static void Save(SerializedObject so) { so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(so.targetObject); }
}
