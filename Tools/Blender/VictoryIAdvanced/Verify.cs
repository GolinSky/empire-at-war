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
public static class VerifyVictoryAdvanced
{
    public static string Main()
    {
        const string VIEW="Assets/Prefabs/Models/Ships/VictoryIAdvancedShipView.prefab";
        const string DATA="Assets/Settings/Data/Ship/VictoryIAdvancedShipData.asset";
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var health=Config(root,"HealthComponent");var targets=References(health.FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        Check(targets.Length==10 && targets.Select(p=>p.Id).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,10)),"Ten ordered target IDs");
        var guns=References(Config(root,"WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(guns.Length==16,"Sixteen weapons");
        foreach(var expected in new[]{new{type=34,count=4},new{type=35,count=2},new{type=36,count=6},new{type=28,count=4}})
            Check(guns.Count(g=>(int)g.WeaponType==expected.type)==expected.count,"Weapon count "+expected.type);
        Check(targets.OfType<WeaponHardPoint>().Count()==6 && targets.OfType<WeaponHardPoint>().All(p=>(int)p.WeaponType==34||(int)p.WeaponType==35),"Only rockets targetable");
        Check(targets.Count(p=>(int)p.HardPointType==2)==1 && targets.Count(p=>(int)p.HardPointType==1)==2 && targets.Count(p=>(int)p.HardPointType==7)==1,"Four targetable systems");
        var hangar=Config(root,"HangarComponent");var exit=(Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        Check(!hangar.FindProperty("isDestroyable").boolValue && !targets.Contains((HardPoint)hangar.FindProperty("hangarHardPoint").objectReferenceValue),"Non-targetable hangar");
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA));
        Check(Number(data,"Hull")==12000 && Number(data,"Shields")==8000 && Number(data,"Speed")==175,"Requested ship stats");
        Check(exit.localPosition.y<Number(data,"HullBottom"),"Hangar exit below banked hull");
        var bay=data.FindProperty("hangarBays").GetArrayElementAtIndex(0);
        Check(bay.FindPropertyRelative("squadronType").intValue==203 && bay.FindPropertyRelative("reserve").intValue==2 && bay.FindPropertyRelative("maxActive").intValue==1,"Interceptor complement");
        Check(Entry("Assets/Settings/Data/Ship/ShipsData.asset","shipsData.keyValue",202).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship-data registry");
        foreach(var path in new[]{VIEW,DATA})
        {
            var guid=AssetDatabase.AssetPathToGUID(path);var key=Path.GetFileNameWithoutExtension(path);
            Check(Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue==guid,"Asset mapping "+key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable "+key);
        }
        foreach(var key in new[]{"TIEInterceptorSquadronView","TIEInterceptorSquadronData"})
        {
            var guid=Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue;
            Check(!string.IsNullOrEmpty(guid) && AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid))!=null,"Interceptor dependency "+key);
        }
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIAdvancedIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId);string iconKey=iconGuid+":"+localId;
        var unit=Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset","ships.keyValue",202);
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && unit.FindPropertyRelative("iconKey").stringValue==iconKey,"Faction icon");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","shipIconWrapper.keyValue",202).objectReferenceValue==icon,"Battle icon");
        var tooltip=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/TooltipIconData.asset"));
        var icons=tooltip.FindProperty("icons");Check(Enumerable.Range(0,icons.arraySize).Any(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey&&icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue==icon),"Tooltip icon");
        var spawn=Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnShipWrapper.keyValue",202).objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(spawn)=="Assets/Prefabs/Ui/Reinforcement/VictoryIAdvancedReinforcementView.prefab","Placement registration");
        var profiles=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Check(profiles.GetProfile((WeaponType)34).ShotsPerSalvo==4 && profiles.GetProfile((WeaponType)35).ShotsPerSalvo==6,"Rocket bursts");
        Check(profiles.GetProfile((WeaponType)34).Interceptable && profiles.GetProfile((WeaponType)35).Interceptable && !profiles.GetProfile((WeaponType)36).Interceptable,"Full Salvo ordnance classification");
        foreach(var gun in guns)Check(profiles.GetProfile(gun.WeaponType).ShotPrefab!=null,"Shot prefab "+gun.name);
        var catalog=AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        Check(catalog.Get((ShipAbilityId)19).Settings is FullSalvoSettings,"Full Salvo");
        var tractor=(TractorBeamSettings)catalog.Get((ShipAbilityId)22).Settings;
        Check(tractor.SpeedMultiplier>0 && tractor.SpeedMultiplier<1 && tractor.Beam.ShotPrefab is EmpireAtWar.ViewComponents.Weapon.BeamShot,"Tractor Beam");
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/VictoryIAdvanced.prefab");
        var bounds=BoundsOf(visual);Check(Mathf.Abs(bounds.size.z-110)<.01f,"Hull scale");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(spawn));
        Check((BoundsOf(preview).size-bounds.size).magnitude<.01f,"Placement geometry");
        foreach(var p in new[]{root,visual,preview,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Wrecks/VictoryIAdvancedWreckView.prefab")})
        {
            Check(p!=null,"Prefab exists");
            foreach(var t in p.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script "+t.name);
            foreach(var renderer in p.GetComponentsInChildren<Renderer>(true))Check(renderer.sharedMaterials.All(m=>m!=null),"Material "+renderer.name);
        }
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var iterator=new SerializedObject(component).GetIterator();
            while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                Check(iterator.objectReferenceValue!=null || iterator.objectReferenceInstanceIDValue==0,"Broken reference "+component.name+"/"+iterator.propertyPath);
        }
        var geometry=new System.Collections.Generic.List<object>();
        foreach(var conversion in JArray.Parse(File.ReadAllText("Temp/VictoryIAdvancedImport/ConversionReport.json")).Where(c=>(string)c["name"]!="TIEInterceptor"))
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
                var t=transforms.Single(b=>b.name==bone.Name.Split('/').Last());
                var p=bone.Value["head"];var expected=new Vector3(-(float)p[0],(float)p[2],-(float)p[1])*.02f;
                error=Mathf.Max(error,Vector3.Distance(t.position,expected));
            }
            Check(error<.0001f,"Unity attachment positions "+name+": "+error);
            geometry.Add(new{name,meshes=meshes.Length,triangles,bones=((JObject)conversion["source"]["bones"]).Count,maximumBoneError=error});
        }
        var report=new{asset=VIEW,hull=12000,shields=8000,speed=175,targetableHardpoints=targets.Select(t=>new{t.name,t.Id,type=(int)t.HardPointType}).ToArray(),weapons=guns.GroupBy(g=>(int)g.WeaponType).Select(g=>new{type=g.Key,count=g.Count()}).ToArray(),bounds=new[]{bounds.size.x,bounds.size.y,bounds.size.z},navigationRadius=Number(data,"NavigationRadius"),bankedRange=new[]{Number(data,"HullBottom"),Number(data,"HullTop")},hangarExit=new[]{exit.localPosition.x,exit.localPosition.y,exit.localPosition.z},geometry,registrations=true,dependencies=true,missingScripts=0,brokenReferences=0,playMode=false,automatedTests=false};
        File.WriteAllText("Temp/VictoryIAdvancedImport/Verification.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return JsonConvert.SerializeObject(report);
    }
    static SerializedObject Config(GameObject root,string name)=>new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name));
    static UnityEngine.Object[] References(SerializedProperty list)=>Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(string path,string list,object key){var array=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(list);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(r=>key is int n?r.FindPropertyRelative("key").intValue==n:r.FindPropertyRelative("key").stringValue==(string)key).FindPropertyRelative("value");}
    static float Number(SerializedObject data,string name)=>data.FindProperty("<"+name+">k__BackingField").floatValue;
    static Bounds BoundsOf(GameObject root){var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && !(r is LineRenderer) && !(r is ParticleSystemRenderer)).ToArray();var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Check(bool condition,string detail){if(!condition)throw new InvalidOperationException(detail);}
}
