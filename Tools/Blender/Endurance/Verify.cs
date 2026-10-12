using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Editor.Rendering;

public static class VerifyEndurance
{
    private const string SHIP="Assets/Prefabs/Models/Ships/EnduranceShipView.prefab";
    private const string DATA="Assets/Settings/Data/Ship/EnduranceShipData.asset";
    private const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/EnduranceIcon.png";
    private static readonly List<string> _checks=new List<string>();

    public static string Main()
    {
        _checks.Clear();
        var manifest=JObject.Parse(File.ReadAllText("Temp/EnduranceImport/ArtManifest.json"));
        foreach(var variant in manifest.Properties())
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>((string)variant.Value["model_path"]);
            var meshes=model.GetComponentsInChildren<MeshFilter>(true).Select(f=>(Name:f.name,Mesh:f.sharedMesh)).Concat(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>(Name:r.name,Mesh:r.sharedMesh))).ToArray();
            foreach(var row in ((JObject)variant.Value["after"]["meshes"]).Properties())
            {
                if((int)row.Value["triangles"]==0)continue;
                var mesh=meshes.Single(m=>m.Name==row.Name).Mesh;
                int degenerate=variant.Name=="Endurance" ? (row.Name=="Hull Primary" ? 6 : row.Name=="hull" ? 8 : 0) : 0;
                Check(Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(long)mesh.GetIndexCount(i))/3==(int)row.Value["triangles"]-degenerate,variant.Name+"/"+row.Name+" triangles");
                Check(mesh.uv.Length==mesh.vertexCount,variant.Name+"/"+row.Name+" UVs");
                Check(mesh.subMeshCount==row.Value["materials"].Count(),variant.Name+"/"+row.Name+" slots");
            }
            var transforms=model.GetComponentsInChildren<Transform>(true);
            foreach(var bone in ((JObject)variant.Value["after"]["bones"]).Properties())
                Check(transforms.Any(t=>t.name==bone.Name && ((string)bone.Value["parent"]==null || t.parent.name==(string)bone.Value["parent"])),variant.Name+" bone "+bone.Name);
            foreach(var row in variant.Value["materials"])
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>((string)row["material_path"]);
                Check(material!=null,variant.Name+" material "+(string)row["name"]);
                if(row["base"].Type!=JTokenType.Null)Check(material.GetTexture("_BaseMap")!=null,material.name+" albedo");
                if(row["normal"].Type!=JTokenType.Null)Check(material.GetTexture("_BumpMap")!=null,material.name+" normal");
                if(material.shader.name=="EmpireAtWar/Ship Lit")Check(material.GetFloat("_TeamMaskStrength")==0 && material.GetFloat("_TeamLiveryStrength")==1 && material.GetFloat("_TeamLiveryHueRange")==.06f && material.GetFloat("_TeamLiveryMinSaturation")==.5f,material.name+" red-paint-only team color");
            }
        }
        string[] prefabs={"Assets/Prefabs/Models/Ships/Endurance.prefab",SHIP,"Assets/Prefabs/Models/Wrecks/EnduranceWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/EnduranceReinforcementView.prefab"};
        foreach(string path in prefabs)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab.transform.localScale==Vector3.one,path+" root scale");
            foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,path+"/"+transform.name+" scripts");
                foreach(var component in transform.GetComponents<Component>())
                {
                    var property=new SerializedObject(component).GetIterator();
                    while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceEntityIdValue==EntityId.None || property.objectReferenceValue!=null,path+"/"+transform.name+"/"+property.propertyPath);
                }
            }
            Check(!AssetDatabase.GetDependencies(path).Any(p=>p.Contains("HomeOne") || p.Contains("Imperator")),path+" donor geometry/data removed");
        }
        Check(UnitHelperMeshStripper.FindHelpers(prefabs,new string[0]).All(h=>h.BlockedBy!=null),"No removable helper meshes");
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(SHIP);
        var data=AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
        Check(data.Hull==9000 && data.Shields==7000 && data.Speed==18,"Balance stats");
        var weapons=root.GetComponentsInChildren<WeaponHardPoint>(true);
        Check(weapons.Length==12,"12 weapons");
        foreach(var pair in new[]{(WeaponType.MediumTurboLaser,4),(WeaponType.MediumIonCannon,2),(WeaponType.PointDefense,6)})
            Check(weapons.Count(w=>w.WeaponType==pair.Item1)==pair.Item2,pair.Item1+" composition");
        var health=new SerializedObject(Component(root,"HealthComponent"));
        var units=health.FindProperty("<ShipUnits>k__BackingField");
        Check(units.arraySize==22,"22 targetable hardpoints");
        var points=Enumerable.Range(0,units.arraySize).Select(i=>(HardPoint)units.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Check(points.Select(p=>p.Id).Distinct().Count()==22,"Unique hardpoint IDs");
        Check(weapons.All(w=>points.Contains(w)),"All weapons targetable");
        foreach(var point in points)Check(point.GetComponents<Collider>().Length==1 && data.HardPointHealth.Any(h=>h.HardPointType==point.HardPointType),point.name+" collider/health");
        Check(points.Count(p=>p.HardPointType==HardPointType.CommandArray)==1,"Command Array");
        var move=new SerializedObject(Component(root,"ShipMoveComponent"));var body=(Transform)move.FindProperty("bodyTransform").objectReferenceValue;
        Check(points.All(p=>p.transform.IsChildOf(body)),"Hardpoints bank with hull");
        var hangar=new SerializedObject(Component(root,"HangarComponent"));
        Check(hangar.FindProperty("bayHardPoints").arraySize==4 && hangar.FindProperty("bayLaunchPoints").arraySize==4,"Four independently bound hangars");
        var collider=root.GetComponents<BoxCollider>().Single();var hull=new Bounds(collider.center,collider.size);
        Check(Math.Abs(hull.size.z-180)<.001f,"180-unit hull length");
        for(int i=0;i<4;i++)Check(((Transform)hangar.FindProperty("bayLaunchPoints").GetArrayElementAtIndex(i).objectReferenceValue).position.y<Math.Min(hull.min.y,data.HullBottom),"Bay "+i+" launch outside banked hull");
        Check(data.HangarBays.All(b=>(int)b.SquadronType==300) && data.HangarBays.Sum(b=>b.MaxActive)==6 && data.HangarBays.Sum(b=>b.Reserve)==12,"Six active plus six replacement X-Wings");
        Check(data.Abilities.Contains(ShipAbilityId.FighterCoordination) && data.Abilities.Contains(ShipAbilityId.TractorBeam),"Carrier abilities");
        Check(data.Wreck!=null,"Dedicated wreck data");
        Check(data.WeaponLoadout.Sum(l=>l.Count)==12,"AI weapon loadout");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        Check(icon!=null && icon.texture.width==512 && icon.texture.height==512,"512px sprite");
        var roster=Row("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset","ships.keyValue",322).FindPropertyRelative("value");
        Check(roster.FindPropertyRelative("<Name>k__BackingField").stringValue=="Endurance Fleet Carrier" && roster.FindPropertyRelative("<Price>k__BackingField").intValue==6000 && roster.FindPropertyRelative("<BuildTime>k__BackingField").intValue==65,"Roster name/price/build time");
        Check(roster.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon,"Roster icon");
        Check(Row("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","shipIconWrapper.keyValue",322).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var tooltip=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Tooltip/TooltipIconData.asset")).FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Any(i=>tooltip.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue==icon),"Tooltip icon");
        Check(Row("Assets/Settings/Data/Ship/ShipsData.asset","shipsData.keyValue",322).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship data registry");
        var preview=(MonoBehaviour)Row("Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnShipWrapper.keyValue",322).FindPropertyRelative("value").objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(preview)==prefabs[3],"Placement registry");
        var spawn=new SerializedObject(preview);var hologram=(Material)spawn.FindProperty("hologramMaterial").objectReferenceValue;
        var previewRenderers=spawn.FindProperty("meshRenderers");
        for(int i=0;i<previewRenderers.arraySize;i++)Check(((MeshRenderer)previewRenderers.GetArrayElementAtIndex(i).objectReferenceValue).sharedMaterials.All(m=>m==hologram),"Placement slot "+i);
        var ability=Row("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset","definitions.keyValue",52).FindPropertyRelative("value");
        Check(ability.FindPropertyRelative("settings").managedReferenceValue is FighterCoordinationSettings && ability.FindPropertyRelative("duration").floatValue==10 && ability.FindPropertyRelative("recoveryDelay").floatValue==45,"Fighter Coordination definition");
        foreach(string path in new[]{SHIP,DATA})
        {
            var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Check(entry!=null && entry.address==Path.GetFileNameWithoutExtension(path),path+" Addressables");
        }
        Check(!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorUtility.scriptCompilationFailed,"Editor compile/import complete");
        var report=new JObject{["checks"]=_checks.Count,["models"]=manifest.Count,["weapons"]=12,["targetableHardpoints"]=22,["bounds"]=new JArray(hull.size.x,hull.size.y,hull.size.z),["shipId"]=322,["abilityId"]=52,["status"]="passed",["runtimeAcceptance"]="Not run",["automatedTests"]="Not run; ALO guide requires explicit request"};
        File.WriteAllText("Temp/EnduranceImport/Verification.json",report.ToString());
        return report.ToString();
    }

    private static void Check(bool result,string label){if(!result)throw new InvalidOperationException(label);_checks.Add(label);}
    private static MonoBehaviour Component(GameObject root,string type)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==type);
    private static SerializedProperty Row(string path,string field,int key){var values=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)).FindProperty(field);return Enumerable.Range(0,values.arraySize).Select(i=>values.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);}
}
