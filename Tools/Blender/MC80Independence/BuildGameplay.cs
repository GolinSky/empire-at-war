using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EmpireAtWar.ViewComponents.Health;

public static class BuildMC80IndependenceGameplay
{
    const string VISUAL = "Assets/Prefabs/Models/Ships/MC80Independence.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/MC80IndependenceReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/MC80IndependenceWreckView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/MC80IndependenceShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Ship/Wreck/MC80IndependenceWreckData.asset";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Independence construction requires Edit Mode.");
        AssetDatabase.CopyAsset("Assets/Prefabs/Models/Ships/HomeOneShipView.prefab",GAMEPLAY);
        var root = PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;
        float bottom, top;
        try
        {
            root.name = "MC80IndependenceShipView";
            var movement = Component(root,"ShipMoveComponent");
            var body = (Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            var health = Component(root,"HealthComponent");
            var shield = (MonoBehaviour)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            const string SHIELD_MESH="Assets/Art/Models/Shields/MC80IndependenceShield.asset";
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
            Transform Bone(string name) => bones.Single(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase));
            var weapons = new List<WeaponHardPoint>();
            var targetable = new List<HardPoint>();
            var audit = JObject.Parse(System.IO.File.ReadAllText("Temp/MC80IndependenceImport/SourceAudit.json"));
            var mountGroup = new GameObject("Hardpoints").transform;
            mountGroup.SetParent(body,false);
            int id = 0;
            var records = new JArray();
            foreach (var source in audit["hardpoints"].Where(s=>(string)s["Is_Targetable"]=="Yes" && !((string)s["name"]).Contains("_Hull")))
            {
                string name = (string)source["name"];
                int type = name.Contains("Engines") ? 1 : name.Contains("Hangar") ? 4 : 0;
                var mount = new GameObject(name);
                mount.transform.SetParent(mountGroup,false);
                mount.transform.position = Bone((string)source["Fire_Bone_A"] ?? (string)source["Attachment_Bone"]).position;
                var hp = type==0 ? (HardPoint)mount.AddComponent<WeaponHardPoint>() : mount.AddComponent<HardPoint>();
                Configure(hp,id++,type,type==0 ? (name.Contains("_MTIC_")?56:55) : 0,80);
                targetable.Add(hp);
                if (hp is WeaponHardPoint) weapons.Add((WeaponHardPoint)hp);
                records.Add(new JObject{["id"]=hp.Id,["name"]=name,["bone"]=(string)source["Attachment_Bone"],["muzzle"]=(string)source["Fire_Bone_A"],["targetable"]=true});
            }
            foreach (var source in audit["hardpoints"].Where(s=>(string)s["Is_Targetable"]=="No" && s["Fire_Projectile_Type"]!=null))
            {
                string name = (string)source["name"];
                var mount = new GameObject(name);
                mount.transform.SetParent(mountGroup,false);
                mount.transform.position = Bone((string)source["Fire_Bone_A"]).position;
                var weapon = mount.AddComponent<WeaponHardPoint>();
                int type = name.Contains("_LTL_") ? 33 : name.Contains("_HLC_") ? 28 : 57;
                Configure(weapon,id++,0,type,80);
                var weaponSettings=new SerializedObject(weapon);weaponSettings.FindProperty("spawnDestroyedExplosion").boolValue=false;weaponSettings.ApplyModifiedPropertiesWithoutUndo();
                weapons.Add(weapon);
                records.Add(new JObject{["id"]=weapon.Id,["name"]=name,["muzzle"]=(string)source["Fire_Bone_A"],["targetable"]=false});
            }
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
            var hangar = Component(root,"HangarComponent");
            var hangarTargets = targetable.Where(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar).ToArray();
            var launches = new Transform[hangarTargets.Length];
            for(int i=0;i<hangarTargets.Length;i++)
            {
                launches[i]=new GameObject("MC80LaunchPoint"+(i+1)).transform;
                launches[i].SetParent(body,false);
                launches[i].localPosition=new Vector3(hangarTargets[i].transform.localPosition.x,bounds.min.y-8,hangarTargets[i].transform.localPosition.z);
            }
            so = new SerializedObject(hangar);
            so.FindProperty("launchPoint").objectReferenceValue=launches[0];
            so.FindProperty("hangarHardPoint").objectReferenceValue=hangarTargets[0];
            so.FindProperty("isDestroyable").boolValue=true;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangar,"bayHardPoints",hangarTargets);
            Assign(hangar,"bayLaunchPoints",launches);
            foreach (var image in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="SelectedImage")) image.sizeDelta=Vector2.one*240;
            var points = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            bottom=points.Min(p=>Mathf.Min(p.y,Mathf.Min((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y)));
            top=points.Max(p=>Mathf.Max(p.y,Mathf.Max((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y)));
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            System.IO.File.WriteAllText("Temp/MC80IndependenceImport/HardpointMapping.json",records.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        BuildVariant(PREVIEW,"Assets/Prefabs/Ui/Reinforcement/HomeOneReinforcementView.prefab",false,bounds);
        BuildVariant(WRECK,"Assets/Prefabs/Models/Wrecks/HomeOneWreckView.prefab",true,bounds);
        AssetDatabase.CopyAsset("Assets/Settings/Data/Ship/Wreck/HomeOneWreckData.asset",WRECK_DATA);
        var data = Load(WRECK_DATA);
        data.targetObject.name="MC80IndependenceWreckData";
        data.FindProperty("<Prefab>k__BackingField").objectReferenceValue=Component(AssetDatabase.LoadAssetAtPath<GameObject>(WRECK),"UnitWreckView");
        Save(data);
        AssetDatabase.CopyAsset("Assets/Settings/Data/Ship/HomeOneShipData.asset",DATA);
        data=Load(DATA); data.targetObject.name="MC80IndependenceShipData";
        Set(data,"Hull",30000); Set(data,"Shields",40000); Set(data,"Speed",17);
        Set(data,"ShieldRegenerateValue",80); Set(data,"HullBottom",bottom); Set(data,"HullTop",top);
        Set(data,"NavigationRadius",120); Set(data,"Range",500); Set(data,"HangarInitialDelay",4); Set(data,"HangarLaunchInterval",15);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=Load(WRECK_DATA).targetObject;
        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=3;
        int[] types={0,1,4};float[] values={1500,4000,1100};
        for(int i=0;i<3;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=1;abilities.GetArrayElementAtIndex(0).intValue=26;
        var bays=data.FindProperty("hangarBays");bays.arraySize=3;
        int[] squadrons={300,3,1};
        for(int i=0;i<3;i++){var bay=bays.GetArrayElementAtIndex(i);bay.FindPropertyRelative("squadronType").intValue=squadrons[i];bay.FindPropertyRelative("reserve").intValue=3;bay.FindPropertyRelative("maxActive").intValue=1;}
        Save(data);AssetDatabase.SaveAssets();
        return "MC80Independence gameplay saved: 20 targetable hardpoints, 36 weapons, three X-Wing/Y-Wing/A-Wing bays; bounds "+bounds.size+"; banked range "+bottom+" .. "+top;
    }

    static void BuildVariant(string path,string donor,bool wreck,Bounds bounds)
    {
        if(wreck)
        {
            EmpireAtWar.Editor.Rendering.ShipWreckBuilder.Build(GAMEPLAY);
            return;
        }
        AssetDatabase.CopyAsset(donor,path);
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name=System.IO.Path.GetFileNameWithoutExtension(path);
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
            PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            foreach(var renderer in renderers.Where(r=>r.sharedMaterial.shader.name!="EmpireAtWar/Ship Lit"))renderer.enabled=false;
            renderers=renderers.Where(r=>r.enabled).ToArray();
            var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            foreach(var renderer in renderers)renderer.sharedMaterials=Enumerable.Repeat(hologram,renderer.sharedMaterials.Length).ToArray();
            Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);
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
