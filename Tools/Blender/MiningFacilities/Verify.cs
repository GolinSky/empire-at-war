using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
public static class VerifyMiningFacilities {
 public static string Main() {
 var paths=new[]{"Assets/Prefabs/Models/MiningFacilities/AsteroidMiningFacilityView.prefab","Assets/Prefabs/Models/DefendStation/AsteroidDefendPlatformView.prefab","Assets/Prefabs/View/CaptureSites/CaptureSite.prefab","Assets/Prefabs/View/CaptureSites/BattleAsteroidCaptureSite.prefab","Assets/Prefabs/Models/Wrecks/AsteroidMiningFacilityWreckView.prefab","Assets/Prefabs/Models/Wrecks/AsteroidDefendPlatformWreckView.prefab"};
 var reports=new List<object>();
 foreach(string path in paths) {
 var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var components=root.GetComponentsInChildren<Component>(true);
 if(components.Any(c=>c==null))throw new Exception("Missing script: "+path);
 foreach(var component in components){var config=new SerializedObject(component);var property=config.GetIterator();while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceInstanceIDValue!=0 && property.objectReferenceValue==null)throw new Exception("Missing reference: "+path+"/"+property.propertyPath);}
 foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) if(renderer.sharedMaterials.Any(m=>m==null)||renderer.GetComponents<MeshFilter>().Single().sharedMesh==null)throw new Exception("Missing mesh/material "+path);
 foreach(var component in components.Where(c=>new[]{"MiningFacility","DefendPlatform","FogVisibilityComponent","TeamColorView","HealthComponent","CaptureSiteView","UnitWreckView","WeaponComponent","ConstructionComponent"}.Contains(c.GetType().Name))) {
 var config=new SerializedObject(component);
 string[] fields=component.GetType().Name switch {
 "MiningFacility" or "DefendPlatform"=>new[]{"explosionHullRenderers"}, "FogVisibilityComponent"=>new[]{"renderers"},"TeamColorView"=>new[]{"meshRenderers"},"HealthComponent"=>new[]{"shieldView","<ShipUnits>k__BackingField"},"CaptureSiteView"=>new[]{"asteroidRocks","captureRing","statusCanvas","progressFill","statusText","tooltipHover"},"UnitWreckView"=>new[]{"meshRenderers","meshFilters"},"WeaponComponent"=>new[]{"hardPoints"},"ConstructionComponent"=>new[]{"renderers","constructionMaterial"},_=>Array.Empty<string>()};
 foreach(string field in fields){var p=config.FindProperty(field);if(p==null)throw new Exception("Missing field "+field);if(p.isArray){if(p.arraySize==0)throw new Exception("Empty required array "+field);for(int i=0;i<p.arraySize;i++){var entry=p.GetArrayElementAtIndex(i);if(entry.objectReferenceValue==null)throw new Exception("Null array ref "+field);}}else if(p.objectReferenceValue==null)throw new Exception("Null required ref "+field);}
 if(component.GetType().Name=="WeaponComponent"&&config.FindProperty("hardPoints").arraySize!=6)throw new Exception("Changed weapon count");
 if(component.GetType().Name=="CaptureSiteView") {var rocks=(Transform)config.FindProperty("asteroidRocks").objectReferenceValue;if(rocks.GetComponentsInChildren<MeshRenderer>(true).Length!=1)throw new Exception("Expected source asteroid");var a=config.FindProperty("rockObstacles");for(int i=0;i<a.arraySize;i++)if(a.GetArrayElementAtIndex(i).objectReferenceValue==null)throw new Exception("Missing obstacle");}
 }
 reports.Add(new{path,guid=AssetDatabase.AssetPathToGUID(path),components=components.Length,renderers=root.GetComponentsInChildren<MeshRenderer>(true).Length,missingReferences=0});
 }
 var report=JsonConvert.SerializeObject(new{prefabs=reports,automatedTests="Not run: ALO guide requires explicit request",verification="Saved prefab reference, material, mesh, required binding, six weapon and construction binding checks passed"},Formatting.Indented);File.WriteAllText("Tools/Blender/MiningFacilities/Evidence/UnityVerification.json",report);return report;
 }
}
