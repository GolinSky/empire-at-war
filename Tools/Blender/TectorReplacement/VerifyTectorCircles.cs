using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class VerifyTectorCircles
{
    public static string Main()
    {
        string[] paths={"Assets/Prefabs/Models/Ships/Empire/Tector.prefab","Assets/Prefabs/Models/Ships/Empire/TectorShipView.prefab","Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab"};
        var used=new HashSet<string>(AssetDatabase.GetDependencies(paths,true));
        var report=new List<string>();
        foreach(var path in paths)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var circles=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("TeamCircles")).ToArray();
            if(circles.Length!=1)throw new Exception(path+": expected one combined circle renderer, found "+circles.Length);
            foreach(var renderer in circles)
            {
                var material=renderer.sharedMaterial;
                var hull=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Models/EmpireShips/Tector/Tector_Remake_Hull_16_MeshBumpColorize.mat");
                if(material.GetFloat("_TeamMaskStrength")!=1f || material.GetTexture("_TeamMaskMap")==null)throw new Exception("Missing worn circle mask: "+path);
                if(material.GetTexture("_BaseMap")!=hull.GetTexture("_BaseMap") || material.GetTexture("_BumpMap")!=hull.GetTexture("_BumpMap"))throw new Exception("Circle must retain original hull albedo and normals: "+path);
            }
            if(path.EndsWith("ShipView.prefab"))
            {
                foreach(var pair in new[]{("TeamColorView","meshRenderers"),("FogVisibilityComponent","renderers")})
                {
                    var component=root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==pair.Item1);
                    var array=new SerializedObject(component).FindProperty(pair.Item2);
                    var refs=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    if(circles.Any(r=>!refs.Contains(r)))throw new Exception("Missing renderer binding: "+pair.Item1);
                }
            }
            report.Add(path+": circle material and bindings valid");
        }
        foreach(var folder in new[]{"Assets/Art/Models/EmpireShips/Tector","Assets/Art/Materials/Models/EmpireShips/Tector","Assets/Art/Materials/Wrecks/Tector"})
        foreach(var guid in AssetDatabase.FindAssets("Tector_TeamCircles",new[]{folder}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            if(!used.Contains(path)){AssetDatabase.DeleteAsset(path);report.Add("Removed unused iteration asset: "+path);}
        }
        AssetDatabase.SaveAssets();
        System.IO.File.WriteAllLines("Tools/Blender/TectorReplacement/CircleVerification.txt",report);
        return string.Join("\n",report);
    }
}
