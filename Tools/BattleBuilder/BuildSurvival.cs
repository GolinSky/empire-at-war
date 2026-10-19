using System;
using System.Linq;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Factions;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.UI;

public static class BuildSurvival
{
    public static string Run()
    {
        const string DATA = "Assets/Settings/Data/Battles/SurvivalWaveData.asset";
        const string HUD = "Assets/Prefabs/Ui/CoreGame/SurvivalHudUi.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Settings/Data/Battles"))
            AssetDatabase.CreateFolder("Assets/Settings/Data", "Battles");
        var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(
            AssetDatabase.FindAssets("t:FactionCatalog").Single()));
        var data = AssetDatabase.LoadAssetAtPath<SurvivalWaveData>(DATA);
        if (data == null) { data = ScriptableObject.CreateInstance<SurvivalWaveData>(); AssetDatabase.CreateAsset(data, DATA); }
        var serialized = new SerializedObject(data);
        var factions = serialized.FindProperty("factions");
        factions.arraySize = catalog.Factions.Count;
        for (int factionIndex = 0; factionIndex < factions.arraySize; factionIndex++)
        {
            var definition = catalog.Factions[factionIndex];
            var ships = definition.Ships.OrderBy(pair => pair.Value.Price).ToArray();
            var squadrons = definition.Squadrons.OrderBy(pair => pair.Value.Price).ToArray();
            var faction = factions.GetArrayElementAtIndex(factionIndex);
            faction.FindPropertyRelative("faction").intValue = (int)definition.FactionType;
            var waves = faction.FindPropertyRelative("waves"); waves.arraySize = 5;
            for (int waveIndex = 0; waveIndex < 5; waveIndex++)
            {
                var wave = waves.GetArrayElementAtIndex(waveIndex);
                wave.FindPropertyRelative("spawnGroupCount").intValue = Math.Min(4, 1 + waveIndex);
                var entries = wave.FindPropertyRelative("entries");
                entries.arraySize = (ships.Length > 0 ? 2 : 0) + (squadrons.Length > 0 ? 1 : 0);
                int index = 0;
                void Add(bool squadron, int id, int count)
                {
                    var entry = entries.GetArrayElementAtIndex(index++);
                    entry.FindPropertyRelative("isSquadron").boolValue = squadron;
                    entry.FindPropertyRelative("unitId").intValue = id;
                    entry.FindPropertyRelative("quantity").intValue = count;
                }
                if (ships.Length > 0)
                {
                    Add(false, (int)ships[Math.Min(ships.Length - 1, waveIndex * ships.Length / 5)].Key, 2 + waveIndex);
                    Add(false, (int)ships[Math.Min(ships.Length - 1, (waveIndex + 1) * ships.Length / 5)].Key, 1 + waveIndex / 2);
                }
                if (squadrons.Length > 0) Add(true, (int)squadrons[waveIndex % squadrons.Length].Key, 2 + waveIndex);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
        var donor = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab");
        var root = new GameObject("SurvivalHudUi", typeof(RectTransform), typeof(CanvasGroup), typeof(SurvivalHudUi));
        try
        {
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(440, 145); rect.anchoredPosition = new Vector2(0, -80);
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(root.transform, false);
            var panelRect = (RectTransform)panel.transform; panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color32(8, 12, 20, 240);
            var textDonor = donor.GetComponentsInChildren<TMP_Text>(true).First();
            TMP_Text Label(string name, string text, float y)
            {
                var label = UnityEngine.Object.Instantiate(textDonor, panel.transform); label.name = name;
                label.text = text; label.fontSize = 20; label.enableAutoSizing = true; label.fontSizeMin = 6; label.fontSizeMax = 22;
                label.color = new Color32(248, 250, 252, 255); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                var r = label.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1);
                r.anchoredPosition = new Vector2(0, y); r.sizeDelta = new Vector2(420, 30); return label;
            }
            var title = Label("Wave", "WAVE 1/5", -8);
            var status = Label("Status", "Prepare defenses · 120s", -40);
            var buttonDonor = new SerializedObject(donor.GetComponent<EmpireAtWar.Entities.MainMenu.Survival.SurvivalUi>())
                .FindProperty("startButton").objectReferenceValue as Button;
            var button = UnityEngine.Object.Instantiate(buttonDonor, panel.transform); button.name = "StartWaveButton";
            button.onClick = new Button.ButtonClickedEvent(); button.interactable = true;
            button.transition = Selectable.Transition.ColorTint; button.spriteState = default;
            var br = (RectTransform)button.transform; br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 1);
            br.anchoredPosition = new Vector2(0, -83); br.sizeDelta = new Vector2(250, 46);
            button.GetComponentInChildren<TMP_Text>(true).text = "START WAVE NOW";
            var view = new SerializedObject(root.GetComponent<SurvivalHudUi>());
            view.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            view.FindProperty("waveText").objectReferenceValue = title;
            view.FindProperty("statusText").objectReferenceValue = status;
            view.FindProperty("startWaveButton").objectReferenceValue = button;
            view.ApplyModifiedPropertiesWithoutUndo();
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Ui/CoreGame")) AssetDatabase.CreateFolder("Assets/Prefabs/Ui", "CoreGame");
            PrefabUtility.SaveAsPrefabAsset(root, HUD);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); PrefabUtility.UnloadPrefabContents(donor); }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID("Assets/Prefabs/Ui/MainMenu/SurvivalUi.prefab")).parentGroup;
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(HUD), group).address = nameof(SurvivalHudUi);
        var dataGroup = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(catalog))).parentGroup;
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(DATA), dataGroup).address = nameof(SurvivalWaveData);
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        return $"Saved {catalog.Factions.Count * 5} waves and Survival HUD.";
    }
}
