// Read-only saved-asset and preview-layout acceptance checks; does not enter Play Mode.
var checks = new System.Collections.Generic.List<string>();
void Check(bool condition, string message)
{
    if (!condition) throw new System.InvalidOperationException(message);
    checks.Add(message);
}
var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/UiService.prefab");
var build = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/SkirmishGame/CaptureSiteBuildUi.prefab");
var buildPreviewScene = build.scene;
var core = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab");
var corePreviewScene = core.scene;
try
{
    var ui = root.GetComponent<EmpireAtWar.Ui.Base.UiService>();
    var chrome = core.GetComponent<EmpireAtWar.Views.Game.CoreGameUi>();
    chrome.SetParent(root.transform.Find("DefaultCanvas"));
    core.transform.SetAsFirstSibling();
    chrome.ContextualPopup.SetParent(root.transform.Find("PopupCanvas/Contextual"),false);
    Check(core.transform.GetSiblingIndex()==0,"CoreGame chrome draws behind routed HUD content");
    Check(chrome.ContextualPopup.GetComponentInParent<UnityEngine.Canvas>().name=="PopupCanvas","Orbital popup stays above HUD content");
    Check(root.GetComponent<UnityEngine.Canvas>() == null,"Service root has no Canvas");
    Check(root.GetComponentsInChildren<UnityEngine.Canvas>(true).Length == 4,"Four direct service Canvases");
    foreach(var pair in new[]{("FeedbackCanvas",-10),("DefaultCanvas",0),("DynamicCanvas",10),("PopupCanvas",20)})
    {
        var canvas = root.transform.Find(pair.Item1).GetComponent<UnityEngine.Canvas>();
        Check(canvas.transform.parent==root.transform && canvas.sortingOrder==pair.Item2, pair.Item1+" placement and sort order");
    }
    Check(root.transform.Find("FeedbackCanvas").GetComponent<UnityEngine.UI.GraphicRaycaster>()==null,"Feedback remains noninteractive");
    var placement = typeof(EmpireAtWar.Ui.Base.UiService).GetMethod("GetPlacement",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    var mapping = UnityEditor.AssetDatabase.LoadAssetAtPath<EmpireAtWar.Repository.Data.AssetMappingData>("Assets/Settings/AssetMappingData.asset");
    foreach(EmpireAtWar.Ui.Base.UiType type in System.Enum.GetValues(typeof(EmpireAtWar.Ui.Base.UiType)))
    {
        if(type==EmpireAtWar.Ui.Base.UiType.Interaction) continue;
        var parent=(UnityEngine.Transform)placement.Invoke(ui,new object[]{type});
        var canvas=parent.GetComponentInParent<UnityEngine.Canvas>();
        Check(canvas!=null && canvas.transform.parent==root.transform,type+" resolves a bound service slot");
        string expected = type switch
        {
            EmpireAtWar.Ui.Base.UiType.MiniMap or EmpireAtWar.Ui.Base.UiType.Ship or EmpireAtWar.Ui.Base.UiType.ShipGroup
                or EmpireAtWar.Ui.Base.UiType.ShipBuild or EmpireAtWar.Ui.Base.UiType.Fps => "DynamicCanvas",
            EmpireAtWar.Ui.Base.UiType.UnitOrderFeedback => "FeedbackCanvas",
            EmpireAtWar.Ui.Base.UiType.PauseMenu or EmpireAtWar.Ui.Base.UiType.Settings or EmpireAtWar.Ui.Base.UiType.Tooltip
                or EmpireAtWar.Ui.Base.UiType.Fade or EmpireAtWar.Ui.Base.UiType.EndGame or EmpireAtWar.Ui.Base.UiType.CaptureSiteBuild => "PopupCanvas",
            _ => "DefaultCanvas"
        };
        Check(canvas.name==expected,type+" uses "+expected);
        string key=type+"Ui";
        string assetKey=mapping.GetAssetKey(key);
        string path=UnityEditor.AssetDatabase.GUIDToAssetPath(assetKey);
        if(string.IsNullOrEmpty(path))
        {
            var entry=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.groups.SelectMany(g=>g.entries).Single(e=>e.address==assetKey);
            path=UnityEditor.AssetDatabase.GUIDToAssetPath(entry.guid);
        }
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
        Check(prefab.GetComponent<EmpireAtWar.Ui.Base.BaseUi>()!=null,key+" resolves its normal view prefab");
        Check(prefab.GetComponentsInChildren<UnityEngine.Canvas>(true).Length==0,key+" inherits its Canvas");
        var so=new UnityEditor.SerializedObject(prefab.GetComponent<EmpireAtWar.Ui.Base.BaseUi>());
        Check(so.FindProperty("canvasGroup").objectReferenceValue==prefab.GetComponent<UnityEngine.CanvasGroup>(),key+" retains its bound CanvasGroup");
    }
    try { placement.Invoke(ui,new object[]{EmpireAtWar.Ui.Base.UiType.Interaction}); throw new System.Exception("Reserved view was accepted"); }
    catch(System.Reflection.TargetInvocationException e) { Check(e.InnerException is System.ArgumentOutOfRangeException,"Reserved Interaction fails visibly"); }
    var canvasRect=root.transform.Find("PopupCanvas").GetComponent<UnityEngine.RectTransform>();
    canvasRect.sizeDelta=new UnityEngine.Vector2(1920f,1080f);
    var view=build.GetComponent<EmpireAtWar.Entities.CaptureSites.CaptureSiteBuildUi>();
    view.SetParent(root.transform.Find("PopupCanvas/Contextual"));
    var panel=(UnityEngine.RectTransform)new UnityEditor.SerializedObject(view).FindProperty("panel").objectReferenceValue;
    Check(build.GetComponent<UnityEngine.UI.Graphic>()==null,"Capture view root has no background graphic");
    foreach(var size in new[]{new UnityEngine.Vector2(1920f,1080f),new UnityEngine.Vector2(1280f,720f),new UnityEngine.Vector2(2560f,1440f)})
    {
        canvasRect.sizeDelta=size;
        UnityEngine.Canvas.ForceUpdateCanvases();
        var bounds=((UnityEngine.RectTransform)panel.parent).rect;
        foreach(var point in new[]{bounds.min,bounds.center,bounds.max})
        {
            view.SetPosition(UnityEngine.RectTransformUtility.WorldToScreenPoint(null,panel.parent.TransformPoint(point)));
            var visibleSize=UnityEngine.Vector2.Scale(panel.rect.size,panel.localScale);
            var pos=panel.anchoredPosition;
            Check(pos.x-visibleSize.x*.5f>=bounds.xMin-.01f && pos.x+visibleSize.x*.5f<=bounds.xMax+.01f
                && pos.y>=bounds.yMin-.01f && pos.y+visibleSize.y<=bounds.yMax+.01f,"Capture choices clamp at "+size+" / "+point);
            if(point==bounds.center)
                Check(UnityEngine.Vector2.Distance(pos,point+UnityEngine.Vector2.up*24f)<.01f,"Capture choices preserve the 24 px offset at "+size);
        }
    }
    view.Hide();
    Check(build.activeSelf && !view.IsVisible && !build.GetComponent<UnityEngine.CanvasGroup>().blocksRaycasts,"Capture Hide preserves the root and releases input");
    view.Show();
    Check(build.activeSelf && view.IsVisible && build.GetComponent<UnityEngine.CanvasGroup>().blocksRaycasts,"Capture Show restores input");
    ui.SetHudVisible(false);
    Check(!root.transform.Find("PopupCanvas/Contextual").gameObject.activeSelf,"HUD hide also hides contextual panels");
    Check(!root.transform.Find("DefaultCanvas").GetComponent<UnityEngine.Canvas>().enabled
        && !root.transform.Find("DynamicCanvas").GetComponent<UnityEngine.Canvas>().enabled
        && !root.transform.Find("FeedbackCanvas").GetComponent<UnityEngine.Canvas>().enabled
        && root.transform.Find("PopupCanvas").GetComponent<UnityEngine.Canvas>().enabled,"HUD hide covers Feedback/Default/Dynamic and preserves Popup");
    ui.SetHudVisible(true);
    foreach(var selection in new[]{(true,false),(false,false),(false,true),(true,false)})
    {
        ui.SetContentLayout(selection.Item1,selection.Item2);
        ui.SetContentVisible(true);
        var faction=root.transform.Find("DefaultCanvas/FactionLayout");
        var ship=root.transform.Find("DynamicCanvas/ShipLayout");
        Check(faction.gameObject.activeSelf==selection.Item1 && ship.gameObject.activeSelf!=selection.Item1,"Content selection branch "+selection);
        foreach(var layout in new[]{faction,ship})
        {
            var scroll=layout.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            Check(scroll.content.parent==scroll.viewport && scroll.viewport.GetComponent<UnityEngine.UI.RectMask2D>()!=null,layout.name+" keeps coherent scroll/mask hierarchy");
        }
    }
    foreach(var path in new[]{"Assets/Prefabs/View/CaptureSites/CaptureSite.prefab","Assets/Prefabs/View/CaptureSites/BattleAsteroidCaptureSite.prefab"})
    {
        var site=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
        Check(site.GetComponentsInChildren<EmpireAtWar.Entities.CaptureSites.SiteFacilityOptionView>(true).Length==0,path+" has no screen build widgets");
        Check(site.GetComponentsInChildren<UnityEngine.Canvas>(true).Length==1,path+" retains its world status Canvas");
    }
    var pop=root.transform.Find("PopupCanvas");
    Check(string.Join(",",pop.Cast<UnityEngine.Transform>().Select(t=>t.name))=="Contextual,EndGame,Modal,Tooltip,Fade","Explicit popup ordering survives creation order");
    var factionLayout=root.transform.Find("DefaultCanvas/FactionLayout").GetComponent<EmpireAtWar.Ui.Base.UiContentLayout>();
    var originalContent=factionLayout.Content;
    for(int cycle=0;cycle<3;cycle++)
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Ui/Factions/FactionUi.prefab");
        var instance=UnityEngine.Object.Instantiate(prefab,factionLayout.Viewport);
        try
        {
            var faction=instance.GetComponent<EmpireAtWar.Views.Factions.FactionUi>();
            faction.SetParent(factionLayout.Viewport);
            faction.SetContentLayout(factionLayout);
            Check(factionLayout.Content==instance.transform,"Faction owns its scrolling cards, cycle "+cycle);
            factionLayout.DetachContent();
            Check(factionLayout.Content==originalContent,"Faction teardown restores the service slot, cycle "+cycle);
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
    foreach(var size in new[]{new UnityEngine.Vector2(1920f,1080f),new UnityEngine.Vector2(1280f,720f),new UnityEngine.Vector2(2560f,1440f)})
    {
        foreach(var name in new[]{"DefaultCanvas","DynamicCanvas"})
            root.transform.Find(name).GetComponent<UnityEngine.RectTransform>().sizeDelta=size;
        foreach(var selection in new[]{(true,false),(false,false),(false,true)})
        {
            chrome.SetContentLayout(selection.Item1,selection.Item2);
            ui.SetContentLayout(selection.Item1,selection.Item2);
            ui.SetContentVisible(true);
            UnityEngine.Canvas.ForceUpdateCanvases();
            var expected=core.transform.Find("Panel").GetComponent<UnityEngine.RectTransform>();
            var actual=root.transform.Find(selection.Item1?"DefaultCanvas/FactionLayout":"DynamicCanvas/ShipLayout").GetComponent<UnityEngine.RectTransform>();
            var a=new UnityEngine.Vector3[4]; var b=new UnityEngine.Vector3[4];
            expected.GetWorldCorners(a); actual.GetWorldCorners(b);
            Check(a.Zip(b,(x,y)=>UnityEngine.Vector3.Distance(x,y)).All(d=>d<.01f),"Routed content matches HUD panel bounds at "+size+" / "+selection);
        }
    }
    System.IO.Directory.CreateDirectory("Logs/UiCanvasRouting");
    System.IO.File.WriteAllText("Logs/UiCanvasRouting/verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(checks,Newtonsoft.Json.Formatting.Indented));
    return new {passed=checks.Count,evidence="Logs/UiCanvasRouting/verification.json"};
}
finally
{
    // The build root was moved into the service preview scene.
    if(build!=null) UnityEngine.Object.DestroyImmediate(build);
    if(core!=null) UnityEngine.Object.DestroyImmediate(core);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(buildPreviewScene);
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(corePreviewScene);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
