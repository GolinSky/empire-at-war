using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.Squadrons.Data;

public static class BuildXWingGameplay
{
    const string VISUAL="Assets/Prefabs/Models/Squadrons/XWing.prefab";
    const string GAMEPLAY="Assets/Prefabs/Models/Squadrons/XWingSquadronView.prefab";
    const string PLACEMENT="Assets/Prefabs/Ui/Reinforcement/XWingReinforcementView.prefab";
    const string DATA="Assets/Settings/Data/Squadron/XWingSquadronData.asset";
    const string MODEL="Assets/Art/Models/RebellionShips/XWing/XWing.fbx";

    public static string Main()
    {
        if(!System.IO.File.Exists(GAMEPLAY))AssetDatabase.CopyAsset("Assets/Prefabs/Models/Squadrons/V19TorrentSquadronView.prefab",GAMEPLAY);
        var root=PrefabUtility.LoadPrefabContents(GAMEPLAY);
        float radius=0;
        Mesh previewMesh=null;
        try
        {
            root.name="XWingSquadronView";
            var fighters=root.GetComponentsInChildren<FighterView>(true).OrderBy(f=>f.Id).ToArray();
            if(fighters.Length!=5)throw new Exception("Expected five donor fighters.");
            var weapons=new System.Collections.Generic.List<WeaponHardPoint>();
            var animations=new System.Collections.Generic.List<Animation>();
            var open=AssetDatabase.LoadAllAssetsAtPath(MODEL).OfType<AnimationClip>().Single(c=>c.name=="OpenSFoils");
            var closed=AssetDatabase.LoadAllAssetsAtPath(MODEL).OfType<AnimationClip>().Single(c=>c.name=="CloseSFoils");
            foreach(var fighter in fighters)
            {
                var config=new SerializedObject(fighter);
                var body=(Transform)config.FindProperty("body").objectReferenceValue;
                var trailTemplate=UnityEngine.Object.Instantiate((TrailRenderer)config.FindProperty("engineTrails").GetArrayElementAtIndex(0).objectReferenceValue,body);
                var gunTemplate=UnityEngine.Object.Instantiate(fighter.GetComponentsInChildren<WeaponHardPoint>(true).First().gameObject,body);
                foreach(var gun in fighter.GetComponentsInChildren<WeaponHardPoint>(true).Where(g=>g.gameObject!=gunTemplate).ToArray())UnityEngine.Object.DestroyImmediate(gun.gameObject);
                var oldVisuals=body.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer || r is SkinnedMeshRenderer).Select(r=>{var t=r.transform;while(t.parent!=body)t=t.parent;return t.gameObject;}).Distinct().ToArray();
                foreach(var old in oldVisuals)UnityEngine.Object.DestroyImmediate(old);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),body);
                var bones=visual.GetComponentsInChildren<Transform>(true);
                var animation=visual.GetComponentsInChildren<Animation>(true).Single();
                animation.playAutomatically=false;
                open.SampleAnimation(animation.gameObject,open.length);
                animations.Add(animation);
                var guns=new WeaponHardPoint[4];
                for(int i=0;i<4;i++)
                {
                    var mount=i==0?gunTemplate:UnityEngine.Object.Instantiate(gunTemplate,body);
                    mount.name="LaserCannon"+i;
                    mount.transform.SetParent(bones.Single(t=>t.name=="MuzzleA_0"+i),false);
                    mount.transform.localPosition=Vector3.zero;
                    mount.transform.rotation=body.rotation;
                    var gun=mount.GetComponents<WeaponHardPoint>().Single();
                    var settings=new SerializedObject(gun);
                    settings.FindProperty("<Id>k__BackingField").intValue=fighter.Id*4+i;
                    settings.FindProperty("<WeaponType>k__BackingField").intValue=24;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    guns[i]=gun;weapons.Add(gun);
                }
                config.FindProperty("gun").objectReferenceValue=guns[0];
                var additional=config.FindProperty("additionalGuns");additional.arraySize=3;
                for(int i=0;i<3;i++)additional.GetArrayElementAtIndex(i).objectReferenceValue=guns[i+1];
                var trails=config.FindProperty("engineTrails");
                foreach(var trail in fighter.GetComponentsInChildren<TrailRenderer>(true).Where(t=>t!=trailTemplate).ToArray())UnityEngine.Object.DestroyImmediate(trail.gameObject);
                trails.arraySize=4;
                var hull=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="X_Wing_LOD1");
                for(int i=0;i<4;i++)
                {
                    var wing=bones.Single(t=>t.name=="bone_wing_0"+i);
                    var section=wing.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="XWing_"+wing.name);
                    var nozzle=section.sharedMesh.vertices.Select(v=>section.transform.TransformPoint(v)).ToArray();
                    float rear=nozzle.Min(p=>p.z);
                    var ring=nozzle.Where(p=>Mathf.Abs(p.z-rear)<.001f).ToArray();
                    var center=ring.Aggregate(Vector3.zero,(sum,p)=>sum+p)/ring.Length;
                    var trail=i==0?trailTemplate:UnityEngine.Object.Instantiate(trailTemplate,body);
                    trail.name="EngineTrail"+i;trail.transform.SetParent(wing,true);trail.transform.position=center;
                    trail.startWidth=.09f;trail.endWidth=.01f;
                    trails.GetArrayElementAtIndex(i).objectReferenceValue=trail;
                }
                var collider=(SphereCollider)config.FindProperty("hitCollider").objectReferenceValue;
                collider.center=Vector3.zero;collider.radius=2.5f;
                config.ApplyModifiedPropertiesWithoutUndo();
                var slot=SquadronFormation.GetSlot(fighter.Id,5);
                fighter.transform.localPosition=new Vector3(slot.X,slot.Y,slot.Z);
                if(previewMesh==null){previewMesh=new Mesh();hull.BakeMesh(previewMesh);previewMesh.name="XWingOpenPreviewMesh";}
            }
            var view=root.GetComponents<SFoilsView>().Length==0?root.AddComponent<SFoilsView>():root.GetComponents<SFoilsView>().Single();
            var sf=new SerializedObject(view);
            sf.FindProperty("opened").objectReferenceValue=open;
            sf.FindProperty("closed").objectReferenceValue=closed;
            sf.ApplyModifiedPropertiesWithoutUndo();Assign(view,"animations",animations.ToArray());
            Assign(root.GetComponents<SquadronFlightComponent>().Single(),"fighters",fighters);
            Assign(root.GetComponents<SquadronHealthComponent>().Single(),"fighters",fighters);
            Assign(root.GetComponentsInChildren<WeaponComponent>(true).Single(),"hardPoints",weapons.ToArray());
            Assign(root.GetComponents<FogVisibilityComponent>().Single(),"hardPoints",weapons.ToArray());
            Assign(root.GetComponents<FogVisibilityComponent>().Single(),"renderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray());
            Assign(root.GetComponents<TeamColorView>().Single(),"meshRenderers",root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray());
            var bounds=BoundsOf(root);
            radius=Mathf.Ceil(new Vector2(Mathf.Max(Mathf.Abs(bounds.min.x),Mathf.Abs(bounds.max.x)),Mathf.Max(Mathf.Abs(bounds.min.z),Mathf.Abs(bounds.max.z))).magnitude);
            foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="SelectedImage"))rect.sizeDelta=Vector2.one*radius*2;
            PrefabUtility.SaveAsPrefabAsset(root,GAMEPLAY);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        UnityEngine.Object.DestroyImmediate(previewMesh);
        if(!System.IO.File.Exists(DATA))AssetDatabase.CopyAsset("Assets/Settings/Data/Squadron/V19TorrentSquadronData.asset",DATA);
        var data=AssetDatabase.LoadAssetAtPath<SquadronData>(DATA);data.name="XWingSquadronData";
        var so=new SerializedObject(data);
        string[] fields={"NavigationRadius","CruiseSpeed","CombatSpeed","Acceleration","TurnRate","MaxBankAngle","BankResponse","FormationSpacing","LoiterRadius","BreakDistance","ExtendDistance","MemberHull","MemberShields","ShieldRegenerateValue","ShieldRegenerateDelay","HullRepairPerSecond","Height","Range"};
        float[] values={radius,20,32,22,110,40,5,5,18,5,30,60,20,3,1,0,11,70};
        for(int i=0;i<fields.Length;i++)so.FindProperty("<"+fields[i]+">k__BackingField").floatValue=values[i];
        var abilities=so.FindProperty("<Abilities>k__BackingField");abilities.arraySize=1;abilities.GetArrayElementAtIndex(0).intValue=18;
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data);
        if(!System.IO.File.Exists(PLACEMENT))AssetDatabase.CopyAsset("Assets/Prefabs/Ui/Reinforcement/V19TorrentReinforcementView.prefab",PLACEMENT);
        root=PrefabUtility.LoadPrefabContents(PLACEMENT);
        try
        {
            root.name="XWingReinforcementView";
            foreach(var child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            for(int i=0;i<5;i++)
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),root.transform);
                PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                var slot=SquadronFormation.GetSlot(i,5);visual.transform.localPosition=new Vector3(slot.X,slot.Y,slot.Z);
                foreach(var animation in visual.GetComponentsInChildren<Animation>(true))UnityEngine.Object.DestroyImmediate(animation);
                foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToArray())
                {
                    if(!skin.enabled){UnityEngine.Object.DestroyImmediate(skin.gameObject);continue;}
                    throw new Exception("Placement must use the exact rigid hull sections.");
                }
                foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))renderer.sharedMaterials=Enumerable.Repeat(hologram,renderer.sharedMaterials.Length).ToArray();
            }
            var bounds=BoundsOf(root);var box=root.GetComponents<BoxCollider>().Single();box.center=bounds.center;box.size=bounds.size;
            var spawn=root.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
            Assign(spawn,"meshRenderers",root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray());
            var config=new SerializedObject(spawn);config.FindProperty("height").floatValue=data.Height;config.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,PLACEMENT);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return "Saved five X-Wings, twenty moving muzzles, twenty engine trails, S-Foils bindings, data and five-craft placement. Radius "+radius;
    }
    static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Assign(UnityEngine.Object target,string name,UnityEngine.Object[] values){var so=new SerializedObject(target);var p=so.FindProperty(name);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
}
