using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Editor;
using EmpireAtWar.Editor.Rendering;

public static class BuildISDIIReplacementShip
{
    const string VIEW="Assets/Prefabs/Models/Ships/ISDIIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ISDIIShipData.asset";
    public static string Main()
    {
        Copy("Assets/Prefabs/Models/Ships/ImperatorShipView.prefab",VIEW);
        var root=PrefabUtility.LoadPrefabContents(VIEW);
        Bounds bounds;float hullBottom=0,hullTop=0;
        try
        {
            root.name="ISDIIShipView";
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
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDII.prefab"),body);
            var weapons=new List<WeaponHardPoint>();var targets=new List<HardPoint>();
            var audit=JObject.Parse(File.ReadAllText("Temp/ISDIIReplacement/SourceAudit.json"));
            foreach(var hp in audit["hardpoints"])
            {
                string name=(string)hp["name"],kind=(string)hp["Type"];
                if(kind=="HARD_POINT_DUMMY_ART" || (string)hp["Fire_Projectile_Type"]=="Proj_Facing_Dummy")continue;
                var attachment=audit["attachments"].SingleOrDefault(t=>(string)t["hardpoint"]==name);
                string model=attachment==null?"":(string)attachment["variant"]+"/";
                if(kind.Contains("WEAPON"))
                {
                    int type=name.Contains("OTL")?70:name.Contains("QIC")?71:name.Contains("Center_TL")?4:kind.Contains("ION")?10:name.EndsWith("TL_01")||name.EndsWith("TL_02")?36:33;
                    AddWeapon(name,model+(string)hp["Fire_Bone_A"],type,true,float.Parse((string)hp["Fire_Cone_Width"],System.Globalization.CultureInfo.InvariantCulture),name.Contains("OTL"));
                }
                else AddSystem(name,(string)hp["Attachment_Bone"],kind.Contains("SHIELD")?2:kind.Contains("FIGHTER")?4:7);
            }
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
            hangar.FindProperty("hangarHardPoint").objectReferenceValue=targets.Single(h=>(int)h.HardPointType==4);hangar.ApplyModifiedPropertiesWithoutUndo();
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
                return search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase) && !t.Cast<Transform>().Any(c=>c.name.EndsWith("_Static")));
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
                var hp=Mount(name,bone).AddComponent<WeaponHardPoint>();Configure(hp,0,targets.Count,target);
                var so=new SerializedObject(hp);so.FindProperty("<WeaponType>k__BackingField").intValue=type;
                so.FindProperty("mainBattery").boolValue=mainBattery;
                float side=hp.transform.localPosition.x<0?-90:90;
                so.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=Mathf.Max(-180,side-cone/2);so.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=Mathf.Min(180,side+cone/2);
                so.ApplyModifiedPropertiesWithoutUndo();weapons.Add(hp);
            }
            void AddSystem(string name,string bone,int type){var hp=Mount(name,bone).AddComponent<HardPoint>();Configure(hp,type,targets.Count,true);}
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA));
        SetFloat(data,"Range",350);SetFloat(data,"HullBottom",hullBottom);SetFloat(data,"HullTop",hullTop);
        var hpHealth=data.FindProperty("hardPointHealth");
        for(int i=hpHealth.arraySize-1;i>=0;i--)if(hpHealth.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==1)hpHealth.DeleteArrayElementAtIndex(i);
        var loadout=data.FindProperty("weaponLoadout");
        var savedView=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var counts=savedView.GetComponentsInChildren<WeaponHardPoint>(true).GroupBy(h=>h.WeaponType).ToArray();
        loadout.arraySize=counts.Length;
        for(int i=0;i<counts.Length;i++) {var row=loadout.GetArrayElementAtIndex(i);row.FindPropertyRelative("weaponType").intValue=(int)counts[i].Key;row.FindPropertyRelative("count").intValue=counts[i].Count();}
        var wreck=ShipWreckBuilder.Build("Assets/Prefabs/Models/Wrecks/Source/ISDIIShipView.prefab");
        var wd=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Ship/Wreck/ISDIIWreckData.asset"));
        wd.FindProperty("<Prefab>k__BackingField").objectReferenceValue=wreck;Save(wd);
        data.FindProperty("<Wreck>k__BackingField").objectReferenceValue=wd.targetObject;Save(data);AssetDatabase.SaveAssets();
        return "ISD II: source loadout, every weapon targetable, refitted shield and source death-clone wreck. Existing balance preserved. Bounds "+bounds;
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static void Copy(string source,string target){if(!File.Exists(target))AssetDatabase.CopyAsset(source,target);}
    static void Assign(UnityEngine.Object obj,string name,UnityEngine.Object[] values){var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static void SetFloat(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
    static Bounds BoundsOf(Renderer[] renderers){var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
}
