using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;

public static class BuildEnduranceShip
{
    const string VIEW="Assets/Prefabs/Models/Ships/Rebellion/EnduranceShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/Rebellion/EnduranceShipData.asset";
    public static string Main()
    {
        Copy("Assets/Prefabs/Models/Ships/Rebellion/HomeOneShipView.prefab",VIEW);
        var root=PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;float hullBottom=0,hullTop=0;
        try
        {
            root.name="EnduranceShipView";
            var move=Component(root,"ShipMoveComponent");
            var body=(Transform)new SerializedObject(move).FindProperty("bodyTransform").objectReferenceValue;
            body.localScale=Vector3.one;
            var shield=Component(root,"Shield");
            var shieldRenderer=(Renderer)new SerializedObject(shield).FindProperty("shieldRenderer").objectReferenceValue;
            int hardPointLayer=root.GetComponentsInChildren<WeaponHardPoint>(true).First().gameObject.layer;
            foreach(var hp in root.GetComponentsInChildren<HardPoint>(true))UnityEngine.Object.DestroyImmediate(hp.gameObject);
            foreach(string controller in new[]{"WeaponComponent","HangarComponent","AudioShipComponent","AudioDialogShipComponent"})
            {
                var t=Component(root,controller).transform;if(t!=body && t.IsChildOf(body))t.SetParent(root.transform,true);
            }
            shieldRenderer.transform.SetParent(body,true);
            foreach(var child in body.Cast<Transform>().Where(t=>t!=shieldRenderer.transform).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r=>r!=shieldRenderer && (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray())
            {
                if(renderer==null)continue;
                var old=renderer.transform;while(old.parent!=root.transform)old=old.parent;UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Rebellion/Endurance.prefab"),body);
            var weapons=new List<WeaponHardPoint>();var targets=new List<HardPoint>();
            for(int i=0;i<4;i++) AddWeapon("MediumTurbolaser"+(i+1),"EndurancePart"+(i+2).ToString("00")+"/FP_01",4,true);
            AddWeapon("MediumIonCannon1","EndurancePart06/FP_01",32,true);
            AddWeapon("MediumIonCannon2","EndurancePart09/FP_01",32,true);
            for(int i=0;i<4;i++) AddWeapon("PointDefense"+(i+1),"EndurancePart"+(i+10).ToString("00")+"/FP_01",14,true);
            AddWeapon("PointDefense5","BL_02",14,true);
            AddWeapon("PointDefense6","BL_11",14,true);
            var opaque=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            var opaqueBounds=BoundsOf(opaque);
            AddSystem("ShieldGenerator","Shield_Gen",2);
            AddSystemAt("EngineLeft",new Vector3(-opaqueBounds.size.x*.2f,0,opaqueBounds.min.z+5),1);
            AddSystemAt("EngineRight",new Vector3(opaqueBounds.size.x*.2f,0,opaqueBounds.min.z+5),1);
            AddSystem("TractorBeamLeft","BL_05",7);
            AddSystem("TractorBeamRight","BL_08",7);
            AddSystem("CommandArray","EndurancePart00/Core",11);
            string[] bayBones={"Hanger","HB_02","HB_03","HB_04"};
            foreach(string bone in bayBones) AddSystem("Hangar_"+bone,bone,4);
            Assign(Component(root,"WeaponComponent"),"hardPoints",weapons.ToArray());
            Assign(Component(root,"HealthComponent"),"<ShipUnits>k__BackingField",targets.ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",targets.Concat(weapons.Where(w=>!targets.Contains(w))).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled || r==shieldRenderer).ToArray());
            var hull=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Assign(Component(root,"TeamColorView"),"meshRenderers",hull.Concat(new[]{shieldRenderer}).ToArray());
            Assign(Component(root,"Ship"),"explosionHullRenderers",hull.Where(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray());
            var solidHull=hull.Where(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            bounds=BoundsOf(solidHull);
            var box=root.GetComponents<BoxCollider>().Single();box.center=bounds.center;box.size=bounds.size;
            var hangarComponent=Component(root,"HangarComponent");
            var hangar=new SerializedObject(hangarComponent);
            var bays=targets.Where(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar).ToArray();
            var launches=new List<Transform>();
            foreach(var bay in bays)
            {
                var launch=new GameObject(bay.name+"LaunchPoint").transform;
                launch.SetParent(body,false);
                launch.position=new Vector3(bay.transform.position.x,bounds.min.y-8,bay.transform.position.z);
                launch.rotation=root.transform.rotation;
                launches.Add(launch);
            }
            hangar.FindProperty("launchPoint").objectReferenceValue=launches[0];
            hangar.FindProperty("hangarHardPoint").objectReferenceValue=bays[0];
            hangar.ApplyModifiedPropertiesWithoutUndo();
            Assign(hangarComponent,"bayHardPoints",bays);
            Assign(hangarComponent,"bayLaunchPoints",launches.ToArray());
            var points=solidHull.SelectMany(r=>r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v=>r.transform.TransformPoint(v))).ToArray();
            var banked=points.SelectMany(p=>new[]{p,Quaternion.Euler(0,0,5)*p,Quaternion.Euler(0,0,-5)*p}).ToArray();hullBottom=banked.Min(p=>p.y);hullTop=banked.Max(p=>p.y);
            var health=new SerializedObject(Component(root,"HealthComponent"));
            health.FindProperty("ionFieldBounds").boundsValue=bounds;health.ApplyModifiedPropertiesWithoutUndo();
            var fog=new SerializedObject(Component(root,"FogVisibilityComponent"));fog.ApplyModifiedPropertiesWithoutUndo();
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="SelectedImage"))rect.sizeDelta=new Vector2(bounds.size.z*1.17f,bounds.size.z*1.17f);
            ShieldHullBaker.Bake(root.transform,(EmpireAtWar.ViewComponents.Health.Shield)shield);
            PrefabUtility.SaveAsPrefabAsset(root,VIEW);
            Transform Bone(string name)
            {
                var pieces=name.Split('/');var search=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(pieces.Length==1?"EnduranceRig":pieces[0]));
                return search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase) && t.GetComponents<Component>().Length==1 && (pieces.Length>1 || !t.GetComponentsInParent<Transform>(true).Any(p=>p.name.StartsWith("EndurancePart"))));
            }
            GameObject Mount(string name,string bone)
            {
                return MountAt(name,Bone(bone).position);
            }
            GameObject MountAt(string name,Vector3 position){var mount=new GameObject(name);mount.layer=hardPointLayer;mount.transform.SetParent(body,false);mount.transform.position=position;mount.transform.rotation=root.transform.rotation;return mount;}
            void AddSystemAt(string name,Vector3 position,int type){var hp=MountAt(name,position).AddComponent<HardPoint>();Configure(hp,type,targets.Count,true);}
            void Configure(HardPoint hp,int type,int id,bool target)
            {
                var so=new SerializedObject(hp);so.FindProperty("<Id>k__BackingField").intValue=id;so.FindProperty("<HardPointType>k__BackingField").intValue=type;
                so.FindProperty("spawnDestroyedExplosion").boolValue=target;so.ApplyModifiedPropertiesWithoutUndo();
                if(target){var collider=hp.gameObject.AddComponent<SphereCollider>();collider.radius=type==0?3:4;targets.Add(hp);}
            }
            void AddWeapon(string name,string bone,int type,bool target)
            {
                var hp=Mount(name,bone).AddComponent<WeaponHardPoint>();Configure(hp,0,target?targets.Count:16+weapons.Count-10,target);
                var so=new SerializedObject(hp);so.FindProperty("<WeaponType>k__BackingField").intValue=type;
                so.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=-180;so.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=180;
                so.ApplyModifiedPropertiesWithoutUndo();weapons.Add(hp);
            }
            void AddSystem(string name,string bone,int type){var hp=Mount(name,bone).AddComponent<HardPoint>();Configure(hp,type,targets.Count,true);}
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        const string WRECK="Assets/Prefabs/Models/Wrecks/EnduranceWreckView.prefab";
        const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/EnduranceReinforcementView.prefab";
        const string WRECK_DATA="Assets/Settings/Data/Models/Wrecks/Rebellion/EnduranceWreckData.asset";
        Directory.CreateDirectory("Assets/Art/Materials/Wrecks/Endurance");AssetDatabase.Refresh();
        BuildVariant(PREVIEW,"Assets/Prefabs/Ui/Reinforcement/HomeOneReinforcementView.prefab",false,bounds);
        BuildVariant(WRECK,"Assets/Prefabs/Models/Wrecks/HomeOneWreckView.prefab",true,bounds);
        Copy("Assets/Settings/Data/Models/Wrecks/Rebellion/HomeOneWreckData.asset",WRECK_DATA);
        var wd=Load(WRECK_DATA);wd.targetObject.name="EnduranceWreckData";
        wd.FindProperty("<Prefab>k__BackingField").objectReferenceValue=Component(AssetDatabase.LoadAssetAtPath<GameObject>(WRECK),"UnitWreckView");Save(wd);
        Copy("Assets/Settings/Data/Ship/Rebellion/HomeOneShipData.asset",DATA);
        var data=Load(DATA);data.targetObject.name="EnduranceShipData";
        SetFloat(data,"Hull",9000);SetFloat(data,"Shields",7000);SetFloat(data,"Speed",18);
        SetFloat(data,"ShieldRegenerateValue",7000f/600f);SetFloat(data,"NavigationRadius",105);
        SetFloat(data,"HullBottom",hullBottom);SetFloat(data,"HullTop",hullTop);
        SetFloat(data,"HangarInitialDelay",4);SetFloat(data,"HangarLaunchInterval",8);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=wd.targetObject;
        var healthRows=data.FindProperty("hardPointHealth");healthRows.arraySize=6;
        int[] types={0,1,2,4,7,11};float[] healthValues={400,600,600,600,400,500};
        for(int i=0;i<types.Length;i++){var row=healthRows.GetArrayElementAtIndex(i);row.FindPropertyRelative("hardPointType").intValue=types[i];row.FindPropertyRelative("health").floatValue=healthValues[i];row.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var abilities=data.FindProperty("abilities");abilities.arraySize=2;abilities.GetArrayElementAtIndex(0).intValue=52;abilities.GetArrayElementAtIndex(1).intValue=22;
        var hangarRows=data.FindProperty("hangarBays");hangarRows.arraySize=4;
        int[] active={2,2,1,1};
        for(int i=0;i<4;i++){var row=hangarRows.GetArrayElementAtIndex(i);row.FindPropertyRelative("squadronType").intValue=300;row.FindPropertyRelative("maxActive").intValue=active[i];row.FindPropertyRelative("reserve").intValue=active[i]*2;}
        var saved=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var loadout=EmpireAtWar.Editor.AI.WeaponLoadoutBaker.DeriveLoadout(saved);
        var loadoutProperty=data.FindProperty("weaponLoadout");loadoutProperty.arraySize=loadout.Count;
        for(int i=0;i<loadout.Count;i++){var entry=loadoutProperty.GetArrayElementAtIndex(i);entry.FindPropertyRelative("weaponType").intValue=(int)loadout[i].WeaponType;entry.FindPropertyRelative("count").intValue=loadout[i].Count;}
        Save(data);AssetDatabase.SaveAssets();
        return "Endurance saved: 12 targetable weapons, 10 systems, 4 independent X-Wing hangars; 6 active + 6 replacement squadrons. Bounds "+bounds;
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static void Copy(string source,string target){if(!File.Exists(target))AssetDatabase.CopyAsset(source,target);}
    static void Assign(UnityEngine.Object obj,string name,UnityEngine.Object[] values){var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static void SetFloat(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static Bounds BoundsOf(Renderer[] renderers){var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}

    static void BuildVariant(string path,string donor,bool wreck,Bounds bounds)
    {
        Copy(donor,path);
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name=System.IO.Path.GetFileNameWithoutExtension(path);
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Rebellion/Endurance.prefab"),root.transform);
            PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var renderers=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            if(wreck)
            {
                foreach(var renderer in renderers.Where(r=>r.sharedMaterial.shader.name!="EmpireAtWar/Ship Lit"))renderer.enabled=false;
                renderers=renderers.Where(r=>r.enabled).ToArray();
                foreach(var source in renderers.SelectMany(r=>r.sharedMaterials).Distinct())
                {
                    string materialPath="Assets/Art/Materials/Wrecks/Endurance/"+source.name+"_Wreck.mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(material==null){material=new Material(source){shader=Shader.Find("EmpireAtWar/Ship Wreck")};AssetDatabase.CreateAsset(material,materialPath);}
                    foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m==source?material:m).ToArray();
                }
                var view=Component(root,"UnitWreckView");Assign(view,"meshRenderers",renderers);Assign(view,"meshFilters",renderers.Select(r=>r.GetComponent<MeshFilter>()).ToArray());
            }
            else
            {
                foreach(var renderer in renderers.Where(r=>r.sharedMaterial.shader.name!="EmpireAtWar/Ship Lit"))renderer.enabled=false;
                renderers=renderers.Where(r=>r.enabled).ToArray();
                var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
                foreach(var renderer in renderers)renderer.sharedMaterials=Enumerable.Repeat(hologram,renderer.sharedMaterials.Length).ToArray();
                Assign(Component(root,"UnitSpawnView"),"meshRenderers",renderers);var spawn=new SerializedObject(Component(root,"UnitSpawnView"));spawn.FindProperty("hologramMaterial").objectReferenceValue=hologram;spawn.ApplyModifiedPropertiesWithoutUndo();
                var collider=root.GetComponents<BoxCollider>().Single();collider.center=bounds.center;collider.size=bounds.size;collider.isTrigger=true;
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
}
