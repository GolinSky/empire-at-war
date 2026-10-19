using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using EmpireAtWar.Entities.Saves;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.MainMenu.Play;

public static class BuildSaveUi
{
    private const string BROWSER = "Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab";
    private const string PLAY = "Assets/Prefabs/Ui/MainMenu/PlayUi.prefab";
    private const string PAUSE = "Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab";
    private static TMP_Text _fontDonor;
    private static Button _buttonDonor;

    public static string Run()
    {
        var playAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PLAY);
        _buttonDonor = playAsset.GetComponentsInChildren<Button>(true).Single(b => b.name == "StartDemoButton");
        _fontDonor = _buttonDonor.GetComponentInChildren<TMP_Text>(true);
        BuildBrowser();
        BuildMenus();
        AssetDatabase.SaveAssets();
        return "Saved SaveBrowserUi, PlayUi and PauseMenuUi with route bindings.";
    }

    private static void Bind(Component target, string name, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(name).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform r, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.pivot = new Vector2(.5f, .5f);
        r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
    }

    private static void Place(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = new Vector2(.5f, 1);
        r.pivot = new Vector2(.5f, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
    }

    private static Image Background(RectTransform r, Color32 color)
    {
        var image = r.gameObject.AddComponent<Image>(); image.color = color; return image;
    }

    private static TMP_Text Text(Transform parent, string name, string text, float size = 24)
    {
        var t = UnityEngine.Object.Instantiate(_fontDonor, parent);
        t.name = name; t.text = text; t.fontSize = size; t.enableAutoSizing = false;
        t.fontSizeMin = 6; t.fontSizeMax = size; t.color = new Color32(248,250,252,255);
        t.alignment = TextAlignmentOptions.MidlineLeft; t.margin = Vector4.zero;
        t.raycastTarget = false; t.rectTransform.localScale = Vector3.one;
        Stretch(t.rectTransform); return t;
    }

    private static Button Button(Transform parent, string name, string text)
    {
        var b = UnityEngine.Object.Instantiate(_buttonDonor, parent);
        b.name = name; b.onClick = new Button.ButtonClickedEvent();
        b.transition = Selectable.Transition.ColorTint; b.spriteState = default;
        b.interactable = true;
        var label = b.GetComponentInChildren<TMP_Text>(true);
        label.text = text; label.enableAutoSizing = false; label.fontSize = 24;
        Stretch(label.rectTransform, 24, 0, 24, 0);
        return b;
    }

    private static Toggle Tab(Transform parent, string name, float x, ToggleGroup group)
    {
        var b = Button(parent, name, name.ToUpperInvariant());
        var image = b.targetGraphic;
        UnityEngine.Object.DestroyImmediate(b);
        var t = image.gameObject.AddComponent<Toggle>();
        t.targetGraphic = image; t.group = group; t.transition = Selectable.Transition.ColorTint;
        var mark = Rect("Selected", t.transform); Stretch(mark, 0, 0, 0, 54);
        t.graphic = Background(mark, new Color32(56,189,248,255));
        Place((RectTransform)t.transform, x, 0, 260, 58);
        return t;
    }

    private static void BuildBrowser()
    {
        var root = new GameObject("SaveBrowserUi", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            Stretch((RectTransform)root.transform);
            var view = root.AddComponent<SaveBrowserUi>();
            Bind(view, "canvasGroup", root.GetComponent<CanvasGroup>());
            var panel = Rect("Panel", root.transform); Stretch(panel);
            Background(panel, new Color32(8,12,20,255));
            var title = Text(panel, "Title", "LOAD GAME", 30);
            Place(title.rectTransform, 0, 44, 1000, 48); title.alignment = TextAlignmentOptions.Center;
            Bind(view, "title", title);
            var divider = Rect("HeaderDivider", panel); Place(divider, 0, 112, 1000, 2);
            Background(divider, new Color32(56,189,248,230));
            var tabs = Rect("ModeTabs", panel); Place(tabs, 0, 140, 540, 58);
            var group = tabs.gameObject.AddComponent<ToggleGroup>();
            var skirmish = Tab(tabs, "Skirmish", -140, group);
            var survival = Tab(tabs, "Survival", 140, group);
            skirmish.isOn = true; survival.isOn = false;
            Bind(view, "modeTabs", tabs.gameObject); Bind(view, "skirmishToggle", skirmish); Bind(view, "survivalToggle", survival);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab");
            var close = UnityEngine.Object.Instantiate(source.GetComponentsInChildren<Button>(true).Single(b => b.name == "CloseButton"), panel);
            close.name = "CloseButton"; close.onClick = new Button.ButtonClickedEvent();
            var cr = (RectTransform)close.transform;
            cr.anchorMin = cr.anchorMax = cr.pivot = Vector2.one;
            cr.anchoredPosition = new Vector2(-36,-36); cr.sizeDelta = new Vector2(52,52);
            Bind(view,"closeButton",close);
            var scroll = Rect("Saves", panel); Stretch(scroll, 160, 190, 160, 230);
            var sr = scroll.gameObject.AddComponent<ScrollRect>(); sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 36;
            Background(scroll,new Color32(15,23,42,255));
            var viewport = Rect("Viewport",scroll); Stretch(viewport,16,12,16,12); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content",viewport); content.anchorMin = new Vector2(0,1); content.anchorMax=Vector2.one;
            content.pivot = new Vector2(.5f,1); content.sizeDelta=Vector2.zero; content.anchoredPosition=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing=12;
            layout.childControlWidth=true; layout.childControlHeight=true; layout.childForceExpandWidth=true; layout.childForceExpandHeight=false;
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport=viewport; sr.content=content;
            var row=Button(content,"SaveRow","Save name    •    Date    •    Battle time");
            var le=row.gameObject.AddComponent<LayoutElement>(); le.preferredHeight=84;
            var rowView=row.gameObject.AddComponent<SaveBrowserRow>(); Bind(rowView,"button",row); Bind(rowView,"label",row.GetComponentInChildren<TMP_Text>(true));
            row.GetComponentInChildren<TMP_Text>(true).alignment=TextAlignmentOptions.MidlineLeft;
            row.gameObject.SetActive(false);
            Bind(view,"content",content); Bind(view,"scrollRect",sr); Bind(view,"rowTemplate",rowView);
            var status=Text(panel,"Status","No saves for this mode.",22);
            status.rectTransform.anchorMin=new Vector2(0,0); status.rectTransform.anchorMax=new Vector2(1,0);
            status.rectTransform.pivot=new Vector2(.5f,0); status.rectTransform.offsetMin=new Vector2(160,110); status.rectTransform.offsetMax=new Vector2(-160,170);
            status.alignment=TextAlignmentOptions.Center; Bind(view,"status",status);
            var create=Button(panel,"SaveAsNewButton","SAVE AS NEW");
            var nr=(RectTransform)create.transform; nr.anchorMin=nr.anchorMax=new Vector2(.5f,0); nr.pivot=new Vector2(.5f,0); nr.anchoredPosition=new Vector2(0,32); nr.sizeDelta=new Vector2(400,62);
            Bind(view,"saveAsNewButton",create);
            var namePanel=Rect("NameEntry",panel); Stretch(namePanel); Background(namePanel,new Color32(3,5,8,245));
            var card=Rect("Card",namePanel); card.anchorMin=card.anchorMax=card.pivot=new Vector2(.5f,.5f); card.sizeDelta=new Vector2(760,380);
            Background(card,new Color32(15,23,42,255));
            var heading=Text(card,"Heading","SAVE AS NEW",28); Place(heading.rectTransform,0,28,660,44);
            var inputRect=Rect("SaveName",card); Place(inputRect,0,92,660,60);
            var bg=Background(inputRect,new Color32(24,34,50,255));
            var input=inputRect.gameObject.AddComponent<TMP_InputField>();
            var vp=Rect("TextArea",inputRect); Stretch(vp,16,6,16,6); vp.gameObject.AddComponent<RectMask2D>();
            var text=Text(vp,"Text","",24); var placeholder=Text(vp,"Placeholder","Enter a save name",24);
            placeholder.color=new Color32(148,163,184,255);
            input.textViewport=vp; input.textComponent=text; input.placeholder=placeholder; input.targetGraphic=bg;
            input.lineType=TMP_InputField.LineType.SingleLine; input.characterLimit=64;
            input.customCaretColor=true; input.caretColor=Color.white; input.caretWidth=2;
            text.enableAutoSizing=false; text.richText=false;
            var confirm=Button(card,"ConfirmName","SAVE"); Place((RectTransform)confirm.transform,172,194,316,62);
            var cancel=Button(card,"CancelName","CANCEL"); Place((RectTransform)cancel.transform,-172,194,316,62);
            var nameStatus=Text(card,"NameStatus","",20); Place(nameStatus.rectTransform,0,276,660,76); Bind(view,"nameStatus",nameStatus);
            Bind(view,"namePanel",namePanel.gameObject); Bind(view,"saveName",input); Bind(view,"confirmNameButton",confirm); Bind(view,"cancelNameButton",cancel);
            namePanel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,BROWSER);
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(BROWSER),settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(PLAY)).parentGroup);
            entry.address=nameof(SaveBrowserUi); EditorUtility.SetDirty(settings);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void BuildMenus()
    {
        var root=PrefabUtility.LoadPrefabContents(PLAY);
        try
        {
            var panel=root.transform.Find("SafeArea/Panel");
            var old=panel.Find("LoadPanel"); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var previous=panel.Find("LoadGameButton"); if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            ((RectTransform)panel).sizeDelta=new Vector2(680,650);
            Place((RectTransform)panel.Find("HeaderBanner"),0,26,600,50);
            var names=new[]{"StartDemoButton","SurvivalButton","SimulationButton"};
            for(int i=0;i<names.Length;i++) Place((RectTransform)panel.Find(names[i]),0,110+i*82,400,62);
            var load=Button(panel,"LoadGameButton","LOAD GAME"); Place((RectTransform)load.transform,0,356,400,62);
            Place((RectTransform)panel.Find("ExitButton"),0,462,400,62);
            Place((RectTransform)panel.Find("FooterDivider"),0,582,600,1); Place((RectTransform)panel.Find("FooterHint"),0,594,600,24);
            Bind(root.GetComponent<PlayUi>(),"loadButton",load);
            PrefabUtility.SaveAsPrefabAsset(root,PLAY);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        root=PrefabUtility.LoadPrefabContents(PAUSE);
        try
        {
            var view=root.GetComponent<PauseMenuUi>();
            var panel=((GameObject)new SerializedObject(view).FindProperty("menuPanel").objectReferenceValue).transform;
            var old=panel.Find("SavePanel"); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var previous=panel.Find("SaveGameButton"); if(previous!=null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            ((RectTransform)panel).sizeDelta=new Vector2(560,600);
            Place((RectTransform)panel.Find("HeaderBanner"),0,26,480,50);
            Place((RectTransform)panel.Find("HeaderBanner/TitleText"),0,0,480,50);
            Place((RectTransform)panel.Find("ResumeButton"),0,126,400,62);
            var save=UnityEngine.Object.Instantiate(panel.Find("ResumeButton").GetComponent<Button>(),panel);
            save.name="SaveGameButton"; save.onClick=new Button.ButtonClickedEvent();
            foreach(var trigger in save.GetComponentsInChildren<EmpireAtWar.Components.Ui.Tooltip.TooltipTrigger>(true)) UnityEngine.Object.DestroyImmediate(trigger);
            save.GetComponentInChildren<TMP_Text>(true).text="SAVE GAME";
            Place((RectTransform)save.transform,0,208,400,62);
            Place((RectTransform)panel.Find("OptionsButton"),0,290,400,62); Place((RectTransform)panel.Find("ExitButton"),0,372,400,62);
            Place((RectTransform)panel.Find("PauseCaption"),0,468,400,70);
            Bind(view,"saveButton",save);
            panel.gameObject.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root,PAUSE);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
