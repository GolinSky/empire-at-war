using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Editor.Rendering;

public static class VerifyISDIRemake
{
    private const string TASK = "Temp/ISDIRemakeImport/";
    private const string VIEW = "Assets/Prefabs/Models/Ships/ISDIShipView.prefab";
    private const string DATA = "Assets/Settings/Data/Ship/ISDIShipData.asset";
    private static readonly List<string> _checks = new List<string>();
    public static string Main()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorUtility.scriptCompilationFailed,"Editor idle in Edit Mode");
        var baseline = JObject.Parse(File.ReadAllText(TASK + "Baseline.json"));
        foreach(var pair in ((JObject)baseline["guids"]).Properties()) Check(AssetDatabase.AssetPathToGUID(pair.Name)==(string)pair.Value,"Retained GUID "+pair.Name);
        var dataAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA);
        var before = JObject.Parse((string)baseline["dataJson"])["MonoBehaviour"];
        var after = JObject.Parse(EditorJsonUtility.ToJson(dataAsset))["MonoBehaviour"];
        foreach(var property in ((JObject)before).Properties().Where(p=>!new[]{"<HullBottom>k__BackingField","<HullTop>k__BackingField","weaponLoadout"}.Contains(p.Name)))
            Check(JToken.DeepEquals(property.Value,after[property.Name]),"Retained balance/dependency "+property.Name);
        var data=new SerializedObject(dataAsset);
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDI.prefab");
        var targets=References(Config(root,"HealthComponent").FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        Check(targets.Length==30 && targets.Select(p=>p.Id).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,30)),"30 unique targetable hardpoints");
        Check(targets.All(p=>p.GetComponents<Collider>().Length==1),"Every hardpoint has a target collider");
        var guns=References(Config(root,"WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(guns.Length==24 && guns.All(targets.Contains),"All 24 weapons in health list");
        foreach(var group in new[]{new{type=2,count=6},new{type=4,count=3},new{type=8,count=6},new{type=31,count=2},new{type=32,count=2},new{type=33,count=5}})
            Check(guns.Count(g=>(int)g.WeaponType==group.type)==group.count,"Weapon count "+group.type);
        var healthTypes=data.FindProperty("hardPointHealth");
        foreach(var type in targets.Select(t=>(int)t.HardPointType).Distinct()) Check(Enumerable.Range(0,healthTypes.arraySize).Any(i=>healthTypes.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==type && healthTypes.GetArrayElementAtIndex(i).FindPropertyRelative("health").floatValue>0),"Hardpoint health type "+type);
        var body=(Transform)Config(root,"ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(body.localScale==Vector3.one && targets.All(t=>t.transform.parent==body),"Hardpoints share identity banking pivot");
        var mappings=JArray.Parse(File.ReadAllText(TASK+"GameplayMapping.json"));float mountError=0;
        var model=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="ISDI" && t.parent.name=="Geometry");
        foreach(var mapping in mappings)
        {
            var hp=guns.Single(g=>g.name==(string)mapping["source"]);
            var pieces=((string)mapping["bone"]).Split('/');var search=pieces.Length==1?model:model.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pieces[0]);
            var bone=search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase));
            mountError=Mathf.Max(mountError,Vector3.Distance(hp.transform.position,bone.position));
        }
        Check(mountError<.0001f,"Gameplay muzzles match source bones");
        var source=JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"));
        foreach(var attachment in source["attachments"])
        {
            var attached=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)attachment["variant"]);
            Check(attached.parent.name.Equals((string)attachment["bone"],StringComparison.OrdinalIgnoreCase) && attached.localPosition.sqrMagnitude<.0000001f,"Attachment parent/position "+attached.name);
            Check(Vector3.Distance(attached.localScale,Vector3.one*.01f)<.000001f && Quaternion.Angle(attached.localRotation,Quaternion.Euler(90,0,0))<.001f,"FBX attachment basis "+attached.name);
        }
        var geometry=new List<object>();
        foreach(var pair in ((JObject)source["variants"]).Properties())
        {
            string name=pair.Name;var report=JObject.Parse(File.ReadAllText(TASK+name+"/ConversionReport.json"));
            var raw=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/ISDI/"+name+".fbx");
            var rendererMeshes=raw.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
            Check(rendererMeshes.Length==((JObject)report["after"]["meshes"]).Count,"Imported mesh count "+name);
            int total=0;
            foreach(var renderer in rendererMeshes)
            {
                var mesh=MeshOf(renderer);var expected=report["after"]["meshes"][renderer.name];
                Check(mesh.triangles.Length/3==(int)expected["triangles"] && mesh.uv.Length==mesh.vertexCount,"Imported triangles/UVs "+name+"/"+renderer.name);
                Check(renderer.sharedMaterials.Length==((JArray)expected["materials"]).Count && mesh.subMeshCount==renderer.sharedMaterials.Length,"Material slots "+name+"/"+renderer.name);
                for(int i=0;i<renderer.sharedMaterials.Length;i++)Check(renderer.sharedMaterials[i].name.StartsWith((string)report["before"]["meshes"][renderer.name]["materials"][i]+"_"),"Original shader slot mapping "+name+"/"+renderer.name);
                total+=mesh.triangles.Length/3;
            }
            float boneError=0;var bones=raw.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponents<Renderer>().Length==0).ToArray();
            foreach(var bone in ((JObject)report["before"]["bones"]).Properties())
            {
                var actual=bones.Single(t=>t.name==bone.Name);var p=bone.Value["head"];var expected=new Vector3(-(float)p[0],(float)p[2],-(float)p[1])*.02f;
                boneError=Mathf.Max(boneError,Vector3.Distance(actual.position,expected));
                var parent=(string)bone.Value["parent"];if(parent!=null)Check(actual.parent.name==parent,"Bone hierarchy "+name+"/"+bone.Name);
            }
            Check(boneError<.0001f,"Unity source bone positions "+name);
            Check((float)report["geometry_error"]<.001f && (float)report["bone_error"]<.001f,"Blender round trip "+name);
            geometry.Add(new{name,meshes=rendererMeshes.Length,triangles=total,bones=((JObject)report["before"]["bones"]).Count,maximumUnityBoneError=boneError});
        }
        Check(Entry("Assets/Settings/Data/Ship/ShipsData.asset","shipsData.keyValue",201).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Unique ship-data registration");
        foreach(var path in new[]{VIEW,DATA})
        {
            var guid=AssetDatabase.AssetPathToGUID(path);var key=Path.GetFileNameWithoutExtension(path);
            Check(Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue==guid,"Unique asset mapping "+key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Existing Addressable "+key);
        }
        foreach(var key in new[]{"TIEInterceptorSquadronView","TIEInterceptorSquadronData","TIEBruteSquadronView","TIEBruteSquadronData","TIEPunisherSquadronView","TIEPunisherSquadronData"})
        {
            var guid=Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue;
            Check(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid))!=null,"Retained hangar dependency "+key);
        }
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId);string iconKey=iconGuid+":"+localId;
        var unit=Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset","ships.keyValue",201);
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && unit.FindPropertyRelative("iconKey").stringValue==iconKey,"Faction icon");
        Check(unit.FindPropertyRelative("<Price>k__BackingField").intValue==4500 && unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue==5 && unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue==40,"Retained faction economy");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","shipIconWrapper.keyValue",201).objectReferenceValue==icon,"Battle icon");
        var icons=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/TooltipIconData.asset")).FindProperty("icons");
        Check(Enumerable.Range(0,icons.arraySize).Select(i=>icons.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Unique tooltip icon");
        var spawn=Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnShipWrapper.keyValue",201).objectReferenceValue;
        string previewPath=AssetDatabase.GetAssetPath(spawn);Check(previewPath=="Assets/Prefabs/Ui/Reinforcement/ISDIReinforcementView.prefab","Unique placement registration");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(previewPath);var spawnConfig=new SerializedObject(spawn);
        var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
        Check(spawnConfig.FindProperty("hologramMaterial").objectReferenceValue==hologram && References(spawnConfig.FindProperty("meshRenderers")).Cast<MeshRenderer>().All(r=>r.enabled && r.sharedMaterials.All(m=>m==hologram)),"Explicit placement hologram bindings");
        Check(preview.GetComponents<BoxCollider>().Single().isTrigger && preview.GetComponents<Rigidbody>().Single().isKinematic && !preview.GetComponents<Rigidbody>().Single().useGravity,"Placement trigger/body");
        var opaque=Visible(root).Where(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();var bounds=BoundsOf(opaque);
        Check(Math.Abs(bounds.size.z-(float)baseline["length"])<.01f && root.transform.localScale==Vector3.one && visual.transform.localScale==Vector3.one && preview.transform.localScale==Vector3.one,"Retained gameplay length and identity roots");
        Check((BoundsOf(Visible(preview)).size-bounds.size).magnitude<.01f,"Placement/hull dimensions");
        var collider=root.GetComponents<BoxCollider>().Single();Check((collider.size-bounds.size).magnitude<.01f && Vector3.Distance(collider.center,bounds.center)<.01f,"Fitted hull collider");
        var ionBounds=Config(root,"HealthComponent").FindProperty("ionFieldBounds").boundsValue;Check((ionBounds.size-bounds.size).magnitude<.01f,"Fitted ion bounds");
        var hangar=Config(root,"HangarComponent");var exit=(Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        Check(hangar.FindProperty("isDestroyable").boolValue && targets.Contains((HardPoint)hangar.FindProperty("hangarHardPoint").objectReferenceValue) && exit.position.y<bounds.min.y && exit.position.y<Number(data,"HullBottom"),"Targetable hangar with launch outside banked hull");
        var owners=References(Config(root,"TeamColorView").FindProperty("meshRenderers"));var fog=References(Config(root,"FogVisibilityComponent").FindProperty("renderers"));
        Check(Visible(root).All(r=>owners.Contains(r) && fog.Contains(r)),"All visible meshes/effects have ownership and fog bindings");
        Check(opaque.All(r=>References(Config(root,"Ship").FindProperty("explosionHullRenderers")).Contains(r)),"Opaque explosion hull bindings");
        Check(targets.All(t=>References(Config(root,"FogVisibilityComponent").FindProperty("hardPoints")).Contains(t)),"Hardpoint fog bindings");
        var shield=(Renderer)Config(root,"Shield").FindProperty("shieldRenderer").objectReferenceValue;Check(shield.transform.IsChildOf(body) && owners.Contains(shield) && fog.Contains(shield),"Explicit banking shield ownership");
        var profiles=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");foreach(var gun in guns)Check(profiles.GetProfile(gun.WeaponType).ShotPrefab!=null,"Existing shot profile "+gun.name);
        var catalog=AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");Check(catalog.Get((ShipAbilityId)21).Settings is BoostEnginePowerSettings,"Retained engine boost ability");
        var tractor=(TractorBeamSettings)catalog.Get((ShipAbilityId)22).Settings;Check(tractor.SpeedMultiplier>0 && tractor.SpeedMultiplier<1 && tractor.Beam.ShotPrefab is EmpireAtWar.ViewComponents.Weapon.BeamShot,"Retained tractor ability");
        var derived=EmpireAtWar.Editor.AI.WeaponLoadoutBaker.DeriveLoadout(root);var loadout=data.FindProperty("weaponLoadout");
        Check(loadout.arraySize==derived.Count && Enumerable.Range(0,loadout.arraySize).All(i=>loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==(int)derived[i].WeaponType && loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue==derived[i].Count),"AI loadout matches mounted weapons");
        var wreckData=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Ship/Wreck/ISDIWreckData.asset"));var wreckObject=wreckData.FindProperty("<Prefab>k__BackingField").objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(wreckObject)=="Assets/Prefabs/Models/Wrecks/ISDIWreckView.prefab","Dedicated source wreck reference");
        var wreck=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(wreckObject));
        Check(Visible(wreck).All(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Wreck" && m.GetTexture("_BaseMap")!=null)),"Textured opaque source wreck");
        var wreckView=Config(wreck,"UnitWreckView");Check(References(wreckView.FindProperty("meshRenderers")).Length==Visible(wreck).Length && References(wreckView.FindProperty("meshFilters")).Length==Visible(wreck).Length,"Wreck renderer/filter bindings");
        var paths=new[]{VIEW,"Assets/Prefabs/Models/Ships/ISDI.prefab",previewPath,"Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab","Assets/Prefabs/Models/Wrecks/ISDIWreckView.prefab"};
        Check(UnitHelperMeshStripper.FindHelpers(paths,Array.Empty<string>()).All(h=>h.BlockedBy!=null),"No strippable helper meshes");
        foreach(string path in paths)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var t in prefab.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script "+path+"/"+t.name);
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))
            {
                var iterator=new SerializedObject(component).GetIterator();
                while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)Check(iterator.objectReferenceValue!=null || iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)),"No broken reference "+path+"/"+iterator.propertyPath);
            }
            foreach(var r in Visible(prefab)) Check(r.sharedMaterials.All(m=>m!=null && m.shader!=null),"Material references "+r.name);
            foreach(var dependency in AssetDatabase.GetDependencies(path,true))Check(!AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath(dependency)).Contains("Obsolete"),"No obsolete dependency "+dependency);
        }
        var stripes=Visible(visual).Where(r=>r.name.StartsWith("TeamStripes")).ToArray();Check(stripes.Length>0 && stripes.All(stripe=>stripe.sharedMaterial.GetFloat("_TeamMaskStrength")==1 && stripe.sharedMaterial.GetTexture("_TeamMaskMap")!=null),"Fitted team stripe material");
        Check(Visible(wreck).Where(r=>r.name.StartsWith("TeamStripes")).All(r=>r.sharedMaterial.GetFloat("_TeamMaskStrength")==1),"Wreck stripe opt-in");
        foreach(var path in JArray.Parse(File.ReadAllText(TASK+"ObsoleteAssets.json")))Check(AssetDatabase.GetLabels(AssetDatabase.LoadMainAssetAtPath((string)path)).Contains("Obsolete"),"Obsolete label "+path);
        foreach(var path in JArray.Parse(File.ReadAllText(TASK+"ObsoleteAssets.json")).Select(p=>(string)p).Where(p=>p.EndsWith(".prefab")))
        {
            foreach(var dependency in AssetDatabase.GetDependencies(path,true)) Check(!((JObject)baseline["guids"]).Properties().Where(p=>p.Name.EndsWith(".prefab") || p.Name.EndsWith(".png")).Any(p=>p.Name==dependency),"Archived snapshot uses archived visuals "+dependency);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))
            {
                var iterator=new SerializedObject(component).GetIterator();while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)Check(iterator.objectReferenceValue!=null || iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)),"Archived reference "+path+"/"+iterator.propertyPath);
            }
        }
        var result=new {checks=_checks.Count,geometry,maximumMountError=mountError,hull=Number(data,"Hull"),shields=Number(data,"Shields"),speed=Number(data,"Speed"),bounds=new[]{bounds.size.x,bounds.size.y,bounds.size.z},targetableHardpoints=30,weapons=24,attachments=14,registrations=true,materials=true,savedReferences=true,retainedBalance=true,retainedHangar=true,obsoleteDependencies=0,playModeAcceptance=false,automatedTests=false};
        File.WriteAllText(TASK+"Verification.json",JsonConvert.SerializeObject(result,Formatting.Indented));File.WriteAllText(TASK+"VerificationChecks.json",JsonConvert.SerializeObject(_checks,Formatting.Indented));return JsonConvert.SerializeObject(result);
    }
    private static Mesh MeshOf(Renderer r)=>r is SkinnedMeshRenderer s?s.sharedMesh:r.GetComponents<MeshFilter>().Single().sharedMesh;
    private static MeshRenderer[] Visible(GameObject root)=>root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
    private static Bounds BoundsOf(Renderer[] renderers){var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
    private static SerializedObject Config(GameObject root,string name)=>new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name));
    private static UnityEngine.Object[] References(SerializedProperty array)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    private static SerializedProperty Entry(string path,string list,object key){var array=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(list);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>key is int n?p.FindPropertyRelative("key").intValue==n:p.FindPropertyRelative("key").stringValue==(string)key).FindPropertyRelative("value");}
    private static float Number(SerializedObject data,string name)=>data.FindProperty("<"+name+">k__BackingField").floatValue;
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);_checks.Add(message);}
}
