using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class ExportFleetPaintInputs
{
    public static string Main()
    {
        var result=new JArray();
        foreach(var ship in new[]{"Procursator","Allegiance","Titan","Tartan"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Empire/"+ship+".prefab");
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && !r.name.StartsWith("TeamStripes") && !r.name.StartsWith("FleetLivery") && (r.name.ToLowerInvariant().Contains("hull") || r.name.ToLowerInvariant().Contains("super")) && r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit"));
            foreach(var material in renderers.SelectMany(r=>r.sharedMaterials).Distinct())
                result.Add(new JObject{["ship"]=ship,["material"]=material.name,["source"]=AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")),["derived"]="Assets/Art/Textures/Models/EmpireShips/"+ship+"/"+ship+"_WornPaint_"+material.name+".png"});
        }
        System.IO.File.WriteAllText("Tools/Blender/EmpireFleetLivery/Evidence/PaintInputs.json",result.ToString());
        return "Captured "+result.Count+" original albedo references without modifying source textures.";
    }
}
