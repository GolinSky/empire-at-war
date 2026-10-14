using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.MainMenu.Main;
using EmpireAtWar.Entities.MainMenu.Play;

public static class BuildPlayMenu
{
    public static string Run()
    {
        const string MAIN = "Assets/Prefabs/Ui/MainMenu/MainMenuUi.prefab";
        const string PLAY = "Assets/Prefabs/Ui/MainMenu/PlayUi.prefab";
        void Bind(Component view, string field, Object value)
        {
            var so = new SerializedObject(view);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        void Style(Button button, string label, float y)
        {
            button.GetComponentInChildren<TMP_Text>(true).text = label;
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(400, 68);
            button.transition = Selectable.Transition.ColorTint;
            button.spriteState = default;
        }
        var play = PrefabUtility.LoadPrefabContents(AssetDatabase.LoadAssetAtPath<GameObject>(PLAY) != null ? PLAY : MAIN);
        try
        {
            play.name = "PlayUi";
            var buttons = play.GetComponentsInChildren<Button>(true);
            var skirmish = buttons.Single(x => x.name == "StartDemoButton");
            var survival = buttons.Single(x => x.name == "SurvivalButton");
            var simulation = buttons.Single(x => x.name == "SimulationButton");
            var back = buttons.Single(x => x.name == "ExitButton");
            var options = buttons.SingleOrDefault(x => x.name == "OptionsButton");
            if (options != null) Object.DestroyImmediate(options.gameObject);
            foreach (var hover in play.GetComponentsInChildren<TooltipHoverView>(true)) Object.DestroyImmediate(hover);
            foreach (var trigger in play.GetComponentsInChildren<TooltipTrigger>(true)) Object.DestroyImmediate(trigger);
            var old = play.GetComponent<MainMenuUi>();
            if (old != null) Object.DestroyImmediate(old);
            var view = play.GetComponent<PlayUi>();
            if (view == null) view = play.AddComponent<PlayUi>();
            Bind(view, "canvasGroup", play.GetComponent<CanvasGroup>());
            Bind(view, "skirmishButton", skirmish);
            Bind(view, "survivalButton", survival);
            Bind(view, "simulationButton", simulation);
            Bind(view, "backButton", back);
            Style(skirmish, "SKIRMISH", 145);
            Style(survival, "SURVIVAL", 60);
            Style(simulation, "BATTLE SIMULATION — COMING SOON", -25);
            Style(back, "BACK", -135);
            simulation.GetComponentInChildren<TMP_Text>(true).fontSize = 20;
            simulation.interactable = false;
            foreach (var text in play.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "TitleText") text.text = "PLAY";
                if (text.name == "FooterHint") text.text = "Choose a game mode";
            }
            PrefabUtility.SaveAsPrefabAsset(play, PLAY);
        }
        finally { PrefabUtility.UnloadPrefabContents(play); }
        var main = PrefabUtility.LoadPrefabContents(MAIN);
        try
        {
            var view = main.GetComponent<MainMenuUi>();
            var so = new SerializedObject(view);
            var start = (Button)so.FindProperty("startDemoButton").objectReferenceValue;
            var options = (Button)so.FindProperty("optionsButton").objectReferenceValue;
            var quit = (Button)so.FindProperty("quitApplicationButton").objectReferenceValue;
            foreach (var button in main.GetComponentsInChildren<Button>(true))
                if (button.name == "SurvivalButton" || button.name == "SimulationButton") Object.DestroyImmediate(button.gameObject);
            Style(start, "PLAY", 120);
            Style(options, "SETTINGS", 20);
            Style(quit, "QUIT", -80);
            var trigger = new SerializedObject(start.GetComponent<TooltipTrigger>());
            trigger.FindProperty("key").stringValue = "Play";
            trigger.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(main, MAIN);
        }
        finally { PrefabUtility.UnloadPrefabContents(main); }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(PLAY),
            settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(MAIN)).parentGroup);
        entry.address = nameof(PlayUi);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return "Saved Main Menu -> Play -> Skirmish / Survival / Battle Simulation and registered PlayUi.";
    }
}


