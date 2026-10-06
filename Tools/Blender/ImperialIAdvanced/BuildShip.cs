using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;

public static class BuildImperialShip
{
    const string VIEW="Assets/Prefabs/Models/Ships/ImperialIAdvancedShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ImperialIAdvancedShipData.asset";
    public static string Main()
    {
        Copy("Assets/Prefabs/Models/Ships/ImperatorShipView.prefab",VIEW);
        var root=PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;float hullBottom=0,hullTop=0;
        try
        {
            root.name="ImperialIAdvancedShipView";
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
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialIAdvanced.prefab"),body);
            var weapons=new List<WeaponHardPoint>();var targets=new List<HardPoint>();
            for(int i=1;i<=6;i++)AddWeapon("HeavyTurbolaser"+i,"ImperialIAdvancedTLD"+i.ToString("00")+"/FP_02",2,true);
            for(int i=1;i<=2;i++)AddWeapon("HeavyTurboIon"+i,"ImperialIAdvancedICD"+i.ToString("00")+"/FP_02",31,true);
            AddWeapon("MediumTurboIon1","IC_03_FP_01",32,true);AddWeapon("MediumTurboIon2","IC_04_FP_01",32,true);
            AddSystem("ShieldGenerator1","HP_Shield_Fire_01",2);
            AddSystem("ShieldGenerator2","HP_Shield_Fire_02",2);
            AddSystem("EngineLeft","HP_Engine_L",1);AddSystem("EngineRight","HP_Engine_R",1);
            AddSystem("TractorBeam","HP_TRAC_BONE_00",7);AddSystem("Hangar","SPAWN_00",4);
            for(int i=1;i<=3;i++)AddWeapon("MediumTripleTurbolaser"+i,"ImperialIAdvancedTLT"+i.ToString("00")+"/FP_01",4,false);
            for(int i=1;i<=4;i++)AddWeapon("LightTurbolaser"+i,"TL_"+i.ToString("00")+"_FP_01",33,false);
            foreach(string bone in new[]{"IC_01","IC_02","IC_05","IC_06"})AddWeapon("LaserCannon"+(weapons.Count-16),bone+"_FP_01",8,false);
            Assign(Component(root,"WeaponComponent"),"hardPoints",weapons.ToArray());
            Assign(Component(root,"HealthComponent"),"<ShipUnits>k__BackingField",targets.ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",targets.Concat(weapons.Where(w=>!targets.Contains(w))).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray());
            var hull=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            Assign(Component(root,"TeamColorView"),"meshRenderers",hull.Concat(new[]{shieldRenderer}).ToArray());
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
                var mount=new GameObject(name);mount.layer=hardPointLayer;mount.transform.SetParent(body,false);mount.transform.position=Bone(bone).position;mount.transform.rotation=root.transform.rotation;return mount;
            }
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
        Copy("Assets/Settings/Data/Ship/ImperatorShipData.asset",DATA);
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA));data.targetObject.name="ImperialIAdvancedShipData";
        SetFloat(data,"Hull",20000);SetFloat(data,"Shields",16000);SetFloat(data,"Speed",250);SetFloat(data,"ShieldRegenerateValue",20);
        SetFloat(data,"NavigationRadius",105);SetFloat(data,"HullBottom",hullBottom);SetFloat(data,"HullTop",hullTop);
        var abilities=data.FindProperty("abilities");abilities.arraySize=2;abilities.GetArrayElementAtIndex(0).intValue=21;abilities.GetArrayElementAtIndex(1).intValue=22;
        var bays=data.FindProperty("hangarBays");bays.arraySize=3;for(int i=0;i<3;i++){var p=bays.GetArrayElementAtIndex(i);p.FindPropertyRelative("squadronType").intValue=203+i;p.FindPropertyRelative("reserve").intValue=2;p.FindPropertyRelative("maxActive").intValue=1;}
        SetFloat(data,"HangarLaunchInterval",30);
        var hpHealth=data.FindProperty("hardPointHealth");hpHealth.arraySize=5;int[] types={0,1,2,4,7};float[] hitPoints={750,1000,1000,2000,1500};
        for(int i=0;i<5;i++){var p=hpHealth.GetArrayElementAtIndex(i);p.FindPropertyRelative("hardPointType").intValue=types[i];p.FindPropertyRelative("health").floatValue=hitPoints[i];p.FindPropertyRelative("hullDamageMultiplier").floatValue=1;}
        var wreck=ShipWreckBuilder.Build(VIEW);const string wreckPath="Assets/Settings/Data/Ship/Wreck/ImperialIAdvancedWreckData.asset";
        Copy("Assets/Settings/Data/Ship/Wreck/ImperatorWreckData.asset",wreckPath);var wd=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(wreckPath));wd.targetObject.name="ImperialIAdvancedWreckData";wd.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreck;Save(wd);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=wd.targetObject;Save(data);AssetDatabase.SaveAssets();
        return "Imperial I Advanced: 16 targetable hardpoints, 21 weapons, two hangar launches of each advanced squadron, shield shell and own wreck. Bounds "+bounds;
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static void Copy(string source,string target){if(!File.Exists(target))AssetDatabase.CopyAsset(source,target);}
    static void Assign(UnityEngine.Object obj,string name,UnityEngine.Object[] values){var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static void SetFloat(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static Bounds BoundsOf(Renderer[] renderers){var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
}
