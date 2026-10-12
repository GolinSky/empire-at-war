using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EmpireAtWar.ViewComponents.Health;

public static class BuildNebulaGameplay
{
    const string VISUAL = "Assets/Prefabs/Models/Ships/Rebellion/Nebula.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/Rebellion/NebulaShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/NebulaReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/NebulaWreckView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Rebellion/NebulaShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Rebellion/NebulaWreckData.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Ship construction requires Edit Mode.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY)==null) AssetDatabase.CopyAsset("Assets/Prefabs/Models/Ships/Rebellion/HomeOneShipView.prefab",GAMEPLAY);
        var root = PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;
        float bottom, top;
        try
        {
            root.name = "NebulaShipView";
            var movement = Component(root,"ShipMoveComponent");
            var body = (Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            body.localScale=Vector3.one;
            body.localPosition=Vector3.zero;
            body.localRotation=Quaternion.identity;
            var health = Component(root,"HealthComponent");
            var shield = (MonoBehaviour)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            const string SHIELD_MESH="Assets/Art/Models/Shields/NebulaShield.asset";
            var shieldFilter=shield.GetComponent<MeshFilter>();
            if(AssetDatabase.LoadAssetAtPath<Mesh>(SHIELD_MESH)==null)
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(shieldFilter.sharedMesh),SHIELD_MESH);
            shieldFilter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(SHIELD_MESH);
            var weaponComponent = Component(root,"WeaponComponent");
            weaponComponent.transform.SetParent(root.transform,true);
            var hangarComponent = Component(root,"HangarComponent");
            hangarComponent.transform.SetParent(root.transform,true);
            Component(root,"AudioShipComponent").transform.SetParent(root.transform,true);
            shield.transform.SetParent(root.transform,true);
            foreach (var hp in root.GetComponentsInChildren<HardPoint>(true)) UnityEngine.Object.DestroyImmediate(hp.gameObject);
            foreach (var child in body.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),body);
            bounds = BoundsOf(visual);
            var bones = visual.GetComponentsInChildren<Transform>(true);
            var hull = bones.Single(t=>t.name=="NebulaRig");
            Transform Bone(string name) => hull.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null);
            var weapons = new List<WeaponHardPoint>();
            var targetable = new List<HardPoint>();
            var audit = JObject.Parse(System.IO.File.ReadAllText("Temp/NebulaImport/SourceAudit.json"));
            var mountGroup = new GameObject("Hardpoints").transform;
            mountGroup.SetParent(body,false);
            int id = 0;
            var records = new JArray();
            HardPoint Mount(string name,int type,int profile,Vector3 position,string source)
            {
                var mount=new GameObject(name); mount.transform.SetParent(mountGroup,false); mount.transform.position=position;
                var hp=type==0 ? (HardPoint)mount.AddComponent<WeaponHardPoint>() : mount.AddComponent<HardPoint>();
                Configure(hp,id++,type,profile,85);targetable.Add(hp);
                mount.AddComponent<SphereCollider>().radius=type==0?1.5f:3f;
                if(hp is WeaponHardPoint)weapons.Add((WeaponHardPoint)hp);
                records.Add(new JObject{["id"]=hp.Id,["name"]=name,["source"]=source,["position"]=new JArray(position.x,position.y,position.z),["profile"]=profile,["targetable"]=true});
                return hp;
            }
            Vector3 PartMuzzle(int index,string name="FP_01")=>bones.Single(t=>t.name=="NebulaPart"+index.ToString("D2")).GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).position;
            for(int i=1;i<=4;i++)for(int side=0;side<2;side++)
                Mount("DualHeavyTurbolaser_"+i+"_"+side,0,3,PartMuzzle(i)+Vector3.right*(side==0?-0.65f:.65f),"NebulaPart"+i.ToString("D2")+"/FP_01 with derived barrel offset");
            for(int i=9;i<=14;i++)Mount("MediumTurbolaser_"+i,0,4,PartMuzzle(i),"NebulaPart"+i.ToString("D2")+"/FP_01");
            for(int i=5;i<=8;i++)Mount("MediumIon_"+i,0,32,PartMuzzle(i),"NebulaPart"+i.ToString("D2")+"/FP_01");
            foreach(string n in new[]{"MUZZLEA_01","MUZZLEA_02"})Mount("AssaultConcussion_"+n,0,65,Bone(n).position,n);
            foreach(string n in new[]{"MUZZLEA_03","MUZZLEA_04","MUZZLEA_05","MUZZLEA_06"})Mount("PointDefense_"+n,0,14,Bone(n).position,n);
            for(int side=0;side<2;side++)
            {
                float x=(side==0?-1:1)*bounds.size.x*.07f;
                Mount("Engine_"+side,1,0,new Vector3(x,bounds.center.y,bounds.min.z+bounds.size.z*.04f),"Derived from stern hull bounds");
                Mount("ShieldGenerator_"+side,2,0,Bone("Shield_Gen").position+Vector3.right*x,"Shield_Gen with paired offset");
                Mount("Hangar_"+side,4,0,Bone("Hanger").position+Vector3.right*x,"Hanger with paired offset");
            }
            Mount("TractorBeam",7,0,Bone("MUZZLEA_00").position,"MUZZLEA_00");
            if(weapons.Count!=24 || targetable.Count!=31)throw new InvalidOperationException("Expected 24 weapons + 7 subsystem hardpoints.");
            var opaque = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit").ToArray();
            var visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Assign(Component(root,"Ship"),"explosionHullRenderers",opaque);
            Assign(health,"<ShipUnits>k__BackingField",targetable.ToArray());
            Assign(weaponComponent,"hardPoints",weapons.ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",targetable.Concat(weapons.Where(w=>!targetable.Contains(w))).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"renderers",visible.Cast<Renderer>().Concat(new Renderer[]{shield.GetComponent<MeshRenderer>(),root.GetComponent<LineRenderer>()}).ToArray());
            Assign(Component(root,"TeamColorView"),"meshRenderers",opaque);
            var so = new SerializedObject(health);
            so.FindProperty("ionFieldBounds.m_Center").vector3Value = bounds.center;
            so.FindProperty("ionFieldBounds.m_Extent").vector3Value = bounds.extents*1.05f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var collider = root.GetComponents<BoxCollider>().Single();
            collider.center = bounds.center; collider.size = bounds.size;
            var shieldMesh = shield.GetComponent<MeshFilter>().sharedMesh.bounds;
            shield.transform.localPosition = bounds.center;
            shield.transform.localRotation = Quaternion.identity;
            shield.transform.localScale = new Vector3(bounds.size.x/shieldMesh.size.x,bounds.size.y/shieldMesh.size.y,bounds.size.z/shieldMesh.size.z)*1.08f;
            var hangars=targetable.Where(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar).ToArray();
            var launches=hangars.Select((h,i)=>
            {
                var launch=new GameObject("NebulaLaunchPoint_"+i).transform;launch.SetParent(body,false);
                launch.localPosition=new Vector3(h.transform.localPosition.x,bounds.min.y-8,h.transform.localPosition.z);
                return launch;
            }).ToArray();
            so=new SerializedObject(hangarComponent);
            so.FindProperty("launchPoint").objectReferenceValue=launches[0];
            so.FindProperty("hangarHardPoint").objectReferenceValue=hangars[0];
            so.FindProperty("isDestroyable").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangarComponent,"bayHardPoints",hangars);Assign(hangarComponent,"bayLaunchPoints",launches);
            foreach (var image in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="SelectedImage")) image.sizeDelta=Vector2.one*160;
            var points = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            bottom=points.Min(p=>Mathf.Min((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y));
            top=points.Max(p=>Mathf.Max((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y));
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            System.IO.File.WriteAllText("Temp/NebulaImport/HardpointMapping.json",records.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        BuildVariant(PREVIEW,"Assets/Prefabs/Ui/Reinforcement/HomeOneReinforcementView.prefab",false,bounds);
        BuildVariant(WRECK,"Assets/Prefabs/Models/Wrecks/HomeOneWreckView.prefab",true,bounds);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA)==null) AssetDatabase.CopyAsset("Assets/Settings/Data/Models/Wrecks/Rebellion/HomeOneWreckData.asset",WRECK_DATA);
        var data = Load(WRECK_DATA);
        data.targetObject.name="NebulaWreckData";
        data.FindProperty("<Prefab>k__BackingField").objectReferenceValue=Component(AssetDatabase.LoadAssetAtPath<GameObject>(WRECK),"UnitWreckView");
        Save(data);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA)==null) AssetDatabase.CopyAsset("Assets/Settings/Data/Ship/Rebellion/HomeOneShipData.asset",DATA);
        data=Load(DATA); data.targetObject.name="NebulaShipData";
        Set(data,"Hull",16000); Set(data,"Shields",15000); Set(data,"Speed",20);
        Set(data,"ShieldRegenerateValue",25f); Set(data,"ShieldRegenerateDelay",3); Set(data,"HullBottom",bottom); Set(data,"HullTop",top);
        Set(data,"NavigationRadius",Mathf.Sqrt(bounds.extents.x*bounds.extents.x+bounds.extents.z*bounds.extents.z)+8); Set(data,"Range",600); Set(data,"HangarInitialDelay",4); Set(data,"HangarLaunchInterval",8);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=Load(WRECK_DATA).targetObject;
        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=5;
        int[] types={0,1,2,4,7};float[] values={750,1500,1250,1200,1000};
        for(int i=0;i<5;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=1;abilities.GetArrayElementAtIndex(0).intValue=38;
        var bays=data.FindProperty("hangarBays");bays.arraySize=2;
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);bay.FindPropertyRelative("squadronType").intValue=300;bay.FindPropertyRelative("reserve").intValue=i==0?3:2;bay.FindPropertyRelative("maxActive").intValue=i==0?2:1;}
        var loadout=data.FindProperty("weaponLoadout");loadout.arraySize=5;
        int[] profiles={3,4,32,65,14};int[] counts={8,6,4,2,4};
        for(int i=0;i<5;i++){var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=profiles[i];row.FindPropertyRelative("count").intValue=counts[i];}
        Save(data);AssetDatabase.SaveAssets();
        return "Nebula gameplay saved: 31 targetable hardpoints, 24 weapons, two X-Wing hangars; bounds "+bounds.size+"; banked range "+bottom+" .. "+top;
    }

    static void BuildVariant(string path,string donor,bool wreck,Bounds bounds)
    {
        if(wreck) { EmpireAtWar.Editor.Rendering.ShipWreckBuilder.Build(GAMEPLAY); return; }
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null) AssetDatabase.CopyAsset(donor,path);
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name=System.IO.Path.GetFileNameWithoutExtension(path);
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            foreach(var renderer in renderers.Where(r=>r.sharedMaterial.shader.name!="EmpireAtWar/Ship Lit")) renderer.enabled=false;
            renderers=renderers.Where(r=>r.enabled).ToArray();
            var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers) renderer.sharedMaterials=Enumerable.Repeat(hologram,renderer.sharedMaterials.Length).ToArray();
            var spawn=Component(root,"UnitSpawnView"); Assign(spawn,"meshRenderers",renderers);
            var spawnSettings=new SerializedObject(spawn); spawnSettings.FindProperty("hologramMaterial").objectReferenceValue=hologram; spawnSettings.ApplyModifiedPropertiesWithoutUndo();
            var collider=root.GetComponents<BoxCollider>().Single();collider.center=bounds.center;collider.size=bounds.size;collider.isTrigger=true;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static void Configure(HardPoint hp,int id,int type,int weapon,float arc)
    {
        var so=new SerializedObject(hp);
        so.FindProperty("<Id>k__BackingField").intValue=id;so.FindProperty("<HardPointType>k__BackingField").intValue=type;
        if(hp is WeaponHardPoint){so.FindProperty("<WeaponType>k__BackingField").intValue=weapon;so.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=-arc;so.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=arc;hp.transform.localRotation=Quaternion.Euler(0,hp.transform.localPosition.x>0?90:-90,0);}
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static MonoBehaviour Component(GameObject root,string type)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==type);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static void Set(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static void Assign(MonoBehaviour component,string field,UnityEngine.Object[] objects){var so=new SerializedObject(component);var array=so.FindProperty(field);array.arraySize=objects.Length;for(int i=0;i<objects.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static Bounds BoundsOf(GameObject root){var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds;}
}
