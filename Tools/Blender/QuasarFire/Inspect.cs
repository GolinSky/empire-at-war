using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;

public static class InspectQuasarFire
{
    const string NAME="QuasarFire";
    const string TASK="Temp/QuasarFireImport/";
    const string VIEW="Assets/Prefabs/Models/Ships/Rebellion/QuasarFireShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/Rebellion/QuasarFireShipData.asset";
    public static string Main()
    {
        var conversion=JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/QuasarFire/QuasarFire.fbx");
        var transforms=model.GetComponentsInChildren<Transform>(true);
        float boneError=0; int triangles=0;
        foreach(var record in ((JObject)conversion["before"]["bones"]).Properties())
        {
            var bone=transforms.Single(t=>t.name==record.Name && !t.GetComponents<Renderer>().Any());
            float[] p=record.Value["head"].Values<float>().ToArray();
            boneError=Mathf.Max(boneError,Vector3.Distance(bone.position,new Vector3(-p[0],p[2],-p[1])*.02f));
            string parent=(string)record.Value["parent"];
            Check(parent==null || bone.parent.name==parent,"Bone parent: "+record.Name);
        }
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var mesh=renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponents<MeshFilter>().Single().sharedMesh;
            var original=conversion["before"]["meshes"][renderer.name];
            Check(mesh.triangles.Length/3==(int)original["triangles"],"Triangle count: "+renderer.name);
            Check(mesh.uv.Length==mesh.vertexCount,"UVs: "+renderer.name);
            Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(original["materials"].Values<string>()),"Material slots: "+renderer.name);
            triangles+=mesh.triangles.Length/3;
        }
        Check(boneError<.00001f,"Attachment tolerance");
        foreach(string path in new[]{VIEW,"Assets/Prefabs/Models/Ships/Rebellion/QuasarFire.prefab","Assets/Prefabs/Ui/Reinforcement/QuasarFireReinforcementView.prefab","Assets/Prefabs/Models/Wrecks/QuasarFireWreckView.prefab"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.transform.localScale==Vector3.one,"Root scale: "+path);
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script");
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                var p=new SerializedObject(component).GetIterator();
                while(p.Next(true)) if(p.propertyType==SerializedPropertyType.ObjectReference)
                    Check(p.objectReferenceValue!=null || p.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference: "+component.name+"."+p.propertyPath);
            }
            Check(!root.GetComponentsInChildren<MeshRenderer>(true).Any(r=>new[]{"Shield","Shadow","Col"}.Contains(r.name)),"Source helper retained");
        }
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var health=Component(ship,"HealthComponent");
        var points=Refs(health,"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=Refs(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(points.Length==9 && points.Select(p=>p.Id).SequenceEqual(Enumerable.Range(0,9)),"Hardpoint IDs");
        Check(weapons.Length==4 && weapons.All(points.Contains),"Targetable weapons");
        Check(weapons.Count(p=>(int)p.WeaponType==1)==2 && weapons.Count(p=>(int)p.WeaponType==14)==2,"Weapon composition");
        Check(points.Count(p=>(int)p.HardPointType==1)==1 && points.Count(p=>(int)p.HardPointType==4)==2 && points.Count(p=>(int)p.HardPointType==7)==2,"Systems");
        var body=(Transform)new SerializedObject(Component(ship,"ShipMoveComponent")).FindProperty("bodyTransform").objectReferenceValue;
        Check(points.All(p=>p.transform.IsChildOf(body)),"Banked hardpoints");
        var hangar=Component(ship,"HangarComponent");
        var bayPoints=Refs(hangar,"bayHardPoints"); var launches=Refs(hangar,"bayLaunchPoints").Cast<Transform>().ToArray();
        Check(bayPoints.Length==2 && bayPoints.All(p=>points.Contains((HardPoint)p)) && launches.Length==2,"Bay bindings");
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA));
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==1700 && data.FindProperty("<Shields>k__BackingField").floatValue==1300 && data.FindProperty("<Speed>k__BackingField").floatValue==34,"Reference stats");
        var bays=data.FindProperty("hangarBays");
        Check(bays.arraySize==2,"Bay count");
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);Check(bay.FindPropertyRelative("squadronType").intValue==(i==0?300:3) && bay.FindPropertyRelative("reserve").intValue==2 && bay.FindPropertyRelative("maxActive").intValue==1,"Complement");}
        var hpHealth=data.FindProperty("hardPointHealth");
        foreach(var point in points)Check(Enumerable.Range(0,hpHealth.arraySize).Any(i=>hpHealth.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)point.HardPointType),"Hardpoint health");
        Check(data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==(int)ShipAbilityId.FighterReinforcements,"Ability assignment");
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        foreach(string path in new[]{VIEW,DATA})Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address==Path.GetFileNameWithoutExtension(path),"Addressable");
        int id=(int)ShipType.QuasarFire;
        var faction=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset"));
        var row=Row(faction.FindProperty("ships.keyValue"),id).FindPropertyRelative("value");
        Check(row.FindPropertyRelative("<Price>k__BackingField").intValue==2400 && row.FindPropertyRelative("<BuildTime>k__BackingField").intValue==35,"Roster balance");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/QuasarFireIcon.png");
        Check(row.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon,"Roster icon");
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Check(Row(ui.FindProperty("shipIconWrapper.keyValue"),id).FindPropertyRelative("value").objectReferenceValue==icon,"Battle icon");
        var previews=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var preview=Row(previews.FindProperty("spawnShipWrapper.keyValue"),id).FindPropertyRelative("value").objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(preview)=="Assets/Prefabs/Ui/Reinforcement/QuasarFireReinforcementView.prefab","Placement registration");
        var spawn=new SerializedObject(preview);var hologram=spawn.FindProperty("hologramMaterial").objectReferenceValue;
        Check(Refs((MonoBehaviour)preview,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.Contains(hologram)),"Hologram slots");
        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var ability=Row(catalog.FindProperty("definitions.keyValue"),(int)ShipAbilityId.FighterReinforcements).FindPropertyRelative("value");
        Check(ability.FindPropertyRelative("recoveryDelay").floatValue==30 && ability.FindPropertyRelative("settings").managedReferenceValue is EmpireAtWar.Services.ShipAbilities.Abilities.FighterReinforcementsSettings,"Ability catalog");
        var report=new JObject{["shipId"]=id,["abilityId"]=(int)ShipAbilityId.FighterReinforcements,["sourceMeshes"]=9,["sourceTriangles"]=triangles,["sourceBones"]=((JObject)conversion["before"]["bones"]).Count,["unityBoneError"]=boneError,["targetableHardpoints"]=points.Length,["weapons"]=weapons.Length,["savedReferencesVerified"]=true,["runtimeVerified"]=false};
        File.WriteAllText(TASK+"VerifiedRegistration.json",report.ToString());
        return report.ToString();
    }
    static void Check(bool pass,string name){if(!pass)throw new InvalidOperationException(name);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    static UnityEngine.Object[] Refs(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
}
