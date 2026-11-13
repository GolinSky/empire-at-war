using System;
using System.Linq;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Repository.Data;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.MPUIKIT;
using Object = UnityEngine.Object;

public static class BuildPlanetSelection
{
    private const string POPUP_PATH = "Assets/Prefabs/Ui/Popups/PlanetSelectionPopupUi.prefab";
    private const string SKIRMISH_PATH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
    private static readonly Color32 _navy = new Color32(16, 26, 41, 255);
    private static readonly Color32 _slate = new Color32(32, 46, 64, 255);
    private static readonly Color32 _gold = new Color32(210, 169, 94, 255);
    private static readonly Color32 _white = new Color32(242, 240, 230, 255);
    private static readonly Color32 _muted = new Color32(176, 191, 208, 255);
    private static readonly Color32 _border = new Color32(109, 139, 168, 255);
    private static TMP_FontAsset _bodyFont;
    private static TMP_FontAsset _boldFont;
    private static TMP_FontAsset _headingFont;

    public static string Run()
    {
        _bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/CoreGameHud/InterTight-Medium SDF.asset");
        _boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/CoreGameHud/InterTight-Bold SDF.asset");
        _headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Fonts/CoreGameHud/Rajdhani-Bold SDF.asset");
        BuildPopup();
        ReplaceDropdown();
        RegisterPopup();
        AssetDatabase.SaveAssets();
        return "Saved planet gallery with 57 cards, search, scrolling, selected state and both Skirmish entry buttons.";
    }

    private static void BuildPopup()
    {
        RectTransform root = Rect("PlanetSelectionPopupUi", null);
        Stretch(root);
        root.sizeDelta = Vector2.zero;
        var view = root.gameObject.AddComponent<PlanetSelectionPopupUi>();
        Bind(view, "canvasGroup", root.GetComponent<CanvasGroup>());
        try
        {
            RectTransform panel = Rect("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(1500, 920);
            Surface(panel, _navy, 16, _border);
            Text(panel, "Eyebrow", "BATTLE LOCATION", 22, _gold, 44, 28, 1200, 30);
            Text(panel, "Title", "Choose your planet", 44, _white, 44, 60, 1250, 58, _headingFont);
            Text(panel, "Description", "Select a planet to return to battle settings.", 24, _muted, 44, 121, 1250, 36);
            Button close = ButtonAt(panel, "CloseButton", "X", 1400, 32, 56, 56);
            MPImage closeSurface = (MPImage)close.targetGraphic;
            var closeShape = closeSurface.Rectangle;
            closeShape.CornerRadius = new Vector4(28, 28, 28, 28);
            closeSurface.Rectangle = closeShape;
            closeSurface.OutlineColor = new Color32(232, 110, 108, 255);
            Bind(view, "closeButton", close);

            RectTransform searchRect = Rect("SearchInput", panel);
            Place(searchRect, 44, 180, 1150, 62);
            MPImage searchSurface = Surface(searchRect, _slate, 8, _border);
            var search = searchRect.gameObject.AddComponent<TMP_InputField>();
            search.targetGraphic = searchSurface;
            search.lineType = TMP_InputField.LineType.SingleLine;
            search.characterLimit = 80;
            search.customCaretColor = true;
            search.caretColor = _gold;
            search.selectionColor = new Color32(109, 184, 226, 100);
            search.caretWidth = 2;
            RectTransform textArea = Rect("Text Area", searchRect);
            Stretch(textArea, 18, 8, 18, 8);
            textArea.gameObject.AddComponent<RectMask2D>();
            TMP_Text inputText = Text(textArea, "Text", "", 26, _white, 0, 0, 0, 0);
            Stretch(inputText.rectTransform);
            TMP_Text placeholder = Text(textArea, "Placeholder", "Search planets...", 26, _muted, 0, 0, 0, 0);
            Stretch(placeholder.rectTransform);
            search.textViewport = textArea;
            search.textComponent = (TextMeshProUGUI)inputText;
            search.placeholder = placeholder;
            Bind(view, "searchInput", search);
            TMP_Text count = Text(panel, "Count", "57 / 57 planets", 23, _muted, 1220, 180, 236, 62);
            count.alignment = TextAlignmentOptions.MidlineRight;
            Bind(view, "countText", count);

            RectTransform scrollRoot = Rect("PlanetScroll", panel);
            Place(scrollRoot, 44, 266, 1412, 566);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45;
            RectTransform viewport = Rect("Viewport", scrollRoot);
            Stretch(viewport, 0, 0, 24, 0);
            var hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(264, 220);
            grid.spacing = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = CreateScrollbar(scrollRoot);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            Bind(view, "scrollRect", scroll);

            PlanetType[] planets = Enum.GetValues(typeof(PlanetType)).Cast<PlanetType>()
                .OrderBy(PlanetSelectionPopupUi.GetPlanetName, StringComparer.OrdinalIgnoreCase).ToArray();
            var cards = new PlanetSelectionCardView[planets.Length];
            for (int i = 0; i < planets.Length; i++) cards[i] = CreateCard(content, planets[i]);
            var serialized = new SerializedObject(view);
            SerializedProperty array = serialized.FindProperty("cards");
            array.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text empty = Text(viewport, "EmptyState", "No planets found. Try another name.", 28, _muted, 0, 80, 1388, 60);
            empty.alignment = TextAlignmentOptions.Center;
            empty.gameObject.SetActive(false);
            Bind(view, "emptyState", empty.gameObject);
            RectTransform divider = Rect("FooterDivider", panel);
            Place(divider, 0, 854, 1500, 1);
            var line = divider.gameObject.AddComponent<Image>();
            line.color = new Color32(52, 67, 87, 255);
            line.raycastTarget = false;
            TMP_Text current = Text(panel, "CurrentPlanet", "Current planet: Coruscant", 24, _white, 44, 870, 800, 34);
            Bind(view, "currentPlanetText", current);
            TMP_Text hint = Text(panel, "FooterHint", "Scroll to explore  /  Esc to cancel", 22, _muted, 900, 870, 556, 34);
            hint.alignment = TextAlignmentOptions.MidlineRight;
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, POPUP_PATH);
        }
        finally { Object.DestroyImmediate(root.gameObject); }
    }

    private static PlanetSelectionCardView CreateCard(Transform parent, PlanetType planet)
    {
        RectTransform rect = Rect(planet.ToString(), parent);
        Surface(rect, _slate, 10, _border);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<MPImage>();
        Style(button);
        var card = rect.gameObject.AddComponent<PlanetSelectionCardView>();
        RectTransform imageRect = Rect("Preview", rect);
        Place(imageRect, 2, 2, 260, 164);
        RawImage preview = imageRect.gameObject.AddComponent<RawImage>();
        preview.raycastTarget = false;
        // Center crop the existing 1344 x 564 previews without stretching their planets.
        float visibleWidth = (260f / 164f) / (1344f / 564f);
        preview.uvRect = new Rect((1f - visibleWidth) * .5f, 0, visibleWidth, 1);
        TMP_Text label = Text(rect, "Name", PlanetSelectionPopupUi.GetPlanetName(planet), 24, _white, 16, 170, 232, 44, _boldFont);
        label.enableAutoSizing = true;
        label.fontSizeMin = 6;
        label.fontSizeMax = 24;
        RectTransform outlineRect = Rect("SelectionOutline", rect);
        Stretch(outlineRect);
        MPImage outline = Surface(outlineRect, _gold, 10, _gold);
        outline.OutlineWidth = 0;
        outline.StrokeWidth = 3;
        outline.raycastTarget = false;
        RectTransform badge = Rect("SelectedIndicator", rect);
        Place(badge, 133, 10, 120, 32);
        Surface(badge, _gold, 4, _gold).raycastTarget = false;
        TMP_Text selected = Text(badge, "Label", "SELECTED", 18, _navy, 0, 0, 120, 32, _boldFont);
        selected.alignment = TextAlignmentOptions.Center;
        badge.gameObject.SetActive(false);
        outline.enabled = false;
        var serialized = new SerializedObject(card);
        serialized.FindProperty("planet").intValue = (int)planet;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Bind(card, "button", button);
        Bind(card, "previewImage", preview);
        Bind(card, "nameText", label);
        Bind(card, "selectedIndicator", badge.gameObject);
        Bind(card, "selectionOutline", outline);
        return card;
    }

    private static Scrollbar CreateScrollbar(Transform parent)
    {
        RectTransform rect = Rect("Scrollbar", parent);
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1, .5f);
        rect.sizeDelta = new Vector2(10, 0);
        Surface(rect, _slate, 5, _slate);
        var scrollbar = rect.gameObject.AddComponent<Scrollbar>();
        RectTransform handle = Rect("Handle", rect);
        Stretch(handle);
        MPImage image = Surface(handle, _border, 5, _border);
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = image;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private static void ReplaceDropdown()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SKIRMISH_PATH);
        try
        {
            var view = root.GetComponent<SkirmishUi>();
            var serialized = new SerializedObject(view);
            var preview = (RawImage)serialized.FindProperty("planetPreviewImage").objectReferenceValue;
            // The popup can be rebuilt without repeating the one-time entry-control migration.
            if (serialized.FindProperty("planetButton").objectReferenceValue != null) return;
            TMP_Dropdown dropdown = root.GetComponentsInChildren<TMP_Dropdown>(true)
                .Single(d => d.transform.parent.name == "PlanetsDropDown");
            TMP_Text caption = dropdown.captionText;
            Graphic background = dropdown.targetGraphic;
            GameObject buttonObject = dropdown.gameObject;
            Object.DestroyImmediate(dropdown.template.gameObject);
            Transform arrow = buttonObject.transform.Find("Arrow");
            if (arrow != null) Object.DestroyImmediate(arrow.gameObject);
            Object.DestroyImmediate(dropdown);
            buttonObject.name = "PlanetButton";
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            background.color = _slate;
            Style(button);
            caption.font = _boldFont;
            caption.color = _white;
            Stretch(caption.rectTransform, 16, 0, 116, 0);
            TMP_Text change = Text(buttonObject.transform, "ChangeLabel", "CHANGE", 19, _muted, 0, 0, 100, 42, _boldFont);
            change.rectTransform.anchorMin = new Vector2(1, 0);
            change.rectTransform.anchorMax = Vector2.one;
            change.rectTransform.pivot = new Vector2(1, .5f);
            change.rectTransform.anchoredPosition = new Vector2(-16, 0);
            change.rectTransform.sizeDelta = new Vector2(100, 0);
            change.alignment = TextAlignmentOptions.MidlineRight;
            Button previewButton = preview.gameObject.AddComponent<Button>();
            RectTransform affordance = Rect("ChangePlanet", preview.transform);
            affordance.anchorMin = affordance.anchorMax = new Vector2(1, 0);
            affordance.pivot = new Vector2(1, 0);
            affordance.anchoredPosition = new Vector2(-12, 12);
            affordance.sizeDelta = new Vector2(180, 38);
            MPImage previewTarget = Surface(affordance, _slate, 8, _border);
            previewButton.targetGraphic = previewTarget;
            Style(previewButton);
            TMP_Text previewLabel = Text(affordance, "Label", "CHANGE PLANET", 18, _white, 0, 0, 180, 38, _boldFont);
            previewLabel.alignment = TextAlignmentOptions.Center;
            preview.raycastTarget = true;
            Bind(view, "planetButton", button);
            Bind(view, "planetLabel", caption);
            Bind(view, "planetPreviewButton", previewButton);
            PrefabUtility.SaveAsPrefabAsset(root, SKIRMISH_PATH);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void RegisterPopup()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(SKIRMISH_PATH)).parentGroup;
        string guid = AssetDatabase.AssetPathToGUID(POPUP_PATH);
        var entry = settings.CreateOrMoveEntry(guid, group);
        entry.address = nameof(PlanetSelectionPopupUi);
        EditorUtility.SetDirty(group);
        EditorUtility.SetDirty(settings);
        var mapping = AssetDatabase.LoadAssetAtPath<AssetMappingData>("Assets/Settings/AssetMappingData.asset");
        var serialized = new SerializedObject(mapping);
        SerializedProperty pairs = serialized.FindProperty("assetMappings.keyValue");
        int index = 0;
        while (index < pairs.arraySize && pairs.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue != entry.address) index++;
        if (index == pairs.arraySize) pairs.InsertArrayElementAtIndex(index);
        SerializedProperty pair = pairs.GetArrayElementAtIndex(index);
        pair.FindPropertyRelative("key").stringValue = entry.address;
        SerializedProperty reference = pair.FindPropertyRelative("value");
        reference.FindPropertyRelative("m_AssetGUID").stringValue = guid;
        reference.FindPropertyRelative("m_SubObjectName").stringValue = string.Empty;
        reference.FindPropertyRelative("m_SubObjectType").stringValue = string.Empty;
        reference.FindPropertyRelative("m_SubObjectGUID").stringValue = string.Empty;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mapping);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static MPImage Surface(RectTransform rect, Color color, float radius, Color border)
    {
        var image = rect.gameObject.AddComponent<MPImage>();
        image.DrawShape = DrawShape.Rectangle;
        var shape = image.Rectangle;
        shape.CornerRadius = new Vector4(radius, radius, radius, radius);
        image.Rectangle = shape;
        image.color = color;
        image.OutlineColor = border;
        image.OutlineWidth = 1.5f;
        return image;
    }

    private static TMP_Text Text(Transform parent, string name, string value, float size, Color color,
        float x, float y, float width, float height, TMP_FontAsset font = null)
    {
        RectTransform rect = Rect(name, parent);
        Place(rect, x, y, width, height);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font == null ? _bodyFont : font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static Button ButtonAt(Transform parent, string name, string label, float x, float y, float width, float height)
    {
        RectTransform rect = Rect(name, parent);
        Place(rect, x, y, width, height);
        MPImage image = Surface(rect, _slate, 10, _border);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Style(button);
        TMP_Text text = Text(rect, "Label", label, 26, _white, 0, 0, width, height, _boldFont);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void Style(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        button.spriteState = default;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(65f / 32, 86f / 46, 112f / 64, 1);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = Color.white;
        colors.disabledColor = new Color(.75f, .75f, .75f, 1);
        colors.fadeDuration = .08f;
        button.colors = colors;
    }

    private static void Bind(Component target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
