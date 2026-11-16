var previewScene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var root=new GameObject("Ginivex");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,previewScene);
try
{
    var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/SeparatistShips/Ginivex/Ginivex.fbx");
    var model=(GameObject)PrefabUtility.InstantiatePrefab(source,previewScene);
    model.transform.SetParent(root.transform,false);
    model.transform.localPosition=new Vector3(0,0,1.058876f);
    foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
    {
        if(renderer.name!="Hull" && renderer.name!="Object01" && renderer.name!="Box01" && renderer.name!="Box02")
            UnityEngine.Object.DestroyImmediate(renderer.gameObject);
    }
    var bounds=root.GetComponentsInChildren<Renderer>(true).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
    PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Models/Squadrons/Ginivex.prefab");
    AssetDatabase.SaveAssets();
    var attachments=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("MuzzleA_")&&!t.name.EndsWith("Flash")).Select(t=>new {name=t.name,position=t.position});
    var result=Newtonsoft.Json.JsonConvert.SerializeObject(new {boundsCenter=bounds.center.ToString("F6"),boundsSize=bounds.size.ToString("F6"),muzzles=attachments.Select(a=>new {a.name,position=a.position.ToString("F6")})});
    System.IO.File.WriteAllText("Temp/GinivexImport/UnityArtReport.json",result);
    return result;
}
finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(previewScene); }
