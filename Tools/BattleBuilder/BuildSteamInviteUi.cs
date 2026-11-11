using System;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.Popups;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildSteamInviteUi
{
    private const string POPUP_PATH = "Assets/Prefabs/Ui/Popups/SteamInvitePopupUi.prefab";
    private const string SURVIVAL_PATH = "Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab";
    private static TMP_Text _text;
    private static TMP_Dropdown _dropdown;

    public static string UpgradeAvatars()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(POPUP_PATH);
        try
        {
            var popup = new SerializedObject(RootView<SteamInvitePopupUi>(root));
            var view = new SerializedObject(popup.FindProperty("inviteView").objectReferenceValue);
            ConfigureAvatars((TMP_Dropdown)view.FindProperty("friends").objectReferenceValue);
            PrefabUtility.SaveAsPrefabAsset(root, POPUP_PATH);
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return "Saved avatar bindings and friend row layout in the shared Steam invite popup.";
    }

    private static void ConfigureAvatars(TMP_Dropdown friends)
    {
        friends.captionImage = Avatar(friends.transform, friends.captionImage, 12);
        friends.itemImage = Avatar(friends.itemText.transform.parent, friends.itemImage, 36);
        FriendLabel(friends.captionText, 56);
        FriendLabel(friends.itemText, 80);
        var item = (RectTransform)friends.itemText.transform.parent;
        item.sizeDelta = new Vector2(item.sizeDelta.x, 48);
    }

    private static Image Avatar(Transform parent, Image existing, float left)
    {
        Image image = existing != null ? existing : Rect(parent, "Avatar").gameObject.AddComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(left, 0);
        rect.sizeDelta = new Vector2(32, 32);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static void FriendLabel(TMP_Text label, float left)
    {
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(left, 4);
        label.rectTransform.offsetMax = new Vector2(-36, -4);
        label.enableAutoSizing = true;
        label.fontSizeMin = 6;
        label.fontSizeMax = label.fontSize = 20;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    public static string Run()
    {
        GameObject donor = PrefabUtility.LoadPrefabContents(SURVIVAL_PATH);
        try
        {
            var fields = new SerializedObject(RootView<SurvivalUi>(donor));
            _text = (TMP_Text)fields.FindProperty("startLabel").objectReferenceValue;
            _dropdown = (TMP_Dropdown)fields.FindProperty("playerFaction").objectReferenceValue;
            Build(donor.transform);
            AssetDatabase.SaveAssets();
        }
        finally { PrefabUtility.UnloadPrefabContents(donor); }
        return "Saved the shared Steam invite popup.";
    }

    private static void Build(Transform parent)
    {
        RectTransform root = Rect(parent, nameof(SteamInvitePopupUi));
        Stretch(root);
        CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        var popup = new SerializedObject(root.gameObject.AddComponent<SteamInvitePopupUi>());
        RectTransform overlay = Rect(root, "SteamInvites");
        Stretch(overlay);
        SteamInviteView view = overlay.gameObject.AddComponent<SteamInviteView>();

        RectTransform panel = Rect(overlay, "Panel");
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(700, 370);
        Background(panel, new Color32(8, 12, 20, 250), 16);
        Label(panel, "Title", "INVITE FRIENDS", 28, -24, 570, 44, 28).color = new Color32(56, 189, 248, 255);
        RectTransform divider = Rect(panel, "HeaderDivider");
        Place(divider, 28, -76, 644, 2);
        divider.gameObject.AddComponent<Image>().color = new Color32(56, 189, 248, 230);

        TMP_Dropdown friends = UnityEngine.Object.Instantiate(_dropdown, panel);
        friends.name = "OnlineFriends";
        friends.onValueChanged = new TMP_Dropdown.DropdownEvent();
        friends.ClearOptions();
        friends.captionText.richText = false;
        friends.itemText.richText = false;
        var dropdownRect = (RectTransform)friends.transform;
        dropdownRect.anchorMin = new Vector2(0, 0.5f);
        dropdownRect.anchorMax = new Vector2(1, 0.5f);
        dropdownRect.pivot = new Vector2(0.5f, 0.5f);
        dropdownRect.anchoredPosition = Vector2.zero;
        dropdownRect.sizeDelta = new Vector2(-56, 52);
        friends.template.sizeDelta = new Vector2(friends.template.sizeDelta.x, 320);
        RootView<ScrollRect>(friends.template.gameObject).scrollSensitivity = 8;
        foreach (RectTransform child in dropdownRect)
        {
            if (child.name != "Arrow") continue;
            child.anchorMin = child.anchorMax = child.pivot = new Vector2(1, 0.5f);
            child.anchoredPosition = new Vector2(-16, 0);
            break;
        }
        ConfigureAvatars(friends);
        TMP_Text status = Label(panel, "Status", "Choose an online friend to invite.", 0, -88, -56, 64, 22);
        status.rectTransform.anchorMax = Vector2.one;
        status.rectTransform.pivot = new Vector2(0.5f, 1);
        status.richText = false;
        Button refresh = Button(panel, "REFRESH", 28, -290, 220, false);
        Button invite = Button(panel, "INVITE", 452, -290, 220, true);
        Button close = Button(panel, "X", 620, -24, 52, false);
        MPImage closeBackground = (MPImage)close.targetGraphic;
        closeBackground.DrawShape = DrawShape.Circle;
        closeBackground.OutlineColor = new Color32(239, 68, 68, 255);
        closeBackground.OutlineWidth = 1.5f;
        var bindings = new SerializedObject(view);
        Bind(bindings, "friends", friends);
        Bind(bindings, "status", status);
        Bind(bindings, "inviteButton", invite);
        Bind(bindings, "refreshButton", refresh);
        Bind(bindings, "closeButton", close);
        bindings.ApplyModifiedPropertiesWithoutUndo();
        Bind(popup, "canvasGroup", canvasGroup);
        Bind(popup, "inviteView", view);
        popup.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, POPUP_PATH);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static T RootView<T>(GameObject root) where T : Component
    {
        SerializedProperty components = new SerializedObject(root).FindProperty("m_Component");
        for (int index = 0; index < components.arraySize; index++)
            if (components.GetArrayElementAtIndex(index).FindPropertyRelative("component").objectReferenceValue is T view)
                return view;
        throw new InvalidOperationException($"{root.name} has no {typeof(T).Name} root view.");
    }

    private static RectTransform Rect(Transform parent, string name)
    {
        var result = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        result.gameObject.layer = parent.gameObject.layer;
        result.SetParent(parent, false);
        return result;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static MPImage Background(RectTransform rect, Color color, float radius)
    {
        MPImage background = rect.gameObject.AddComponent<MPImage>();
        background.color = color;
        background.DrawShape = DrawShape.Rectangle;
        var shape = background.Rectangle;
        shape.CornerRadius = Vector4.one * radius;
        background.Rectangle = shape;
        background.OutlineWidth = 1.5f;
        background.OutlineColor = new Color32(30, 41, 59, 153);
        return background;
    }

    private static TMP_Text Label(Transform parent, string name, string text, float x, float y,
        float width, float height, float size)
    {
        TMP_Text label = UnityEngine.Object.Instantiate(_text, parent);
        label.name = name;
        label.text = text;
        label.enableAutoSizing = true;
        label.fontSizeMin = 6;
        label.fontSizeMax = size;
        label.fontSize = size;
        label.fontStyle = name == "Title" || name == "CloseButtonText" ? FontStyles.Bold : FontStyles.Normal;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.color = new Color32(248, 250, 252, 255);
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        Place(label.rectTransform, x, y, width, height);
        return label;
    }

    private static Button Button(Transform parent, string text, float x, float y, float width, bool primary)
    {
        RectTransform rect = Rect(parent, text == "X" ? "CloseButton" : text);
        Place(rect, x, y, width, 52);
        MPImage background = Background(rect, primary ? new Color32(37, 99, 235, 255) : new Color32(24, 34, 50, 255), 10);
        if (primary) background.OutlineColor = new Color32(96, 165, 250, 255);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.spriteState = default;
        TMP_Text label = Label(rect, text == "X" ? "CloseButtonText" : "Label", text, 8, -6, width - 16, 40, 22);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void Bind(SerializedObject fields, string field, UnityEngine.Object value) =>
        fields.FindProperty(field).objectReferenceValue = value;
}
