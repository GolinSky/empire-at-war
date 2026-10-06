using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using Object = UnityEngine.Object;

public static class YWingBomberGameplay
{
    private const string DATA = "Assets/Settings/Data/Squadron/YWingBomberSquadronData.asset";
    private const string VIEW = "Assets/Prefabs/Models/Squadrons/YWingBomberSquadronView.prefab";
    private const string VISUAL = "Assets/Prefabs/Models/Squadrons/YWingBomber.prefab";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/YWingBomberReinforcementView.prefab";

    public static object Main()
    {
        AssetDatabase.CopyAsset("Assets/Settings/Data/Squadron/YWingSquadronData.asset", DATA);
        var data = AssetDatabase.LoadAssetAtPath<SquadronData>(DATA); data.name = "YWingBomberSquadronData";
        var serialized = new SerializedObject(data);
        Float(serialized,"MemberHull",30); Float(serialized,"MemberShields",30);
        Float(serialized,"CruiseSpeed",30); Float(serialized,"CombatSpeed",30);
        Float(serialized,"NavigationRadius",18); Float(serialized,"FormationSpacing",4);
        Float(serialized,"ShieldRegenerateValue",0.15f); Float(serialized,"ShieldRegenerateDelay",1);
        Float(serialized,"LaserShieldDamageMultiplier",0.7f); Float(serialized,"HullRepairPerSecond",0);
        var abilities = serialized.FindProperty("<Abilities>k__BackingField");
        abilities.arraySize = 1; abilities.GetArrayElementAtIndex(0).intValue = (int)ShipAbilityId.IonShot;
        Save(serialized);
        BuildWeapons();
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Models/Squadrons/YWingSquadronView.prefab");
        root.name = "YWingBomberSquadronView";
        try
        {
            var fighters = root.GetComponentsInChildren<FighterView>(true).OrderBy(f=>f.Id).ToList();
            var sixth = Object.Instantiate(fighters[0].gameObject,root.transform);
            sixth.name = "Fighter5";
            var sixthView = sixth.GetComponent<FighterView>();
            var sixthData = new SerializedObject(sixthView); sixthData.FindProperty("id").intValue=5; sixthData.ApplyModifiedPropertiesWithoutUndo();
            fighters.Add(sixthView);
            var weapons = new List<WeaponHardPoint>();
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/YWingBomberIcon.png");
            foreach(var fighter in fighters)
            {
                var offset = SquadronFormation.GetSlot(fighter.Id,4);
                fighter.transform.localPosition = new Vector3(offset.X,offset.Y,offset.Z);
                var view = new SerializedObject(fighter);
                var body = (Transform)view.FindProperty("body").objectReferenceValue;
                foreach(Transform child in body.Cast<Transform>().Where(t=>t.GetComponentsInChildren<MeshFilter>(true).Length>0).ToArray()) Object.DestroyImmediate(child.gameObject);
                body.localRotation=Quaternion.identity; body.localScale=Vector3.one;
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),body);
                var transforms = visual.GetComponentsInChildren<Transform>(true);
                foreach(var gun in fighter.GetComponentsInChildren<WeaponHardPoint>(true)) Object.DestroyImmediate(gun.gameObject);
                var names = new[]{"MuzzleA_00","MuzzleB_00","MuzzleC_00","MuzzleC_01"};
                var types = new[]{WeaponType.LightDualLaser,WeaponType.HeavyDualIonStunner,WeaponType.MediumProtonTorpedo,WeaponType.MediumProtonTorpedo};
                var guns = new WeaponHardPoint[4];
                for(int i=0;i<4;i++)
                {
                    var go=new GameObject(types[i].ToString()+i); go.transform.SetParent(body,false);
                    go.transform.position=transforms.Single(t=>t.name==names[i]).position;
                    var gun=go.AddComponent<WeaponHardPoint>(); var gunData=new SerializedObject(gun);
                    gunData.FindProperty("<Id>k__BackingField").intValue=fighter.Id*4+i;
                    gunData.FindProperty("<WeaponType>k__BackingField").intValue=(int)types[i];
                    gunData.FindProperty("<HardPointType>k__BackingField").intValue=0;
                    gunData.FindProperty("spawnDestroyedExplosion").boolValue=false;
                    gunData.FindProperty("yAxisRange.<Min>k__BackingField").floatValue=i==1?-89.5f:-22.5f;
                    gunData.FindProperty("yAxisRange.<Max>k__BackingField").floatValue=i==1?89.5f:22.5f;
                    gunData.FindProperty("prewarmEffects").intValue=i<2?2:1;
                    gunData.ApplyModifiedPropertiesWithoutUndo(); guns[i]=gun; weapons.Add(gun);
                }
                view.FindProperty("gun").objectReferenceValue=guns[0]; List(view.FindProperty("additionalGuns"),guns.Skip(1).Cast<Object>().ToArray());
                var collider=(SphereCollider)view.FindProperty("hitCollider").objectReferenceValue; collider.center=Vector3.zero; collider.radius=2.3f;
                var trails=view.FindProperty("engineTrails");
                var engines=transforms.Where(t=>t.name=="PE_Ywing"||t.name=="PE_Ywing.001").ToArray();
                for(int i=0;i<trails.arraySize;i++)
                {
                    var trail=(TrailRenderer)trails.GetArrayElementAtIndex(i).objectReferenceValue;
                    trail.transform.position=engines[i].position; trail.startColor=new Color(1,0.2f,0.1f,0.7f); trail.endColor=Color.clear;
                }
                view.ApplyModifiedPropertiesWithoutUndo();
            }
            FieldList(root.GetComponent<SquadronHealthComponent>(),"fighters",fighters.Cast<Object>().ToArray());
            FieldList(root.GetComponent<SquadronFlightComponent>(),"fighters",fighters.Cast<Object>().ToArray());
            FieldList(root.GetComponentInChildren<WeaponComponent>(true),"hardPoints",weapons.Cast<Object>().ToArray());
            var fog=root.GetComponent<FogVisibilityComponent>();
            FieldList(fog,"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && !(r is ParticleSystemRenderer)).Cast<Object>().ToArray());
            FieldList(fog,"hardPoints",weapons.Cast<Object>().ToArray());
            FieldList(root.GetComponent<TeamColorView>(),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&!r.name.StartsWith("Plane")).Cast<Object>().ToArray());
            var selected=new SerializedObject(root.GetComponent<SelectionComponent>());
            ((UnityEngine.UI.Image)selected.FindProperty("selectedImage").objectReferenceValue).rectTransform.sizeDelta=new Vector2(36,36);
            var iconData=new SerializedObject(root.GetComponent<SquadronIconComponent>());
            foreach(var field in new[]{"iconImage","silhouetteImage"}) ((UnityEngine.UI.Image)iconData.FindProperty(field).objectReferenceValue).sprite=icon;
            PrefabUtility.SaveAsPrefabAsset(root,VIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        BuildPreview(); AssetDatabase.SaveAssets();
        return new { data=DATA,view=VIEW,preview=PREVIEW,members=6,weapons=24 };
    }

    private static void BuildWeapons()
    {
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset")); var list=data.FindProperty("weapons");
        AddWeapon(list,15,46,"Light Dual Laser",5,2,0.08f,0.785f,50);
        AddWeapon(list,10,47,"Heavy Dual Ion Stunner",4,2,0.08f,4.91f,70);
        AddWeapon(list,22,48,"Medium Proton-Torpedo Launcher",90,1,0,20,70);
        Save(data);
    }

    private static void AddWeapon(SerializedProperty list,int source,int type,string name,float damage,int shots,float interval,float reload,float range)
    {
        int index=Enumerable.Range(0,list.arraySize).Single(i=>list.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==source);
        list.InsertArrayElementAtIndex(index); var p=list.GetArrayElementAtIndex(index);
        p.FindPropertyRelative("weaponType").intValue=type; p.FindPropertyRelative("displayName").stringValue=name;
        p.FindPropertyRelative("damage").floatValue=damage; p.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        p.FindPropertyRelative("shotInterval").floatValue=interval; p.FindPropertyRelative("reload").floatValue=reload; p.FindPropertyRelative("range").floatValue=range;
        if(type==46) p.FindPropertyRelative("color").colorValue=new Color(1,0.2f,0.1f,1);
    }

    private static void BuildPreview()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/Reinforcement/YWingReinforcementView.prefab"); root.name="YWingBomberReinforcementView";
        try
        {
            foreach(Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            for(int i=0;i<6;i++)
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
                var slot=SquadronFormation.GetSlot(i,4); visual.transform.localPosition=new Vector3(slot.X,slot.Y,slot.Z);
                var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true)) if(renderer.enabled) renderer.sharedMaterials=Enumerable.Repeat(hologram,renderer.sharedMaterials.Length).ToArray();
            }
            FieldList(root.GetComponent<UnitSpawnView>(),"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).Cast<Object>().ToArray());
            var filters=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>().enabled).ToArray();
            var bounds=new Bounds(filters[0].transform.TransformPoint(filters[0].sharedMesh.bounds.center),Vector3.zero);
            foreach(var filter in filters)
            {
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++) bounds.Encapsulate(filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            }
            var collider=root.GetComponent<BoxCollider>(); collider.center=bounds.center; collider.size=bounds.size;
            root.transform.localScale=Vector3.one; PrefabUtility.SaveAsPrefabAsset(root,PREVIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Float(SerializedObject data,string field,float value) => data.FindProperty("<"+field+">k__BackingField").floatValue=value;
    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
    private static void List(SerializedProperty list,Object[] values) { list.arraySize=values.Length; for(int i=0;i<values.Length;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; }
    private static void FieldList(Object target,string name,Object[] values) { var data=new SerializedObject(target); List(data.FindProperty(name),values); data.ApplyModifiedPropertiesWithoutUndo(); }
}
