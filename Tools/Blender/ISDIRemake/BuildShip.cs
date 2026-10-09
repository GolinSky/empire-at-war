using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;

public static class BuildISDIRemakeShip
{
    const string VIEW="Assets/Prefabs/Models/Ships/ISDIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ISDIShipData.asset";
    public static string Main()
    {
        Copy("Assets/Prefabs/Models/Ships/ImperatorShipView.prefab",VIEW);
        var root=PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;float hullBottom=0,hullTop=0;
        try
        {
            root.name="ISDIShipView";
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
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDI.prefab"),body);
            var weapons=new List<WeaponHardPoint>();var targets=new List<HardPoint>();
            AddWeapon("HP_ISD_1_New_HICT_01","ISDIPart01/FP_01",31,true);
            AddWeapon("HP_ISD_1_New_HICT_02","ISDIPart02/FP_01",31,true);
            AddWeapon("HP_ISD_1_New_HTLT_01","ISDIPart03/FP_02",2,true);
            AddWeapon("HP_ISD_1_New_HTLT_02","ISDIPart04/FP_02",2,true);
            AddWeapon("HP_ISD_1_New_HTLT_03","ISDIPart05/FP_02",2,true);
            AddWeapon("HP_ISD_1_New_HTLT_04","ISDIPart06/FP_02",2,true);
            AddWeapon("HP_ISD_1_New_HTLT_05","ISDIPart07/FP_02",2,true);
            AddWeapon("HP_ISD_1_New_HTLT_06","ISDIPart08/FP_02",2,true);
            AddWeapon("HP_ISD_1_New_LC_01","IC_01_FP_01",8,true);
            AddWeapon("HP_ISD_1_New_LC_02","IC_02_FP_01",8,true);
            AddWeapon("HP_ISD_1_New_LC_03","IC_03_FP_01",8,true);
            AddWeapon("HP_ISD_1_New_LC_04","IC_04_FP_01",8,true);
            AddWeapon("HP_ISD_1_New_LC_05","IC_05_FP_01",8,true);
            AddWeapon("HP_ISD_1_New_LC_06","IC_06_FP_01",8,true);
            AddWeapon("HP_ISD_1_New_TL_01","TL_01_FP_01",33,true);
            AddWeapon("HP_ISD_1_New_TL_02","TL_02_FP_01",33,true);
            AddWeapon("HP_ISD_1_New_TL_03","TL_03_FP_01",33,true);
            AddWeapon("HP_ISD_1_New_TL_04","TL_04_FP_01",33,true);
            AddWeapon("HP_ISD_1_New_TL_05","TL_05_FP_01",33,true);
            AddWeapon("HP_ISD_Center_TL_01","ISDIPart09/FP_03",4,true);
            AddWeapon("HP_ISD_Center_TL_02","ISDIPart10/FP_03",4,true);
            AddWeapon("HP_ISD_Center_TL_03","ISDIPart11/FP_03",4,true);
            AddWeapon("HP_ISD_New_QIC_01","ISDIPart12/FP_01",32,true);
            AddWeapon("HP_ISD_New_QIC_02","ISDIPart13/FP_01",32,true);
            var opaque=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
            var opaqueBounds=BoundsOf(opaque);
            var shieldPosition=Bone("HP_SG").position;
            AddSystemAt("ShieldGenerator1",shieldPosition+Vector3.left*opaqueBounds.size.x*.06f,2);
            AddSystemAt("ShieldGenerator2",shieldPosition+Vector3.right*opaqueBounds.size.x*.06f,2);
            var engine=visual.GetComponentsInChildren<MeshFilter>(true).Single(f=>f.name=="Engine_Glow_Big");
            var enginePoints=engine.sharedMesh.vertices.Select(v=>engine.transform.TransformPoint(v)).ToArray();
            foreach(int side in new[]{-1,1}){var engineSidePoints=enginePoints.Where(p=>side*p.x>opaqueBounds.size.x*.08f).ToArray();var position=engineSidePoints.Aggregate(Vector3.zero,(sum,p)=>sum+p)/engineSidePoints.Length;AddSystemAt(side<0?"EngineLeft":"EngineRight",position,1);}
            AddSystem("TractorBeam","HP_TRAC_BONE_00",7);AddSystem("Hangar","SPAWN_00",4);
            Assign(Component(root,"WeaponComponent"),"hardPoints",weapons.ToArray());
            Assign(Component(root,"HealthComponent"),"<ShipUnits>k__BackingField",targets.ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"hardPoints",targets.Concat(weapons.Where(w=>!targets.Contains(w))).ToArray());
            Assign(Component(root,"FogVisibilityComponent"),"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled || r==shieldRenderer).ToArray());
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
            var fog=new SerializedObject(Component(root,"FogVisibilityComponent"));fog.ApplyModifiedPropertiesWithoutUndo();
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="SelectedImage"))rect.sizeDelta=new Vector2(bounds.size.z*1.17f,bounds.size.z*1.17f);
            ShieldHullBaker.Bake(root.transform,(EmpireAtWar.ViewComponents.Health.Shield)shield);
            PrefabUtility.SaveAsPrefabAsset(root,VIEW);
            Transform Bone(string name)
            {
                var pieces=name.Split('/');var search=pieces.Length==1?visual.transform:visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pieces[0]);
                return search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase));
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
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA));
        SetFloat(data,"HullBottom",hullBottom);SetFloat(data,"HullTop",hullTop);
        var wreck=ShipWreckBuilder.Build("Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab");
        var wd=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Ship/Wreck/ISDIWreckData.asset"));
        wd.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreck;Save(wd);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=wd.targetObject;
        var saved=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var loadout=EmpireAtWar.Editor.AI.WeaponLoadoutBaker.DeriveLoadout(saved);
        var loadoutProperty=data.FindProperty("weaponLoadout");loadoutProperty.arraySize=loadout.Count;
        for(int i=0;i<loadout.Count;i++){var entry=loadoutProperty.GetArrayElementAtIndex(i);entry.FindPropertyRelative("weaponType").intValue=(int)loadout[i].WeaponType;entry.FindPropertyRelative("count").intValue=loadout[i].Count;}
        Save(data);AssetDatabase.SaveAssets();
        return "Remake ISD I: 24 targetable weapons and six retained systems, existing balance/complement/abilities, fitted shield and dedicated source wreck. Bounds "+bounds;
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static void Copy(string source,string target){if(!File.Exists(target))AssetDatabase.CopyAsset(source,target);}
    static void Assign(UnityEngine.Object obj,string name,UnityEngine.Object[] values){var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static void SetFloat(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static Bounds BoundsOf(Renderer[] renderers){var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
}
