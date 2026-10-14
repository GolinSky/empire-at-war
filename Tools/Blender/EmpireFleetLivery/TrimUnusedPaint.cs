using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
public static class TrimFleetPaint
{
    public static string Main()
    {
        var rows=JArray.Parse(File.ReadAllText("Tools/Blender/EmpireFleetLivery/Evidence/PaintInputs.json"));var removed=new JArray();
        foreach(var ship in new[]{"Procursator","Allegiance","Titan","Tartan"})
        {
            var paths=new[]{"Assets/Prefabs/Models/Ships/Empire/"+ship+".prefab","Assets/Prefabs/Models/Ships/Empire/"+ship+"ShipView.prefab","Assets/Prefabs/Models/Wrecks/"+ship+"WreckView.prefab","Assets/Prefabs/Ui/Reinforcement/"+ship+"ReinforcementView.prefab"};
            var used=paths.SelectMany(p=>AssetDatabase.GetDependencies(p,true)).ToHashSet();
            foreach(var row in rows.Where(r=>(string)r["ship"]==ship))
            {
                string path=(string)row["derived"];if(used.Contains(path))continue;
                AssetDatabase.DeleteAsset(path);removed.Add(path);
            }
        }
        AssetDatabase.SaveAssets();File.WriteAllText("Tools/Blender/EmpireFleetLivery/Evidence/UnusedPaint.json",removed.ToString());return "Removed "+removed.Count+" unused derived albedos; original textures unchanged.";
    }
}
