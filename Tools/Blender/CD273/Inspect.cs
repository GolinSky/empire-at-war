using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class InspectCD273
{
    const int SHIP_ID=308;
    const int ABILITY_ID=37;
    const string TASK="Temp/CD273Import/";
    const string VISUAL="Assets/Prefabs/Models/Ships/Rebellion/CD273.prefab";
    const string GAMEPLAY="Assets/Prefabs/Models/Ships/Rebellion/CD273ShipView.prefab";
    const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/CD273ReinforcementView.prefab";
    const string WRECK="Assets/Prefabs/Models/Wrecks/CD273WreckView.prefab";
    const string DATA="Assets/Settings/Data/Ship/Rebellion/CD273ShipData.asset";

    public static string Main()
    {
        var conversion=JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"))["models"]["CD273"];
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/CD273/CD273.fbx");
        var transforms=model.GetComponentsInChildren<Transform>(true);
        float boneError=0;int triangles=0;
        foreach(var bone in ((JObject)conversion["before"]["bones"]).Properties())
        {
            var transform=transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
            var head=bone.Value["head"].Values<float>().ToArray();
            boneError=Mathf.Max(boneError,Vector3.Distance(transform.position,new Vector3(-head[0],head[2],-head[1])*.02f));
            string parent=(string)bone.Value["parent"];
            Check(parent==null || transform.parent.name==parent,"Bone parent: "+bone.Name);
        }
        var sourceMaterials=JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"))["variants"]["CD273"]["material_audit"]["meshes"];
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var mesh=renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
            string name=renderer.name.Split('.')[0];var expected=conversion["after"]["meshes"][name];
            Check(mesh.triangles.Length/3==(int)expected["triangles"],"Unity triangles: "+name);
            Check(mesh.uv.Length==mesh.vertexCount,"Unity UVs: "+name);
            Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(conversion["before"]["meshes"][name]["materials"].Values<string>()),"Material remap: "+name);
            var slots=sourceMaterials.Single(m=>(string)m["name"]==name)["materials"];
            Check(slots.Count()==mesh.subMeshCount,"Source slots: "+name);
            if(!(bool)expected["hidden"])
                for(int i=0;i<mesh.subMeshCount;i++)Check(mesh.GetIndices(i).Length/3==(int)slots[i]["triangleCount"],"Source slot triangles: "+name);
            triangles+=mesh.triangles.Length/3;
        }
        Check(boneError<.00001f,"Unity attachment error");
        var saved=new JArray();
        foreach(string path in new[]{VISUAL,GAMEPLAY,PREVIEW,WRECK})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.transform.localScale==Vector3.one,"Root scale: "+path);
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script: "+path);
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                var property=new SerializedObject(component).GetIterator();
                while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)
                    Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference: "+component.name+"."+property.propertyPath);
            }
            var geometry=path==GAMEPLAY?root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="CD273" && t.parent.name=="BodyPivot").gameObject:root;
            var bounds=BoundsOf(geometry);
            float expectedLength=path==WRECK ? BoundsOfOpaque(AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL)).size.z : 60;
            Check(Mathf.Abs(bounds.size.z-expectedLength)<.001f,"Hull length: "+path);
            Check(!geometry.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.name=="Coll" || r.name.Contains("Shadow") || r.name=="Shield"),"Source helpers: "+path);
            saved.Add(new JObject{["path"]=path,["size"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z)});
        }
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY);
        var targets=References(Component(ship,"HealthComponent"),"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(targets.Length==10 && targets.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,10)),"Ten ordered targets");
        Check(weapons.Length==8 && weapons.All(w=>targets.Contains(w)),"Targetable weapons");
        int[] profiles={23,13,14},counts={4,2,2};
        for(int i=0;i<3;i++)Check(weapons.Count(w=>(int)w.WeaponType==profiles[i])==counts[i],"Weapon count: "+profiles[i]);
        Check(targets.Count(h=>(int)h.HardPointType==1)==1 && targets.Count(h=>(int)h.HardPointType==2)==1,"Engine/shield");
        Check(References(Component(ship,"FogVisibilityComponent"),"hardPoints").SequenceEqual(targets),"Fog targets");
        foreach(var mapping in JArray.Parse(File.ReadAllText(TASK+"HardpointMapping.json")).Where(m=>m["derivedShield"]==null))
        {
            var bone=ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)mapping["bone"] && t.IsChildOf(ship.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="CD273Rig")));
            Check(Vector3.Distance(bone.position,targets[(int)mapping["id"]].transform.position)<.00001f,"Gameplay socket: "+bone.name);
        }
        Check(ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Front").position.z>ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Rear").position.z,"Bow +Z");
        var data=Load(DATA);
        Check(Number(data,"Hull")==2400 && Number(data,"Shields")==1700 && Number(data,"Speed")==52,"Requested stats");
        Check(data.FindProperty("<ShipClass>k__BackingField").intValue==2,"Heavy corvette class");
        Check(data.FindProperty("hangarBays").arraySize==0,"No hangar loadout");
        Check(!ship.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c.GetType().Name=="HangarComponent"),"No unbound donor HangarComponent");
        Check(data.FindProperty("hardPointHealth").arraySize==3,"Hardpoint health entries");
        Check(data.FindProperty("abilities").arraySize==1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==ABILITY_ID,"Ability loadout");
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var opaque=visual.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit").ToArray();
        Check(References(Component(ship,"TeamColorView"),"meshRenderers").Length==opaque.Length,"Team renderer ownership");
        foreach(var material in opaque.SelectMany(r=>r.sharedMaterials).Distinct())
        {
            var normal=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")));
            Check(normal.textureType==TextureImporterType.NormalMap && !normal.sRGBTexture && normal.flipGreenChannel,"Normal-map import");
            Check(material.GetFloat("_TeamRimStrength")==0,"Neutral hull preservation");
        }
        var spawn=Component(AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW),"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Hologram slots");
        var wreckData=new SerializedObject(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue);
        Check(AssetDatabase.GetAssetPath(wreckData.FindProperty("<Prefab>k__BackingField").objectReferenceValue)==WRECK,"Own wreck");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/CD273Icon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);string iconKey=iconGuid+":"+iconId;
        var faction=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),SHIP_ID).FindPropertyRelative("value");
        Check(faction.FindPropertyRelative("<Price>k__BackingField").intValue==2200 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue==32,"Roster price/build");
        Check(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Select(i=>tooltip.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==spawn,"Placement registration");
        Check(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"),SHIP_ID).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship-data registration");
        foreach(string path in new[]{GAMEPLAY,DATA})
        {
            string key=Path.GetFileNameWithoutExtension(path),guid=AssetDatabase.AssetPathToGUID(path);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable address");
            var mappings=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mappings.arraySize).Select(i=>mappings.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key).FindPropertyRelative("value.m_AssetGUID").stringValue==guid,"Asset mapping");
        }
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),ABILITY_ID).FindPropertyRelative("value");
        Check(ability.FindPropertyRelative("displayName").stringValue=="Power to Engines" && ability.FindPropertyRelative("settings.statModifier.speedMultiplier").floatValue==1.35f,"Engine ability");
        foreach(string field in new[]{"damageMultiplier","fireDelayMultiplier","shieldRegenMultiplier","damageTakenMultiplier"})Check(ability.FindPropertyRelative("settings.statModifier."+field).floatValue==1,"No inherited ability penalties");
        Check(ability.FindPropertyRelative("duration").floatValue==20 && ability.FindPropertyRelative("recoveryDelay").floatValue==50,"Ability timing");
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var definitions=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset").FindProperty("weapons");
        foreach(int type in profiles)
        {
            var definition=Enumerable.Range(0,definitions.arraySize).Select(i=>definitions.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("weaponType").intValue==type);
            int family=definition.FindPropertyRelative("family").intValue;
            var resolved=((EmpireAtWar.Services.Audio.ShipSfxData)audio.targetObject).GetWeapon((EmpireAtWar.Components.AttackComponent.WeaponType)type,(EmpireAtWar.Components.AttackComponent.WeaponFamily)family);
            Check(resolved!=null,"Resolved weapon audio");
        }
        Check(Enumerable.Range(0,audio.FindProperty("abilities").arraySize).Any(i=>audio.FindProperty("abilities").GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID),"Ability audio");
        var report=new JObject{["meshes"]=15,["bones"]=107,["triangles"]=triangles,["boneErrorUnits"]=boneError,["prefabs"]=saved,["targetableHardpoints"]=10,["registrationsVerified"]=true};
        File.WriteAllText(TASK+"UnityInspection.json",report.ToString());return report.ToString();
    }
    static float Number(SerializedObject data,string name)=>data.FindProperty("<"+name+">k__BackingField").floatValue;
    static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static Bounds BoundsOf(GameObject root){var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds;}
    static Bounds BoundsOfOpaque(GameObject root){var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit").SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds;}
}
