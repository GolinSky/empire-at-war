using System;
using EmpireAtWar.Entities.MainMenu.Online;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildSteamInviteUi
{
    private const string SKIRMISH_PATH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
    private const string SURVIVAL_PATH = "Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab";
    private static TMP_Text _text;
    private static TMP_Dropdown _dropdown;

    public static string Run()
    {
        var donor = new SerializedObject(AssetDatabase.LoadAssetAtPath<SurvivalUi>(SURVIVAL_PATH));
        _text = (TMP_Text)donor.FindProperty("startLabel").objectReferenceValue;
        _dropdown = (TMP_Dropdown)donor.FindProperty("playerFaction").objectReferenceValue;
        Build<SkirmishUi>(SKIRMISH_PATH);
        Build<SurvivalUi>(SURVIVAL_PATH);
        AssetDatabase.SaveAssets();
        return "Saved Steam friend pickers in Skirmish and Survival lobby prefabs.";
    }

    private static void Build<T>(string path) where T : Component
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var fields = new SerializedObject(RootView<T>(root));
            var previous = fields.FindProperty("steamInvites").objectReferenceValue as SteamInviteView;
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            RectTransform overlay = Rect(root.transform, "SteamInvites");
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            SteamInviteView view = overlay.gameObject.AddComponent<SteamInviteView>();
            RectTransform blocker = Rect(overlay, "Backdrop");
            blocker.anchorMin = Vector2.zero;
            blocker.anchorMax = Vector2.one;
            blocker.offsetMin = blocker.offsetMax = Vector2.zero;
            blocker.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

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
            Place((RectTransform)friends.transform, 28, -108, 644, 52);
            TMP_Text status = Label(panel, "Status", "Choose an online friend to invite.", 28, -180, 644, 80, 22);
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
            Bind(fields, "steamInvites", view);
            fields.ApplyModifiedPropertiesWithoutUndo();
            overlay.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
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
