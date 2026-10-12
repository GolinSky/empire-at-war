// Populated station-selection layout check. Horizontal overflow is scrollable; vertical overflow is a failure.
var service = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/UiService.prefab");
try
{
    var layout = service.transform.Find("DefaultCanvas/FactionLayout").GetComponent<EmpireAtWar.Ui.Base.UiContentLayout>();
    var so = new UnityEditor.SerializedObject(layout);
    var scroll = (UnityEngine.UI.ScrollRect)so.FindProperty("scroll").objectReferenceValue;
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Ui/Factions/FactionUi.prefab");
    var instance = UnityEngine.Object.Instantiate(prefab, layout.Viewport);
    var faction = instance.GetComponent<EmpireAtWar.Views.Factions.FactionUi>();
    faction.SetParent(layout.Viewport);
    faction.SetContentLayout(layout);
    var card = (EmpireAtWar.Views.Factions.FactionUnitUi)new UnityEditor.SerializedObject(faction).FindProperty("factionUnitPrefab").objectReferenceValue;
    for (int i=0; i<30; i++) UnityEngine.Object.Instantiate(card, faction.transform);
    service.GetComponent<EmpireAtWar.Ui.Base.UiService>().SetContentLayout(true,false);
    service.GetComponent<EmpireAtWar.Ui.Base.UiService>().SetContentVisible(true);
    var results = new System.Collections.Generic.List<object>();
    foreach (var size in new[]{new UnityEngine.Vector2(1280f,720f),new UnityEngine.Vector2(1920f,1080f),new UnityEngine.Vector2(2560f,1440f)})
    {
        service.transform.Find("DefaultCanvas").GetComponent<UnityEngine.RectTransform>().sizeDelta=size;
        UnityEngine.Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        var bounds=scroll.viewport.rect;
        if(!scroll.horizontal || scroll.vertical || scroll.content.rect.width<=bounds.width)
            throw new System.InvalidOperationException("Faction roster must retain horizontal scrolling at "+size);
        if(instance.GetComponent<UnityEngine.UI.GridLayoutGroup>().cellSize!=((UnityEngine.RectTransform)card.transform).rect.size)
            throw new System.InvalidOperationException("Faction routing must preserve the card prefab's authored size.");
        var corners=new UnityEngine.Vector3[4];
        foreach(UnityEngine.Transform child in faction.transform)
        {
            ((UnityEngine.RectTransform)child).GetWorldCorners(corners);
            var points=corners.Select(scroll.viewport.InverseTransformPoint).ToArray();
            if(points.Any(p=>p.y<bounds.yMin-.01f || p.y>bounds.yMax+.01f))
                throw new System.InvalidOperationException("Faction card exceeds viewport height at "+size+": "+bounds.height+" px viewport; card y "+points.Min(p=>p.y)+".."+points.Max(p=>p.y));
        }
        results.Add(new {resolution=size.ToString(),viewportHeight=bounds.height,cards=30,cell=instance.GetComponent<UnityEngine.UI.GridLayoutGroup>().cellSize.ToString(),horizontalScrolling=true});
    }
    layout.DetachContent();
    return results;
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(service); }
