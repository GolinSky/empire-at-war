using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CaptureISDIIFinalChecks
{
    public static string Main()
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling)throw new InvalidOperationException("Editor must be stopped and compilation complete.");
        foreach(string color in new[]{"Blue","White"})
        {
            string path="Assets/Art/Materials/Models/EmpireShips/ISDIIReplacement/ISDIIReplacement_ISDII_Engine_Glow_Fancy_"+color+"_MeshShield.mat";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material.IsKeywordEnabled("_EMISSION") || material.GetFloat("_Cull")!=0)throw new InvalidOperationException("Engine settings lost on reimport: "+path);
        }
        AssetDatabase.SaveAssets();
        var paths=new[]{"Assets/Prefabs/Models/Ships/ISDII.prefab","Assets/Prefabs/Models/Ships/ISDIIShipView.prefab","Assets/Prefabs/Models/Wrecks/ISDIIWreckView.prefab","Assets/Prefabs/Ui/Reinforcement/ISDIIReinforcementView.prefab"};
        var shaders=paths.SelectMany(AssetDatabase.GetDependencies).Distinct().Select(AssetDatabase.LoadMainAssetAtPath).OfType<Material>().Select(m=>m.shader).Distinct().ToArray();
        var scenes=Enumerable.Range(0,EditorSceneManager.sceneCount).Select(i=>EditorSceneManager.GetSceneAt(i)).ToArray();
        var dirty=paths.Where(p=>EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(p))).ToArray();
        var compilation=JObject.Parse(File.ReadAllText("Temp/pipeline_recompile_status.json"));
        if((bool)compilation["failed"] || dirty.Length>0 || scenes.Any(s=>s.isDirty) || shaders.Any(ShaderUtil.ShaderHasError))throw new InvalidOperationException("Final import/compilation/persistence checks failed.");
        var result=new JObject {
            {"version",Application.unityVersion},{"compiling",EditorApplication.isCompiling},{"playing",EditorApplication.isPlaying},
            {"dirty_prefabs",new JArray(dirty)},{"compilation_status",compilation},
            {"shaders",JArray.FromObject(shaders.Select(s=>new {name=s.name,error=ShaderUtil.ShaderHasError(s)}))},
            {"scenes",JArray.FromObject(scenes.Select(s=>new {name=s.name,path=s.path,isDirty=s.isDirty}))},
            {"engine_reimport_verified",true}
        };
        File.WriteAllText("Temp/ISDIIReplacement/FinalEditorChecks.json",result.ToString());
        return "Engine emission/culling survived reimport; shaders compile, prefabs are saved, and open scenes are clean.";
    }
}
