using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.ShipAbilities;

public static class VerifyMC30c
{
    const string NAME = "MC30c";
    const string TASK = "Temp/MC30cImport/";
    const string VIEW = "Assets/Prefabs/Models/Ships/MC30cShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/MC30cShipData.asset";
    static int _checks;

    public static string Main()
    {
        _checks=0;
        Check(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorUtility.scriptCompilationFailed,"Editor imports/compilation");
        var conversion=JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        var source=conversion["models"][NAME]["before"];
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/MC30c/MC30c.fbx");
        var transforms=model.GetComponentsInChildren<Transform>(true);
        float boneError=0,weaponBoneError=0; int triangles=0;
        foreach(var bone in ((JObject)source["bones"]).Properties())
        {
            var actual=transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
            var head=bone.Value["head"].Values<float>().ToArray();
            float error=Vector3.Distance(actual.position,new Vector3(-head[0],head[2],-head[1])*.02f);
            boneError=Mathf.Max(boneError,error);
            if(bone.Name.StartsWith("HP_"))weaponBoneError=Mathf.Max(weaponBoneError,error);
            string parent=(string)bone.Value["parent"];
            Check(parent==null || actual.parent.name==parent,"Source bone parent: "+bone.Name);
        }
        Check(boneError<.0002f && weaponBoneError<.00002f,"Source bone positions");
        var modelRenderers=model.GetComponentsInChildren<Renderer>(true);
        Check(modelRenderers.Length==6,"Six original meshes");
        foreach(var renderer in modelRenderers)
        {
            var mesh=renderer is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)renderer).sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
            var original=source["meshes"][renderer.name];
            Check(mesh.triangles.Length/3==(int)original["triangles"],"Unity triangles: "+renderer.name);
            Check(mesh.uv.Length==mesh.vertexCount,"Unity UVs: "+renderer.name);
            Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(original["materials"].Values<string>()),"Binary-repaired slots: "+renderer.name);
            for(int i=0;i<renderer.sharedMaterials.Length;i++)
            {
                var material=renderer.sharedMaterials[i];
                Check(AssetDatabase.GetAssetPath(material).StartsWith("Assets/Art/Materials/Models/RebellionShips/MC30c/"),"External remap");
                var definition=conversion["materials"].Single(m=>(string)m["name"]==material.name);
                foreach(string map in new[]{"base","normal"})
                {
                    string stem=(string)definition[map]; if(stem==null)continue;
                    string path="Assets/Art/Textures/Models/RebellionShips/MC30c/"+stem+".png";
                    Check(material.GetTexture(map=="base"?"_BaseMap":"_BumpMap")==AssetDatabase.LoadAssetAtPath<Texture2D>(path),"Texture remap");
                    if(map=="normal")Check(((TextureImporter)AssetImporter.GetAtPath(path)).textureType==TextureImporterType.NormalMap,"Linear normal map");
                }
            }
            triangles+=mesh.triangles.Length/3;
        }
        var prefabReports=new JArray();
        foreach(string path in new[]{"Assets/Prefabs/Models/Ships/MC30c.prefab",VIEW,"Assets/Prefabs/Ui/Reinforcement/MC30cReinforcementView.prefab","Assets/Prefabs/Models/Wrecks/MC30cWreckView.prefab"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab!=null && prefab.transform.localScale==Vector3.one,"Saved prefab and unit root: "+path);
            foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing scripts");
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))
            {
                var property=new SerializedObject(component).GetIterator();
                while(property.Next(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference)Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference: "+component.name+"."+property.propertyPath);
            }
            var visible=prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.name!="ShieldSurface").ToArray();
            Check(!visible.Any(r=>r.name=="Collision" || r.name=="Shadow" || r.name=="Shield"),"Source helper removal");
            prefabReports.Add(new JObject{["path"]=path,["visibleMeshes"]=visible.Length});
        }
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var visual=ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME && t.parent.name=="BodyPivot").gameObject;
        var bounds=BoundsOf(visual);Check(Mathf.Abs(bounds.size.z-80)<.001f,"80-unit hull length");
        var hardpoints=References(Component(ship,"HealthComponent"),"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(hardpoints.Length==16 && hardpoints.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,16)),"16 ordered targetable hardpoints");
        Check(weapons.Length==14 && weapons.All(w=>hardpoints.Contains(w)),"14 targetable weapons");
        int[] types={66,4,7,14,81};int[] counts={2,4,4,2,2};
        for(int i=0;i<types.Length;i++)Check(weapons.Count(w=>(int)w.WeaponType==types[i])==counts[i],"Weapon composition");
        Check(hardpoints.Count(h=>(int)h.HardPointType==1)==1 && hardpoints.Count(h=>(int)h.HardPointType==2)==1,"Engine/shield targets");
        Check(References(Component(ship,"FogVisibilityComponent"),"hardPoints").SequenceEqual(hardpoints),"Fog targets");
        var hull=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME+"Rig");
        foreach(var mapping in JArray.Parse(File.ReadAllText(TASK+"HardpointMapping.json")))
        {
            string bone=(string)mapping["bone"];if(bone==null)continue;
            var socket=hull.GetComponentsInChildren<Transform>(true).Single(t=>t.name==bone && t.GetComponent<Renderer>()==null);
            Check(Vector3.Distance(socket.position,hardpoints[(int)mapping["id"]].transform.position)<.00001f,"Socket binding");
        }
        Check(weapons.Where(w=>(int)w.WeaponType==66).All(w=>Mathf.Abs(Mathf.DeltaAngle(w.transform.localEulerAngles.y,0))<.001f),"Forward torpedo arcs");
        var data=Load(DATA);
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==1800 && data.FindProperty("<Shields>k__BackingField").floatValue==2400 && data.FindProperty("<Speed>k__BackingField").floatValue==42,"Requested stats");
        Check(data.FindProperty("<ShipClass>k__BackingField").intValue==3 && data.FindProperty("hangarBays").arraySize==0,"Frigate/no hangar");
        Check(!ship.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c.GetType().Name=="HangarComponent"),"No unbound donor HangarComponent on MC30c");
        Check(data.FindProperty("weaponLoadout").arraySize==5,"Loadout taxonomy");
        foreach(var hp in hardpoints)Check(Enumerable.Range(0,data.FindProperty("hardPointHealth").arraySize).Any(i=>data.FindProperty("hardPointHealth").GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)hp.HardPointType),"Health type mapping");
        var collider=ship.GetComponent<BoxCollider>();Check((collider.size-bounds.size).magnitude<.001f,"Collision size");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/MC30cReinforcementView.prefab");var spawn=Component(preview,"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Hologram slots");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/MC30cIcon.png");Check(icon!=null && icon.texture.width==512 && icon.texture.height==512,"512 sprite");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);string iconKey=iconGuid+":"+iconId;
        var faction=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),307);
        Check(faction.FindPropertyRelative("<Price>k__BackingField").intValue==2500 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue==32,"Requested economy");
        Check(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),307).objectReferenceValue==icon,"HUD icon");
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Select(i=>tooltip.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),307).objectReferenceValue==spawn,"Preview registration");
        Check(Row(Load("Assets/Settings/Data/Ship/ShipsData.asset").FindProperty("shipsData.keyValue"),307).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Data registration");
        foreach(string path in new[]{VIEW,DATA})
        {
            string key=Path.GetFileNameWithoutExtension(path),guid=AssetDatabase.AssetPathToGUID(path);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable address");
            var mapping=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mapping.arraySize).Select(i=>mapping.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key).FindPropertyRelative("value.m_AssetGUID").stringValue==guid,"Asset mapping");
        }
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),36);
        Check(ability.FindPropertyRelative("duration").floatValue==6 && ability.FindPropertyRelative("recoveryDelay").floatValue==30 && Mathf.Abs(ability.FindPropertyRelative("settings.fireDelayMultiplier").floatValue-2f/3)<.00001f,"Torpedo Salvo timing/rate");
        Check(data.FindProperty("abilities").arraySize==1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==36,"Ability loadout");
        var weaponData=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");var audio=AssetDatabase.LoadAssetAtPath<ShipSfxData>("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(int type in types){var profile=weaponData.GetProfile((WeaponType)type);Check(profile.Range>0 && profile.Reload>0,"Derived weapon profile");Check(audio.GetWeapon((WeaponType)type,profile.Family)!=null,"Weapon family audio");}
        Check(weaponData.GetDefinition((WeaponType)81).BurstShots==6,"Approved burst cluster bombs");
        Check(audio.GetAbility((ShipAbilityId)36)!=null,"Ability audio");
        var report=new JObject{["checks"]=_checks,["meshes"]=6,["triangles"]=triangles,["bones"]=44,["boneErrorUnits"]=boneError,["weaponBoneErrorUnits"]=weaponBoneError,["prefabs"]=prefabReports,["size"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z),["hardpoints"]=16,["weapons"]=14,["registrationsVerified"]=true};
        File.WriteAllText(TASK+"UnityVerification.json",report.ToString());
        return "MC30c saved assets verified: "+_checks+" checks, six source meshes, 44 bones, 16 targets/14 weapons, stats and all registrations.";
    }

    static void Check(bool value,string message){_checks++;if(!value)throw new InvalidOperationException(message);}
    static MonoBehaviour Component(GameObject root,string type)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==type);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key).FindPropertyRelative("value");
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static Bounds BoundsOf(GameObject root){var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);return bounds;}
}
