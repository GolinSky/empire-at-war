using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.ReinforcementZones;
public static class InspectPlanetDecorations
{
 public static string Main()
 {
  var settings=AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
  var sites=AssetDatabase.LoadAssetAtPath<CaptureSiteData>("Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
  var mine=AssetDatabase.LoadAssetAtPath<CaptureSiteView>("Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
  float radius=sites.GetSettings(CaptureSiteKind.Mining).Radius * Mathf.Max(Mathf.Abs(mine.transform.lossyScale.x),Mathf.Abs(mine.transform.lossyScale.z));
  var generator=new MapLayoutGenerator(settings,new MapFeatureRadii(AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>("Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab").Radius,radius,AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>("Assets/Prefabs/View/ReinforcementZones/MainReinforcementZone.prefab").Radius));
  var players=new[]{new PlayerSlot(new PlayerId(0),new TeamId(0),FactionType.Republic,PlayerController.Human,EnemyAiDifficulty.Medium,0),new PlayerSlot(new PlayerId(1),new TeamId(1),FactionType.Separatist,PlayerController.Ai,EnemyAiDifficulty.Medium,1)};
  var report=new JArray();
  foreach(var planet in new[]{PlanetType.Kuat,PlanetType.Tatooine,PlanetType.Kessel})
  foreach(MapSize size in Enum.GetValues(typeof(MapSize)))
  {
   var layout=generator.Generate(size,players,new System.Random(7),7,planet);
   report.Add(new JObject{["planet"]=planet.ToString(),["size"]=size.ToString(),["seed"]=7,["count"]=layout.Decorations.Count});
  }
  File.WriteAllText("Tools/Blender/PlanetDecorations/PlacementEvidence.json",report.ToString());
  return report.ToString();
 }
}
