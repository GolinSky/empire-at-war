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

public static class VerifyAcclamatorAssault
{
    const string VIEW = "Assets/Prefabs/Models/Ships/AcclamatorAssaultShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/AcclamatorAssaultShipData.asset";
    public static string Main()
    {
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        Check(root.transform.localPosition==Vector3.zero && root.transform.localScale==Vector3.one,"Gameplay root identity");
        var health=Config(root,"HealthComponent");
        var targets=References(health.FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        var guns=References(Config(root,"WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        var defense=root.GetComponentsInChildren<MissileInterceptorHardPoint>(true);
        Check(targets.Length==4 && targets.Select(p=>p.Id).SequenceEqual(Enumerable.Range(0,4)),"Four ordered health targets");
        Check(guns.Length==6 && guns.Count(g=>(int)g.WeaponType==63)==2 && guns.Count(g=>(int)g.WeaponType==28)==4,"Two assault launchers and four heavy lasers");
        Check(targets.OfType<WeaponHardPoint>().Count()==2 && targets.OfType<WeaponHardPoint>().All(g=>(int)g.WeaponType==63),"Only missile launchers are targetable weapons");
        Check(targets.Count(p=>(int)p.HardPointType==2)==1 && targets.Count(p=>(int)p.HardPointType==1)==1,"Shield generator and engine");
        Check(defense.Length==6 && defense.All(p=>!targets.Contains(p)),"Six non-targetable missile interceptors");
        Check(root.GetComponentsInChildren<HardPoint>(true).Length==14 && root.GetComponentsInChildren<HardPoint>(true).Select(p=>p.Id).Distinct().Count()==14,"Exactly 14 unique mounts");
        Check(!root.GetComponentsInChildren<HardPoint>(true).Any(p=>(int)p.HardPointType==4),"No hangar hardpoint");
        foreach(var p in defense)
        {
            var config=new SerializedObject(p);
            Check(config.FindProperty("laser").objectReferenceValue!=null,"Explicit interceptor beam");
            Check(config.FindProperty("range").floatValue==40 && config.FindProperty("reload").floatValue==.6f && config.FindProperty("interceptChance").floatValue==.5f,"Existing interceptor tuning");
        }
        var hangar=Config(root,"HangarComponent");
        Check(!hangar.FindProperty("isDestroyable").boolValue && hangar.FindProperty("hangarHardPoint").objectReferenceValue==null,"Non-destroyable bay without hardpoint");
        var launch=(Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA));
        Check(Number(data,"Hull")==5000 && Number(data,"Shields")==2500 && Number(data,"Speed")==40,"Requested stats");
        Check(launch.localPosition.y<Number(data,"HullBottom"),"Launch exit below banked hull");
        var bays=data.FindProperty("hangarBays");Check(bays.arraySize==2,"Two launch bays");
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);Check(bay.FindPropertyRelative("squadronType").intValue==200+i && bay.FindPropertyRelative("reserve").intValue==(i==0?3:2) && bay.FindPropertyRelative("maxActive").intValue==1,"Source TIE complement");}
        Check(data.FindProperty("hardPointHealth").arraySize==3,"Only weapon, shield and engine health categories");
        var abilities=data.FindProperty("abilities");Check(abilities.arraySize==1 && abilities.GetArrayElementAtIndex(0).intValue==31,"Own ability");
        var body=(Transform)Config(root,"ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(root.GetComponentsInChildren<HardPoint>(true).All(p=>p.transform.IsChildOf(body)) && launch.IsChildOf(body),"All mounts and launch exit bank with body");
        var profiles=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var missile=profiles.GetProfile((WeaponType)63);
        Check(missile.ShotsPerSalvo==4 && missile.Damage==30 && missile.ShotInterval==1 && missile.Reload==15 && missile.Range==250 && missile.Interceptable && missile.ShotPrefab!=null,"Source assault salvo and projectile");
        foreach(var gun in guns)Check(profiles.GetProfile(gun.WeaponType).ShotPrefab!=null,"Resolved weapon profile");
        var ability=Entry("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset","definitions.keyValue",31);
        Check(ability.FindPropertyRelative("duration").floatValue==20 && ability.FindPropertyRelative("recoveryDelay").floatValue==60,"Ability timings");
        var modifiers=ability.FindPropertyRelative("settings.statModifier");
        foreach(var row in new[]{new{name="damageMultiplier",value=1f},new{name="fireDelayMultiplier",value=.5f},new{name="speedMultiplier",value=.25f},new{name="shieldRegenMultiplier",value=0f},new{name="damageTakenMultiplier",value=1.5f}})
            Check(modifiers.FindPropertyRelative(row.name).floatValue==row.value,"Ability modifier "+row.name);
        Check(Entry("Assets/Settings/Data/Ship/ShipsData.asset","shipsData.keyValue",209).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship registry");
        foreach(var path in new[]{VIEW,DATA})
        {
            var guid=AssetDatabase.AssetPathToGUID(path);var key=Path.GetFileNameWithoutExtension(path);
            Check(Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue==guid,"Asset mapping "+key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable "+key);
        }
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/AcclamatorAssaultIcon.png");
        Check(icon!=null && icon.texture.width==512 && icon.texture.height==512,"Own 512px sprite");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId);string iconKey=iconGuid+":"+localId;
        var unit=Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset","ships.keyValue",209);
        Check(unit.FindPropertyRelative("<Name>k__BackingField").stringValue=="AcclamatorAssault","Empire roster");
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && unit.FindPropertyRelative("iconKey").stringValue==iconKey,"Faction icon");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","shipIconWrapper.keyValue",209).objectReferenceValue==icon,"HUD icon");
        var icons=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/TooltipIconData.asset")).FindProperty("icons");
        Check(Enumerable.Range(0,icons.arraySize).Count(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey && icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue==icon)==1,"Tooltip icon");
        var spawn=Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnShipWrapper.keyValue",209).objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(spawn)=="Assets/Prefabs/Ui/Reinforcement/AcclamatorAssaultReinforcementView.prefab","Own placement mapping");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(spawn));
        Check(preview.transform.localPosition==Vector3.zero && preview.transform.localRotation==Quaternion.identity && preview.transform.localScale==Vector3.one,"Placement root identity");
        Check(preview.GetComponents<Rigidbody>().Single().isKinematic && !preview.GetComponents<Rigidbody>().Single().useGravity && preview.GetComponents<BoxCollider>().All(c=>c.isTrigger),"Placement physics");
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/AcclamatorAssault.prefab");var bounds=BoundsOf(visual);
        Check(Mathf.Abs(bounds.size.z-70)<.01f && bounds.center.magnitude<.01f,"Centered 70-unit hull");
        var opaque=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")).ToArray();
        var opaqueBounds=opaque[0].bounds;foreach(var renderer in opaque.Skip(1))opaqueBounds.Encapsulate(renderer.bounds);
        Check((BoundsOf(preview).size-opaqueBounds.size).magnitude<.01f && preview.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled)==opaque.Length,"Matching opaque placement geometry");
        var wreckData=new SerializedObject(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue);var wreck=((Component)wreckData.FindProperty("<Prefab>k__BackingField").objectReferenceValue).gameObject;
        Check(AssetDatabase.GetAssetPath(wreck)=="Assets/Prefabs/Models/Wrecks/AcclamatorAssaultWreckView.prefab","Own wreck");
        Check(References(Config(wreck,"UnitWreckView").FindProperty("meshRenderers")).Length==opaque.Length,"Complete wreck geometry");
        var team=References(Config(root,"TeamColorView").FindProperty("meshRenderers")).Cast<MeshRenderer>().ToArray();
        Check(root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).All(r=>team.Contains(r)),"Visible team renderer bindings");
        var stripe=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Models/EmpireShips/AcclamatorAssault/AcclamatorAssault_HullStripeMask.png");
        Check(stripe!=null && stripe.width==2048 && !((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(stripe))).sRGBTexture,"Saved linear stripe mask");
        var surfaces=opaque.SelectMany(r=>r.sharedMaterials).Concat(wreck.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials)).Distinct().ToArray();
        Check(surfaces.Length==14 && surfaces.All(m=>m.GetTexture("_BaseMap")!=null && m.GetColor("_BaseColor")==new Color(.5f,.5f,.5f,1) && m.GetFloat("_TeamRimStrength")==0),"Preserved albedo, subdued hull and no team rim glow");
        var painted=surfaces.Where(m=>m.name.Contains("Ev_acclamator_diffuse")).ToArray();
        Check(painted.Length==2 && painted.All(m=>m.GetTexture("_TeamMaskMap")==stripe && m.GetFloat("_TeamMaskStrength")==1),"Shared living/wreck stripe mask");
        var fog=References(Config(root,"FogVisibilityComponent").FindProperty("hardPoints"));Check(fog.Length==14,"Fog binds all mounts");
        foreach(var prefab in new[]{root,visual,preview,wreck})
        {
            foreach(var t in prefab.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script "+t.name);
            foreach(var r in prefab.GetComponentsInChildren<Renderer>(true))Check(r.sharedMaterials.All(m=>m!=null),"Missing material "+r.name);
            foreach(var c in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var iterator=new SerializedObject(c).GetIterator();
                while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                    Check(iterator.objectReferenceValue!=null || iterator.objectReferenceEntityIdValue==EntityId.None,"Broken reference "+c.name+"/"+iterator.propertyPath);
            }
        }
        Check(!AssetDatabase.GetDependencies(VIEW,true).Any(p=>p.Contains("RepublicShips/Acclamator") || p.EndsWith("/AcclamatorShipView.prefab") || p.EndsWith("/MunificentShipView.prefab")),"No donor model/prefab dependencies");
        var geometry=new System.Collections.Generic.List<object>();
        foreach(var conversion in JArray.Parse(File.ReadAllText("Temp/AcclamatorAssaultImport/ConversionReport.json")))
        {
            string name=(string)conversion["name"];var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/"+name+"/"+name+".fbx");
            var meshes=model.GetComponentsInChildren<MeshFilter>(true);int triangles=meshes.Sum(m=>m.sharedMesh.triangles.Length/3);
            Check(meshes.Length==((JObject)conversion["source"]["meshes"]).Count && triangles==conversion["source"]["meshes"].Children<JProperty>().Sum(p=>(int)p.Value["triangles"]),"Imported meshes and triangles "+name);
            Check(meshes.All(m=>m.sharedMesh.uv.Length>0),"UVs "+name);
            float error=0;var transforms=model.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponents<MeshFilter>().Length==0).ToArray();
            foreach(var bone in conversion["source"]["bones"].Children<JProperty>())
            {
                var t=transforms.Single(b=>b.name==bone.Name.Split('/').Last());var p=bone.Value["head"];var expected=new Vector3(-(float)p[0],(float)p[2],-(float)p[1])*.02f;
                error=Mathf.Max(error,Vector3.Distance(t.position,expected));
                if(bone.Value["parent"].Type!=JTokenType.Null)Check(t.parent.name==(string)bone.Value["parent"],"Bone parent "+bone.Name);
            }
            Check(error<.0001f,"Imported attachments "+name);geometry.Add(new{name,meshes=meshes.Length,triangles,bones=((JObject)conversion["source"]["bones"]).Count,maximumBoneError=error});
        }
        var report=new{asset=VIEW,hull=5000,shields=2500,speed=40,targetableHardpoints=targets.Length,heavyLasers=4,missileLaunchers=2,missileDefense=defense.Length,hangarHardpoint=false,
            geometry,registrations=true,missingScripts=0,brokenReferences=0,previewIdentity=true,wreckMeshes=opaque.Length,teamRendererBindings=team.Length,teamColorStripes=true,hullAlbedoMultiplier=.5f,teamRimGlow=false,playModeStarted=false,automatedTests=false};
        File.WriteAllText("Temp/AcclamatorAssaultImport/Verification.json",JsonConvert.SerializeObject(report,Formatting.Indented));return JsonConvert.SerializeObject(report);
    }
    static SerializedObject Config(GameObject root,string name)=>new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name));
    static UnityEngine.Object[] References(SerializedProperty array)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(string path,string list,object key){var array=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(list);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(r=>key is int n?r.FindPropertyRelative("key").intValue==n:r.FindPropertyRelative("key").stringValue==(string)key).FindPropertyRelative("value");}
    static float Number(SerializedObject data,string name)=>data.FindProperty("<"+name+">k__BackingField").floatValue;
    static Bounds BoundsOf(GameObject root){var rs=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled && !(r is LineRenderer) && !(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static void Check(bool condition,string detail){if(!condition)throw new InvalidOperationException(detail);}
}
