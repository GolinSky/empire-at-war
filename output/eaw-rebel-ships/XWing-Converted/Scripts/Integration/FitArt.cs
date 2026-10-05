using System.Linq;
using UnityEditor;
using UnityEngine;
public static class FitXWingArt
{
    public static string Main()
    {
        const string VISUAL="Assets/Prefabs/Models/Squadrons/XWing.prefab";
        var root=PrefabUtility.LoadPrefabContents(VISUAL);
        try
        {
            var model=root.transform.GetChild(0);
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
            var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            model.localScale*=4f/bounds.size.z;
            bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
            model.localPosition-=bounds.center;
            PrefabUtility.SaveAsPrefabAsset(root,VISUAL);AssetDatabase.SaveAssets();
            return "Rigid visible geometry centered and fitted to four units; imported renderer bounds were affected by skin scaling.";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
