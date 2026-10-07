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

public static class BuildISDIIIShip
{
    const string VISUAL="Assets/Prefabs/Models/Ships/ISDIII.prefab";
    const string GAMEPLAY="Assets/Prefabs/Models/Ships/ISDIIIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ISDIIIShipData.asset";
    const string WRECK_DATA="Assets/Settings/Data/Ship/Wreck/ISDIIIWreckData.asset";
    const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/ISDIIIReinforcementView.prefab";
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("ISD III construction requires Edit Mode.");
        Copy("Assets/Prefabs/Models/Ships/ISDIShipView.prefab",GAMEPLAY);
        Copy("Assets/Settings/Data/Ship/ISDIShipData.asset",DATA);
        Copy("Assets/Settings/Data/Ship/Wreck/ISDIWreckData.asset",WRECK_DATA);
        var audit=JObject.Parse(File.ReadAllText("Temp/ISDIIIImport/SourceAudit.json"));
        var root=PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;float bottom,top,radius;
        try
        {
            root.name="ISDIIIShipView";
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
            var armed=audit["hardpoints"].Where(h=>h["Fire_Projectile_Type"]!=null && (string)h["Fire_Projectile_Type"]!="Proj_DUMMY_BONE").ToArray();
            foreach(var hp in armed.Where(h=>(string)h["Is_Targetable"]=="Yes"))
            {
                var point=CreateSourceWeapon(body,visual,hp,id++);targets.Add(point);weapons.Add(point);
                mappings.Add(Mapping(hp,point,true));
            }
            HardPoint hangar=null;
            foreach(var hp in audit["hardpoints"].Where(h=>(string)h["Is_Targetable"]=="Yes" && h["Fire_Projectile_Type"]==null && (string)h["Type"]!="HARD_POINT_DUMMY_ART"))
            {
                int type=(string)hp["Type"]=="HARD_POINT_SHIELD_GENERATOR"?2:(string)hp["Type"]=="HARD_POINT_ENGINE"?1:(string)hp["Type"]=="HARD_POINT_FIGHTER_BAY"?4:7;
                var point=CreateSystem(body,visual,(string)hp["name"],(string)hp["Attachment_Bone"],type,id++);targets.Add(point);
                if(type==4)hangar=point;
                mappings.Add(new{name=point.name,bone=(string)hp["Attachment_Bone"],targetable=true,id=point.Id});
            }
            foreach(var hp in armed.Where(h=>(string)h["Is_Targetable"]=="No" && (string)h["Fire_Projectile_Type"]!="Proj_Composite_Beam_Orange"))
            {
                var point=CreateSourceWeapon(body,visual,hp,id++);weapons.Add(point);mappings.Add(Mapping(hp,point,false));
            }
            var compositeHp=armed.Single(h=>(string)h["Fire_Projectile_Type"]=="Proj_Composite_Beam_Orange");
            var composite=CreateSourceWeapon(body,visual,compositeHp,id++);mappings.Add(Mapping(compositeHp,composite,false));
            var ship=new SerializedObject(Component(root,"Ship"));ship.FindProperty("compositeBeamMuzzle").objectReferenceValue=composite.transform;ship.ApplyModifiedPropertiesWithoutUndo();
            var launch=new GameObject("ISDIIILaunchPoint").transform;launch.SetParent(body,false);
            launch.position=Bone(visual,"SPAWN_00").position;launch.localPosition=new Vector3(launch.localPosition.x,bottom-8,launch.localPosition.z);
            var hangarComponent=Component(root,"HangarComponent");var hangarConfig=new SerializedObject(hangarComponent);
            hangarConfig.FindProperty("hangarHardPoint").objectReferenceValue=hangar;
            hangarConfig.FindProperty("launchPoint").objectReferenceValue=launch;
            hangarConfig.FindProperty("isDestroyable").boolValue=true;hangarConfig.ApplyModifiedPropertiesWithoutUndo();
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
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",weapons.Cast<HardPoint>().Concat(targets.Where(t=>!(t is WeaponHardPoint))).Concat(new HardPoint[]{composite}).ToArray());
            Assign(Component(root,"TeamColorView"),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true));
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            File.WriteAllText("Temp/ISDIIIImport/HardpointMapping.json",JsonConvert.SerializeObject(mappings,Formatting.Indented));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=AssetDatabase.LoadAssetAtPath<ShipData>(DATA);data.name="ISDIIIShipData";var so=new SerializedObject(data);
        Set(so,"Hull",28000);Set(so,"Shields",18000);Set(so,"Speed",25);Set(so,"ShieldRegenerateValue",22.5f);so.FindProperty("<ShipClass>k__BackingField").intValue=4;Set(so,"ShieldRegenerateDelay",1);
        Set(so,"HullBottom",bottom);Set(so,"HullTop",top);Set(so,"NavigationRadius",radius);Set(so,"Range",625);Set(so,"BodyRotationMaxAngle",8);
        var list=so.FindProperty("hardPointHealth");list.arraySize=5;
        int[] types={0,2,1,7,4};float[] hpValues={1000,1000,1000,1500,2000};
        for(int i=0;i<5;i++){var row=list.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=hpValues[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        list=so.FindProperty("abilities");list.arraySize=2;list.GetArrayElementAtIndex(0).intValue=25;list.GetArrayElementAtIndex(1).intValue=22;
        list=so.FindProperty("hangarBays");list.arraySize=2;
        for(int i=0;i<2;i++){var bay=list.GetArrayElementAtIndex(i);bay.FindPropertyRelative("squadronType").intValue=i==0?202:205;bay.FindPropertyRelative("reserve").intValue=i==0?3:2;bay.FindPropertyRelative("maxActive").intValue=1;}
        Set(so,"HangarLaunchInterval",30);
        so.FindProperty("<Wreck>k__BackingField").objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA);Save(so);
        var wreckView=ShipWreckBuilder.Build(GAMEPLAY);var wreck=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA));wreck.targetObject.name="ISDIIIWreckData";wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreckView;Save(wreck);
        Copy("Assets/Prefabs/Ui/Reinforcement/VictoryReinforcementView.prefab",PREVIEW);root=PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name="ISDIIIReinforcementView";
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.sharedMaterials.Any(m=>m.shader.name!="EmpireAtWar/Ship Lit"))renderer.enabled=false;
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>hologram).ToArray();
            Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/ISDIIIImport/GameplayBounds.json",JsonConvert.SerializeObject(new{size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},bottom,top,radius},Formatting.Indented));
        return "ISD III: 31 automatic weapons plus ability-only composite beam; 19 targets; 28000/18000/25; targetable hangar with Avengers and Punishers; own preview and wreck.";
    }
    static void Copy(string source,string target){if(!File.Exists(target)&&!AssetDatabase.CopyAsset(source,target))throw new InvalidOperationException(target);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static Transform Bone(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase) && InSourceRig(t,root.transform));
    static bool InSourceRig(Transform bone,Transform root)
    {
        for(var parent=bone.parent;parent!=root && parent!=null;parent=parent.parent)
            if(parent.name.EndsWith("_Turret"))return false;
        return true;
    }
    static WeaponHardPoint CreateSourceWeapon(Transform body,GameObject visual,JToken hp,int id)
    {
        string projectile=(string)hp["Fire_Projectile_Type"];
        int type=projectile=="Proj_Space_TL_Heavy_Dual_Green"?43:projectile=="Proj_Space_IonCannon_Shield_Heavy_Dual"?44:projectile=="Proj_Composite_Beam_Orange"?45:projectile=="Proj_Space_LC_Heavy_Green"?28:projectile=="Proj_Space_IonCannon_Shield_Medium"?32:(int)hp["Fire_Pulse_Count"]==3?38:4;
        string attachment=(string)hp["Attachment_Bone"];
        var parent=hp["Model_To_Attach"]!=null?visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==attachment+"_Turret").gameObject:visual;
        var muzzle=Bone(parent,(string)hp["Fire_Bone_A"]);
        return CreateWeaponAt(body,(string)hp["name"],muzzle.position,type,id,(float)hp["Fire_Cone_Width"]);
    }
    static object Mapping(JToken hp,HardPoint point,bool targetable)=>new{name=point.name,bone=hp["Model_To_Attach"]!=null?(string)hp["Attachment_Bone"]+"_Turret/"+(string)hp["Fire_Bone_A"]:(string)hp["Fire_Bone_A"],targetable,id=point.Id};
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
