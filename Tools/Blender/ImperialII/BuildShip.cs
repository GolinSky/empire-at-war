using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;

public static class BuildImperialIIShip
{
    const string VIEW="Assets/Prefabs/Models/Ships/ImperialIIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ImperialIIShipData.asset";
    public static string Main()
    {
        Copy("Assets/Prefabs/Models/Ships/ImperatorShipView.prefab",VIEW);
        var root=PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;float hullBottom=0,hullTop=0;
        try
        {
            root.name="ImperialIIShipView";
            var move=Component(root,"ShipMoveComponent");
            var body=(Transform)new SerializedObject(move).FindProperty("bodyTransform").objectReferenceValue;
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
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialII.prefab"),body);
            var weapons=new List<WeaponHardPoint>();var targets=new List<HardPoint>();
            for(int i=1;i<=8;i++)AddWeapon("RapidDualTurbolaser"+i,"ImperialIITLO"+i.ToString("00")+"/FP_02",41,true,100,true);
            for(int i=1;i<=2;i++)AddWeapon("LongRangeTurboIon"+i,"ImperialIIICQ"+i.ToString("00")+"/FP_01",42,true,80,false);
            AddSystem("ShieldGenerator1","HP_Shield_Fire_01",2);
            AddSystem("ShieldGenerator2","HP_Shield_Fire_02",2);
            AddSystem("EngineLeft","HP_Engine_L",1);AddSystem("EngineMiddle","HP_Engine_M",1);AddSystem("EngineRight","HP_Engine_R",1);
            AddSystem("TractorBeam","HP_TRAC_BONE_00",7);AddSystem("Hangar","SPAWN_00",4);
            for(int i=1;i<=3;i++)AddWeapon("MediumTripleTurbolaser"+i,"ImperialIITLT"+i.ToString("00")+"/FP_01",4,false,110,false);
            for(int i=1;i<=4;i++)AddWeapon("LightTurbolaser"+i,"TL_"+i.ToString("00")+"_FP_01",33,false,160,false);
            AddWeapon("LightTurbolaser5","IC_03_FP_01",33,false,160,false);AddWeapon("LightTurbolaser6","IC_04_FP_01",33,false,160,false);
            int laserIndex=0;
            foreach(string bone in new[]{"IC_01","IC_02","IC_05","IC_06"})AddWeapon("HeavyLaserCannon"+(++laserIndex),bone+"_FP_01",28,false,200,false);
            Assign(Component(root,"WeaponComponent"),"hardPoints",weapons.ToArray());
            Assign(Component(root,"HealthComponent"),"<ShipUnits>k__BackingField",targets.ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",targets.Concat(weapons.Where(w=>!targets.Contains(w))).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray());
            var hull=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Assign(Component(root,"TeamColorView"),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true).ToArray());
            Assign(Component(root,"Ship"),"explosionHullRenderers",hull.Where(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray());
            var hangar=new SerializedObject(Component(root,"HangarComponent"));
            var launch=new GameObject("HangarLaunchPoint");launch.transform.SetParent(body,false);launch.transform.position=Bone("SPAWN_00").position;launch.transform.rotation=root.transform.rotation;
            hangar.FindProperty("launchPoint").objectReferenceValue=launch.transform;
            hangar.FindProperty("hangarHardPoint").objectReferenceValue=targets.Last();hangar.ApplyModifiedPropertiesWithoutUndo();
            var solidHull=hull.Where(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            bounds=BoundsOf(solidHull);
            var box=root.GetComponents<BoxCollider>().Single();box.center=bounds.center;box.size=bounds.size;
            launch.transform.position=new Vector3(launch.transform.position.x,bounds.min.y-8,launch.transform.position.z);
            var points=solidHull.SelectMany(r=>r.GetComponents<MeshFilter>().Single().sharedMesh.vertices.Select(v=>r.transform.TransformPoint(v))).ToArray();
            var banked=points.SelectMany(p=>new[]{p,Quaternion.Euler(0,0,5)*p,Quaternion.Euler(0,0,-5)*p}).ToArray();hullBottom=banked.Min(p=>p.y);hullTop=banked.Max(p=>p.y);
            var health=new SerializedObject(Component(root,"HealthComponent"));
            health.FindProperty("ionFieldBounds").boundsValue=bounds;health.ApplyModifiedPropertiesWithoutUndo();
            var fog=new SerializedObject(Component(root,"FogVisibilityComponent"));fog.FindProperty("revealRadius").floatValue=105;fog.ApplyModifiedPropertiesWithoutUndo();
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="SelectedImage"))rect.sizeDelta=new Vector2(210,210);
            ShieldHullBaker.Bake(root.transform,(EmpireAtWar.ViewComponents.Health.Shield)shield);
            PrefabUtility.SaveAsPrefabAsset(root,VIEW);
            Transform Bone(string name)
            {
                var pieces=name.Split('/');var search=pieces.Length==1?visual.transform:visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pieces[0]);
                return search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase));
            }
            GameObject Mount(string name,string bone)
            {
                var mount=new GameObject(name);mount.layer=hardPointLayer;mount.transform.SetParent(body,false);mount.transform.position=Bone(bone).position;mount.transform.rotation=body.rotation;return mount;
            }
            void Configure(HardPoint hp,int type,int id,bool target)
            {
                var so=new SerializedObject(hp);so.FindProperty("<Id>k__BackingField").intValue=id;so.FindProperty("<HardPointType>k__BackingField").intValue=type;
                so.FindProperty("spawnDestroyedExplosion").boolValue=target;so.ApplyModifiedPropertiesWithoutUndo();
                if(target){var collider=hp.gameObject.AddComponent<SphereCollider>();collider.radius=type==0?3:4;targets.Add(hp);}
            }
            void AddWeapon(string name,string bone,int type,bool target,float cone,bool mainBattery)
            {
                var hp=Mount(name,bone).AddComponent<WeaponHardPoint>();Configure(hp,0,target?targets.Count:17+weapons.Count-10,target);
                var so=new SerializedObject(hp);so.FindProperty("<WeaponType>k__BackingField").intValue=type;
                so.FindProperty("mainBattery").boolValue=mainBattery;
                float side=hp.transform.localPosition.x<0?-90:90;
                so.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=Mathf.Max(-180,side-cone/2);so.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=Mathf.Min(180,side+cone/2);
                so.ApplyModifiedPropertiesWithoutUndo();weapons.Add(hp);
            }
            void AddSystem(string name,string bone,int type){var hp=Mount(name,bone).AddComponent<HardPoint>();Configure(hp,type,targets.Count,true);}
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        Copy("Assets/Settings/Data/Ship/ImperatorShipData.asset",DATA);
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA));data.targetObject.name="ImperialIIShipData";
        SetFloat(data,"Hull",21000);SetFloat(data,"Shields",18000);SetFloat(data,"Speed",25);SetFloat(data,"Range",525);SetFloat(data,"ShieldRegenerateValue",20);
        SetFloat(data,"NavigationRadius",105);SetFloat(data,"HullBottom",hullBottom);SetFloat(data,"HullTop",hullTop);
        var abilities=data.FindProperty("abilities");abilities.arraySize=2;abilities.GetArrayElementAtIndex(0).intValue=24;abilities.GetArrayElementAtIndex(1).intValue=22;
        var bays=data.FindProperty("hangarBays");bays.arraySize=3;for(int i=0;i<3;i++){var p=bays.GetArrayElementAtIndex(i);p.FindPropertyRelative("squadronType").intValue=203+i;p.FindPropertyRelative("reserve").intValue=2;p.FindPropertyRelative("maxActive").intValue=1;}
        SetFloat(data,"HangarLaunchInterval",30);
        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=5;int[] types={0,1,2,4,7};float[] hitPoints={750,1000,1000,2000,1500};
        for(int i=0;i<5;i++){var p=hpHealth.GetArrayElementAtIndex(i);p.FindPropertyRelative("hardPointType").intValue=types[i];p.FindPropertyRelative("health").floatValue=hitPoints[i];p.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var wreck=ShipWreckBuilder.Build(VIEW);
        var wreckRoot=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Models/Wrecks/ImperialIIWreckView.prefab");
        try
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Materials/Wrecks/ImperialII"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid), renamed=path.Replace("ImperialIAdvanced","ImperialII_Common");
                if(path==renamed)continue;
                var replacement=AssetDatabase.LoadAssetAtPath<Material>(renamed);
                if(replacement!=null)
                {
                    var original=AssetDatabase.LoadAssetAtPath<Material>(path);
                    foreach(var renderer in wreckRoot.GetComponentsInChildren<MeshRenderer>(true))
                        renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m==original?replacement:m).ToArray();
                    AssetDatabase.DeleteAsset(path);
                }
                else { string error=AssetDatabase.MoveAsset(path,renamed);if(error.Length>0)throw new InvalidOperationException(error); }
            }
            PrefabUtility.SaveAsPrefabAsset(wreckRoot,"Assets/Prefabs/Models/Wrecks/ImperialIIWreckView.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(wreckRoot);}
        const string wreckPath="Assets/Settings/Data/Ship/Wreck/ImperialIIWreckData.asset";
        Copy("Assets/Settings/Data/Ship/Wreck/ImperatorWreckData.asset",wreckPath);var wd=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(wreckPath));wd.targetObject.name="ImperialIIWreckData";wd.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreck;Save(wd);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=wd.targetObject;Save(data);AssetDatabase.SaveAssets();
        return "Imperial II: 17 targetable hardpoints, 23 weapons, two hangar launches of each squadron, shield shell and own wreck. Bounds "+bounds;
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static void Copy(string source,string target){if(!File.Exists(target))AssetDatabase.CopyAsset(source,target);}
    static void Assign(UnityEngine.Object obj,string name,UnityEngine.Object[] values){var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static void SetFloat(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static Bounds BoundsOf(Renderer[] renderers){var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
}
