using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

public static class RemoveObsoleteISDIIVisuals
{
    const string MANIFEST = "Temp/ISDIIReplacement/ObsoleteVisuals.json";
    public static string Main()
    {
        var manifest=JObject.Parse(File.ReadAllText(MANIFEST));
        var paths=manifest["obsolete"].Values<string>().ToHashSet();
        if((bool?)manifest["removed"]==true)
        {
            if(paths.Any(p=>File.Exists(p)||File.Exists(p+".meta")))throw new InvalidOperationException("An obsolete ISD II visual was recreated.");
            return "Obsolete ISD II visuals were already removed.";
        }
        var references=new JArray();
        foreach(string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/") && !paths.Contains(p) && !AssetDatabase.IsValidFolder(p)))
        {
            var dependencies=AssetDatabase.GetDependencies(path,false).Where(paths.Contains).ToArray();
            if(dependencies.Length>0)references.Add(new JObject{{"asset",path},{"obsolete_dependencies",new JArray(dependencies)}});
        }
        if(references.Count>0)throw new InvalidOperationException("Retained assets reference obsolete ISD II visuals: "+references);
        manifest["removed_guids"]=JObject.FromObject(paths.ToDictionary(p=>p,AssetDatabase.AssetPathToGUID));
        foreach(string path in paths)
            if(File.Exists(path) && !AssetDatabase.DeleteAsset(path))throw new InvalidOperationException("Cannot delete obsolete ISD II visual: "+path);
        foreach(string folder in new[]{"Assets/Art/Models/EmpireShips/ISDIIObsolete","Assets/Art/Models/EmpireShips/ISDII","Assets/Art/Materials/Models/EmpireShips/ISDII","Assets/Art/Textures/Models/EmpireShips/ISDII"})
        {
            if(!Directory.Exists(folder))continue;
            if(Directory.EnumerateFileSystemEntries(folder).Any())throw new InvalidOperationException("Unexpected remaining content: "+folder);
            if(!AssetDatabase.DeleteAsset(folder))throw new InvalidOperationException("Cannot remove empty obsolete folder: "+folder);
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        manifest["removed"]=true;
        manifest["retained_references"]=references;
        manifest["removal_policy"]="Delete the old ISD II visual set and snapshots; preserve replacement assets, active prefab/icon GUIDs, and other ships' art.";
        File.WriteAllText(MANIFEST,manifest.ToString());
        return "Removed "+paths.Count+" obsolete ISD II visual assets/snapshots and four empty folders; zero retained asset dependencies.";
    }
}
