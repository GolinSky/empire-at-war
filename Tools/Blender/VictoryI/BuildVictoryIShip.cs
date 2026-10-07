using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Editor.Rendering;

public static class BuildVictoryIShip
{
    const string VISUAL="Assets/Prefabs/Models/Ships/VictoryI.prefab";
    const string GAMEPLAY="Assets/Prefabs/Models/Ships/VictoryIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/VictoryIShipData.asset";
    const string WRECK_DATA="Assets/Settings/Data/Ship/Wreck/VictoryIWreckData.asset";
    const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/VictoryIReinforcementView.prefab";
    public static string Main()
    {
        Copy("Assets/Prefabs/Models/Ships/VictoryShipView.prefab",GAMEPLAY);
        Copy("Assets/Settings/Data/Ship/VictoryShipData.asset",DATA);
        Copy("Assets/Settings/Data/Ship/Wreck/VictoryWreckData.asset",WRECK_DATA);
        var audit=JObject.Parse(File.ReadAllText("Temp/VictoryIImport/SourceAudit.json"));
        var root=PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;float bottom,top,radius;
        try
        {
            root.name="VictoryIShipView";
            var movement=Component(root,"ShipMoveComponent");
            var body=(Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            var health=Component(root,"HealthComponent");
            var healthConfig=new SerializedObject(health);
            var shield=(MonoBehaviour)healthConfig.FindProperty("shieldView").objectReferenceValue;
            var shieldTransform=shield.transform;
            shieldTransform.SetParent(root.transform,true);
            var weaponComponent=Component(root,"WeaponComponent");
            weaponComponent.transform.SetParent(root.transform,true);
            foreach(var child in body.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            body.localPosition=Vector3.zero;body.localRotation=Quaternion.identity;body.localScale=Vector3.one;
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),body);
            bounds=BoundsOf(visual);
            var points=visual.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponents<MeshRenderer>().Single().enabled)
                .SelectMany(f=>f.sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
            bottom=float.MaxValue;top=float.MinValue;
            foreach(float bank in new[]{-8f,0f,8f})foreach(var point in points)
            {float y=(Quaternion.Euler(0,0,bank)*point).y;bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            radius=Mathf.Ceil(new Vector2(bounds.extents.x,bounds.extents.z).magnitude)+5;
            var targets=new List<HardPoint>();var weapons=new List<WeaponHardPoint>();
            var mappings=new List<object>();int id=0;
            foreach(var hp in audit["hardpoints"].Where(h=>((string)h["name"]).StartsWith("HP_Victory_Missile")||((string)h["name"]).StartsWith("HP_Victory_Torpedo")))
            {
                int type=((string)hp["name"]).Contains("Missile")?34:35;
                var point=CreateWeapon(body,visual,(string)hp["name"],(string)hp["Fire_Bone_A"],type,id++,(float)hp["Fire_Cone_Width"]);
                targets.Add(point);weapons.Add(point);mappings.Add(new{name=(string)hp["name"],bone=(string)hp["Fire_Bone_A"],targetable=true,id=point.Id});
            }
            foreach(var entry in new[]{new{bone="HP_S_BONE_03",type=2,name="ShieldGenerator"},new{bone="HP_E_BONE_01",type=1,name="Engine1"},new{bone="HP_E_BONE_02",type=1,name="Engine2"},new{bone="HP_T_BONE",type=7,name="TractorBeam"}})
            {
                var point=CreateSystem(body,visual,entry.name,entry.bone,entry.type,id++);targets.Add(point);
                mappings.Add(new{name=entry.name,bone=entry.bone,targetable=true,id=point.Id});
            }
            foreach(var hp in audit["hardpoints"].Where(h=>((string)h["name"]).StartsWith("HP_Victory_Weapon_")))
            {
                var point=CreateWeapon(body,visual,(string)hp["name"],(string)hp["Fire_Bone_A"],28,id++,(float)hp["Fire_Cone_Width"]);
                weapons.Add(point);mappings.Add(new{name=(string)hp["name"],bone=(string)hp["Fire_Bone_A"],targetable=false,id=point.Id});
            }
            for(int i=1;i<=6;i++)
            {
                var turret=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="LightDualTurbolaser"+i);
                var muzzle=turret.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="FB_00");
                var point=CreateWeaponAt(body,"LightDualTurbolaser"+i,muzzle.position,36,id++,100f);weapons.Add(point);
                mappings.Add(new{name=point.name,bone="T_0"+i+"/FB_00",targetable=false,id=point.Id});
            }
            var hangar=CreateSystem(body,visual,"Hangar","SPAWN_00",4,id++);
            var launch=new GameObject("VictoryILaunchPoint").transform;launch.SetParent(body,false);
            launch.position=Bone(visual,"SPAWN_00").position;launch.localPosition=new Vector3(launch.localPosition.x,bottom-8,launch.localPosition.z);
            var hangarComponent=Component(root,"HangarComponent");var hangarConfig=new SerializedObject(hangarComponent);
            hangarConfig.FindProperty("hangarHardPoint").objectReferenceValue=hangar;
            hangarConfig.FindProperty("launchPoint").objectReferenceValue=launch;
            hangarConfig.FindProperty("isDestroyable").boolValue=false;hangarConfig.ApplyModifiedPropertiesWithoutUndo();
            Assign(health,"<ShipUnits>k__BackingField",targets.ToArray());
            weaponComponent.transform.SetParent(body,false);
            Assign(weaponComponent,"hardPoints",weapons.ToArray());
            shieldTransform.SetParent(body,true);shieldTransform.localPosition=bounds.center;
            var shieldMesh=shieldTransform.GetComponents<MeshFilter>().Single().sharedMesh.bounds;
            shieldTransform.localScale=new Vector3(bounds.size.x/shieldMesh.size.x,bounds.size.y/shieldMesh.size.y,bounds.size.z/shieldMesh.size.z)*1.06f;
            shieldTransform.localRotation=Quaternion.identity;
            healthConfig.Update();healthConfig.FindProperty("ionFieldBounds").boundsValue=new Bounds(new Vector3(bounds.center.x,(bottom+top)*.5f,bounds.center.z),new Vector3(bounds.size.x,top-bottom,bounds.size.z));healthConfig.ApplyModifiedPropertiesWithoutUndo();
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="SelectedImage"))rect.sizeDelta=Vector2.one*radius*2;
            var hull=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            Assign(Component(root,"Ship"),"explosionHullRenderers",hull);
            var visible=visual.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
            Assign(Component(root,"FogVisibilityComponent"),"renderers",visible.Concat(new Renderer[]{shieldTransform.GetComponents<MeshRenderer>().Single(),root.GetComponents<LineRenderer>().Single()}).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",weapons.Cast<HardPoint>().Concat(targets.Where(t=>!(t is WeaponHardPoint))).Concat(new[]{hangar}).ToArray());
            Assign(Component(root,"TeamColorView"),"meshRenderers",visible.OfType<MeshRenderer>().Concat(shieldTransform.GetComponents<MeshRenderer>()).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            File.WriteAllText("Temp/VictoryIImport/HardpointMapping.json",JsonConvert.SerializeObject(mappings,Formatting.Indented));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=AssetDatabase.LoadAssetAtPath<ShipData>(DATA);data.name="VictoryIShipData";var so=new SerializedObject(data);
        Set(so,"Hull",12000);Set(so,"Shields",8000);Set(so,"Speed",175);Set(so,"ShieldRegenerateValue",13.33f);Set(so,"ShieldRegenerateDelay",1);
        Set(so,"HullBottom",bottom);Set(so,"HullTop",top);Set(so,"NavigationRadius",radius);Set(so,"Range",250);Set(so,"BodyRotationMaxAngle",8);
        var list=so.FindProperty("hardPointHealth");list.arraySize=4;
        int[] types={0,2,1,7};float[] hpValues={750,1500,750,500};
        for(int i=0;i<4;i++){var row=list.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=hpValues[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        list=so.FindProperty("abilities");list.arraySize=2;list.GetArrayElementAtIndex(0).intValue=19;list.GetArrayElementAtIndex(1).intValue=22;
        list=so.FindProperty("hangarBays");list.arraySize=1;var bay=list.GetArrayElementAtIndex(0);bay.FindPropertyRelative("squadronType").intValue=203;bay.FindPropertyRelative("reserve").intValue=2;bay.FindPropertyRelative("maxActive").intValue=1;
        so.FindProperty("<Wreck>k__BackingField").objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA);Save(so);
        var wreckView=ShipWreckBuilder.Build(GAMEPLAY);var wreck=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA));wreck.targetObject.name="VictoryIWreckData";wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreckView;Save(wreck);
        Copy("Assets/Prefabs/Ui/Reinforcement/VictoryReinforcementView.prefab",PREVIEW);root=PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name="VictoryIReinforcementView";
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>hologram).ToArray();
            Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/VictoryIImport/GameplayBounds.json",JsonConvert.SerializeObject(new{size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},bottom,top,radius},Formatting.Indented));
        return "Victory I: 16 weapons, 10 targetable hardpoints, non-destroyable hangar, 12000/8000/175, Full Salvo + Tractor Beam, two Interceptor launches / one active, own preview and wreck.";
    }
    static void Copy(string source,string target){if(!File.Exists(target)&&!AssetDatabase.CopyAsset(source,target))throw new InvalidOperationException(target);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static Transform Bone(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    static WeaponHardPoint CreateWeapon(Transform body,GameObject visual,string name,string bone,int weapon,int id,float width)=>CreateWeaponAt(body,name,Bone(visual,bone).position,weapon,id,width);
    static WeaponHardPoint CreateWeaponAt(Transform body,string name,Vector3 position,int weapon,int id,float width)
    {
        var point=new GameObject(name).AddComponent<WeaponHardPoint>();point.transform.SetParent(body,false);point.transform.position=position;point.transform.rotation=body.rotation;
        var so=new SerializedObject(point);so.FindProperty("<HardPointType>k__BackingField").intValue=0;so.FindProperty("<Id>k__BackingField").intValue=id;so.FindProperty("<WeaponType>k__BackingField").intValue=weapon;
        float side=point.transform.localPosition.x<0?-90:90;
        so.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=Mathf.Max(-180,side-width*.5f);so.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=Mathf.Min(180,side+width*.5f);so.ApplyModifiedPropertiesWithoutUndo();return point;
    }
    static HardPoint CreateSystem(Transform body,GameObject visual,string name,string bone,int type,int id)
    {var point=new GameObject(name).AddComponent<HardPoint>();point.transform.SetParent(body,false);point.transform.position=Bone(visual,bone).position;var so=new SerializedObject(point);so.FindProperty("<HardPointType>k__BackingField").intValue=type;so.FindProperty("<Id>k__BackingField").intValue=id;so.ApplyModifiedPropertiesWithoutUndo();return point;}
    static void Assign(MonoBehaviour component,string field,UnityEngine.Object[] objects){var so=new SerializedObject(component);var array=so.FindProperty(field);array.arraySize=objects.Length;for(int i=0;i<objects.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Set(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
}
