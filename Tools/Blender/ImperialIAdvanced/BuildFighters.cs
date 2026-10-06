using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using EmpireAtWar.Components.Squadrons.Flight;

public static class BuildImperialFighters
{
    public static string Main()
    {
        string[] names={"TIEInterceptor","TIEBrute","TIEPunisher"};int[] counts={8,6,4};float[] hulls={15,50,55};
        for(int variant=0;variant<3;variant++)
        {
            string name=names[variant],donor=variant==2?"TIEBomber":"TIEFighter";
            string view="Assets/Prefabs/Models/Squadrons/"+name+"SquadronView.prefab";
            if(!File.Exists(view))AssetDatabase.CopyAsset("Assets/Prefabs/Models/Squadrons/"+donor+"SquadronView.prefab",view);
            var root=PrefabUtility.LoadPrefabContents(view);
            float radius=0;
            try
            {
                root.name=name+"SquadronView";
                var fighters=root.GetComponentsInChildren<FighterView>(true).OrderBy(f=>f.Id).ToList();
                while(fighters.Count>counts[variant]){UnityEngine.Object.DestroyImmediate(fighters.Last().gameObject);fighters.RemoveAt(fighters.Count-1);}
                while(fighters.Count<counts[variant])fighters.Add(UnityEngine.Object.Instantiate(fighters[0],fighters[0].transform.parent));
                var weapons=new List<WeaponHardPoint>();
                for(int member=0;member<fighters.Count;member++)
                {
                    var fighter=fighters[member];fighter.name=name+member;var so=new SerializedObject(fighter);
                    var body=(Transform)so.FindProperty("body").objectReferenceValue;
                    var trailTemplate=(TrailRenderer)so.FindProperty("engineTrails").GetArrayElementAtIndex(0).objectReferenceValue;
                    var trail=UnityEngine.Object.Instantiate(trailTemplate,body);
                    foreach(var old in fighter.GetComponentsInChildren<WeaponHardPoint>(true))UnityEngine.Object.DestroyImmediate(old.gameObject);
                    foreach(var old in body.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer || r is SkinnedMeshRenderer).ToArray())if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                    foreach(var old in fighter.GetComponentsInChildren<TrailRenderer>(true).Where(t=>t!=trail).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Squadrons/"+name+".prefab"),body);
                    var guns=new List<WeaponHardPoint>();
                    if(variant<2)for(int i=0;i<(variant==0?4:2);i++)AddGun("MuzzleA_"+i.ToString("00"),variant==0?15:21,false);
                    else
                    {
                        AddGun("MuzzleA_00",15,false);
                        AddGun("MuzzleB_00",22,true);AddGun("MuzzleB_02",22,true);
                        AddGun("MuzzleB_01",13,true);AddGun("MuzzleB_03",13,true);
                    }
                    so.FindProperty("id").intValue=member;so.FindProperty("gun").objectReferenceValue=guns[0];
                    var additional=so.FindProperty("additionalGuns");additional.arraySize=guns.Count-1;for(int i=1;i<guns.Count;i++)additional.GetArrayElementAtIndex(i-1).objectReferenceValue=guns[i];
                    var trails=so.FindProperty("engineTrails");trails.arraySize=1;trails.GetArrayElementAtIndex(0).objectReferenceValue=trail;
                    trail.name="EngineTrail";trail.transform.SetParent(body,false);trail.transform.localPosition=new Vector3(0,0,variant==0?-1.5f:-2);trail.startWidth=.12f;trail.endWidth=.015f;
                    var collider=(SphereCollider)so.FindProperty("hitCollider").objectReferenceValue;collider.center=Vector3.zero;collider.radius=variant==0?2:3.5f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    var slot=SquadronFormation.GetSlot(member,5);fighter.transform.localPosition=new Vector3(slot.X,slot.Y,slot.Z);
                    void AddGun(string bone,int type,bool warhead)
                    {
                        var anchor=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(bone,StringComparison.OrdinalIgnoreCase));
                        var mount=new GameObject((warhead?"Warhead":"Laser")+guns.Count);mount.transform.SetParent(body,false);mount.transform.position=anchor.position;mount.transform.rotation=fighter.transform.rotation;
                        WeaponHardPoint gun=warhead?(WeaponHardPoint)mount.AddComponent<FighterTorpedoHardPoint>():mount.AddComponent<WeaponHardPoint>();
                        var settings=new SerializedObject(gun);settings.FindProperty("<Id>k__BackingField").intValue=weapons.Count;settings.FindProperty("<WeaponType>k__BackingField").intValue=type;
                        settings.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=-90;settings.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=90;
                        settings.FindProperty("spawnDestroyedExplosion").boolValue=false;if(warhead)settings.FindProperty("fighterIndex").intValue=member;settings.ApplyModifiedPropertiesWithoutUndo();guns.Add(gun);weapons.Add(gun);
                    }
                }
                Assign(Component(root,"SquadronFlightComponent"),"fighters",fighters.ToArray());Assign(Component(root,"SquadronHealthComponent"),"fighters",fighters.ToArray());
                Assign(Component(root,"WeaponComponent"),"hardPoints",weapons.ToArray());Assign(Component(root,"FogVisibilityComponent"),"hardPoints",weapons.ToArray());
                Assign(Component(root,"FogVisibilityComponent"),"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray());
                Assign(Component(root,"TeamColorView"),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray());
                var hull=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var bounds=hull[0].bounds;foreach(var renderer in hull.Skip(1))bounds.Encapsulate(renderer.bounds);
                radius=Mathf.Ceil(new Vector2(Mathf.Max(Mathf.Abs(bounds.min.x),Mathf.Abs(bounds.max.x)),Mathf.Max(Mathf.Abs(bounds.min.z),Mathf.Abs(bounds.max.z))).magnitude);
                foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="SelectedImage"))rect.sizeDelta=Vector2.one*radius*2;
                PrefabUtility.SaveAsPrefabAsset(root,view);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            string dataPath="Assets/Settings/Data/Squadron/"+name+"SquadronData.asset";
            if(!File.Exists(dataPath))AssetDatabase.CopyAsset("Assets/Settings/Data/Squadron/"+donor+"SquadronData.asset",dataPath);
            var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dataPath));data.targetObject.name=name+"SquadronData";
            SetFloat(data,"MemberHull",hulls[variant]);SetFloat(data,"MemberShields",variant==2?30:0);SetFloat(data,"ShieldRegenerateValue",variant==2?.15f:0);
            SetFloat(data,"NavigationRadius",radius);
            SetFloat(data,"CruiseSpeed",variant==0?40:variant==1?30:24);SetFloat(data,"CombatSpeed",variant==0?45:variant==1?35:27);
            data.FindProperty("<Abilities>k__BackingField").arraySize=0;Save(data);
        }
        AssetDatabase.SaveAssets();return "Built eight TIE Interceptors, six TIE Brutes and four TIE Punishers per squadron; explicit fighter, collider, muzzle, weapon, fog and trail bindings.";
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static void Assign(UnityEngine.Object obj,string name,UnityEngine.Object[] values){var so=new SerializedObject(obj);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static void SetFloat(SerializedObject so,string name,float value)=>so.FindProperty("<"+name+">k__BackingField").floatValue=value;
}
