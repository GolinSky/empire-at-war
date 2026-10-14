using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.MainMenu.Survival;
using EmpireAtWar.Entities.MainMenu.Main;
using UnityEditor.AddressableAssets;

public static class BuildPhase0Ui
{
    public static string Run()
    {
        const string SKIRMISH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
        const string SURVIVAL = "Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab";
        const string MAIN = "Assets/Prefabs/Ui/MainMenu/MainMenuUi.prefab";

        void Bind(Component view, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        TMP_Text Label(Transform parent, TMP_Text donor, string name, string text, float x, float y, float width)
        {
            var label = UnityEngine.Object.Instantiate(donor, parent);
            label.name = name;
            label.text = text;
            label.fontSize = 20;
            label.enableAutoSizing = true;
            label.fontSizeMin = 6;
            label.fontSizeMax = 20;
            label.raycastTarget = false;
            Place(label.rectTransform, x, y, width, 32);
            return label;
        }

        Toggle ToggleField(Transform parent, TMP_Text donor, float x, float y)
        {
            var root = new GameObject("SuperWeaponsToggle", typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            Place((RectTransform)root.transform, x, y, 448, 36);
            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(root.transform, false);
            Place((RectTransform)box.transform, 0, -2, 30, 30);
            var background = box.GetComponent<Image>();
            background.color = new Color32(24, 34, 50, 255);
            var check = new GameObject("Check", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            Place((RectTransform)check.transform, 7, -7, 16, 16);
            var graphic = check.GetComponent<Image>();
            graphic.color = new Color32(56, 189, 248, 255);
            var toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = graphic;
            toggle.isOn = true;
            Label(root.transform, donor, "Label", "Super weapons", 46, 0, 390);
            return toggle;
        }

        var skirmish = PrefabUtility.LoadPrefabContents(SKIRMISH);
        try
        {
            var view = skirmish.GetComponent<SkirmishUi>();
            var oldToggle = skirmish.transform.Find("Background/BattleSettings/SuperWeaponsToggle");
            if (oldToggle != null) UnityEngine.Object.DestroyImmediate(oldToggle.gameObject);
            var settings = (RectTransform)skirmish.transform.Find("Background/BattleSettings");
            settings.sizeDelta = new Vector2(520, 712);
            var donor = settings.Find("StartingMoneyField/FactionName").GetComponent<TMP_Text>();
            Bind(view, "superWeaponsToggle", ToggleField(settings, donor, 36, -670));
            PrefabUtility.SaveAsPrefabAsset(skirmish, SKIRMISH);
        }
        finally { PrefabUtility.UnloadPrefabContents(skirmish); }

        var survival = PrefabUtility.LoadPrefabContents(SKIRMISH);
        try
        {
            survival.name = "SurvivalUi";
            var oldView = survival.GetComponent<SkirmishUi>();
            var oldSerialized = new SerializedObject(oldView);
            var close = (Button)oldSerialized.FindProperty("closeButton").objectReferenceValue;
            var start = (Button)oldSerialized.FindProperty("startGameButton").objectReferenceValue;
            var fieldTemplate = survival.transform.Find("Background/BattleSettings/MapSizeField").gameObject;
            var labelTemplate = fieldTemplate.transform.Find("FactionName").GetComponent<TMP_Text>();
            var settings = survival.transform.Find("Background/BattleSettings");
            UnityEngine.Object.DestroyImmediate(survival.transform.Find("Background/PlayersField").gameObject);
            // Preserve the existing panel frame and author the new fields before removing the old ones.
            var fields = new System.Collections.Generic.Dictionary<string, TMP_Dropdown>();
            string[] names = { "playerFaction", "allyFaction", "enemyFaction", "mapSize", "difficulty" };
            string[] labels = { "Your faction", "AI ally (optional)", "Wave enemy faction", "Map size", "AI difficulty" };
            for (int i = 0; i < names.Length; i++)
            {
                var field = UnityEngine.Object.Instantiate(fieldTemplate, settings);
                field.name = names[i];
                Place((RectTransform)field.transform, i < 3 ? 48 : 920, -132 - (i < 3 ? i : i - 3) * 126, 700, 90);
                field.transform.Find("FactionName").GetComponent<TMP_Text>().text = labels[i];
                fields.Add(names[i], field.GetComponentInChildren<TMP_Dropdown>(true));
            }
            var toggle = ToggleField(settings, labelTemplate, 920, -384);
            toggle.name = "SurvivalSuperWeapons";
            Label(settings, labelTemplate, "SurvivalRules", "Defend a level-5 station through five waves.", 48, -548, 1600);
            Label(settings, labelTemplate, "Availability", "Setup preview — Survival gameplay is not yet available.", 48, -596, 1600);
            string[] remove = { "PlanetPreviewFrame", "PlanetsDropDown", "MapSizeField", "VictoryConditionField", "StartingMoneyField", "EconomyDivider", "SuperWeaponsToggle" };
            foreach (string name in remove) UnityEngine.Object.DestroyImmediate(settings.Find(name).gameObject);
            Place((RectTransform)settings, 64, -208, 1792, 676);
            settings.Find("SectionTitle").GetComponent<TMP_Text>().text = "SURVIVAL SETUP";
            survival.transform.Find("Background/HeaderBanner/TitleText").GetComponent<TMP_Text>().text = "SURVIVAL";
            survival.transform.Find("Background/DeploymentFooter/ReadinessText").GetComponent<TMP_Text>().text = "COMING SOON";
            survival.transform.Find("Background/DeploymentFooter/MatchSummaryText").GetComponent<TMP_Text>().text = "Configure your defense. Starting a Survival battle will be enabled with the wave system.";
            start.interactable = false;
            start.GetComponentInChildren<TMP_Text>(true).text = "NOT YET AVAILABLE";
            var hover = oldView.TooltipHover;
            UnityEngine.Object.DestroyImmediate(oldView);
            UnityEngine.Object.DestroyImmediate(hover);
            var view = survival.AddComponent<SurvivalUi>();
            Bind(view, "canvasGroup", survival.GetComponent<CanvasGroup>());
            Bind(view, "closeButton", close);
            Bind(view, "startButton", start);
            Bind(view, "superWeapons", toggle);
            foreach (var pair in fields) Bind(view, pair.Key, pair.Value);
            PrefabUtility.SaveAsPrefabAsset(survival, SURVIVAL);
        }
        finally { PrefabUtility.UnloadPrefabContents(survival); }

        var main = PrefabUtility.LoadPrefabContents(MAIN);
        try
        {
            var view = main.GetComponent<MainMenuUi>();
            var so = new SerializedObject(view);
            var start = (Button)so.FindProperty("startDemoButton").objectReferenceValue;
            var options = (Button)so.FindProperty("optionsButton").objectReferenceValue;
            var quit = (Button)so.FindProperty("quitApplicationButton").objectReferenceValue;
            foreach (string name in new[] { "SurvivalButton", "SimulationButton" })
            {
                var old = start.transform.parent.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var survivalButton = UnityEngine.Object.Instantiate(start, start.transform.parent);
            survivalButton.name = "SurvivalButton";
            survivalButton.GetComponentInChildren<TMP_Text>(true).text = "SURVIVAL";
            var simulation = UnityEngine.Object.Instantiate(start, start.transform.parent);
            simulation.name = "SimulationButton";
            simulation.GetComponentInChildren<TMP_Text>(true).text = "SIMULATION — COMING SOON";
            simulation.interactable = false;
            Button[] buttons = { start, survivalButton, simulation, options, quit };
            for (int i = 0; i < buttons.Length; i++)
            {
                var rect = (RectTransform)buttons[i].transform;
                rect.anchoredPosition = new Vector2(0, 170 - i * 75);
                rect.sizeDelta = new Vector2(420, 68);
                buttons[i].transition = Selectable.Transition.ColorTint;
                buttons[i].spriteState = default;
            }
            Bind(view, "survivalButton", survivalButton);
            Bind(view, "simulationButton", simulation);
            PrefabUtility.SaveAsPrefabAsset(main, MAIN);
        }
        finally { PrefabUtility.UnloadPrefabContents(main); }

        var addressables = AddressableAssetSettingsDefaultObject.Settings;
        var entry = addressables.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(SURVIVAL),
            addressables.FindAssetEntry(AssetDatabase.AssetPathToGUID(SKIRMISH)).parentGroup);
        entry.address = "SurvivalUi";
        EditorUtility.SetDirty(addressables);
        AssetDatabase.SaveAssets();
        return "Saved Skirmish toggle, Survival setup and main menu buttons; registered SurvivalUi.";

    }
}
