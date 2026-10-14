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
using EmpireAtWar.Editor;

public static class BuildAllegianceShip
{
    const string VISUAL="Assets/Prefabs/Models/Ships/Empire/Allegiance.prefab";
    const string GAMEPLAY="Assets/Prefabs/Models/Ships/Empire/AllegianceShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/Empire/AllegianceShipData.asset";
    const string WRECK_DATA="Assets/Settings/Data/Models/Wrecks/Empire/AllegianceWreckData.asset";
    const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/AllegianceReinforcementView.prefab";
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Allegiance construction requires Edit Mode.");
        Copy("Assets/Prefabs/Models/Ships/Empire/ISDIShipView.prefab",GAMEPLAY);
        Copy("Assets/Settings/Data/Ship/Empire/ISDIShipData.asset",DATA);
        Copy("Assets/Settings/Data/Models/Wrecks/Empire/ISDIWreckData.asset",WRECK_DATA);
        var audit=JObject.Parse(File.ReadAllText("Temp/AllegianceImport/SourceAudit.json"));
        var root=PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;float bottom,top,radius;
        try
        {
            root.name="AllegianceShipView";
            var movement=Component(root,"ShipMoveComponent");
            var body=(Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            var health=Component(root,"HealthComponent");
            var healthConfig=new SerializedObject(health);
            var shield=(MonoBehaviour)healthConfig.FindProperty("shieldView").objectReferenceValue;
            var shieldTransform=shield.transform;
            int weaponLayer=root.GetComponentsInChildren<WeaponHardPoint>(true).First().gameObject.layer;
            foreach(var hangar in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c.GetType().Name=="HangarComponent").ToArray()) UnityEngine.Object.DestroyImmediate(hangar.gameObject);
            Component(root,"AudioShipComponent").transform.SetParent(root.transform,true);
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
            foreach(float bank in new[]{-5f,0f,5f})foreach(var point in points)
            {float y=(Quaternion.Euler(0,0,bank)*point).y;bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            radius=Mathf.Ceil(new Vector2(bounds.extents.x,bounds.extents.z).magnitude)+5;
            var targets=new List<HardPoint>();var weapons=new List<WeaponHardPoint>();
            var mappings=new List<object>();int id=0;
            int[] profiles={3,20,5,32};
            string[][] groups={
                new[]{"TL_01","TL_02","TL_03"},
                new[]{"QTL_01","QTL_03","QTL_05","QTL_07","QTL_09","QTL_11"},
                new[]{"QL_01","QL_07","QL_15","QL_18","QL_24","QL_30"},
                new[]{"IC_01","IC_02","IC_05","IC_06","ICT_01","ICT_04"}};
            for(int group=0;group<groups.Length;group++) foreach(string socket in groups[group])
            {
                var source=audit["hardpoints"].Single(h=>(string)h["Attachment_Bone"]==socket && h["Fire_Projectile_Type"]!=null && (string)h["name"]!="HP_ASD_Facing");
                var parent=!string.IsNullOrEmpty((string)source["Model_To_Attach"])?visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==socket+"_Turret").gameObject:visual;
                var muzzle=Bone(parent,(string)source["Fire_Bone_A"]);
                var point=CreateWeaponAt(body,"Battery_"+socket,muzzle.position,profiles[group],id++,180);
                point.gameObject.layer=weaponLayer; var battery=new SerializedObject(point);battery.FindProperty("mainBattery").boolValue=group<2;battery.ApplyModifiedPropertiesWithoutUndo(); targets.Add(point); weapons.Add(point);
                mappings.Add(new {name=point.name,attachment=socket,fireBone=muzzle.name,weaponType=profiles[group],id=point.Id,position=new[]{muzzle.position.x,muzzle.position.y,muzzle.position.z}});
            }
            var engines=visual.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Engine_Exhaust").bounds;
            for(int i=0;i<3;i++)
            {
                var point=CreateSystem(body,visual,"Engine"+(i+1),"Shield_Gen",1,id++);
                point.gameObject.layer=weaponLayer; point.transform.position=engines.center+Vector3.right*engines.extents.x*(i-1)*.7f; targets.Add(point);
            }
            var generator=Bone(visual,"Shield_Gen");
            for(int i=0;i<2;i++)
            {
                var point=CreateSystem(body,visual,"ShieldGenerator"+(i+1),"Shield_Gen",2,id++);
                point.gameObject.layer=weaponLayer; point.transform.position=generator.position+Vector3.right*(i==0?-1:1)*bounds.size.x*.04f; targets.Add(point);
            }
            foreach(var spec in new[]{new{name="TractorBeam",bone="Spawn_01",type=7},new{name="CommandArray",bone="objBridge",type=11}})
            {var point=CreateSystem(body,visual,spec.name,spec.bone,spec.type,id++);point.gameObject.layer=weaponLayer;targets.Add(point);}
            Assign(health,"<ShipUnits>k__BackingField",targets.ToArray());
            weaponComponent.transform.SetParent(body,false);
            Assign(weaponComponent,"hardPoints",weapons.ToArray());
            shieldTransform.SetParent(body,true);shieldTransform.localPosition=bounds.center;
            ShieldHullBaker.Bake(root.transform,(Shield)shield);
            healthConfig.Update();healthConfig.FindProperty("ionFieldBounds").boundsValue=new Bounds(new Vector3(bounds.center.x,(bottom+top)*.5f,bounds.center.z),new Vector3(bounds.size.x,top-bottom,bounds.size.z));healthConfig.ApplyModifiedPropertiesWithoutUndo();
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="SelectedImage"))rect.sizeDelta=Vector2.one*radius*2;
            var hull=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            Assign(Component(root,"Ship"),"explosionHullRenderers",hull);
            var visible=visual.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
            Assign(Component(root,"FogVisibilityComponent"),"renderers",visible.Concat(new Renderer[]{shieldTransform.GetComponents<MeshRenderer>().Single(),root.GetComponents<LineRenderer>().Single()}).ToArray());
            Assign(Component(root,"TeamColorView"),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true));
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            File.WriteAllText("Temp/AllegianceImport/HardpointMapping.json",JsonConvert.SerializeObject(mappings,Formatting.Indented));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=AssetDatabase.LoadAssetAtPath<ShipData>(DATA);data.name="AllegianceShipData";var so=new SerializedObject(data);
        Set(so,"Hull",20000);Set(so,"Shields",16000);Set(so,"Speed",12);Set(so,"ShieldRegenerateValue",80);so.FindProperty("<ShipClass>k__BackingField").intValue=5;so.FindProperty("<HeightTier>k__BackingField").intValue=(int)ShipHeightTier.Deep;Set(so,"RotationSpeed",1.5f);Set(so,"TurnAcceleration",1.5f);Set(so,"ShieldRegenerateDelay",1);
        Set(so,"HullBottom",bottom);Set(so,"HullTop",top);Set(so,"NavigationRadius",radius);Set(so,"Range",750);Set(so,"BodyRotationMaxAngle",5);
        var list=so.FindProperty("hardPointHealth");list.arraySize=5;
        int[] types={0,2,1,7,11};float[] hpValues={600,1200,1200,800,800};
        for(int i=0;i<5;i++){var row=list.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=hpValues[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        list=so.FindProperty("abilities");list.arraySize=2;list.GetArrayElementAtIndex(0).intValue=24;list.GetArrayElementAtIndex(1).intValue=22;
        so.FindProperty("hangarBays").arraySize=0;
        list=so.FindProperty("weaponLoadout");list.arraySize=4;
        int[] weaponTypes={3,20,5,32},counts={3,6,6,6};
        for(int i=0;i<4;i++){var row=list.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=weaponTypes[i];row.FindPropertyRelative("count").intValue=counts[i];}
        so.FindProperty("<Wreck>k__BackingField").objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA);Save(so);
        var wreckView=ShipWreckBuilder.Build(GAMEPLAY);var wreck=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA));wreck.targetObject.name="AllegianceWreckData";wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreckView;Save(wreck);
        Copy("Assets/Prefabs/Ui/Reinforcement/VictoryReinforcementView.prefab",PREVIEW);root=PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name="AllegianceReinforcementView";
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.sharedMaterials.Any(m=>m.shader.name!="EmpireAtWar/Ship Lit"))renderer.enabled=false;
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>hologram).ToArray();
            Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);
            var spawn=new SerializedObject(Component(root,"UnitSpawnView"));spawn.FindProperty("hologramMaterial").objectReferenceValue=hologram;spawn.ApplyModifiedPropertiesWithoutUndo();
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/AllegianceImport/GameplayBounds.json",JsonConvert.SerializeObject(new{size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},bottom,top,radius},Formatting.Indented));
        return "Allegiance: 21 weapons, 28 targets, 20000/16000/12, no hangar, Power to Main Batteries + Tractor Beam.";
    }
    static void Copy(string source,string target){if(!File.Exists(target)&&!AssetDatabase.CopyAsset(source,target))throw new InvalidOperationException(target);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static Transform Bone(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name && InSourceRig(t,root.transform) && !t.GetComponents<MeshFilter>().Any());
    static bool InSourceRig(Transform bone,Transform root)
    {
        for(var parent=bone.parent;parent!=root && parent!=null;parent=parent.parent)
            if(parent.name.EndsWith("_Turret"))return false;
        return true;
    }
    static WeaponHardPoint CreateWeaponAt(Transform body,string name,Vector3 position,int weapon,int id,float width)
    {
        var point=new GameObject(name).AddComponent<WeaponHardPoint>();point.transform.SetParent(body,false);point.transform.position=position;point.transform.rotation=Quaternion.Euler(0,point.transform.localPosition.x<0?-90:90,0);
        var so=new SerializedObject(point);so.FindProperty("<HardPointType>k__BackingField").intValue=0;so.FindProperty("<Id>k__BackingField").intValue=id;so.FindProperty("<WeaponType>k__BackingField").intValue=weapon;

        so.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=-width*.5f;so.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=width*.5f;so.ApplyModifiedPropertiesWithoutUndo();return point;
    }
    static HardPoint CreateSystem(Transform body,GameObject visual,string name,string bone,int type,int id)
    {var point=new GameObject(name).AddComponent<HardPoint>();point.transform.SetParent(body,false);point.transform.position=bone=="Engines"?Vector3.zero:Bone(visual,bone).position;var so=new SerializedObject(point);so.FindProperty("<HardPointType>k__BackingField").intValue=type;so.FindProperty("<Id>k__BackingField").intValue=id;so.ApplyModifiedPropertiesWithoutUndo();return point;}
    static void Assign(MonoBehaviour component,string field,UnityEngine.Object[] objects){var so=new SerializedObject(component);var array=so.FindProperty(field);array.arraySize=objects.Length;for(int i=0;i<objects.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Set(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
}
