using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Services.ShipAbilities;

public static class VerifyISDIIReplacement
{
    const string ROOT = "Temp/ISDIIReplacement/";
    public static string Main()
    {
        var manifest=JObject.Parse(File.ReadAllText(ROOT+"ArtManifest.json"));
        var reference=JObject.Parse(File.ReadAllText(ROOT+"GeometryReference.json"));
        var results=new JObject();var art=new JArray();
        foreach(var entry in manifest.Properties())
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>((string)entry.Value["model_path"]);
            var transforms=model.GetComponentsInChildren<Transform>(true);
            float boneError=0,geometryError=0;int cornerCount=0;
            foreach(var bone in ((JObject)entry.Value["after"]["bones"]).Properties())
            {
                string parent=(string)bone.Value["parent"];
                var candidates=transforms.Where(t=>t.name==bone.Name && t.GetComponents<Renderer>().Length==0).ToArray();
                var transform=candidates.Single(t=>parent==null || t.parent.name==parent);
                var xyz=bone.Value["head"].Values<float>().ToArray();
                var expected=new Vector3(-xyz[0],xyz[2],-xyz[1])*.02f;
                boneError=Mathf.Max(boneError,Vector3.Distance(model.transform.InverseTransformPoint(transform.position),expected));
            }
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var expected=entry.Value["after"]["meshes"][renderer.name];
                var skin=renderer as SkinnedMeshRenderer;
                var mesh=skin==null?renderer.GetComponents<MeshFilter>().Single().sharedMesh:new Mesh();
                if(skin!=null)skin.BakeMesh(mesh,true);
                Check(mesh.triangles.Length/3==(int)expected["triangles"],entry.Name+" triangle count: "+renderer.name);
                Check(mesh.uv.Length==mesh.vertexCount,entry.Name+" UVs: "+renderer.name);
                Check(renderer.sharedMaterials.All(m=>AssetDatabase.GetAssetPath(m).StartsWith("Assets/Art/Materials/Models/EmpireShips/ISDIIReplacement/")),"Unmapped material: "+renderer.name);
                var corners=reference[entry.Name][renderer.name].Select(t=>t.Values<float>().ToArray()).ToArray();
                var buckets=new Dictionary<(int,int,int),List<(Vector3,Vector2)>>();
                foreach(var corner in corners)
                {
                    var p=new Vector3(-corner[0],corner[2],-corner[1])*.02f;var key=Cell(p);
                    if(!buckets.TryGetValue(key,out var values)){values=new List<(Vector3,Vector2)>();buckets.Add(key,values);}
                    values.Add((p,new Vector2(corner[3],corner[4])));
                }
                var positions=mesh.vertices;var uvs=mesh.uv;
                foreach(int index in mesh.triangles.Distinct())
                {
                    var p=model.transform.InverseTransformPoint(renderer.transform.TransformPoint(positions[index]));
                    var cell=Cell(p);float minimum=float.MaxValue;
                    for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                    {
                        if(!buckets.TryGetValue((cell.Item1+x,cell.Item2+y,cell.Item3+z),out var values))continue;
                        foreach(var corner in values)if(Vector2.Distance(uvs[index],corner.Item2)<.00002f)minimum=Mathf.Min(minimum,Vector3.Distance(p,corner.Item1));
                    }
                    Check(minimum<.0001f,entry.Name+" geometry/UV mismatch: "+renderer.name+" "+minimum);
                    geometryError=Mathf.Max(geometryError,minimum);cornerCount++;
                }
                if(skin!=null)UnityEngine.Object.DestroyImmediate(mesh);
            }
            Check(boneError<.0001f,entry.Name+" attachment error "+boneError);
            art.Add(new JObject{{"model",entry.Name},{"bone_error",boneError},{"geometry_error",geometryError},{"verified_vertices",cornerCount}});
        }
        results["art"]=art;
        var prefabs=new JArray();
        foreach(string path in new[]{"Assets/Prefabs/Models/Ships/ISDII.prefab","Assets/Prefabs/Models/Ships/ISDIIShipView.prefab","Assets/Prefabs/Models/Wrecks/ISDIIWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/ISDIIReinforcementView.prefab"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab.transform.localScale==Vector3.one,"Nonidentity root: "+path);
            foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script: "+path);
            var renderers=prefab.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            foreach(var renderer in renderers)
            {
                Check(renderer.GetComponents<MeshFilter>().Single().sharedMesh!=null,"Missing mesh: "+renderer.name);
                Check(renderer.sharedMaterials.Length>0 && renderer.sharedMaterials.All(m=>m!=null),"Missing material: "+renderer.name);
                Check(renderer.sharedMaterials.All(m=>!m.name.Contains("_Helper_")),"Visible helper: "+renderer.name);
            }
            foreach(var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var serialized=new SerializedObject(component);var property=serialized.GetIterator();
                while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)
                    Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue==EntityId.None,"Broken reference: "+path+" "+property.propertyPath);
            }
            var dependencies=AssetDatabase.GetDependencies(path);
            Check(!dependencies.Any(p=>p.Contains("Obsolete")||p.Contains("/ISDI/")||p.Contains("/ISDII/ISDII_") && p.Contains("/Models/EmpireShips/")),"Obsolete/ISD I dependency: "+path);
            foreach(var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string type=component.GetType().Name;
                if(type=="TeamColorView"||type=="UnitSpawnView"||type=="UnitWreckView")
                {
                    var array=new SerializedObject(component).FindProperty("meshRenderers");
                    var bound=Enumerable.Range(0,array.arraySize).Select(i=>(MeshRenderer)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    Check(renderers.All(bound.Contains),"Unbound visible renderer: "+path+" "+type);
                }
            }
            prefabs.Add(new JObject{{"path",path},{"visible_renderers",renderers.Length},{"bounds",Bounds(renderers).size.ToString()}});
        }
        results["prefabs"]=prefabs;
        var view=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDIIShipView.prefab");
        var engines=new JArray();
        foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.name.StartsWith("Engine_Glow")))
        {
            var mesh=renderer.GetComponents<MeshFilter>().Single().sharedMesh;
            var material=renderer.sharedMaterials.Single();
            int triangles=mesh.triangles.Length/3;
            Check(triangles==(renderer.name.Contains("Big")?1428:1840),"Incomplete engine geometry: "+renderer.name);
            Check(material.GetFloat("_Cull")==0 && material.IsKeywordEnabled("_EMISSION"),"Engine culling/emission: "+renderer.name);
            Check(material.GetColor("_EmissionColor")==new Color(2,2,2,1) && material.GetTexture("_EmissionMap")==material.GetTexture("_BaseMap"),"Engine emission mapping: "+renderer.name);
            Check((material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive)!=0,"Emission disabled on material validation: "+renderer.name);
            engines.Add(new JObject{{"renderer",renderer.name},{"triangles",triangles},{"cull",0},{"emission_intensity",2},{"emission_enabled",true}});
        }
        Check(engines.Count==2,"Both source engine mesh groups must be visible");
        results["engines"]=engines;
        var weapons=view.GetComponentsInChildren<WeaponHardPoint>(true);var hardpoints=view.GetComponentsInChildren<HardPoint>(true);
        Check(weapons.Length==24 && hardpoints.Length==27 && hardpoints.Select(h=>h.Id).Distinct().Count()==27,"Hardpoint loadout/IDs");
        var health=Component(view,"HealthComponent");var targets=new SerializedObject(health).FindProperty("<ShipUnits>k__BackingField");Check(targets.arraySize==27,"Target bindings");
        var audit=JObject.Parse(File.ReadAllText(ROOT+"SourceAudit.json"));float mountError=0;
        foreach(var hp in audit["hardpoints"])
        {
            string name=(string)hp["name"];
            if((string)hp["Type"]=="HARD_POINT_DUMMY_ART" || (string)hp["Fire_Projectile_Type"]=="Proj_Facing_Dummy")continue;
            var actual=hardpoints.Single(h=>h.name==name);
            var attachment=audit["attachments"].SingleOrDefault(t=>(string)t["hardpoint"]==name);
            var search=attachment==null?view.transform:view.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)attachment["variant"]);
            string bone=(string)(hp["Fire_Bone_A"]??hp["Attachment_Bone"]);
            var expected=search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(bone,StringComparison.OrdinalIgnoreCase));
            mountError=Mathf.Max(mountError,Vector3.Distance(actual.transform.position,expected.position));
        }
        Check(mountError<.0001f,"Muzzle/attachment positions "+mountError);
        results["ship"]=new JObject{{"weapons",weapons.Length},{"targets",hardpoints.Length},{"mount_error",mountError},{"loadout",JObject.FromObject(weapons.GroupBy(w=>(int)w.WeaponType).ToDictionary(g=>g.Key.ToString(),g=>g.Count()))}};
        var data=AssetDatabase.LoadAssetAtPath<ShipData>("Assets/Settings/Data/Ship/ISDIIShipData.asset");
        Check(data.Hull==5500 && data.Shields==4200 && data.Speed==25,"Preserved ship balance");
        Check(data.Abilities.SequenceEqual(new[]{ShipAbilityId.PowerToMainBatteries,ShipAbilityId.TractorBeam}),"Preserved abilities");
        Check(data.HangarBays.Select(b=>(int)b.SquadronType).SequenceEqual(new[]{203,204,205}),"Preserved hangar bays");
        var profiles=new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Data/Models/Weapon/WeaponsData.asset")).FindProperty("weapons");
        var audio=new SerializedObject(AssetDatabase.LoadMainAssetAtPath("Assets/Settings/Data/Models/Audio/ShipSfxData.asset")).FindProperty("weapons");
        foreach(int type in new[]{70,71})
        {
            var profile=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("weaponType").intValue==type);
            Check(profile.FindPropertyRelative("shotsPerSalvo").intValue==1 && profile.FindPropertyRelative("range").floatValue==(type==70?350:300),"Source-specific profile");
            Check(Enumerable.Range(0,audio.arraySize).Count(i=>audio.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==type)==1,"Unique weapon audio");
        }
        var obsolete=JObject.Parse(File.ReadAllText(ROOT+"ObsoleteVisuals.json"));
        foreach(string path in obsolete["obsolete"].Values<string>())Check(!File.Exists(path) && !File.Exists(path+".meta") && AssetDatabase.LoadMainAssetAtPath(path)==null,"Retained obsolete asset: "+path);
        Check((bool)obsolete["removed"] && !obsolete["retained_references"].Any(),"Obsolete removal/dependency evidence");
        results["obsolete_visuals"] = obsolete["obsolete"].Count();
        var registrations=new JArray();
        foreach(string path in new[]{"Assets/Prefabs/Models/Ships/ISDIIShipView.prefab","Assets/Settings/Data/Ship/ISDIIShipData.asset"})
        {
            string guid=AssetDatabase.AssetPathToGUID(path);var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
            Check(entry!=null && entry.address==Path.GetFileNameWithoutExtension(path),"Addressable: "+path);
            registrations.Add(new JObject{{"path",path},{"guid",guid},{"address",entry.address}});
        }
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIcon.png");
        Check(Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset","ships.keyValue").FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon,"Faction icon");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","shipIconWrapper.keyValue").objectReferenceValue==icon,"HUD icon");
        var spawn=Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnShipWrapper.keyValue").objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(spawn)=="Assets/Prefabs/Ui/Reinforcement/ISDIIReinforcementView.prefab","Placement registration");
        results["registrations"]=registrations;
        File.WriteAllText(ROOT+"VerifiedUnity.json",results.ToString());
        return "Verified 16 model geometries/materials/UVs/bones, four persisted prefabs, 24 weapons/27 targets, exact mount positions and unique Empire registrations.";
    }
    static (int,int,int) Cell(Vector3 p)=>(Mathf.FloorToInt(p.x/.01f),Mathf.FloorToInt(p.y/.01f),Mathf.FloorToInt(p.z/.01f));
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static Bounds Bounds(Renderer[] renderers){var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);return bounds;}
    static MonoBehaviour Component(GameObject root,string type)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==type);
    static SerializedProperty Entry(string path,string field)
    {
        var array=new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path)).FindProperty(field);
        return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==205).FindPropertyRelative("value");
    }
}
