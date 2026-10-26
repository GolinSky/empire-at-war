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

public static class VerifyProcursator
{
    const string NAME = "Procursator";
    const string TASK = "Temp/ProcursatorImport/";
    const string VIEW = "Assets/Prefabs/Models/Ships/Empire/ProcursatorShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Empire/ProcursatorShipData.asset";
    static int _checks;

    public static string Main()
    {
        _checks=0;
        Check(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorUtility.scriptCompilationFailed,"Editor imports/compilation");
        var conversion=JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        float boneError=0,muzzleError=0; int triangles=0,meshCount=0,boneCount=0;
        foreach(var entry in ((JObject)conversion["models"]).Properties())
        {
            var source=entry.Value["before"];
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/Procursator/"+entry.Name+".fbx");
            var transforms=model.GetComponentsInChildren<Transform>(true);
            foreach(var bone in ((JObject)source["bones"]).Properties())
            {
                var actual=transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
                var head=bone.Value["head"].Values<float>().ToArray();
                float error=Vector3.Distance(actual.position,new Vector3(-head[0],head[2],-head[1])*.02f);
                boneError=Mathf.Max(boneError,error);boneCount++;
                string parent=(string)bone.Value["parent"];
                Check(parent==null || actual.parent.name==parent,"Source bone parent: "+bone.Name);
            }
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var mesh=renderer is SkinnedMeshRenderer?((SkinnedMeshRenderer)renderer).sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
                var original=source["meshes"][renderer.name];
                Check(mesh.triangles.Length/3==(int)original["triangles"],"Unity triangles: "+entry.Name+"/"+renderer.name);
                Check(mesh.uv.Length==mesh.vertexCount,"Unity UVs");
                Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(original["materials"].Values<string>()),"Binary material slots");
                foreach(var material in renderer.sharedMaterials)
                {
                    Check(AssetDatabase.GetAssetPath(material).StartsWith("Assets/Art/Materials/Models/EmpireShips/Procursator/"),"External remap");
                    var definition=conversion["materials"].Single(m=>(string)m["name"]==material.name);
                    foreach(string map in new[]{"base","normal"})
                    {
                        string stem=(string)definition[map];if(stem==null)continue;
                        string path="Assets/Art/Textures/Models/EmpireShips/Procursator/"+stem+".png";
                        Check(material.GetTexture(map=="base"?"_BaseMap":"_BumpMap")==AssetDatabase.LoadAssetAtPath<Texture2D>(path),"Texture remap");
                    }
                }
                triangles+=mesh.triangles.Length/3;meshCount++;
            }
        }
        Check(boneError<.0002f,"Unity source bone positions");
        var prefabReports=new JArray();
        foreach(string path in new[]{"Assets/Prefabs/Models/Ships/Empire/Procursator.prefab",VIEW,"Assets/Prefabs/Ui/Reinforcement/ProcursatorReinforcementView.prefab","Assets/Prefabs/Models/Wrecks/ProcursatorWreckView.prefab"})
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
        var visual=ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME && t.parent.name=="BankingBody").gameObject;
        var bounds=BoundsOf(visual);Check(Mathf.Abs(bounds.size.z-126.5464f)<.001f,"Venator 1.06x hull length");
        var hardpoints=References(Component(ship,"HealthComponent"),"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(hardpoints.Length==17 && hardpoints.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,17)),"17 ordered targetable hardpoints");
        Check(weapons.Length==15 && weapons.All(w=>hardpoints.Contains(w)),"15 targetable weapons");
        var injectables=References(Component(ship,"EntityBindings"),"<Injectables>k__BackingField");
        Check(injectables.All(c=>c!=null),"Injection list contains no emptied donor slots");
        Check(injectables.Length==18 && injectables.Distinct().Count()==18,"18 unique injectable components");
        Check(hardpoints.All(h=>injectables.Contains(h)) && injectables.Contains(Component(ship,"TeamColorView")),"Every current hardpoint/team view is injected");
        int[] types={87,5,86,32};int[] counts={3,2,8,2};
        for(int i=0;i<types.Length;i++)Check(weapons.Count(w=>(int)w.WeaponType==types[i])==counts[i],"Weapon composition");
        Check(hardpoints.Count(h=>(int)h.HardPointType==1)==1 && hardpoints.Count(h=>(int)h.HardPointType==2)==1,"Engine/shield targets");
        var audit=JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"));
        var allBones=visual.GetComponentsInChildren<Transform>(true);var hull=allBones.Single(t=>t.name==NAME+"Rig");
        foreach(var hp in hardpoints)
        {
            var row=audit["hardpoints"].Single(r=>(string)r["name"]==hp.name);
            var values=row["values"];string bone=(string)values[hp is WeaponHardPoint?"Fire_Bone_A":"Attachment_Bone"][0];
            var attachment=audit["attachments"].SingleOrDefault(a=>(string)a["hardpoint"]==hp.name);
            var owner=attachment==null?hull:allBones.Single(t=>t.name==(string)attachment["variant"]);
            var socket=owner.GetComponentsInChildren<Transform>(true).Single(t=>t.name==bone && t.GetComponent<Renderer>()==null);
            float error=Vector3.Distance(socket.position,hp.transform.position);muzzleError=Mathf.Max(muzzleError,error);
            Check(error<.00001f,"Source projectile/subsystem origin: "+hp.name);
        }
        Check(weapons.Where(w=>(int)w.WeaponType==87 || (int)w.WeaponType==32).All(w=>Mathf.Abs((w.MinYaw+w.MaxYaw)*.5f)<.001f),"Forward heavy/ion arcs");
        var data=Load(DATA);
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==6800 && data.FindProperty("<Shields>k__BackingField").floatValue==4200 && data.FindProperty("<Speed>k__BackingField").floatValue==19,"Requested stats");
        Check(data.FindProperty("<ShipClass>k__BackingField").intValue==4 && data.FindProperty("hangarBays").arraySize==0,"Capital/no hangar");
        Check(!ship.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>c.GetType().Name=="HangarComponent"),"No unbound donor HangarComponent on Procursator");
        Check(data.FindProperty("weaponLoadout").arraySize==4,"Loadout taxonomy");
        foreach(var hp in hardpoints)Check(Enumerable.Range(0,data.FindProperty("hardPointHealth").arraySize).Any(i=>data.FindProperty("hardPointHealth").GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)hp.HardPointType),"Health type mapping");
        var collider=ship.GetComponent<BoxCollider>();Check((collider.size-bounds.size).magnitude<.001f,"Collision size");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ProcursatorReinforcementView.prefab");var spawn=Component(preview,"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Hologram slots");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ProcursatorIcon.png");Check(icon!=null && icon.texture.width==512 && icon.texture.height==512,"512 sprite");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);string iconKey=iconGuid+":"+iconId;
        var faction=Row(Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset").FindProperty("ships.keyValue"),212);
        Check(faction.FindPropertyRelative("<Price>k__BackingField").intValue==5500 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue==55,"Requested economy");
        Check(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),212).objectReferenceValue==icon,"HUD icon");
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Select(i=>tooltip.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),212).objectReferenceValue==spawn,"Preview registration");
        Check(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"),212).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Data registration");
        foreach(string path in new[]{VIEW,DATA})
        {
            string key=Path.GetFileNameWithoutExtension(path),guid=AssetDatabase.AssetPathToGUID(path);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable address");
            var mapping=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mapping.arraySize).Select(i=>mapping.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key).FindPropertyRelative("value.m_AssetGUID").stringValue==guid,"Asset mapping");
        }
        Check(data.FindProperty("abilities").arraySize==1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==5,"Existing Power to Weapons loadout");
        var weaponData=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");var audio=AssetDatabase.LoadAssetAtPath<ShipSfxData>("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(int type in types){var profile=weaponData.GetProfile((WeaponType)type);Check(profile.Range>0 && profile.Reload>0,"Derived weapon profile");Check(audio.GetWeapon((WeaponType)type,profile.Family)!=null,"Weapon family audio");}
        Check((int)weaponData.GetDefinition((WeaponType)86).Mount==3,"Light triple mount");
        foreach(int type in new[]{86,87})
        {
            var definition=weaponData.GetDefinition((WeaponType)type);
            Check(definition.Unit=="Procursator" && (int)definition.Scope==2,"Own weapon scope");
            Check(!string.IsNullOrEmpty(definition.Reason),"Weapon provenance reason");
        }
        Check(data.FindProperty("<Range>k__BackingField").floatValue>=types.Max(t=>weaponData.GetProfile((WeaponType)t).Range),"Radar covers longest battery");
        Check(audio.GetAbility((ShipAbilityId)5)!=null,"Ability audio");
        var report=new JObject{["checks"]=_checks,["meshes"]=meshCount,["triangles"]=triangles,["bones"]=boneCount,["boneErrorUnits"]=boneError,["muzzleErrorUnits"]=muzzleError,["prefabs"]=prefabReports,["size"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z),["hardpoints"]=17,["weapons"]=15,["registrationsVerified"]=true};
        File.WriteAllText(TASK+"UnityVerification.json",report.ToString());
        return "Procursator saved assets verified: "+_checks+" checks, 12 source models, 17 targets/15 weapons, stats and all registrations.";
    }

    static void Check(bool value,string message){_checks++;if(!value)throw new InvalidOperationException(message);}
    static MonoBehaviour Component(GameObject root,string type)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==type);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key).FindPropertyRelative("value");
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static Bounds BoundsOf(GameObject root){var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);return bounds;}
}
