using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildGolanPlatform
{
    private const string VISUAL = "Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatform.prefab";
    private const string VIEW = "Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatformView.prefab";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/AotrGolanIIIDefensePlatformReinforcementView.prefab";
    private const string DATA = "Assets/Settings/Data/Models/DefendPlatform/AotrGolanIIIDefensePlatformData.asset";
    private const string MODELS = "Assets/Art/Models/SpaceStations/AotrGolanIII/";

    public static string Main()
    {
        var audit = JObject.Parse(File.ReadAllText("Temp/AotrGolanIIIImport/SourceAudit.json"));
        if (!File.Exists(DATA)) AssetDatabase.CopyAsset("Assets/Settings/Data/Models/DefendPlatform/DefendPlatformData.asset", DATA);
        var data = Load(DATA);
        data.targetObject.name = "AotrGolanIIIDefensePlatformData";
        var component = data.FindProperty("<ComponentData>k__BackingField");
        component.FindPropertyRelative("<Hull>k__BackingField").floatValue = 10000;
        component.FindPropertyRelative("<Shields>k__BackingField").floatValue = 12000;
        component.FindPropertyRelative("<WeaponRange>k__BackingField").floatValue = 500;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue = null;
        Save(data);
        if (!File.Exists(VIEW)) AssetDatabase.CopyAsset("Assets/Prefabs/Models/DefendStation/DefendPlatformView.prefab",VIEW);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            root.name = "AotrGolanIIIDefensePlatformView";
            root.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
            root.transform.localScale = Vector3.one;
            foreach (var point in root.GetComponentsInChildren<HardPoint>(true)) UnityEngine.Object.DestroyImmediate(point.gameObject);
            var shield = root.GetComponentsInChildren<Shield>(true).Single();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.transform != shield.transform).ToArray())
                UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            var model = visual.transform.GetChild(0);
            var weapons = new List<WeaponHardPoint>();
            var healthPoints = new List<HardPoint>();
            var poses = new JArray();
            foreach (var hp in audit["hardpoints"])
            {
                string name = (string)hp["name"];
                WeaponType type;
                bool generator = name.StartsWith("HP_Golan3_Shield_");
                if (name.StartsWith("HP_Golan3_HTL_Turret_")) type = WeaponType.HeavyLongRangeDualTurbolaser;
                else if (name.StartsWith("HP_Golan3_Torpedo_")) type = WeaponType.HeavyProtonTorpedo;
                else if (name.StartsWith("HP_Golan3_Missile_")) type = WeaponType.HeavyAssaultMissile;
                else if (name.StartsWith("HP_Golan3_HLC_")) type = WeaponType.Laser;
                else if (generator) type = default;
                else continue;
                string anchorName = (string)(hp["Fire_Bone_A"] ?? hp["Attachment_Bone"]);
                Vector3 position;
                if (hp["artModel"] != null)
                {
                    var art = visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == name + "_Art");
                    var raw = AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + (string)hp["artModel"] + ".fbx");
                    var muzzle = raw.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(anchorName,StringComparison.OrdinalIgnoreCase));
                    position = art.TransformPoint(muzzle.position);
                }
                else position = model.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(anchorName,StringComparison.OrdinalIgnoreCase)).position;
                var go = new GameObject(name);
                go.transform.SetParent(root.transform,false);
                go.transform.position = position;
                float yaw = Mathf.Atan2(position.x,position.z)*Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0,yaw,0);
                HardPoint point;
                if (generator) point = go.AddComponent<HardPoint>();
                else
                {
                    var weapon = go.AddComponent<WeaponHardPoint>();
                    weapon.SetWeaponType(type);
                    var serialized = new SerializedObject(weapon);
                    float cone = (float)hp["Fire_Cone_Width"];
                    serialized.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = -cone/2;
                    serialized.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = cone/2;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    weapons.Add(weapon);
                    point = weapon;
                }
                var pointData = new SerializedObject(point);
                pointData.FindProperty("<Id>k__BackingField").intValue = healthPoints.Count;
                pointData.FindProperty("<HardPointType>k__BackingField").intValue = generator ? (int)HardPointType.ShieldGenerator : (int)HardPointType.Weapon;
                pointData.ApplyModifiedPropertiesWithoutUndo();
                healthPoints.Add(point);
                poses.Add(new JObject { ["name"]=name,["anchor"]=anchorName,["weapon"]=generator ? "ShieldGenerator" : type.ToString(),["position"]=new JArray(position.x,position.y,position.z),["yaw"]=yaw });
            }
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            var hull = renderers.Where(r => r.sharedMaterials.Any(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            var bounds = new Bounds(renderers[0].bounds.center,renderers[0].bounds.size);
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var collider = root.GetComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size;
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var serialized = new SerializedObject(behaviour);
                switch (behaviour.GetType().Name)
                {
                    case "DefendPlatform": Assign(serialized.FindProperty("explosionHullRenderers"),hull); break;
                    case "HealthComponent": Assign(serialized.FindProperty("<ShipUnits>k__BackingField"),healthPoints.ToArray()); serialized.FindProperty("ionFieldBounds").boundsValue = new Bounds(collider.center,collider.size); break;
                    case "WeaponComponent": Assign(serialized.FindProperty("hardPoints"),weapons.ToArray()); break;
                    case "FogVisibilityComponent": Assign(serialized.FindProperty("renderers"),renderers.Append(shield.GetComponent<MeshRenderer>()).ToArray()); Assign(serialized.FindProperty("hardPoints"),healthPoints.ToArray()); break;
                    case "TeamColorView": Assign(serialized.FindProperty("meshRenderers"),renderers.Append(shield.GetComponent<MeshRenderer>()).ToArray()); break;
                    case "SelectionComponent":
                        var image = (UnityEngine.UI.Image)serialized.FindProperty("selectedImage").objectReferenceValue;
                        image.rectTransform.sizeDelta = new Vector2(bounds.size.x,bounds.size.z)*1.15f; break;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EmpireAtWar.Editor.ShieldHullBaker.Bake(root.transform,shield);
            PrefabUtility.SaveAsPrefabAsset(root,VIEW);
            File.WriteAllText("Temp/AotrGolanIIIImport/GameplayMounts.json",poses.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        if (!File.Exists(PREVIEW)) AssetDatabase.CopyAsset("Assets/Prefabs/Ui/Reinforcement/DefendPlatformReinforcementView.prefab",PREVIEW);
        root = PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name = "AotrGolanIIIDefensePlatformReinforcementView";
            root.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
            root.transform.localScale = Vector3.one;
            foreach (var child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(m => hologram).ToArray();
            var spawn = root.GetComponents<MonoBehaviour>().Single(m => m.GetType().Name == "UnitSpawnView");
            var serialized = new SerializedObject(spawn);
            Assign(serialized.FindProperty("meshRenderers"),renderers);
            serialized.FindProperty("hologramMaterial").objectReferenceValue = hologram;
            serialized.FindProperty("height").floatValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW).GetComponent<BoxCollider>();
            root.GetComponent<BoxCollider>().center = view.center;
            root.GetComponent<BoxCollider>().size = view.size;
            PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return "Saved 10,000 hull / 12,000 shield platform with 36 weapons, 3 targetable shield generators, fitted collider/shield, renderer bindings and own placement preview.";
    }
    private static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
    private static void Assign(SerializedProperty property,UnityEngine.Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i=0;i<objects.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }
}
