using System;
using System.IO;
using System.Linq;
using EmpireAtWar;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class VerifyGolan
{
    private const string TASK="Temp/AotrGolanIIIImport/";
    private const string MODELS="Assets/Art/Models/SpaceStations/AotrGolanIII/";
    private const string VISUAL="Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatform.prefab";
    private const string VIEW="Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatformView.prefab";
    private const string DATA="Assets/Settings/Data/Models/DefendPlatform/AotrGolanIIIDefensePlatformData.asset";
    private const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/AotrGolanIIIDefensePlatformReinforcementView.prefab";
    public static string Main()
    {
        var reports=JArray.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        var audit=JObject.Parse(File.ReadAllText(TASK+"SourceAudit.json"));
        var geometry=new JObject();
        foreach(var report in reports)
        {
            string name=(string)report["name"];
            var raw=AssetDatabase.LoadAssetAtPath<GameObject>(MODELS+name+".fbx");
            var bones=new JObject();
            foreach(var bone in raw.GetComponentsInChildren<Transform>(true).Where(t=>report["source"]["bones"][t.name]!=null))
                bones[bone.name]=new JObject { ["parent"]=report["source"]["bones"][bone.name]["parent"], ["position"]=V(bone.position) };
            Check(bones.Count==((JObject)report["source"]["bones"]).Count,"Bone count: "+name);
            var meshes=new JObject();
            foreach(var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))
            {
                var row=report["source"]["meshes"][renderer.name];
                Check(renderer.transform.parent.name==(string)row["parent"],"Mesh parent: "+name+"/"+renderer.name);
                var sourceMaterials=renderer.sharedMaterials;
                Check(sourceMaterials.All(m=>m!=null&&AssetDatabase.GetAssetPath(m).StartsWith("Assets/Art/Materials/Models/SpaceStations/AotrGolanIII/")),"Material remap: "+name);
                var sourceMesh=audit["models"][name]["meshes"].Single(m=>(string)m["key"]==renderer.name);
                Check(sourceMaterials.Length==sourceMesh["materials"].Count(),"Material slot count: "+name+"/"+renderer.name);
                for(int slot=0;slot<sourceMaterials.Length;slot++)
                {
                    string reference=(string)sourceMesh["materials"][slot]["properties"]["BaseTexture"];
                    if(reference!=null) Check(sourceMaterials[slot].GetTexture("_BaseMap").name==(string)audit["textures"][reference.ToLowerInvariant()]["name"],"Source texture slot: "+name+"/"+renderer.name);
                    if(!row["hidden"].Value<bool>()) Check(renderer.GetComponent<MeshFilter>().sharedMesh.GetIndices(slot).Length==(int)sourceMesh["materials"][slot]["triangleCount"]*3,"Source submesh triangles: "+name+"/"+renderer.name);
                }
                if(!row["hidden"].Value<bool>())
                {
                    var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                    meshes[renderer.name]=new JObject {
                        ["vertices"]=new JArray(mesh.vertices.Select(v=>V(renderer.transform.TransformPoint(v)))),
                        ["uv"]=new JArray(mesh.uv.Select(v=>new JArray(v.x,v.y))),
                        ["normals"]=new JArray(mesh.normals.Select(v=>V(renderer.localToWorldMatrix.inverse.transpose.MultiplyVector(v).normalized))),
                        ["triangles"]=new JArray(mesh.triangles),
                        ["materials"]=new JArray(sourceMaterials.Select(AssetDatabase.GetAssetPath)) };
                }
            }
            geometry[name]=new JObject { ["bones"]=bones,["meshes"]=meshes };
        }
        File.WriteAllText(TASK+"UnityGeometry.json",geometry.ToString(Newtonsoft.Json.Formatting.None));
        foreach(string path in new[]{VISUAL,VIEW,PREVIEW})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                Check(root.transform.localScale==Vector3.one&&root.transform.localRotation==Quaternion.identity&&root.transform.localPosition==Vector3.zero,"Root transform: "+path);
                foreach(var component in root.GetComponentsInChildren<Component>(true))
                {
                    Check(component!=null,"Missing script: "+path);
                    var iterator=new SerializedObject(component).GetIterator();
                    while(iterator.Next(true))
                        if(iterator.propertyType==SerializedPropertyType.ObjectReference&&iterator.objectReferenceValue==null)
                            Check(iterator.objectReferenceInstanceIDValue==0,"Broken reference: "+path+"/"+iterator.propertyPath);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var points=prefab.GetComponentsInChildren<WeaponHardPoint>(true);
        Check(points.Length==36,"Weapon count");
        foreach(var group in new[]{new{type=WeaponType.HeavyLongRangeDualTurbolaser,count=12},new{type=WeaponType.HeavyAssaultMissile,count=4},new{type=WeaponType.HeavyProtonTorpedo,count=12},new{type=WeaponType.Laser,count=8}})
            Check(points.Count(p=>p.WeaponType==group.type)==group.count,"Weapon composition: "+group.type);
        var health=prefab.GetComponent<HealthComponent>();
        Check(health.ShipUnits.Count==39&&health.ShipUnits.Select(p=>p.Id).Distinct().Count()==39,"Targetable hardpoints/ids");
        Check(points.All(p=>health.ShipUnits.Contains(p)),"Targetable weapons");
        var shield=prefab.GetComponentsInChildren<Shield>(true).Single();
        Check(new SerializedObject(shield).FindProperty("hullPlanes").arraySize==1024,"Shield planes");
        var data=AssetDatabase.LoadAssetAtPath<DefendPlatformData>(DATA);
        Check(data.ComponentData.Hull==10000&&data.ComponentData.Shields==12000,"Requested health");
        var weapons=AssetDatabase.LoadAssetAtPath<EmpireAtWar.Components.AttackComponent.WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        foreach(var point in points) Check(weapons.GetProfile(point.WeaponType)!=null,"Weapon profile");
        var catalog=AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>("Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
        Check(catalog.Get(DefendPlatformType.GolanIII).Name=="Golan III","Catalog entry");
        Check(catalog.Get(DefendPlatformType.Xq6).Name=="XQ-6","Existing catalog entry");
        var mapping=new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/AssetMappingData.asset")).FindProperty("assetMappings.keyValue");
        foreach(string path in new[]{VIEW,DATA})
        {
            string key=Path.GetFileNameWithoutExtension(path);
            var row=Enumerable.Range(0,mapping.arraySize).Select(i=>mapping.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key);
            Check(row.FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(path),"Asset mapping: "+key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address==key,"Addressable: "+key);
        }
        foreach(string faction in new[]{"Empire","Rebellion","Republic","Separatist"})
        {
            var definition=AssetDatabase.LoadAssetAtPath<EmpireAtWar.Models.Factions.FactionDefinition>("Assets/Settings/Data/Factions/"+faction+"/"+faction+"Faction.asset");
            Check(definition.DefendPlatforms.Contains(DefendPlatformType.GolanIII)&&definition.DefendPlatforms.Contains(DefendPlatformType.Xq6),"Shared roster: "+faction);
        }
        var dependencies=AssetDatabase.GetDependencies(VIEW,true);
        Check(dependencies.Contains(VISUAL)&&reports.All(r=>dependencies.Contains(MODELS+(string)r["name"]+".fbx")),"Shared artwork references");
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var model=visual.transform.GetChild(0);
        var sourceBones=(JArray)audit["models"]["AotrGolanIII"]["bones"];
        var matrices=new Matrix4x4[sourceBones.Count];
        for(int i=0;i<sourceBones.Count;i++)
        {
            var matrix=Matrix4x4.identity;
            for(int r=0;r<3;r++) for(int c=0;c<4;c++) matrix[r,c]=(float)sourceBones[i]["matrix"][r*4+c];
            long parent=(long)sourceBones[i]["parent_index"];
            matrices[i]=parent==uint.MaxValue?matrix:matrices[parent]*matrix;
        }
        var basis=new Matrix4x4(new Vector4(-1,0,0,0),new Vector4(0,0,-1,0),new Vector4(0,1,0,0),new Vector4(0,0,0,1));
        foreach(var hp in audit["hardpoints"].Where(h=>h["artModel"]!=null))
        {
            string boneName=(string)hp["Attachment_Bone"];
            int index=Enumerable.Range(0,sourceBones.Count).Single(i=>((string)sourceBones[i]["name"]).Equals(boneName,StringComparison.OrdinalIgnoreCase));
            var pose=basis*matrices[index]*basis.inverse;
            Vector4 p=pose.GetColumn(3); pose.SetColumn(3,new Vector4(p.x*.02f,p.y*.02f,p.z*.02f,1));
            var expected=model.localToWorldMatrix*pose;
            var art=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)hp["name"]+"_Art");
            for(int r=0;r<4;r++) for(int c=0;c<4;c++) Check(Mathf.Abs(expected[r,c]-art.localToWorldMatrix[r,c])<.001f,"XML attachment pose: "+art.name);
        }
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        Check(preview.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m.name=="Hologram")),"Placement materials");
        Check(AssetDatabase.GetDependencies(PREVIEW,true).Contains(VISUAL),"Shared preview artwork");
        var reinforcement=new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Data/Reinforcement/ReinforcementData.asset")).FindProperty("defendPlatformWrapper.keyValue");
        var spawn=Enumerable.Range(0,reinforcement.arraySize).Select(i=>reinforcement.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==2).FindPropertyRelative("value").objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(spawn)==PREVIEW,"Own placement registration");
        var result=new JObject { ["success"]=true,["models"]=reports.Count,["weapons"]=36,["targetableHardpoints"]=39,
            ["visual"]=VISUAL,["gameplay"]=VIEW,["data"]=DATA,["sharedArtwork"]=true };
        File.WriteAllText(TASK+"VerifiedRegistration.json",result.ToString());
        return result.ToString();
    }
    private static JArray V(Vector3 v)=>new JArray(v.x,v.y,v.z);
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
