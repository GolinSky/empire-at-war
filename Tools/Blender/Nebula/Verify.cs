using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class VerifyNebula
{
    const string TASK="Temp/NebulaImport/";
    const string VIEW="Assets/Prefabs/Models/Ships/NebulaShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/NebulaShipData.asset";
    const int SHIP_ID=330;
    static readonly List<string> _checks=new List<string>();

    public static string Main()
    {
        Check((int)EmpireAtWar.Models.Factions.ShipType.Nebula==SHIP_ID,"Compiled enum uses unique Nebula identifier");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Nebula.prefab");
        var data=Load(DATA);
        var targets=References(Config(root,"HealthComponent").FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        var guns=References(Config(root,"WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(targets.Length==31 && targets.Select(h=>h.Id).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,31)),"31 unique targetable hardpoints");
        Check(guns.Length==24 && guns.All(targets.Contains),"24 weapons bound to health");
        Check(targets.All(h=>h.GetComponents<Collider>().Length==1),"Target colliders");
        foreach(var group in new[]{new{type=3,count=8},new{type=4,count=6},new{type=32,count=4},new{type=65,count=2},new{type=14,count=4}})
            Check(guns.Count(g=>(int)g.WeaponType==group.type)==group.count,"Weapon count "+group.type);
        foreach(var group in new[]{new{type=1,count=2},new{type=2,count=2},new{type=4,count=2},new{type=7,count=1}})
            Check(targets.Count(g=>(int)g.HardPointType==group.type)==group.count,"Subsystem count "+group.type);
        var health=data.FindProperty("hardPointHealth");
        foreach(int type in targets.Select(t=>(int)t.HardPointType).Distinct())
            Check(Enumerable.Range(0,health.arraySize).Any(i=>health.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==type && health.GetArrayElementAtIndex(i).FindPropertyRelative("health").floatValue>0),"Health for type "+type);
        var body=(Transform)Config(root,"ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(root.transform.localScale==Vector3.one && body.localScale==Vector3.one && targets.All(t=>t.transform.IsChildOf(body)),"Identity roots and banking hardpoints");
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==16000 && data.FindProperty("<Shields>k__BackingField").floatValue==15000 && data.FindProperty("<Speed>k__BackingField").floatValue==20,"Reference hull/shields/speed");
        var hangar=Config(root,"HangarComponent");var bays=data.FindProperty("hangarBays");
        var bayTargets=References(hangar.FindProperty("bayHardPoints")).Cast<HardPoint>().ToArray();
        var launches=References(hangar.FindProperty("bayLaunchPoints")).Cast<Transform>().ToArray();
        Check(bays.arraySize==2 && bayTargets.Length==2 && launches.Length==2,"Two explicitly bound hangars");
        Check(bayTargets.All(targets.Contains) && launches.All(t=>t.IsChildOf(body)),"Hangar target and launch ownership");
        Check(Enumerable.Range(0,2).All(i=>bays.GetArrayElementAtIndex(i).FindPropertyRelative("squadronType").intValue==300),"X-Wing substitution");
        Check(Enumerable.Range(0,2).Sum(i=>bays.GetArrayElementAtIndex(i).FindPropertyRelative("reserve").intValue)==5 && Enumerable.Range(0,2).Sum(i=>bays.GetArrayElementAtIndex(i).FindPropertyRelative("maxActive").intValue)==3,"3 active / 2 replacement squadrons");
        var collider=root.GetComponents<BoxCollider>().Single();
        Check(launches.All(t=>t.position.y<collider.bounds.min.y),"Launches below hull");
        var source=JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"));
        var report=JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        float boneError=0;
        foreach(var pair in ((JObject)source["variants"]).Properties())
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/Nebula/"+pair.Name+".fbx");
            var expected=report["models"][pair.Name]["after"];
            var renderers=model.GetComponentsInChildren<Renderer>(true);
            Check(renderers.Length==((JObject)expected["meshes"]).Count,"Imported mesh count "+pair.Name);
            foreach(var renderer in renderers)
            {
                var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
                var record=expected["meshes"][renderer.name];
                Check(mesh.triangles.Length/3==(int)record["triangles"],"Imported triangles "+pair.Name+"/"+renderer.name);
                Check(mesh.uv.Length==mesh.vertexCount && mesh.subMeshCount==renderer.sharedMaterials.Length,"UVs and slots "+pair.Name+"/"+renderer.name);
                Check(renderer.sharedMaterials.All(m=>m!=null && AssetDatabase.GetAssetPath(m).StartsWith("Assets/Art/Materials/Models/RebellionShips/Nebula/")),"External material remaps "+pair.Name+"/"+renderer.name);
            }
            var rig=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pair.Name+"Rig");
            foreach(var bone in ((JObject)expected["bones"]).Properties())
            {
                var transform=rig.GetComponentsInChildren<Transform>(true).Single(t=>t.name==bone.Name && t.GetComponents<Renderer>().Length==0);
                string parent=(string)bone.Value["parent"];
                Check(transform.parent.name==(parent??rig.name),"Bone parent "+pair.Name+"/"+bone.Name);
                var head=bone.Value["head"];
                var position=new Vector3(-(float)head[0],(float)head[2],-(float)head[1])*.02f;
                boneError=Mathf.Max(boneError,Vector3.Distance(transform.position,position));
            }
        }
        Check(boneError<.0001f,"Unity bone transport tolerance");
        foreach(var attachment in source["attachments"])
        {
            var part=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)attachment["variant"]);
            Check(part.parent.name.Equals((string)attachment["bone"],StringComparison.OrdinalIgnoreCase) && part.localPosition==Vector3.zero,"Source attachment "+part.name);
        }
        foreach(string path in new[]{VIEW,"Assets/Prefabs/Models/Ships/Nebula.prefab","Assets/Prefabs/Models/Wrecks/NebulaWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/NebulaReinforcementView.prefab"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"No missing scripts "+path+"/"+transform.name);
                foreach(var component in transform.GetComponents<Component>().Where(c=>c!=null))
                {
                    var property=new SerializedObject(component).GetIterator();
                    while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue==UnityEngine.EntityId.None,"No broken reference "+path+"/"+property.propertyPath);
                }
            }
            Check(prefab.transform.localScale==Vector3.one,"Prefab root identity "+path);
            Check(!prefab.GetComponentsInChildren<MeshRenderer>(true).Any(r=>!r.enabled && r.GetComponents<MonoBehaviour>().All(c=>c.GetType().Name!="Shield") && System.Text.RegularExpressions.Regex.IsMatch(r.name,"collis.*ion|shadow|shield|lod_?\\d|^col$",System.Text.RegularExpressions.RegexOptions.IgnoreCase)),"No disabled source helpers "+path);
        }
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/NebulaIcon.png");
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var roster=Row(faction.FindProperty("ships.keyValue"),SHIP_ID).FindPropertyRelative("value");
        Check(roster.FindPropertyRelative("<Name>k__BackingField").stringValue=="Nebula Star Destroyer" && roster.FindPropertyRelative("<Price>k__BackingField").intValue==7200 && roster.FindPropertyRelative("<BuildTime>k__BackingField").intValue==72,"Reference roster name/cost/build time");
        Check(roster.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon,"Roster icon");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey=iconGuid+":"+iconId;
        var icons=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,icons.arraySize).Select(i=>icons.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon registry");
        var ships=Load("Assets/Settings/Data/Ship/ShipsData.asset");
        Check(Row(ships.FindProperty("shipsData.keyValue"),SHIP_ID).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship data registry");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/NebulaReinforcementView.prefab");
        var spawn=Config(preview,"UnitSpawnView");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==spawn.targetObject,"Own placement mapping");
        var hologram=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
        Check(References(spawn.FindProperty("meshRenderers")).Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Placement material slots");
        foreach(string path in new[]{VIEW,DATA})
        {
            var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Check(entry!=null && entry.address==Path.GetFileNameWithoutExtension(path),"Addressable "+path);
            var mappings=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mappings.arraySize).Select(i=>mappings.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(path),"Asset mapping "+path);
        }
        var definition=(ShipAbilityDefinition)Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),38).FindPropertyRelative("value").boxedValue;
        var modifier=((BoostWeaponPowerSettings)definition.Settings).StatModifier;
        var donor=(ShipAbilityDefinition)Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),5).FindPropertyRelative("value").boxedValue;
        Check(!ReferenceEquals(definition.Settings,donor.Settings),"Nebula modifier settings are independent");
        Check(definition.Duration==10 && definition.RecoveryDelay==50 && modifier.FireDelayMultiplier==.8f && modifier.ShieldRegenMultiplier==0 && modifier.DamageMultiplier==1,"Overcharge Weapons settings");
        var result=new JObject{["checks"]=_checks.Count,["boneError"]=boneError,["scriptCompilationFailed"]=EditorUtility.scriptCompilationFailed,["success"]=true};
        File.WriteAllText(TASK+"Verification.json",result.ToString());
        return result.ToString();
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedObject Config(GameObject root,string type)=>new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==type));
    static UnityEngine.Object[] References(SerializedProperty property)=>Enumerable.Range(0,property.arraySize).Select(i=>property.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);_checks.Add(label);}
}
