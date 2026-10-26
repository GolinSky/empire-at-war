using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class VerifyMC50
{
    private const string TASK = "Temp/MC50Import/";
    private const string VISUAL = "Assets/Prefabs/Models/Ships/Rebellion/MC50.prefab";
    private const string GAMEPLAY = "Assets/Prefabs/Models/Ships/Rebellion/MC50ShipView.prefab";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/MC50ReinforcementView.prefab";
    private const string WRECK = "Assets/Prefabs/Models/Wrecks/MC50WreckView.prefab";
    private const string DATA = "Assets/Settings/Data/Ship/Rebellion/MC50ShipData.asset";
    private static int _checks;

    public static string Main()
    {
        var report = new JObject();
        var conversion = JObject.Parse(File.ReadAllText(TASK+"MC50/ConversionReport.json"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/MC50/MC50.fbx");
        var transforms = model.GetComponentsInChildren<Transform>(true);
        float boneError = 0;
        foreach (var bone in ((JObject)conversion["before"]["bones"]).Properties())
        {
            var transform = transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
            var head = bone.Value["head"].Values<float>().ToArray();
            boneError = Mathf.Max(boneError,Vector3.Distance(transform.position,new Vector3(-head[0],head[2],-head[1])*.02f));
            Check(bone.Value["parent"].Type==JTokenType.Null || transform.parent.name==(string)bone.Value["parent"],"Bone parent");
        }
        Check(boneError<.00001f,"Unity bone displacement");
        int triangles = 0;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var mesh = renderer is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)renderer).sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
            Check(mesh.triangles.Length/3==(int)conversion["before"]["meshes"][renderer.name]["triangles"],"Triangle count");
            Check(mesh.uv.Length==mesh.vertexCount,"UV count");
            Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(conversion["before"]["meshes"][renderer.name]["materials"].Values<string>()),"Material slots");
            triangles += mesh.triangles.Length/3;
        }
        var ship = AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY);
        var prefabs = new JArray();
        foreach (var path in new[]{VISUAL,GAMEPLAY,PREVIEW,WRECK})
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.transform.localScale==Vector3.one,"Root scale");
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing scripts");
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                var property = new SerializedObject(component).GetIterator();
                while(property.Next(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference "+component.name+"."+property.propertyPath);
            }
            var geometry = path==GAMEPLAY ? root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="MC50" && t.parent.name=="BodyPivot").gameObject : root;
            var bounds = BoundsOf(geometry);
            if(path==VISUAL || path==GAMEPLAY) Check(Mathf.Abs(bounds.size.z-110)<.001f,"Hull length "+path);
            else
            {
                var opaqueBounds=BoundsOf(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL),true);
                Check(Vector3.Distance(bounds.size,opaqueBounds.size)<.001f,"Opaque preview/wreck hull dimensions");
                Check(Vector3.Distance(bounds.center,opaqueBounds.center)<.001f,"Opaque preview/wreck hull center");
            }
            Check(!geometry.GetComponentsInChildren<MeshRenderer>(true).Any(r=>new[]{"Shadow","Col","Shield"}.Contains(r.name)),"Source helper removed");
            prefabs.Add(new JObject{["path"]=path,["size"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z)});
        }
        var targets = References(Component(ship,"HealthComponent"),"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons = References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(targets.Length==16 && targets.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,16)),"16 ordered targets");
        Check(weapons.Length==14 && weapons.All(w=>targets.Contains(w)),"14 targetable weapons");
        int[] profiles = {2,5,32,8,14}, counts = {2,4,2,4,2};
        for(int i=0;i<profiles.Length;i++) Check(weapons.Count(w=>(int)w.WeaponType==profiles[i])==counts[i],"Weapon composition");
        var rig = ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="MC50Rig");
        foreach(var row in JArray.Parse(File.ReadAllText(TASK+"HardpointMapping.json")))
            Check(Vector3.Distance(targets.Single(h=>h.Id==(int)row["id"]).transform.position,rig.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)row["socket"] && t.GetComponent<Renderer>()==null).position)<.00001f,"Mount socket position");
        Check(References(Component(ship,"FogVisibilityComponent"),"hardPoints").Length==16,"Fog targets");
        Check(!ship.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c.GetType().Name=="HangarComponent"),"No hangar");
        var data = Load(DATA);
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==4200 && data.FindProperty("<Shields>k__BackingField").floatValue==3000 && data.FindProperty("<Speed>k__BackingField").floatValue==27,"Requested stats");
        Check(data.FindProperty("<ShipClass>k__BackingField").intValue==8,"Cruiser class");
        Check(data.FindProperty("hangarBays").arraySize==0 && data.FindProperty("hardPointHealth").arraySize==3,"Systems and no squadron complement");
        Check(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue.name=="MC50WreckData","Own wreck data");
        Check(!AssetDatabase.GetDependencies(new[]{GAMEPLAY,DATA},true).Any(p=>p.Contains("HomeOne")),"No donor dependencies");
        var spawn = Component(AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW),"UnitSpawnView");
        var hologram = new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Hologram slots");
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/MC50Icon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey = iconGuid+":"+iconId;
        var faction = Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),311).FindPropertyRelative("value");
        Check(faction.FindPropertyRelative("<Price>k__BackingField").intValue==3400 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue==42,"Requested economy");
        Check(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),311).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var tooltip = Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Select(i=>tooltip.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),311).FindPropertyRelative("value").objectReferenceValue==spawn,"Preview registration");
        Check(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"),311).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Data registration");
        foreach(var path in new[]{GAMEPLAY,DATA})
        {
            string key = Path.GetFileNameWithoutExtension(path), guid = AssetDatabase.AssetPathToGUID(path);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable address");
            var mappings = Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mappings.arraySize).Select(i=>mappings.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key).FindPropertyRelative("value.m_AssetGUID").stringValue==guid,"Asset mapping");
        }
        var ability = Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),40).FindPropertyRelative("value");
        Check(data.FindProperty("abilities").arraySize==1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==40,"Ability loadout");
        Check(ability.FindPropertyRelative("displayName").stringValue=="Hit and Run" && ability.FindPropertyRelative("duration").floatValue==8 && ability.FindPropertyRelative("recoveryDelay").floatValue==35,"Ability timing");
        Check(ability.FindPropertyRelative("settings.statModifier.speedMultiplier").floatValue==1.25f,"Speed boost");
        Check((int)EmpireAtWar.Models.Factions.ShipType.MC50==311,"Compiled ship ID");
        report["checks"]=_checks; report["meshes"]=10; report["triangles"]=triangles; report["bones"]=((JObject)conversion["before"]["bones"]).Count; report["boneErrorUnits"]=boneError;
        report["prefabs"]=prefabs; report["targetableHardpoints"]=16; report["weaponCounts"]=new JArray(counts); report["registrationsVerified"]=true;
        File.WriteAllText(TASK+"UnityInspection.json",report.ToString());
        return report.ToString();
    }

    private static void Check(bool condition,string label){_checks++;if(!condition)throw new InvalidOperationException(label);}
    private static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    private static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    private static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    private static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    private static Bounds BoundsOf(GameObject root,bool opaque=false){var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && (!opaque || r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit")).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds;}
}
