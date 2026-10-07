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

public static class BuildAcclamatorAssaultShip
{
    const string TASK = "Temp/AcclamatorAssaultImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/AcclamatorAssault.prefab";
    const string VIEW = "Assets/Prefabs/Models/Ships/AcclamatorAssaultShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/AcclamatorAssaultShipData.asset";
    const string WRECK = "Assets/Settings/Data/Ship/Wreck/AcclamatorAssaultWreckData.asset";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/AcclamatorAssaultReinforcementView.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Acclamator construction requires Edit Mode.");
        Copy("Assets/Prefabs/Models/Ships/AcclamatorShipView.prefab", VIEW);
        Copy("Assets/Settings/Data/Ship/AcclamatorShipData.asset", DATA);
        Copy("Assets/Settings/Data/Ship/Wreck/AcclamatorWreckData.asset", WRECK);
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;
        float bottom = float.MaxValue, top = float.MinValue, radius;
        try
        {
            root.name = "AcclamatorAssaultShipView";
            var body = (Transform)new SerializedObject(Component(root,"ShipMoveComponent")).FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root,"HealthComponent"); var healthConfig = new SerializedObject(health);
            var shield = (Shield)healthConfig.FindProperty("shieldView").objectReferenceValue;
            var weaponComponent = Component(root,"WeaponComponent");
            int weaponLayer = root.GetComponentsInChildren<WeaponHardPoint>(true).First().gameObject.layer;
            if(PrefabUtility.IsAnyPrefabInstanceRoot(body.gameObject))PrefabUtility.UnpackPrefabInstance(body.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            shield.transform.SetParent(root.transform,true); weaponComponent.transform.SetParent(root.transform,true);
            foreach (var child in body.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach (var point in root.GetComponentsInChildren<HardPoint>(true)) UnityEngine.Object.DestroyImmediate(point.gameObject);
            body.name="AcclamatorAssaultBody";
            body.localPosition = Vector3.zero; body.localRotation = Quaternion.identity; body.localScale = Vector3.one;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),body);
            bounds = BoundsOf(visual);
            var vertices = visual.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponents<MeshRenderer>().Single().enabled)
                .SelectMany(f=>f.sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
            foreach (float bank in new[]{-20f,0f,20f}) foreach (var vertex in vertices)
            { float y=(Quaternion.Euler(0,0,bank)*vertex).y; bottom=Mathf.Min(bottom,y); top=Mathf.Max(top,y); }
            radius = Mathf.Ceil(new Vector2(bounds.extents.x,bounds.extents.z).magnitude)+2;
            var weapons = new List<WeaponHardPoint>(); var targets = new List<HardPoint>(); var points = new List<HardPoint>();
            var mappings = new List<object>(); int id=4;
            var audit = JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"));
            foreach (var hp in audit["hardpoints"].Where(h=>((string)h["name"]).Contains("Assault_Missile") || ((string)h["name"]).Contains("_HLC_")))
            {
                bool missile=((string)hp["name"]).Contains("Assault_Missile");
                var weapon=new GameObject((string)hp["name"]).AddComponent<WeaponHardPoint>();
                weapon.gameObject.layer=weaponLayer; weapon.transform.SetParent(body,false);
                weapon.transform.position=Bone(visual,(string)hp["Fire_Bone_A"]).position;
                // Bone rotation is not a Unity yaw; the two authored sides face outward.
                weapon.transform.localRotation=Quaternion.Euler(0,weapon.transform.localPosition.x<0?-90:90,0);
                var config=new SerializedObject(weapon); config.FindProperty("<HardPointType>k__BackingField").intValue=0;
                config.FindProperty("<Id>k__BackingField").intValue=missile?targets.Count:id++; config.FindProperty("<WeaponType>k__BackingField").intValue=missile?63:28;
                float half=float.Parse((string)hp["Fire_Cone_Width"],System.Globalization.CultureInfo.InvariantCulture)*.5f;
                config.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=-half;
                config.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=half;
                config.ApplyModifiedPropertiesWithoutUndo(); weapons.Add(weapon); points.Add(weapon);
                if(missile) targets.Add(weapon);
                mappings.Add(new{name=weapon.name,bone=(string)hp["Fire_Bone_A"],id=weapon.Id,targetable=missile});
            }
            foreach(var row in new[]{new{name="ShieldGenerator",bone="SG",type=2},new{name="Engine",bone="ENG",type=1}})
            {
                var point=new GameObject(row.name).AddComponent<HardPoint>(); point.gameObject.layer=weaponLayer;
                point.transform.SetParent(body,false); point.transform.position=Bone(visual,row.bone).position;
                var config=new SerializedObject(point); config.FindProperty("<HardPointType>k__BackingField").intValue=row.type;
                config.FindProperty("<Id>k__BackingField").intValue=targets.Count; config.ApplyModifiedPropertiesWithoutUndo();
                targets.Add(point); points.Add(point); mappings.Add(new{name=row.name,bone=row.bone,id=point.Id,targetable=true});
            }
            var interceptorTemplate=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/MunificentShipView.prefab")
                .GetComponentsInChildren<MissileInterceptorHardPoint>(true).First();
            for(int i=1;i<=6;i++)
            {
                var turret=Bone(visual,"QTL_"+i.ToString("00")+"_Turret");
                var mount=UnityEngine.Object.Instantiate(interceptorTemplate.gameObject,body);
                mount.name="PointDefense"+i; mount.layer=weaponLayer;
                var point=mount.GetComponents<MissileInterceptorHardPoint>().Single();
                mount.transform.SetPositionAndRotation(Bone(turret.gameObject,"FB_02").position,body.rotation); mount.transform.localScale=Vector3.one;
                var config=new SerializedObject(point); config.FindProperty("<Id>k__BackingField").intValue=id++;
                config.FindProperty("spawnDestroyedExplosion").boolValue=false; config.ApplyModifiedPropertiesWithoutUndo();
                points.Add(point); mappings.Add(new{name=mount.name,bone=turret.name+"/FB_02",id=point.Id,targetable=false});
            }
            var hangarConfig=new SerializedObject(Component(root,"HangarComponent"));
            var launch=new GameObject("AcclamatorAssaultLaunchPoint").transform; launch.SetParent(body,false);
            launch.position=Bone(visual,"Hangar").position; launch.localPosition=new Vector3(launch.localPosition.x,bottom-8,launch.localPosition.z);
            hangarConfig.FindProperty("launchPoint").objectReferenceValue=launch;
            hangarConfig.FindProperty("hangarHardPoint").objectReferenceValue=null;
            hangarConfig.FindProperty("isDestroyable").boolValue=false; hangarConfig.ApplyModifiedPropertiesWithoutUndo();
            Assign(health,"<ShipUnits>k__BackingField",targets.ToArray());
            weaponComponent.transform.SetParent(body,false); Assign(weaponComponent,"hardPoints",weapons.ToArray());
            shield.transform.SetParent(body,true); ShieldHullBaker.Bake(root.transform,shield);
            healthConfig.Update(); healthConfig.FindProperty("ionFieldBounds").boundsValue=new Bounds(new Vector3(0,(bottom+top)*.5f,0),new Vector3(bounds.size.x,top-bottom,bounds.size.z)); healthConfig.ApplyModifiedPropertiesWithoutUndo();
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="SelectedImage"))rect.sizeDelta=Vector2.one*radius*2;
            var visible=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Assign(Component(root,"Ship"),"explosionHullRenderers",visible.Where(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray());
            var shieldRenderer=shield.GetComponents<MeshRenderer>().Single();
            Assign(Component(root,"FogVisibilityComponent"),"renderers",visible.Cast<Renderer>().Concat(new Renderer[]{shieldRenderer,root.GetComponents<LineRenderer>().Single()}).Concat(points.OfType<MissileInterceptorHardPoint>().SelectMany(p=>p.GetComponentsInChildren<LineRenderer>(true))).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",points.ToArray());
            Assign(Component(root,"TeamColorView"),"meshRenderers",visible.Concat(new[]{shieldRenderer}).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root,VIEW); File.WriteAllText(TASK+"HardpointMapping.json",JsonConvert.SerializeObject(mappings,Formatting.Indented));
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<ShipData>(DATA)); data.targetObject.name="AcclamatorAssaultShipData";
        Set(data,"Hull",5000);Set(data,"Shields",2500);Set(data,"Speed",40);Set(data,"ShieldRegenerateValue",4.16f);Set(data,"ShieldRegenerateDelay",1);
        Set(data,"HullBottom",bottom);Set(data,"HullTop",top);Set(data,"NavigationRadius",radius);Set(data,"Range",250);
        var hpHealth=data.FindProperty("hardPointHealth"); hpHealth.arraySize=3;
        int[] types={0,2,1};float[] values={750,500,750};
        for(int i=0;i<3;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=1;abilities.GetArrayElementAtIndex(0).intValue=31;
        var bays=data.FindProperty("hangarBays");bays.arraySize=2;
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);bay.FindPropertyRelative("squadronType").intValue=200+i;bay.FindPropertyRelative("reserve").intValue=i==0?3:2;bay.FindPropertyRelative("maxActive").intValue=1;}
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK);Save(data);
        var wreckView=ShipWreckBuilder.Build(VIEW);var wreck=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK));wreck.targetObject.name="AcclamatorAssaultWreckData";
        wreck.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreckView;Save(wreck);
        Copy("Assets/Prefabs/Ui/Reinforcement/AcclamatorReinforcementView.prefab",PREVIEW);root=PrefabUtility.LoadPrefabContents(PREVIEW);
        try
        {
            root.name="AcclamatorAssaultReinforcementView";
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one;
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=renderer.enabled&&renderer.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit");
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>hologram).ToArray();
            Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);
            foreach(var collider in root.GetComponents<BoxCollider>()){collider.center=bounds.center;collider.size=bounds.size;}
            PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();File.WriteAllText(TASK+"GameplayBounds.json",JsonConvert.SerializeObject(new{size=new[]{bounds.size.x,bounds.size.y,bounds.size.z},bottom,top,radius},Formatting.Indented));
        return "Saved four targets, two launchers, four heavy lasers, six missile interceptors, TIE fighter/bomber bays without a hangar hardpoint, preview and wreck.";
    }
    static void Copy(string source,string target){if(!File.Exists(target)&&!AssetDatabase.CopyAsset(source,target))throw new InvalidOperationException(target);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static Transform Bone(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
    static void Assign(MonoBehaviour component,string field,UnityEngine.Object[] objects){var config=new SerializedObject(component);var array=config.FindProperty(field);array.arraySize=objects.Length;for(int i=0;i<objects.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];config.ApplyModifiedPropertiesWithoutUndo();}
    static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);return bounds;}
    static void Set(SerializedObject data,string name,float value)=>data.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static void Save(SerializedObject data){data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);}
}
