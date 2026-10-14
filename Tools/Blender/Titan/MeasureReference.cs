using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class MeasureTitanReference {
 public static string Main() {
  var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Republic/VenatorShipView.prefab");
  var p=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterial.shader.name=="EmpireAtWar/Ship Lit").SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
  return "Venator length="+(p.Max(v=>v.z)-p.Min(v=>v.z));
 }
}

