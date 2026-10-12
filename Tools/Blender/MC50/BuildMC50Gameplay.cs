using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EmpireAtWar.ViewComponents.Health;

public static class BuildMC50Gameplay
{
    const string VISUAL = "Assets/Prefabs/Models/Ships/Rebellion/MC50.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/Rebellion/MC50ShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/MC50ReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/MC50WreckView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Rebellion/MC50ShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Models/Wrecks/Rebellion/MC50WreckData.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Ship construction requires Edit Mode.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY)==null) AssetDatabase.CopyAsset("Assets/Prefabs/Models/Ships/Rebellion/HomeOneShipView.prefab",GAMEPLAY);
        var root = PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;
        float bottom, top;
        try
        {
            root.name = "MC50ShipView";
            var movement = Component(root,"ShipMoveComponent");
            var body = (Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            body.localScale=Vector3.one;
            body.localPosition=Vector3.zero;
            body.localRotation=Quaternion.identity;
            var health = Component(root,"HealthComponent");
            var shield = (MonoBehaviour)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            const string SHIELD_MESH="Assets/Art/Models/Shields/MC50Shield.asset";
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
            var hull = bones.Single(t=>t.name=="MC50Rig");
            Transform Bone(string name) => hull.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null);
            var weapons = new List<WeaponHardPoint>();
            var targetable = new List<HardPoint>();
            var mountGroup = new GameObject("Hardpoints").transform;
            mountGroup.SetParent(body,false);
            int id = 0;
            var records = new JArray();
            string[] sockets={"HP_03_FP_01","HP_04_FP_01","HP_01_FP_01","HP_01_FP_02","HP_02_FP_01","HP_02_FP_02","HP_03_FP_02","HP_04_FP_02","HP_01_Fire","HP_02_Fire","HP_03_Fire","HP_04_Fire","PE_MC_TIDE_S.001","PE_MC_TIDE_S.002"};
            int[] profiles={2,2,5,5,5,5,32,32,8,8,8,8,14,14};
            for(int i=0;i<sockets.Length;i++)
            {
                var mount=new GameObject("MC50Weapon"+i.ToString("D2")); mount.transform.SetParent(mountGroup,false);
                mount.transform.position=Bone(sockets[i]).position;
                var hp=mount.AddComponent<WeaponHardPoint>(); Configure(hp,id++,0,profiles[i],85);
                weapons.Add(hp); targetable.Add(hp);
                records.Add(new JObject{["id"]=hp.Id,["socket"]=sockets[i],["weaponProfile"]=profiles[i],["targetable"]=true});
            }
            foreach(var system in new[]{new {Name="Engine",Socket="PE_MC_TIDE_M",Type=1},new {Name="ShieldGenerator",Socket="Shield",Type=2}})
            {
                var mount=new GameObject(system.Name); mount.transform.SetParent(mountGroup,false); mount.transform.position=Bone(system.Socket).position;
                var hp=mount.AddComponent<HardPoint>(); Configure(hp,id++,system.Type,0,0); targetable.Add(hp);
                records.Add(new JObject{["id"]=hp.Id,["socket"]=system.Socket,["system"]=system.Name,["targetable"]=true});
            }
            if(weapons.Count!=14 || targetable.Count!=16) throw new InvalidOperationException("Expected 14 weapons + engine/shield.");
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
            // No hangar is specified by the requested Rebel units balance.
            UnityEngine.Object.DestroyImmediate(hangarComponent.gameObject);
            foreach (var image in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="SelectedImage")) image.sizeDelta=Vector2.one*160;
            var points = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            bottom=points.Min(p=>Mathf.Min((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y));
            top=points.Max(p=>Mathf.Max((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y));
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            System.IO.File.WriteAllText("Temp/MC50Import/HardpointMapping.json",records.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        BuildVariant(PREVIEW,"Assets/Prefabs/Ui/Reinforcement/HomeOneReinforcementView.prefab",false,bounds);
        BuildVariant(WRECK,"Assets/Prefabs/Models/Wrecks/HomeOneWreckView.prefab",true,bounds);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA)==null) AssetDatabase.CopyAsset("Assets/Settings/Data/Models/Wrecks/Rebellion/HomeOneWreckData.asset",WRECK_DATA);
        var data = Load(WRECK_DATA);
        data.targetObject.name="MC50WreckData";
        data.FindProperty("<Prefab>k__BackingField").objectReferenceValue=Component(AssetDatabase.LoadAssetAtPath<GameObject>(WRECK),"UnitWreckView");
        Save(data);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA)==null) AssetDatabase.CopyAsset("Assets/Settings/Data/Ship/Rebellion/HomeOneShipData.asset",DATA);
        data=Load(DATA); data.targetObject.name="MC50ShipData";
        Set(data,"Hull",4200); Set(data,"Shields",3000); Set(data,"Speed",27);
        data.FindProperty("<ShipClass>k__BackingField").intValue=8;
        Set(data,"ShieldRegenerateValue",20); Set(data,"ShieldRegenerateDelay",3); Set(data,"HullBottom",bottom); Set(data,"HullTop",top);
        Set(data,"NavigationRadius",65); Set(data,"Range",600); Set(data,"HangarInitialDelay",4); Set(data,"HangarLaunchInterval",8);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=Load(WRECK_DATA).targetObject;
        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=3;
        int[] types={0,1,2};float[] values={300,700,600};
        for(int i=0;i<3;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=1;abilities.GetArrayElementAtIndex(0).intValue=40;
        data.FindProperty("hangarBays").arraySize=0;
        var loadout=data.FindProperty("weaponLoadout"); loadout.arraySize=5;
        int[] profileIds={2,5,32,8,14}; int[] counts={2,4,2,4,2};
        for(int i=0;i<5;i++){var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=profileIds[i];row.FindPropertyRelative("count").intValue=counts[i];}
        Save(data);AssetDatabase.SaveAssets();
        return "MC50 gameplay saved: 16 targetable hardpoints, 14 weapons, no hangar; bounds "+bounds.size+"; banked range "+bottom+" .. "+top;
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
