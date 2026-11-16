// Populate the station's two-row grid past viewport width; vertical overflow is a failure.
var service = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/UiService.prefab");
try
{
    var layout = service.transform.Find("DefaultCanvas/FactionLayout").GetComponent<EmpireAtWar.Ui.Base.UiContentLayout>();
    var so = new UnityEditor.SerializedObject(layout);
    var scroll = (UnityEngine.UI.ScrollRect)so.FindProperty("scroll").objectReferenceValue;
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Ui/Factions/FactionUi.prefab");
    var instance = UnityEngine.Object.Instantiate(prefab, layout.Viewport);
    var faction = instance.GetComponent<EmpireAtWar.Entities.Faction.Ui.FactionUi>();
    faction.SetParent(layout.Viewport);
    faction.SetContentLayout(layout);
    var factionProperties = new UnityEditor.SerializedObject(faction);
    var card = (EmpireAtWar.Entities.Faction.Ui.FactionUnitUi)factionProperties.FindProperty("stationUnitPrefab").objectReferenceValue;
    var baseCard = (EmpireAtWar.Entities.Faction.Ui.FactionUnitUi)factionProperties.FindProperty("factionUnitPrefab").objectReferenceValue;
    var row = (UnityEngine.RectTransform)factionProperties.FindProperty("unitsRow").objectReferenceValue;
    for (int i = 0; i < 60; i++) UnityEngine.Object.Instantiate(card, row);
    var cardSize = ((UnityEngine.RectTransform)card.transform).rect.size;
    var icon = (UnityEngine.UI.Image)new UnityEditor.SerializedObject(card).FindProperty("unitIconImage").objectReferenceValue;
    var baseIcon = (UnityEngine.UI.Image)new UnityEditor.SerializedObject(baseCard).FindProperty("unitIconImage").objectReferenceValue;
    if ((icon.rectTransform.rect.size - baseIcon.rectTransform.rect.size * 1.2f).sqrMagnitude > .0001f)
        throw new System.InvalidOperationException("Station card art must be 20% larger than the shared popup card art.");
    service.GetComponent<EmpireAtWar.Ui.Base.UiService>().SetContentLayout(true,false);
    service.GetComponent<EmpireAtWar.Ui.Base.UiService>().SetContentVisible(true);
    var results = new System.Collections.Generic.List<object>();
    foreach (var size in new[]{new UnityEngine.Vector2(1280f,720f),new UnityEngine.Vector2(1920f,1080f),new UnityEngine.Vector2(2560f,1440f)})
    {
        service.transform.Find("DefaultCanvas").GetComponent<UnityEngine.RectTransform>().sizeDelta=size;
        UnityEngine.Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        var bounds=scroll.viewport.rect;
        if(scroll.horizontal || scroll.vertical || instance.transform.childCount != 1)
            throw new System.InvalidOperationException("Faction content must contain only one scrolling station grid.");
        if(cardSize != new UnityEngine.Vector2(102f, 102f))
            throw new System.InvalidOperationException("Station cards must retain their authored 102 x 102 size.");
        var rowScroll = row.parent.parent.GetComponent<UnityEngine.UI.ScrollRect>();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        if(rowScroll.content != row || rowScroll.viewport != row.parent ||
            !rowScroll.horizontal || rowScroll.vertical || row.rect.width <= rowScroll.viewport.rect.width)
            throw new System.InvalidOperationException("The station grid must scroll horizontally at " + size);
        var grid = row.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if(grid.cellSize != cardSize || grid.constraint != UnityEngine.UI.GridLayoutGroup.Constraint.FixedRowCount ||
            grid.constraintCount != 2 || !UnityEngine.Mathf.Approximately(bounds.height, cardSize.y * 2 + grid.spacing.y))
            throw new System.InvalidOperationException("The Faction viewport must fit two rows exactly.");
        var corners=new UnityEngine.Vector3[4];
        foreach(UnityEngine.Transform child in row)
        {
            ((UnityEngine.RectTransform)child).GetWorldCorners(corners);
            var points=corners.Select(scroll.viewport.InverseTransformPoint).ToArray();
            if(points.Any(p=>p.y<bounds.yMin-.01f || p.y>bounds.yMax+.01f))
                throw new System.InvalidOperationException("Faction card exceeds viewport height at "+size+": "+bounds.height+" px viewport; card y "+points.Min(p=>p.y)+".."+points.Max(p=>p.y));
            var rowPoints = corners.Select(rowScroll.viewport.InverseTransformPoint).ToArray();
            var rowBounds = rowScroll.viewport.rect;
            if(rowPoints.Any(p => p.y < rowBounds.yMin - .01f || p.y > rowBounds.yMax + .01f))
                throw new System.InvalidOperationException("Faction card exceeds the station grid viewport at " + size);
        }
        results.Add(new {resolution=size.ToString(),viewportHeight=bounds.height,rows=grid.constraintCount,cards=60,cell=cardSize.ToString(),horizontalScrolling=true});
    }
    layout.DetachContent();
    return results;
}
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(service); }
