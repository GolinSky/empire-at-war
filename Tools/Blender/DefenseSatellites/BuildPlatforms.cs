using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;

public static class BuildSatellitePlatforms
{
    private const string PREFABS = "Assets/Prefabs/Models/DefendStation/";
    private const string DATA = "Assets/Settings/Data/Models/DefendPlatform/";
    public static string Main()
    {
        string[] names={"QuadLaserSatellite","TurbolaserSatellite","IonSatellite","MassDriverSatellite","MissileSatellite"};
        WeaponType[] types={WeaponType.DualRepeatingPointDefense,WeaponType.DualMediumTurboLaser,WeaponType.MediumIonCannon,WeaponType.MassDriver,WeaponType.HeavyAssaultMissile};
        for(int n=0;n<names.Length;n++) Build(names[n],types[n]);
        AssetDatabase.SaveAssets();
        return "Saved five satellite prefabs and data with six targetable weapon mounts each.";
    }
    private static void Build(string name,WeaponType type)
    {
        string dataPath=DATA+name+"Data.asset", viewPath=PREFABS+name+"View.prefab";
        if(!File.Exists(dataPath)) AssetDatabase.CopyAsset(DATA+"DefendPlatformData.asset",dataPath);
        var data=Load(dataPath);
        data.targetObject.name=name+"Data";
        var component=data.FindProperty("<ComponentData>k__BackingField");
        component.FindPropertyRelative("<Hull>k__BackingField").floatValue=1000;
        component.FindPropertyRelative("<Shields>k__BackingField").floatValue=1000;
        component.FindPropertyRelative("<Range>k__BackingField").floatValue=600;
        component.FindPropertyRelative("<VisionRange>k__BackingField").floatValue=600;
        component.FindPropertyRelative("<SpawnBlockRadius>k__BackingField").floatValue=30;
        var hp=component.FindPropertyRelative("hardPointHealth");hp.arraySize=2;
        for(int i=0;i<2;i++)
        {
            var row=hp.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("hardPointType").intValue=i==0?(int)HardPointType.Weapon:(int)HardPointType.PointDefense;
            row.FindPropertyRelative("health").floatValue=300;
            row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;
        }
        data.FindProperty("<Income>k__BackingField").floatValue=0;
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=null;
        var loadout=data.FindProperty("weaponLoadout");loadout.arraySize=1;
        loadout.GetArrayElementAtIndex(0).FindPropertyRelative("weaponType").intValue=(int)type;
        loadout.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue=6;
        Save(data);
        if(!File.Exists(viewPath)) AssetDatabase.CopyAsset(PREFABS+"DefendPlatformView.prefab",viewPath);
        var root=PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            root.name=name+"View";
            root.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
            root.transform.localScale=Vector3.one;
            foreach(var point in root.GetComponentsInChildren<HardPoint>(true)) UnityEngine.Object.DestroyImmediate(point.gameObject);
            var shield = root.GetComponentsInChildren<Shield>(true).Single();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.transform != shield.transform).ToArray())
                UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS+name+".prefab"),root.transform);

            var weapons=new List<WeaponHardPoint>();
            var healthPoints=new List<HardPoint>();
            for(int i=0;i<6;i++)
            {
                var mount=visual.transform.Find("Turret_"+i.ToString("D2"));
                var go=new GameObject("Weapon_"+i.ToString("D2"));go.transform.SetParent(root.transform,false);
                var mountRenderers=mount.GetComponentsInChildren<MeshRenderer>(true);
                var mountBounds=mountRenderers[0].bounds;
                foreach(var renderer in mountRenderers.Skip(1)) mountBounds.Encapsulate(renderer.bounds);
                go.transform.position=mount.Find("Muzzle").position;
                go.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(mountBounds.center.x,mountBounds.center.z)*Mathf.Rad2Deg,0);
                var weapon=go.AddComponent<WeaponHardPoint>();weapon.SetWeaponType(type);
                var point=new SerializedObject(weapon);
                point.FindProperty("<Id>k__BackingField").intValue=i;
                point.FindProperty("destroyedVfxAnchor").objectReferenceValue=go.transform;
                point.FindProperty("<HardPointType>k__BackingField").intValue=type==WeaponType.DualRepeatingPointDefense?(int)HardPointType.PointDefense:(int)HardPointType.Weapon;
                point.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=-180;
                point.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=180;
                point.ApplyModifiedPropertiesWithoutUndo();
                weapons.Add(weapon);healthPoints.Add(weapon);
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
                    case "EntityBindings":
                        Assign(serialized.FindProperty("<Injectables>k__BackingField"),
                            root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c is HardPoint || c.GetType().Name=="UnitRenderStateComponent").ToArray()); break;
                    case "DefendPlatform": Assign(serialized.FindProperty("explosionHullRenderers"),hull); break;
                    case "HealthComponent": Assign(serialized.FindProperty("<ShipUnits>k__BackingField"),healthPoints.ToArray()); serialized.FindProperty("ionFieldBounds").boundsValue = new Bounds(collider.center,collider.size); break;
                    case "WeaponComponent": Assign(serialized.FindProperty("hardPoints"),weapons.ToArray()); break;
                    case "FogVisibilityComponent": Assign(serialized.FindProperty("renderers"),renderers.Append(shield.GetComponent<MeshRenderer>()).ToArray()); serialized.FindProperty("revealRadius").floatValue = Mathf.Max(bounds.extents.x,bounds.extents.z); break;
                    case "UnitRenderStateComponent": Assign(serialized.FindProperty("meshRenderers"),renderers.Append(shield.GetComponent<MeshRenderer>()).ToArray()); break;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EmpireAtWar.Editor.ShieldHullBaker.Bake(root.transform,shield);
            PrefabUtility.SaveAsPrefabAsset(root,viewPath);

        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
    private static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
    private static void Assign(SerializedProperty property,UnityEngine.Object[] objects)
    {
        property.arraySize = objects.Length;
        for (int i=0;i<objects.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }
}
