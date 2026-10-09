using System;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using Newtonsoft.Json;using EmpireAtWar.Editor.Rendering;
public static class FinalizeISDIRemake
{
 public static string Main(){
 string[] paths={"Assets/Prefabs/Models/Ships/ISDI.prefab","Assets/Prefabs/Models/Ships/ISDIShipView.prefab","Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab","Assets/Prefabs/Models/Wrecks/ISDIWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/ISDIReinforcementView.prefab"};
 var helpers=UnitHelperMeshStripper.FindHelpers(paths,Array.Empty<string>());File.WriteAllText("Temp/ISDIRemakeImport/HelperReport.json",JsonConvert.SerializeObject(helpers,Formatting.Indented));
 string strip=UnitHelperMeshStripper.Strip(paths,Array.Empty<string>());File.WriteAllText("Temp/ISDIRemakeImport/HelperStrip.txt",strip);
 var faction=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset"));var entries=faction.FindProperty("ships.keyValue");var value=Enumerable.Range(0,entries.arraySize).Select(i=>entries.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==201).FindPropertyRelative("value");
 value.FindPropertyRelative("description").stringValue="4,500 hull; 4,000 shields; speed 15. Twenty-four targetable weapons: six heavy two-burst turbolasers, two heavy two-burst turbo-ion cannons, two medium turbo-ion cannons, three medium three-burst turbolasers, five light turbolasers and six laser cannons. Two shield generators, two engines, tractor beam and hangar are targetable. Launches TIE Interceptors, TIE Brutes and TIE Punishers. Boost Engine Power doubles speed, quadruples weapon delays and pauses shield regeneration for 20 s. Tractor Beam restricts enemy corvette or frigate movement.";
 faction.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(faction.targetObject);AssetDatabase.SaveAssets();
 var archived=AssetDatabase.FindAssets("l:Obsolete").Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("ISDIObsoleteAOTR") || new[]{"ISDI_ObsoleteAOTR","ISDIShipView_ObsoleteAOTR","ISDIWreckView_ObsoleteAOTR","ISDIReinforcementView_ObsoleteAOTR","ISDIIcon_ObsoleteAOTR","ISDISilhouette_ObsoleteAOTR","ISDIShipViewShield_ObsoleteAOTR"}.Contains(Path.GetFileNameWithoutExtension(p))).OrderBy(p=>p).ToArray();
 File.WriteAllText("Temp/ISDIRemakeImport/ObsoleteAssets.json",JsonConvert.SerializeObject(archived,Formatting.Indented));
 AssetDatabase.Refresh();AssetDatabase.ForceReserializeAssets(archived.Where(p=>new[]{".mat",".asset",".prefab"}.Contains(Path.GetExtension(p))));AssetDatabase.SaveAssets();
 return "Stripped ISD I helpers; updated only Empire ship 201 description; persisted "+archived.Length+" obsolete asset records. "+strip;
 }
}
