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

public static class BuildTectorShip
{
    const string VISUAL="Assets/Prefabs/Models/Ships/Tector.prefab";
    const string GAMEPLAY="Assets/Prefabs/Models/Ships/TectorShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/TectorShipData.asset";
    const string WRECK_DATA="Assets/Settings/Data/Ship/Wreck/TectorWreckData.asset";
    const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab";
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Tector construction requires Edit Mode.");
        Copy("Assets/Prefabs/Models/Ships/ISDIIShipView.prefab",GAMEPLAY);
        Copy("Assets/Settings/Data/Ship/ISDIIShipData.asset",DATA);
        Copy("Assets/Settings/Data/Ship/Wreck/ISDIIWreckData.asset",WRECK_DATA);
        var audit=JObject.Parse(File.ReadAllText("Temp/TectorImport/SourceAudit.json"));
        var root=PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;float bottom,top,radius;
        try
        {
            root.name="TectorShipView";
            var movement=Component(root,"ShipMoveComponent");
            var body=(Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            var health=Component(root,"HealthComponent");
            var healthConfig=new SerializedObject(health);
            var shield=(MonoBehaviour)healthConfig.FindProperty("shieldView").objectReferenceValue;
            var shieldTransform=shield.transform;
            shieldTransform.SetParent(root.transform,true);
            var weaponComponent=Component(root,"WeaponComponent");
            weaponComponent.transform.SetParent(root.transform,true);
            foreach(var particle in root.GetComponentsInChildren<ParticleSystem>(true))particle.transform.SetParent(root.transform,true);
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
            var armed=audit["hardpoints"].Where(h=>h["Fire_Projectile_Type"]!=null && (string)h["Fire_Projectile_Type"]!="Proj_DUMMY_BONE").ToArray();
            foreach(var hp in armed.Where(h=>(string)h["Is_Targetable"]=="Yes"))
            {
                var point=CreateSourceWeapon(body,visual,hp,id++);targets.Add(point);weapons.Add(point);
                mappings.Add(Mapping(hp,point,true));
            }
            foreach(var hp in audit["hardpoints"].Where(h=>(string)h["Is_Targetable"]=="Yes" && h["Fire_Projectile_Type"]==null && (string)h["Type"]!="HARD_POINT_DUMMY_ART"))
            {
                int type=(string)hp["Type"]=="HARD_POINT_SHIELD_GENERATOR"?2:(string)hp["Type"]=="HARD_POINT_ENGINE"?1:(string)hp["Type"]=="HARD_POINT_FIGHTER_BAY"?4:7;
                var point=CreateSystem(body,visual,(string)hp["name"],(string)hp["Attachment_Bone"],type,id++);targets.Add(point);
                mappings.Add(new{name=point.name,bone=(string)hp["Attachment_Bone"],targetable=true,id=point.Id});
            }
            foreach(var hp in armed.Where(h=>(string)h["Is_Targetable"]=="No" && (string)h["Fire_Projectile_Type"]!="Proj_Composite_Beam_Orange"))
            {
                var point=CreateSourceWeapon(body,visual,hp,id++);weapons.Add(point);mappings.Add(Mapping(hp,point,false));
            }
            var hangarComponent=Component(root,"HangarComponent");var hangarConfig=new SerializedObject(hangarComponent);
            hangarConfig.FindProperty("hangarHardPoint").objectReferenceValue=null;
            hangarConfig.FindProperty("launchPoint").objectReferenceValue=body;
            hangarConfig.FindProperty("bayHardPoints").arraySize=0;hangarConfig.FindProperty("bayLaunchPoints").arraySize=0;
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
            string[] effects={"EngineL2","EngineR2","EngineL"};string[] exhaustBones={"Engine_L_Particles","Engine_R_Particles","Engine_M_Particles"};
            for(int i=0;i<3;i++)
            {
                var effect=root.GetComponentsInChildren<ParticleSystem>(true).Single(p=>p.name==effects[i]).transform;
                effect.SetParent(body,true);effect.position=Bone(visual,exhaustBones[i]).position;effect.rotation=body.rotation;
            }
            Assign(Component(root,"FogVisibilityComponent"),"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",weapons.Cast<HardPoint>().Concat(targets.Where(t=>!(t is WeaponHardPoint))).ToArray());
            Assign(Component(root,"TeamColorView"),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true));
            ShieldHullBaker.Bake(root.transform,(EmpireAtWar.ViewComponents.Health.Shield)shield);
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            File.WriteAllText("Temp/TectorImport/HardpointMapping.json",JsonConvert.SerializeObject(mappings,Formatting.Indented));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=AssetDatabase.LoadAssetAtPath<ShipData>(DATA);data.name="TectorShipData";var so=new SerializedObject(data);
        Set(so,"Hull",22000);Set(so,"Shields",16000);Set(so,"Speed",22);Set(so,"ShieldRegenerateValue",20);so.FindProperty("<ShipClass>k__BackingField").intValue=4;Set(so,"ShieldRegenerateDelay",1);
        Set(so,"HullBottom",bottom);Set(so,"HullTop",top);Set(so,"NavigationRadius",radius);Set(so,"Range",525);Set(so,"BodyRotationMaxAngle",5);
        var list=so.FindProperty("hardPointHealth");list.arraySize=4;
        int[] types={0,2,1,7};float[] hpValues={750,1000,1000,1500};
        for(int i=0;i<4;i++){var row=list.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=hpValues[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        list=so.FindProperty("abilities");list.arraySize=2;list.GetArrayElementAtIndex(0).intValue=32;list.GetArrayElementAtIndex(1).intValue=22;
        so.FindProperty("hangarBays").arraySize=0;
        so.FindProperty("<Wreck>k__BackingField").objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA);Save(so);
        var wreckView=ShipWreckBuilder.Build(GAMEPLAY);var wreck=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA));wreck.targetObject.name="TectorWreckData";wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreckView;Save(wreck);
        Copy("Assets/Prefabs/Ui/Reinforcement/VictoryReinforcementView.prefab",PREVIEW);root=PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name="TectorReinforcementView";
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.sharedMaterials.Any(m=>m.shader.name!="EmpireAtWar/Ship Lit"))renderer.enabled=false;
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>hologram).ToArray();
            var spawn=Component(root,"UnitSpawnView");Assign(spawn,"meshRenderers",renderers);
            var spawnConfig=new SerializedObject(spawn);spawnConfig.FindProperty("hologramMaterial").objectReferenceValue=hologram;
            spawnConfig.FindProperty("height").floatValue=data.Height;spawnConfig.ApplyModifiedPropertiesWithoutUndo();
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/TectorImport/GameplayBounds.json",JsonConvert.SerializeObject(new{size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},bottom,top,radius},Formatting.Indented));
        return "Tector: 25 weapons, 16 targetable hardpoints, 22000/16000/22, no fighters, dedicated preview and wreck.";
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
        int type=projectile=="Proj_Space_TL_Heavy_Green"?43:projectile=="Proj_Space_TL_Medium_LR_Dual_Green"?53:projectile=="Proj_Space_LC_Heavy_Green"?28:projectile=="Proj_Space_TL_Light_Green"?33:38;
        var parent=!string.IsNullOrEmpty((string)hp["Model_To_Attach"])?visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)hp["name"]+"_Turret").gameObject:visual;
        var muzzle=Bone(parent,(string)hp["Fire_Bone_A"]);
        return CreateWeaponAt(body,(string)hp["name"],muzzle.position,type,id,(float)hp["Fire_Cone_Width"]);
    }
    static object Mapping(JToken hp,HardPoint point,bool targetable)=>new{name=point.name,bone=!string.IsNullOrEmpty((string)hp["Model_To_Attach"])?(string)hp["name"]+"_Turret/"+(string)hp["Fire_Bone_A"]:(string)hp["Fire_Bone_A"],targetable,id=point.Id};
    static WeaponHardPoint CreateWeaponAt(Transform body,string name,Vector3 position,int weapon,int id,float width)
    {
        var point=new GameObject(name).AddComponent<WeaponHardPoint>();point.transform.SetParent(body,false);point.transform.position=position;point.transform.rotation=body.rotation;
        var so=new SerializedObject(point);so.FindProperty("<HardPointType>k__BackingField").intValue=0;so.FindProperty("<Id>k__BackingField").intValue=id;so.FindProperty("<WeaponType>k__BackingField").intValue=weapon;
        float side=point.transform.localPosition.x<0?-90:90;
        so.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=Mathf.Max(-180,side-width*.5f);so.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=Mathf.Min(180,side+width*.5f);so.FindProperty("spawnDestroyedExplosion").boolValue=id<16;so.ApplyModifiedPropertiesWithoutUndo();if(id<16){var collider=point.gameObject.AddComponent<SphereCollider>();collider.radius=3;}return point;
    }
    static HardPoint CreateSystem(Transform body,GameObject visual,string name,string bone,int type,int id)
    {var point=new GameObject(name).AddComponent<HardPoint>();point.transform.SetParent(body,false);point.transform.position=Bone(visual,bone).position;var so=new SerializedObject(point);so.FindProperty("<HardPointType>k__BackingField").intValue=type;so.FindProperty("<Id>k__BackingField").intValue=id;so.FindProperty("spawnDestroyedExplosion").boolValue=id<16;so.ApplyModifiedPropertiesWithoutUndo();if(id<16){var collider=point.gameObject.AddComponent<SphereCollider>();collider.radius=3;}return point;}
    static void Assign(MonoBehaviour component,string field,UnityEngine.Object[] objects){var so=new SerializedObject(component);var array=so.FindProperty(field);array.arraySize=objects.Length;for(int i=0;i<objects.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Set(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
}
