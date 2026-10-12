using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Components.AttackComponent;

public static class InspectAssaultFrigateMkII
{
    const string NAME = "AssaultFrigateMkII";
    const string TASK = "Temp/AssaultFrigateMkIIImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Rebellion/AssaultFrigateMkII.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/Rebellion/AssaultFrigateMkIIShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/AssaultFrigateMkIIReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/AssaultFrigateMkIIWreckView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Rebellion/AssaultFrigateMkIIShipData.asset";
    const int SHIP_ID = 309;

    public static string Main()
    {
        var report = new JObject();
        report["editorCompileFailed"] = EditorUtility.scriptCompilationFailed;
        Check(Enum.IsDefined(typeof(EmpireAtWar.Models.Factions.ShipType),SHIP_ID),"Compiled ShipType");
        Check(Enum.IsDefined(typeof(EmpireAtWar.Services.ShipAbilities.ShipAbilityId),45),"Compiled ability ID");
        var conversion = JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        float boneError = 0; int triangles = 0, meshCount = 0, bones = 0;
        foreach (var source in ((JObject)conversion["models"]).Properties())
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/"+NAME+"/"+source.Name+".fbx");
            var transforms = model.GetComponentsInChildren<Transform>(true);
            foreach (var bone in ((JObject)source.Value["before"]["bones"]).Properties())
            {
                var transform = transforms.Single(t => t.name == bone.Name && t.GetComponent<Renderer>() == null);
                var head = bone.Value["head"].Values<float>().ToArray();
                boneError = Mathf.Max(boneError, Vector3.Distance(transform.position, new Vector3(-head[0],head[2],-head[1])*.02f));
                string parent = (string)bone.Value["parent"];
                Check(parent == null || transform.parent.name == parent, "Bone hierarchy: "+bone.Name); bones++;
            }
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = renderer is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)renderer).sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
                string name = renderer.name.Split('.')[0];
                Check(mesh.triangles.Length/3 == (int)source.Value["before"]["meshes"][name]["triangles"], "Unity triangle count: "+name);
                Check(mesh.uv.Length == mesh.vertexCount, "Unity UVs: "+name);
                var slots = conversion["mesh_materials"][source.Name]["meshes"].Single(m => (string)m["name"] == name)["materials"].Select(m => (string)m["materialName"]).ToArray();
                Check(renderer.sharedMaterials.Select(m => m.name).SequenceEqual(slots), "Binary source material assignments: "+name);
                for (int slot = 0; slot < mesh.subMeshCount; slot++)
                {
                    int expected=(int)conversion["mesh_materials"][source.Name]["meshes"].Single(m => (string)m["name"] == name)["materials"][slot]["triangleCount"];
                    if (!(bool)source.Value["before"]["meshes"][name]["hidden"]) Check(mesh.GetTriangles(slot).Length/3==expected,"Visible per-slot triangle count: "+name);
                }
                triangles += mesh.triangles.Length/3; meshCount++;
            }
        }
        Check(boneError < .00001f, "Unity attachment error");
        report["unityGeometry"] = new JObject { ["models"] = 15, ["meshes"] = meshCount, ["triangles"] = triangles, ["bones"] = bones, ["boneErrorUnits"] = boneError };
        var prefabs = new JArray();
        foreach (string path in new[] { VISUAL,GAMEPLAY,PREVIEW,WRECK })
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.transform.localScale == Vector3.one && root.transform.localPosition == Vector3.zero, "Prefab root identity");
            foreach (var transform in root.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script");
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference) Check(property.objectReferenceValue != null || property.objectReferenceEntityIdValue.Equals(default(EntityId)), "Broken reference: "+component.name+"."+property.propertyPath);
            }
            var geometry = path == GAMEPLAY ? root.GetComponentsInChildren<Transform>(true).Single(t => t.name == NAME && t.parent.name == "BodyPivot").gameObject : root;
            var bounds = BoundsOf(geometry);
            Check(Mathf.Abs(bounds.size.z-100) < .001f, "Prefab hull length");
            Check(!geometry.GetComponentsInChildren<MeshRenderer>(true).Any(r => new[] { "Col","Shadow","Shield","Barrel_Shadow","Turret_Shadow" }.Contains(r.name)), "Hidden helpers stripped");
            prefabs.Add(new JObject { ["path"] = path, ["size"] = Vector(bounds.size), ["visibleMeshes"] = geometry.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.enabled) });
        }
        report["prefabs"] = prefabs;
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var hull = visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME+"Rig");
        var audit=JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"));
        foreach (var attachment in audit["attachments"])
        {
            var socket=hull.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)attachment["bone"]);
            var turret=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)attachment["variant"]);
            Check(Vector3.Distance(socket.position,turret.position)<.00001f,"Separate turret socket");
        }
        var ship = AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY);
        var health = Component(ship,"HealthComponent");
        var hardpoints = References(health,"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons = References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(hardpoints.Length == 14 && hardpoints.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,14)),"Targetable hardpoints and IDs");
        Check(weapons.Length == 12 && weapons.All(w=>hardpoints.Contains(w)),"Weapon health bindings");
        int[] profiles={4,82,28,14}; int[] counts={4,2,4,2};
        for(int i=0;i<4;i++) Check(weapons.Count(w=>(int)w.WeaponType==profiles[i])==counts[i],"Requested weapon counts");
        Check(hardpoints.Count(h=>(int)h.HardPointType==1)==1 && hardpoints.Count(h=>(int)h.HardPointType==2)==1,"Requested systems");
        Check(References(Component(ship,"FogVisibilityComponent"),"hardPoints").SequenceEqual(hardpoints),"Fog hardpoints");
        var gameplayVisual=ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME && t.parent.name=="BodyPivot").gameObject;
        var opaque=gameplayVisual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit").ToArray();
        Check(References(Component(ship,"TeamColorView"),"meshRenderers").Cast<MeshRenderer>().ToHashSet().SetEquals(opaque),"Team renderer ownership");
        foreach(var mount in JArray.Parse(File.ReadAllText(TASK+"HardpointMapping.json")))
        {
            string socketName=(string)mount["bone"];
            Transform muzzle;
            if(socketName.StartsWith("LC_Quad_"))
            {
                int part=int.Parse(socketName.Substring("LC_Quad_".Length))-1;
                var turret=gameplayVisual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME+"Part"+part.ToString("D2"));
                muzzle=turret.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="FP_01");
            }
            else muzzle=gameplayVisual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==socketName && t.GetComponent<MeshFilter>()==null);
            Check(Vector3.Distance(muzzle.position,hardpoints[(int)mount["id"]].transform.position)<.00001f,"Hardpoint muzzle mapping");
        }
        var data=Load(DATA);
        Check(Value(data,"Hull")==3500 && Value(data,"Shields")==3000 && Value(data,"Speed")==31,"Reference stats");
        Check(data.FindProperty("hangarBays").arraySize==0,"No hangar loadout");
        Check(!ship.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c.GetType().Name=="HangarComponent"),"No unbound donor HangarComponent");
        var loadout=data.FindProperty("weaponLoadout"); Check(loadout.arraySize==4,"Loadout count");
        for(int i=0;i<4;i++) Check(loadout.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==profiles[i] && loadout.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue==counts[i],"Data loadout");
        var hpHealth=data.FindProperty("hardPointHealth");
        foreach(var hp in hardpoints) Check(Enumerable.Range(0,hpHealth.arraySize).Any(i=>hpHealth.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)hp.HardPointType),"Hardpoint health entry");
        var spawn=Component(AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW),"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Placement hologram slots");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/"+NAME+"Icon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId); string iconKey=iconGuid+":"+iconId;
        var faction=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),SHIP_ID).FindPropertyRelative("value");
        Check(faction.FindPropertyRelative("<Price>k__BackingField").intValue==3200 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue==40,"Roster reference cost/time");
        Check(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Select(i=>tooltip.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==spawn,"Placement registration");
        Check(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"),SHIP_ID).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship-data registration");
        foreach(string asset in new[]{DATA,GAMEPLAY}) Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(asset)).address==Path.GetFileNameWithoutExtension(asset),"Addressable entry");
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),45).FindPropertyRelative("value");
        Check(ability.FindPropertyRelative("duration").floatValue==8 && ability.FindPropertyRelative("recoveryDelay").floatValue==35,"Ability timing");
        var modifiers=ability.FindPropertyRelative("settings.statModifier");
        Check(modifiers.FindPropertyRelative("shieldRegenMultiplier").floatValue==1.6f && modifiers.FindPropertyRelative("damageMultiplier").floatValue==.8f && modifiers.FindPropertyRelative("speedMultiplier").floatValue==.8f,"Exact ability modifiers");
        var weaponData=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        foreach(int profile in profiles) Check(weaponData.GetProfile((WeaponType)profile).Damage>0,"Resolved weapon profile");
        report["gameplay"]=new JObject{["hull"]=3500,["shields"]=3000,["speed"]=31,["price"]=3200,["buildSeconds"]=40,["targetableHardpoints"]=14,["weaponProfiles"]=new JArray(profiles),["weaponCounts"]=new JArray(counts),["registrationsVerified"]=true,["abilityVerified"]=true};
        File.WriteAllText(TASK+"VerificationReport.json",report.ToString());
        return report.ToString();
    }
    static void Check(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static float Value(SerializedObject data,string field)=>data.FindProperty("<"+field+">k__BackingField").floatValue;
    static SerializedProperty Row(SerializedProperty array,int id)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==id);
    static MonoBehaviour Component(GameObject root,string type)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==type);
    static UnityEngine.Object[] References(MonoBehaviour component,string field) { var p=new SerializedObject(component).FindProperty(field); return Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray(); }
    static JArray Vector(Vector3 v)=>new JArray(v.x,v.y,v.z);
    static Bounds BoundsOf(GameObject root) { var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds; }
}
