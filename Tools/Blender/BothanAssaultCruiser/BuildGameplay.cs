using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EmpireAtWar.ViewComponents.Health;

public static class BuildBothanAssaultCruiserGameplay
{
    const string VISUAL = "Assets/Prefabs/Models/Ships/BothanAssaultCruiser.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/BothanAssaultCruiserShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/BothanAssaultCruiserReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/BothanAssaultCruiserWreckView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/BothanAssaultCruiserShipData.asset";
    const string WRECK_DATA = "Assets/Settings/Data/Ship/Wreck/BothanAssaultCruiserWreckData.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Ship construction requires Edit Mode.");
        if(AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY)==null) AssetDatabase.CopyAsset("Assets/Prefabs/Models/Ships/HomeOneShipView.prefab",GAMEPLAY);
        var root = PrefabUtility.LoadPrefabContents(GAMEPLAY);
        Bounds bounds;
        float bottom, top;
        try
        {
            root.name = "BothanAssaultCruiserShipView";
            var movement = Component(root,"ShipMoveComponent");
            var body = (Transform)new SerializedObject(movement).FindProperty("bodyTransform").objectReferenceValue;
            body.localScale=Vector3.one;
            body.localPosition=Vector3.zero;
            body.localRotation=Quaternion.identity;
            var health = Component(root,"HealthComponent");
            var shield = (MonoBehaviour)new SerializedObject(health).FindProperty("shieldView").objectReferenceValue;
            const string SHIELD_MESH="Assets/Art/Models/Shields/BothanAssaultCruiserShield.asset";
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
            var hull = bones.Single(t=>t.name=="BothanAssaultCruiserRig");
            Transform Bone(string name) => hull.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null);
            var weapons = new List<WeaponHardPoint>();
            var targetable = new List<HardPoint>();
            var audit = JObject.Parse(System.IO.File.ReadAllText("Temp/BothanAssaultCruiserImport/SourceAudit.json"));
            var mountGroup = new GameObject("Hardpoints").transform;
            mountGroup.SetParent(body,false);
            int id = 0;
            var records = new JArray();
            HardPoint Mount(string name, string bone, Vector3 offset, int type, int profile=0, float arc=90)
            {
                var mount = new GameObject(name); mount.transform.SetParent(mountGroup,false);
                mount.transform.position = Bone(bone).position;
                mount.transform.localPosition += offset;
                var hp = type==0 || type==8 ? (HardPoint)mount.AddComponent<WeaponHardPoint>() : mount.AddComponent<HardPoint>();
                Configure(hp,id++,type,profile,arc);
                targetable.Add(hp); if(hp is WeaponHardPoint weapon) weapons.Add(weapon);
                records.Add(new JObject{["id"]=hp.Id,["name"]=name,["bone"]=bone,["offsetUnits"]=new JArray(offset.x,offset.y,offset.z),["profile"]=profile,["targetable"]=true});
                return hp;
            }
            for(int side=0;side<2;side++)
            {
                string heavy=side==0?"HP_01":"HP_02", medium=side==0?"HP_07":"HP_08", torpedo=side==0?"HP_05":"HP_06";
                for(int pair=0;pair<2;pair++)
                {
                    var offset=new Vector3(0,0,pair==0?-3:3);
                    Mount("DualHeavyTurbolaser_"+side+"_"+pair,heavy,offset,0,3);
                    Mount("MediumTurbolaser_"+side+"_"+pair,medium,offset,0,4);
                    Mount("ProtonTorpedo_"+side+"_"+pair,torpedo,offset,0,48,100);
                }
                Mount("MediumIonCannon_"+side,side==0?"HP_03":"HP_04",Vector3.zero,0,32);
                Mount("PointDefense_"+side,medium,new Vector3(0,3,0),8,14,180);
                Mount("Engine_"+side,"HP_Engine",new Vector3(side==0?-6:6,0,0),1);
                Mount("Hangar_"+side,"HP_Hangar",new Vector3(side==0?-6:6,0,0),4);
            }
            Mount("ShieldGenerator","HP_Shield",Vector3.zero,2);
            Mount("TractorBeam","HP_Shield",new Vector3(0,-3,10),7);
            if(weapons.Count!=16 || targetable.Count!=22) throw new InvalidOperationException("Expected 16 weapons + six subsystems.");
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
            var hangarUnits=targetable.Where(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar).ToArray();
            var launches=hangarUnits.Select((bay,i)=>{
                var launch=new GameObject("BothanLaunchPoint"+i).transform; launch.SetParent(body,false);
                launch.localPosition=new Vector3(bay.transform.localPosition.x,bounds.min.y-8,bay.transform.localPosition.z);
                launch.localRotation=Quaternion.identity; return launch;
            }).ToArray();
            so=new SerializedObject(hangar);
            so.FindProperty("launchPoint").objectReferenceValue=launches[0];
            so.FindProperty("hangarHardPoint").objectReferenceValue=hangarUnits[0];
            so.FindProperty("isDestroyable").boolValue=true; so.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangar,"bayHardPoints",hangarUnits); Assign(hangar,"bayLaunchPoints",launches);
            foreach (var image in root.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="SelectedImage")) image.sizeDelta=Vector2.one*160;
            var points = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            bottom=points.Min(p=>Mathf.Min((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y));
            top=points.Max(p=>Mathf.Max((Quaternion.Euler(0,0,-5)*p).y,(Quaternion.Euler(0,0,5)*p).y));
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
            System.IO.File.WriteAllText("Temp/BothanAssaultCruiserImport/HardpointMapping.json",records.ToString());
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        BuildVariant(PREVIEW,"Assets/Prefabs/Ui/Reinforcement/HomeOneReinforcementView.prefab",false,bounds);
        BuildVariant(WRECK,"Assets/Prefabs/Models/Wrecks/HomeOneWreckView.prefab",true,bounds);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WRECK_DATA)==null) AssetDatabase.CopyAsset("Assets/Settings/Data/Ship/Wreck/HomeOneWreckData.asset",WRECK_DATA);
        var data = Load(WRECK_DATA);
        data.targetObject.name="BothanAssaultCruiserWreckData";
        data.FindProperty("<Prefab>k__BackingField").objectReferenceValue=Component(AssetDatabase.LoadAssetAtPath<GameObject>(WRECK),"UnitWreckView");
        Save(data);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA)==null) AssetDatabase.CopyAsset("Assets/Settings/Data/Ship/HomeOneShipData.asset",DATA);
        data=Load(DATA); data.targetObject.name="BothanAssaultCruiserShipData";
        Set(data,"Hull",7200); Set(data,"Shields",6500); Set(data,"Speed",22);
        data.FindProperty("<ShipClass>k__BackingField").intValue=8; data.FindProperty("<HeightTier>k__BackingField").intValue=2;
        Set(data,"ShieldRegenerateValue",6500f/2000f*13.333333f); Set(data,"ShieldRegenerateDelay",3); Set(data,"HullBottom",bottom); Set(data,"HullTop",top);
        Set(data,"NavigationRadius",90); Set(data,"Range",750); Set(data,"HangarInitialDelay",4); Set(data,"HangarLaunchInterval",8);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=Load(WRECK_DATA).targetObject;
        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=6;
        int[] types={0,1,2,4,7,8};float[] values={500,700,800,400,400,300};
        for(int i=0;i<6;i++){var row=hpHealth.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=values[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=2;abilities.GetArrayElementAtIndex(0).intValue=42;abilities.GetArrayElementAtIndex(1).intValue=22;
        var bays=data.FindProperty("hangarBays");bays.arraySize=2;
        int[] squadrons={300,3};
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);bay.FindPropertyRelative("squadronType").intValue=squadrons[i];bay.FindPropertyRelative("reserve").intValue=2;bay.FindPropertyRelative("maxActive").intValue=1;}
        var loadout=data.FindProperty("weaponLoadout"); loadout.arraySize=5;
        int[] profiles={3,4,32,48,14}; int[] counts={4,4,2,4,2};
        for(int i=0;i<5;i++){var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=profiles[i];row.FindPropertyRelative("count").intValue=counts[i];}
        Save(data);AssetDatabase.SaveAssets();
        return "Bothan gameplay saved: 22 targetable hardpoints, 16 weapons, independent X-Wing/Y-Wing hangars; bounds "+bounds.size+"; banked range "+bottom+" .. "+top;
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
