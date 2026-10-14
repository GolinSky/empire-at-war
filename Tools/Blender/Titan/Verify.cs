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

public static class VerifyTitan
{
    const string TASK="Temp/TitanImport/";
    const string VIEW="Assets/Prefabs/Models/Ships/Empire/TitanShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/Empire/TitanShipData.asset";
    const int SHIP_ID=214;
    static readonly List<string> _checks=new List<string>();

    public static string Main()
    {
        Check((int)EmpireAtWar.Models.Factions.ShipType.Titan==SHIP_ID,"Compiled enum uses unique Titan identifier");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/Titan.prefab");
        var data=Load(DATA);
        var targets=References(Config(root,"HealthComponent").FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        var guns=References(Config(root,"WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(targets.Length==33 && targets.Select(h=>h.Id).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,33)),"33 unique targetable hardpoints");
        Check(guns.Length==24 && guns.All(targets.Contains),"24 weapons bound to health");
        Check(targets.All(h=>h.GetComponents<Collider>().Length==1),"Target colliders");
        foreach(var group in new[]{new{type=88,count=12},new{type=5,count=6},new{type=11,count=4},new{type=14,count=2}})
            Check(guns.Count(g=>(int)g.WeaponType==group.type)==group.count,"Weapon count "+group.type);
        foreach(var group in new[]{new{type=1,count=4},new{type=2,count=3},new{type=7,count=1},new{type=10,count=1}})
            Check(targets.Count(g=>(int)g.HardPointType==group.type)==group.count,"Subsystem count "+group.type);
        var health=data.FindProperty("hardPointHealth");
        foreach(int type in targets.Select(t=>(int)t.HardPointType).Distinct())
            Check(Enumerable.Range(0,health.arraySize).Any(i=>health.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==type && health.GetArrayElementAtIndex(i).FindPropertyRelative("health").floatValue>0),"Health for type "+type);
        var body=(Transform)Config(root,"ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(root.transform.localScale==Vector3.one && body.localScale==Vector3.one && targets.All(t=>t.transform.IsChildOf(body)),"Identity roots and banking hardpoints");
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==28000 && data.FindProperty("<Shields>k__BackingField").floatValue==18000 && data.FindProperty("<Speed>k__BackingField").floatValue==16,"Reference hull/shields/speed");
        var hangar=Config(root,"HangarComponent");var bays=data.FindProperty("hangarBays");
        var bayTargets=References(hangar.FindProperty("bayHardPoints")).Cast<HardPoint>().ToArray();
        var launches=References(hangar.FindProperty("bayLaunchPoints")).Cast<Transform>().ToArray();
        Check(bays.arraySize==0 && bayTargets.Length==0 && launches.Length==0 && !hangar.FindProperty("isDestroyable").boolValue,"Squadrons disabled without stale hangar bindings");
        Check(!targets.Any(t=>(int)t.HardPointType==4),"No gameplay hangar targets");
        var collider=root.GetComponents<BoxCollider>().Single();
        Check(Mathf.Abs(collider.size.z-472.4936f)<.02f,"Length matches measured Venator ratio");
        Check(guns.All(g=>new Bounds(collider.center,collider.size).Contains(root.transform.InverseTransformPoint(g.transform.position))),"Projectile origins within ship volume");
        var source=JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"));
        var report=JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        float boneError=0;
        foreach(var pair in ((JObject)source["variants"]).Properties())
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/Titan/"+pair.Name+".fbx");
            var expected=report["models"][pair.Name]["after"];
            var renderers=model.GetComponentsInChildren<Renderer>(true);
            Check(renderers.Length==((JObject)expected["meshes"]).Count,"Imported mesh count "+pair.Name);
            foreach(var renderer in renderers)
            {
                var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
                var record=expected["meshes"][renderer.name];
                Check(mesh.triangles.Length/3==(int)record["triangles"],"Imported triangles "+pair.Name+"/"+renderer.name);
                Check(mesh.uv.Length==mesh.vertexCount && mesh.subMeshCount==renderer.sharedMaterials.Length,"UVs and slots "+pair.Name+"/"+renderer.name);
                Check(renderer.sharedMaterials.All(m=>m!=null && AssetDatabase.GetAssetPath(m).StartsWith("Assets/Art/Materials/Models/EmpireShips/Titan/")),"External material remaps "+pair.Name+"/"+renderer.name);
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
        foreach(string path in new[]{VIEW,"Assets/Prefabs/Models/Ships/Empire/Titan.prefab","Assets/Prefabs/Models/Wrecks/TitanWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/TitanReinforcementView.prefab"})
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
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/TitanIcon.png");
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var roster=Row(faction.FindProperty("ships.keyValue"),SHIP_ID).FindPropertyRelative("value");
        Check(roster.FindPropertyRelative("<Name>k__BackingField").stringValue=="Titan-class Battlecruiser" && roster.FindPropertyRelative("<Price>k__BackingField").intValue==13000 && roster.FindPropertyRelative("<BuildTime>k__BackingField").intValue==100,"Reference roster name/cost/build time");
        Check(roster.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon,"Roster icon");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey=iconGuid+":"+iconId;
        var icons=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,icons.arraySize).Select(i=>icons.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon registry");
        var ships=Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Check(Row(ships.FindProperty("shipsData.keyValue"),SHIP_ID).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship data registry");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/TitanReinforcementView.prefab");
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
        var abilities=data.FindProperty("abilities");
        Check(abilities.arraySize==2 && abilities.GetArrayElementAtIndex(0).intValue==5 && abilities.GetArrayElementAtIndex(1).intValue==22,"Approved existing Power to Weapons and Tractor Beam");
        foreach(int id in new[]{5,22})Check(Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),id).FindPropertyRelative("value.settings").managedReferenceValue!=null,"Existing ability settings "+id);
        var paint=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("TeamStripes")).ToArray();
        Check(paint.Length>0 && paint.All(r=>r.sharedMaterial.GetFloat("_TeamMaskStrength")==1),"Derived team stripes");
        var gamePaint=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("TeamStripes")).ToArray();
        Check(gamePaint.Length==paint.Length && gamePaint.All(r=>References(Config(root,"TeamColorView").FindProperty("meshRenderers")).Contains(r)),"Gameplay team stripe ownership");
        Check(!EditorUtility.scriptCompilationFailed,"Unity compilation succeeded");
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
