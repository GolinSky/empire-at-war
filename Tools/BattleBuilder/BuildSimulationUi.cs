using System;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MainMenu.Simulation;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.Map;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;

public static class BuildSimulationUi
{
    private static TMP_Text _text;
    private static TMP_Dropdown _dropdown;
    private static Button _button;
    private static Toggle _toggle;
    private static readonly Color BACKGROUND = new Color32(10, 17, 29, 255);
    public static string Run()
    {
        const string SURVIVAL = "Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab";
        var donor = PrefabUtility.LoadPrefabContents(SURVIVAL);
        try
        {
            var source = new SerializedObject(donor.GetComponent<SurvivalUi>());
            _dropdown = (TMP_Dropdown)source.FindProperty("playerFaction").objectReferenceValue;
            _button = (Button)source.FindProperty("startButton").objectReferenceValue;
            _toggle = (Toggle)source.FindProperty("superWeapons").objectReferenceValue;
            _text = donor.GetComponentsInChildren<TMP_Text>(true).First();
            BuildSetup(); BuildHud();
            var core = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab");
            try
            {
                var coreFields = new SerializedObject(core.GetComponent<EmpireAtWar.Entities.CoreGameUi.CoreGameUi>());
                Bind(coreFields, "modeLabel", core.GetComponentsInChildren<TMP_Text>(true).Single(label => label.text == "SKIRMISH"));
                coreFields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(core, "Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(core); }
            var survivalHud = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/CoreGame/SurvivalHudUi.prefab");
            try
            {
                var hudFields = new SerializedObject(survivalHud.GetComponent<SurvivalHudUi>());
                var oldButton = (Button)hudFields.FindProperty("startWaveButton").objectReferenceValue;
                var button = Button(oldButton.transform.parent, "START WAVE NOW", 95, -83, 250);
                UnityEngine.Object.DestroyImmediate(oldButton.gameObject);
                Bind(hudFields, "startWaveButton", button); hudFields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(survivalHud, "Assets/Prefabs/Ui/CoreGame/SurvivalHudUi.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(survivalHud); }
            _button.interactable = true; _button.GetComponentInChildren<TMP_Text>(true).text = "START SURVIVAL";
            foreach (var label in donor.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name == "Availability") label.text = "120 seconds to prepare. Clear five waves to win.";
                if (label.name == "ReadinessText") label.text = "READY TO DEFEND";
                if (label.name == "MatchSummaryText") label.text = "Your station starts at level 5. The optional ally shares your production base.";
            }
            PrefabUtility.SaveAsPrefabAsset(donor, SURVIVAL);
        }
        finally { PrefabUtility.UnloadPrefabContents(donor); }
        var map = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
        var mapFields = new SerializedObject(map);
        var bounds = mapFields.FindProperty("<SimulationBounds>k__BackingField");
        bounds.FindPropertyRelative("<Min>k__BackingField").vector2Value = Vector2.one * -100000;
        bounds.FindPropertyRelative("<Max>k__BackingField").vector2Value = Vector2.one * 100000;
        mapFields.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(map);
        var play = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/PlayUi.prefab");
        try
        {
            foreach (var label in play.GetComponentsInChildren<TMP_Text>(true))
                if (label.text.Contains("COMING SOON")) label.text = "AI VS AI";
            var fields = new SerializedObject(play.GetComponent<EmpireAtWar.Entities.MainMenu.Play.PlayUi>());
            ((Button)fields.FindProperty("simulationButton").objectReferenceValue).interactable = true;
            PrefabUtility.SaveAsPrefabAsset(play, "Assets/Prefabs/Ui/MainMenu/PlayUi.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(play); }
        AssetDatabase.SaveAssets();
        return "Saved Simulation setup/HUD, Survival launch labels, and simulation map bounds.";
    }

    private static void BuildSetup()
    {
        var root = Root("SimulationSetupUi", typeof(SimulationSetupUi));
        try
        {
            var panel = Panel(root.transform, "Panel", 1800, 960);
            var view = new SerializedObject(root.GetComponent<SimulationSetupUi>());
            Bind(view, "canvasGroup", root.GetComponent<CanvasGroup>());
            Label(panel, "BATTLE SIMULATION", 40, -24, 1300, 48, 32);
            Label(panel, "Choose both fleets, then watch them fight. Add multiple entries to mix units.", 40, -78, 1600);
            foreach (string field in new[] { "factions", "units", "fleets", "quantities", "quantityLabels", "addButtons", "removeButtons" })
                view.FindProperty(field).arraySize = 2;
            for (int side = 0; side < 2; side++)
            {
                float x = 40 + side * 900;
                Label(panel, side == 0 ? "SIDE A" : "SIDE B", x, -144, 800, 42, 26);
                Set(view, "factions", side, Dropdown(panel, "Faction", x, -200));
                Label(panel, "Unit", x, -258, 800);
                Set(view, "units", side, Dropdown(panel, "Unit", x, -300));
                Set(view, "quantityLabels", side, Label(panel, "Quantity: 1", x, -360, 800));
                Set(view, "quantities", side, Slider(panel, "Quantity", x, -408, 1, 100, 1));
                Set(view, "addButtons", side, Button(panel, "ADD TO FLEET", x, -460, 800));
                Label(panel, "Fleet entries", x, -526, 800);
                Set(view, "fleets", side, Dropdown(panel, "Fleet", x, -566));
                Set(view, "removeButtons", side, Button(panel, "REMOVE SELECTED ENTRY", x, -624, 800));
            }
            Bind(view, "distanceLabel", Label(panel, "Separation", 40, -706, 500));
            Bind(view, "distance", Slider(panel, "Distance", 40, -752, 500, 10000, 2000, 500));
            Bind(view, "timeLabel", Label(panel, "Time limit", 630, -706, 500));
            Bind(view, "timeLimit", Slider(panel, "TimeLimit", 630, -752, 30, 3600, 300, 500));
            var toggle = UnityEngine.Object.Instantiate(_toggle, panel); toggle.name = "FastSpeed";
            Place((RectTransform)toggle.transform, 1250, -724, 450, 46);
            toggle.GetComponentInChildren<TMP_Text>(true).text = "Start at 4× speed"; toggle.isOn = false;
            Bind(view, "fastSpeed", toggle);
            Bind(view, "closeButton", Button(panel, "BACK", 40, -858, 360));
            Bind(view, "startButton", Button(panel, "START SIMULATION", 1200, -858, 540));
            view.ApplyModifiedPropertiesWithoutUndo();
            Save(root, "Assets/Prefabs/Ui/MainMenu/SimulationSetupUi.prefab", nameof(SimulationSetupUi));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void BuildHud()
    {
        var root = Root("SimulationHudUi", typeof(SimulationHudUi));
        try
        {
            var r = (RectTransform)root.transform; r.anchorMin = r.anchorMax = new Vector2(.5f, 1);
            r.pivot = new Vector2(.5f, 1); r.sizeDelta = new Vector2(1000, 280); r.anchoredPosition = new Vector2(0, -70);
            var panel = Panel(root.transform, "Panel", 1000, 280);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, 1);
            var view = new SerializedObject(root.GetComponent<SimulationHudUi>());
            Bind(view, "canvasGroup", root.GetComponent<CanvasGroup>());
            Bind(view, "panel", panel);
            Bind(view, "status", Label(panel, "Deploying fleets…", 24, -12, 952, 38, 22));
            string[] labels = { "PAUSE", "SPEED 1×", "RESTART", "NEW" };
            string[] fields = { "pauseButton", "speedButton", "restartButton", "newButton" };
            for (int i = 0; i < 4; i++)
            {
                var button = Button(panel, labels[i], 24 + i * 242, -62, 226);
                Bind(view, fields[i], button);
                if (i < 2) Bind(view, i == 0 ? "pauseLabel" : "speedLabel", button.GetComponentInChildren<TMP_Text>(true));
            }
            Bind(view, "result", Label(panel, "", 24, -132, 952, 128, 22));
            view.ApplyModifiedPropertiesWithoutUndo();
            Save(root, "Assets/Prefabs/Ui/CoreGame/SimulationHudUi.prefab", nameof(SimulationHudUi));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static GameObject Root(string name, Type view)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), view);
        var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return root;
    }
    private static RectTransform Panel(Transform parent, string name, float width, float height)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); panel.transform.SetParent(parent, false);
        var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(width, height); panel.GetComponent<Image>().color = BACKGROUND; return rect;
    }
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
    }
    private static TMP_Text Label(Transform parent, string text, float x, float y, float width, float height = 36, float size = 22)
    {
        var label = UnityEngine.Object.Instantiate(_text, parent); label.name = text.Length == 0 ? "Result" : text;
        label.text = text; label.fontSize = size; label.enableAutoSizing = true; label.fontSizeMin = 6; label.fontSizeMax = size;
        label.alignment = TextAlignmentOptions.Left; label.color = Color.white; label.raycastTarget = false;
        Place(label.rectTransform, x, y, width, height); return label;
    }
    private static TMP_Dropdown Dropdown(Transform parent, string name, float x, float y)
    {
        var field = UnityEngine.Object.Instantiate(_dropdown, parent); field.name = name;
        field.onValueChanged = new TMP_Dropdown.DropdownEvent(); Place((RectTransform)field.transform, x, y, 800, 48);
        return field;
    }
    private static Button Button(Transform parent, string text, float x, float y, float width)
    {
        var root = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); root.transform.SetParent(parent, false);
        var button = root.GetComponent<Button>(); var background = root.GetComponent<Image>();
        background.color = new Color32(24, 64, 85, 255); button.targetGraphic = background;
        var colors = button.colors; colors.disabledColor = new Color(1, 1, 1, .35f); button.colors = colors;
        var label = Label(root.transform, text, 12, -6, width - 24, 40, 22); label.alignment = TextAlignmentOptions.Center;
        Place((RectTransform)button.transform, x, y, width, 52); return button;
    }
    private static Slider Slider(Transform parent, string name, float x, float y, float min, float max, float value, float width = 800)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider)); root.transform.SetParent(parent, false);
        Place((RectTransform)root.transform, x, y, width, 32);
        var track = Panel(root.transform, "Track", width, 8);
        track.GetComponent<Image>().color = new Color32(45, 63, 84, 255);
        var handle = Panel(root.transform, "Handle", 24, 32); handle.GetComponent<Image>().color = new Color32(56, 189, 248, 255);
        var slider = root.GetComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
        handle.sizeDelta = new Vector2(24, 0);
        slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = true; slider.value = value; return slider;
    }
    private static void Bind(SerializedObject view, string field, UnityEngine.Object value) => view.FindProperty(field).objectReferenceValue = value;
    private static void Set(SerializedObject view, string field, int index, UnityEngine.Object value) => view.FindProperty(field).GetArrayElementAtIndex(index).objectReferenceValue = value;
    private static void Save(GameObject root, string path, string address)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID("Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab")).parentGroup;
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group).address = address; EditorUtility.SetDirty(settings);
    }
}
