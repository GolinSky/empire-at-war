using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using EmpireAtWar.Entities.Menu;
using EmpireAtWar.Entities.MainMenu.Play;
using EmpireAtWar.Entities.MainMenu.Skirmish;

public static class BuildSaveUi
{
    private const string PAUSE = "Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab";
    private const string PLAY = "Assets/Prefabs/Ui/MainMenu/PlayUi.prefab";

    public static string Run()
    {
        BuildPause();
        BuildPlay();
        AssetDatabase.SaveAssets();
        return "Saved and bound pause save controls and Play load list.";
    }

    private static void Bind(Component view, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(view);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(view);
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static TMP_Text Label(Transform parent, TMP_Text donor, string name, string text,
        float x, float y, float width, float height = 38)
    {
        var label = UnityEngine.Object.Instantiate(donor, parent);
        label.name = name;
        label.text = text;
        label.fontSize = 20;
        label.enableAutoSizing = true;
        label.fontSizeMin = 6;
        label.fontSizeMax = 20;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color32(248, 250, 252, 255);
        label.raycastTarget = false;
        Place(label.rectTransform, x, y, width, height);
        return label;
    }

    private static Button Button(Transform parent, TMP_Text donor, string name, string text,
        float x, float y, float width, float height = 56)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var image = root.AddComponent<Image>();
        image.color = new Color32(37, 99, 235, 255);
        var button = root.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.spriteState = default;
        Place((RectTransform)root.transform, x, y, width, height);
        var label = Label(root.transform, donor, "Label", text, 12, 0, width - 24, height);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void BuildPause()
    {
        var root = PrefabUtility.LoadPrefabContents(PAUSE);
        try
        {
            var view = root.GetComponent<PauseMenuUi>();
            var fields = new SerializedObject(view);
            var panel = ((GameObject)fields.FindProperty("menuPanel").objectReferenceValue).transform;
            var panelRect = (RectTransform)panel;
            panelRect.sizeDelta = new Vector2(920, 520);
            var donor = panel.Find("HeaderBanner/TitleText").GetComponent<TMP_Text>();
            Place((RectTransform)panel.Find("HeaderBanner"), 40, 26, 840, 50);
            Place(donor.rectTransform, 0, 0, 840, 50);
            donor.alignment = TextAlignmentOptions.Center;
            Place((RectTransform)panel.Find("ResumeButton"), 40, 130, 400, 62);
            Place((RectTransform)panel.Find("OptionsButton"), 40, 214, 400, 62);
            Place((RectTransform)panel.Find("ExitButton"), 40, 298, 400, 62);
            Place((RectTransform)panel.Find("PauseCaption"), 40, 390, 400, 70);
            var previous = panel.Find("SavePanel");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var savePanel = new GameObject("SavePanel", typeof(RectTransform));
            savePanel.transform.SetParent(panel, false);
            Place((RectTransform)savePanel.transform, 480, 106, 400, 380);
            var parent = savePanel.transform;
            Label(parent, donor, "SaveHeading", "SAVE BATTLE", 0, 0, 400);
            var inputRoot = new GameObject("SaveName", typeof(RectTransform));
            inputRoot.transform.SetParent(parent, false);
            Place((RectTransform)inputRoot.transform, 0, 48, 400, 52);
            var background = inputRoot.AddComponent<Image>();
            background.color = new Color32(24, 34, 50, 255);
            var input = inputRoot.AddComponent<TMP_InputField>();
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(inputRoot.transform, false);
            Place((RectTransform)viewport.transform, 12, 0, 376, 52);
            input.textViewport = (RectTransform)viewport.transform;
            input.textComponent = (TMP_Text)Label(viewport.transform, donor, "Text", "", 0, 0, 376, 52);
            input.placeholder = Label(viewport.transform, donor, "Placeholder", "Enter a save name", 0, 0, 376, 52);
            input.targetGraphic = background;
            input.characterLimit = 64;
            Bind(view, "savePanel", savePanel);
            Bind(view, "saveName", input);
            Bind(view, "saveButton", Button(parent, donor, "SaveButton", "SAVE / OVERWRITE", 0, 116, 400));
            Bind(view, "quickSaveButton", Button(parent, donor, "QuickSaveButton", "QUICKSAVE", 0, 188, 194));
            Bind(view, "quickLoadButton", Button(parent, donor, "QuickLoadButton", "QUICKLOAD", 206, 188, 194));
            Bind(view, "saveStatus", Label(parent, donor, "SaveStatus", "Same name replaces the existing save.", 0, 260, 400, 96));
            panel.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PAUSE);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void BuildPlay()
    {
        var root = PrefabUtility.LoadPrefabContents(PLAY);
        try
        {
            var view = root.GetComponent<PlayUi>();
            var panel = root.transform.Find("SafeArea/Panel");
            ((RectTransform)panel).sizeDelta = new Vector2(680, 900);
            var donor = panel.Find("HeaderBanner/TitleText").GetComponent<TMP_Text>();
            Place((RectTransform)panel.Find("HeaderBanner"), 40, 26, 600, 50);
            string[] buttons = { "StartDemoButton", "SurvivalButton", "SimulationButton", "ExitButton" };
            for (int i = 0; i < buttons.Length; i++) Place((RectTransform)panel.Find(buttons[i]), 140, 100 + i * 80, 400, 62);
            Place((RectTransform)panel.Find("FooterDivider"), 40, 842, 600, 1);
            Place((RectTransform)panel.Find("FooterHint"), 40, 852, 600, 24);
            var previous = panel.Find("LoadPanel");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var loadPanel = new GameObject("LoadPanel", typeof(RectTransform));
            loadPanel.transform.SetParent(panel, false);
            Place((RectTransform)loadPanel.transform, 40, 438, 600, 380);
            var parent = loadPanel.transform;
            Label(parent, donor, "LoadHeading", "LOAD BATTLE", 0, 0, 600);
            var skirmish = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab").GetComponent<SkirmishUi>();
            var template = (TMP_Dropdown)new SerializedObject(skirmish).FindProperty("mapSizeDropdown").objectReferenceValue;
            var mode = UnityEngine.Object.Instantiate(template, parent);
            mode.name = "SaveMode";
            mode.onValueChanged = new TMP_Dropdown.DropdownEvent();
            mode.ClearOptions();
            mode.AddOptions(new List<string> { "Skirmish", "Survival" });
            Place((RectTransform)mode.transform, 0, 48, 600, 52);
            var arrow = (RectTransform)mode.transform.Find("Arrow");
            arrow.anchorMin = arrow.anchorMax = new Vector2(1, 0.5f);
            arrow.anchoredPosition = new Vector2(-24, 0);
            mode.captionText.rectTransform.anchorMin = Vector2.zero;
            mode.captionText.rectTransform.anchorMax = Vector2.one;
            mode.captionText.rectTransform.offsetMin = new Vector2(16, 0);
            mode.captionText.rectTransform.offsetMax = new Vector2(-48, 0);
            var slots = UnityEngine.Object.Instantiate(mode, parent);
            slots.name = "SaveSlots";
            slots.ClearOptions();
            slots.AddOptions(new List<string> { "No saved battles" });
            Place((RectTransform)slots.transform, 0, 116, 600, 52);
            Bind(view, "saveMode", mode);
            Bind(view, "saveSlots", slots);
            Bind(view, "loadButton", Button(parent, donor, "LoadButton", "LOAD SELECTED SAVE", 0, 188, 600));
            Bind(view, "loadStatus", Label(parent, donor, "LoadStatus", "No saves for this mode.", 0, 260, 600, 96));
            PrefabUtility.SaveAsPrefabAsset(root, PLAY);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
