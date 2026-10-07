using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
public static class VerifyISDIII
{
    public static string Main()
    {
        const string VIEW="Assets/Prefabs/Models/Ships/ISDIIIShipView.prefab";
        const string DATA="Assets/Settings/Data/Ship/ISDIIIShipData.asset";
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var health=Config(root,"HealthComponent");var targets=References(health.FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        Check(targets.Length==19 && targets.Select(p=>p.Id).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,19)),"Nineteen ordered target IDs");
        var guns=References(Config(root,"WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(guns.Length==31,"Thirty-one automatic weapons");
        foreach(var expected in new[]{new{type=43,count=10},new{type=44,count=2},new{type=38,count=5},new{type=4,count=4},new{type=32,count=4},new{type=28,count=6}})
            Check(guns.Count(g=>(int)g.WeaponType==expected.type)==expected.count,"Weapon count "+expected.type);
        Check(targets.OfType<WeaponHardPoint>().Count()==12 && targets.OfType<WeaponHardPoint>().All(p=>(int)p.WeaponType==43||(int)p.WeaponType==44),"Only heavy dual turbolasers and heavy long-range turbo-ions targetable");
        Check(targets.Count(p=>(int)p.HardPointType==2)==2 && targets.Count(p=>(int)p.HardPointType==1)==3 && targets.Count(p=>(int)p.HardPointType==7)==1 && targets.Count(p=>(int)p.HardPointType==4)==1,"Seven targetable systems");
        var hangar=Config(root,"HangarComponent");var exit=(Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        Check(hangar.FindProperty("isDestroyable").boolValue && targets.Contains((HardPoint)hangar.FindProperty("hangarHardPoint").objectReferenceValue),"Targetable hangar");
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA));
        Check(Number(data,"Hull")==28000 && Number(data,"Shields")==18000 && Number(data,"Speed")==25,"Requested ship stats");
        Check(exit.localPosition.y<Number(data,"HullBottom"),"Hangar exit below banked hull");
        var bays=data.FindProperty("hangarBays");Check(bays.arraySize==2,"Two complement types");
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);Check(bay.FindPropertyRelative("squadronType").intValue==(i==0?202:205) && bay.FindPropertyRelative("reserve").intValue==(i==0?3:2) && bay.FindPropertyRelative("maxActive").intValue==1,"Avenger/Punisher complement");}
        Check(Enumerable.Range(0,data.FindProperty("abilities").arraySize).Select(i=>data.FindProperty("abilities").GetArrayElementAtIndex(i).intValue).SequenceEqual(new[]{25,22}),"Composite/Tractor abilities");
        Check(Entry("Assets/Settings/Data/Ship/ShipsData.asset","shipsData.keyValue",206).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship-data registry");
        foreach(var path in new[]{VIEW,DATA})
        {
            var guid=AssetDatabase.AssetPathToGUID(path);var key=Path.GetFileNameWithoutExtension(path);
            Check(Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue==guid,"Asset mapping "+key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable "+key);
        }
        foreach(var key in new[]{"TIEAvengerSquadronView","TIEAvengerSquadronData","TIEPunisherSquadronView","TIEPunisherSquadronData"})
        {
            var guid=Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue;
            Check(!string.IsNullOrEmpty(guid) && AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid))!=null,"Hangar dependency "+key);
        }
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId);string iconKey=iconGuid+":"+localId;
        var unit=Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset","ships.keyValue",206);
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && unit.FindPropertyRelative("iconKey").stringValue==iconKey,"Faction icon");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","shipIconWrapper.keyValue",206).objectReferenceValue==icon,"Battle icon");
        var tooltip=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/TooltipIconData.asset"));
        var icons=tooltip.FindProperty("icons");Check(Enumerable.Range(0,icons.arraySize).Any(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey&&icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue==icon),"Tooltip icon");
        var spawn=Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnShipWrapper.keyValue",206).objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(spawn)=="Assets/Prefabs/Ui/Reinforcement/ISDIIIReinforcementView.prefab","Placement registration");
        var profiles=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Check(profiles.GetProfile((WeaponType)43).ShotsPerSalvo==2 && profiles.GetProfile((WeaponType)44).ShotsPerSalvo==2 && profiles.GetProfile((WeaponType)38).ShotsPerSalvo==3,"Dual turbo-ion and triple turbolaser bursts");
        foreach(var gun in guns)Check(profiles.GetProfile(gun.WeaponType).ShotPrefab!=null,"Shot prefab "+gun.name);
        var catalog=AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        Check(catalog.Get((ShipAbilityId)25).Settings is CompositeBeamSettings,"Composite Beam");
        var tractor=(TractorBeamSettings)catalog.Get((ShipAbilityId)22).Settings;
        Check(tractor.SpeedMultiplier>0 && tractor.SpeedMultiplier<1 && tractor.Beam.ShotPrefab is EmpireAtWar.ViewComponents.Weapon.BeamShot,"Tractor Beam");
        var beamMuzzle=(Transform)Config(root,"Ship").FindProperty("compositeBeamMuzzle").objectReferenceValue;
        Check(beamMuzzle!=null && beamMuzzle.GetComponents<WeaponHardPoint>().Single().WeaponType==WeaponType.CompositeBeam && !guns.Contains(beamMuzzle.GetComponents<WeaponHardPoint>().Single()),"Ability-only composite muzzle");
        var composite=catalog.Get((ShipAbilityId)25);Check(composite.Duration==6 && composite.RecoveryDelay==45,"Composite timing");
        var damageArray=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset")).FindProperty("damageTypes");
        var damage=Enumerable.Range(0,damageArray.arraySize).Select(i=>damageArray.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("damageType").intValue==12);
        Check(damage.FindPropertyRelative("vsShield").floatValue==.1f && damage.FindPropertyRelative("damage.capital").floatValue==1,"Composite armor/shield modifiers");
        Check(unit.FindPropertyRelative("<Name>k__BackingField").stringValue=="ISD III","Display name");
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDIII.prefab");
        var bounds=BoundsOf(visual);Check(bounds.size.z>190 && bounds.size.z<194,"Hull with mounted turret bounds");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(spawn));
        Check((BoundsOf(preview).size-BoundsOf(visual,true).size).magnitude<.01f,"Placement geometry");
        foreach(var p in new[]{root,visual,preview,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Wrecks/ISDIIIWreckView.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDIIIHeavyDualTurret.prefab"),AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDIIIIonTurret.prefab")})
        {
            Check(p!=null,"Prefab exists");
            foreach(var t in p.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script "+t.name);
            foreach(var renderer in p.GetComponentsInChildren<Renderer>(true))Check(renderer.sharedMaterials.All(m=>m!=null),"Material "+renderer.name);
        }
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var iterator=new SerializedObject(component).GetIterator();
            while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                Check(iterator.objectReferenceValue!=null || iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)),"Broken reference "+component.name+"/"+iterator.propertyPath);
        }
        var geometry=new System.Collections.Generic.List<object>();
        foreach(var conversion in JArray.Parse(File.ReadAllText("Temp/ISDIIIImport/ConversionReport.json")).Where(c=>(string)c["name"]!="TIEInterceptor"))
        {
            string name=(string)conversion["name"];
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/"+name+"/"+name+".fbx");
            var meshes=model.GetComponentsInChildren<MeshFilter>(true);
            Check(meshes.Length==((JObject)conversion["source"]["meshes"]).Count,"Mesh count "+name);
            int triangles=meshes.Sum(m=>m.sharedMesh.triangles.Length/3);
            Check(triangles==conversion["source"]["meshes"].Children<JProperty>().Sum(p=>(int)p.Value["triangles"]),"Triangle count "+name);
            Check(meshes.All(m=>m.sharedMesh.uv.Length>0),"UVs "+name);
            float error=0;var transforms=model.GetComponentsInChildren<Transform>(true);
            foreach(var bone in conversion["source"]["bones"].Children<JProperty>())
            {
                var t=transforms.Single(b=>b.name==bone.Name.Split('/').Last() && b.GetComponents<MeshFilter>().Length==0);
                var p=bone.Value["head"];var expected=new Vector3(-(float)p[0],(float)p[2],-(float)p[1])*.02f;
                error=Mathf.Max(error,Vector3.Distance(t.position,expected));
            }
            Check(error<.002f,"Unity attachment positions "+name+": "+error);
            geometry.Add(new{name,meshes=meshes.Length,triangles,bones=((JObject)conversion["source"]["bones"]).Count,maximumBoneError=error});
        }
        var mappings=JArray.Parse(File.ReadAllText("Temp/ISDIIIImport/HardpointMapping.json"));
        var allPoints=root.GetComponentsInChildren<HardPoint>(true);
        var modelTransforms=root.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponents<HardPoint>().Length==0).ToArray();
        var body=(Transform)Config(root,"ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        float mountError=0;
        foreach(var mapping in mappings)
        {
            var point=allPoints.Single(p=>p.Id==(int)mapping["id"]);
            string bone=(string)mapping["bone"];
            Transform attachment;
            if(bone.Contains("/"))
            {
                var turret=modelTransforms.Single(t=>t.name==bone.Split('/')[0]);
                attachment=turret.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,bone.Split('/')[1],StringComparison.OrdinalIgnoreCase));
            }
            else attachment=modelTransforms.Single(t=>string.Equals(t.name,bone,StringComparison.OrdinalIgnoreCase) && InSourceRig(t,root.transform));
            mountError=Mathf.Max(mountError,Vector3.Distance(point.transform.position,attachment.position));
            Check(point.transform.parent==body,"Banking mount "+point.name);
        }
        Check(mountError<.001f,"Gameplay muzzle/system positions: "+mountError);
        var mainConversion=JArray.Parse(File.ReadAllText("Temp/ISDIIIImport/ConversionReport.json")).Single(c=>(string)c["name"]=="ISDIII");
        var rawModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/ISDIII/ISDIII.fbx");
        var rawTransforms=rawModel.GetComponentsInChildren<Transform>(true);
        float attachmentError=0;
        foreach(string bone in mappings.Select(m=>(string)m["bone"]).Where(b=>!b.Contains("/")))
        {
            var row=mainConversion["source"]["bones"].Children<JProperty>().Single(b=>b.Name.EndsWith("/"+bone,StringComparison.OrdinalIgnoreCase));
            var xyz=row.Value["head"];var expected=new Vector3(-(float)xyz[0],(float)xyz[2],-(float)xyz[1])*.02f;
            attachmentError=Mathf.Max(attachmentError,Vector3.Distance(rawTransforms.Single(t=>string.Equals(t.name,bone,StringComparison.OrdinalIgnoreCase)).position,expected));
        }
        Check(attachmentError<.0001f,"Gameplay source attachment precision: "+attachmentError);
        var report=new{asset=VIEW,hull=28000,shields=18000,speed=25,targetableHardpoints=targets.Select(t=>new{t.name,t.Id,type=(int)t.HardPointType}).ToArray(),weapons=guns.GroupBy(g=>(int)g.WeaponType).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),bounds=new[]{bounds.size.x,bounds.size.y,bounds.size.z},navigationRadius=Number(data,"NavigationRadius"),bankedRange=new[]{Number(data,"HullBottom"),Number(data,"HullTop")},hangarExit=new[]{exit.localPosition.x,exit.localPosition.y,exit.localPosition.z},geometry,maximumMountError=mountError,maximumGameplayAttachmentError=attachmentError,registrations=true,dependencies=true,missingScripts=0,brokenReferences=0,editorPlayMode=EditorApplication.isPlaying,playModeAcceptance=false,automatedTests=false};
        File.WriteAllText("Temp/ISDIIIImport/Verification.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return JsonConvert.SerializeObject(report);
    }
    static bool InSourceRig(Transform bone,Transform root){for(var parent=bone.parent;parent!=root && parent!=null;parent=parent.parent)if(parent.name.EndsWith("_Turret"))return false;return true;}
    static SerializedObject Config(GameObject root,string name)=>new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name));
    static UnityEngine.Object[] References(SerializedProperty list)=>Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(string path,string list,object key){var array=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(list);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(r=>key is int n?r.FindPropertyRelative("key").intValue==n:r.FindPropertyRelative("key").stringValue==(string)key).FindPropertyRelative("value");}
    static float Number(SerializedObject data,string name)=>data.FindProperty("<"+name+">k__BackingField").floatValue;
    static Bounds BoundsOf(GameObject root,bool opaqueOnly=false){var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && !(r is LineRenderer) && !(r is ParticleSystemRenderer) && (!opaqueOnly || r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit"))).ToArray();var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Check(bool condition,string detail){if(!condition)throw new InvalidOperationException(detail);}
}
