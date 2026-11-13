using System;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Popups;
using EmpireAtWar.Models.Players;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildMapArchetypeUi
{
    private const string SKIRMISH_PATH = "Assets/Prefabs/Ui/MainMenu/SkirmishUi.prefab";
    private const string GALLERY_PATH = "Assets/Prefabs/Ui/Popups/PlanetSelectionPopupUi.prefab";
    private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";

    public static string Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SKIRMISH_PATH);
        try
        {
            var fields = new SerializedObject(RootView<SkirmishUi>(root));
            SerializedProperty rows = fields.FindProperty("slotRows");
            if (rows.arraySize != MatchRules.MAX_PLAYERS)
            {
                var first = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(0).objectReferenceValue;
                Transform panel = first.transform.parent;
                RectTransform viewport = Rect(panel, "PlayerRowsViewport", new Vector2(32, -144), new Vector2(1180, 480));
                viewport.gameObject.AddComponent<RectMask2D>();
                var background = viewport.gameObject.AddComponent<Image>();
                background.color = Color.clear;
                RectTransform content = Rect(viewport, "Content", Vector2.zero, new Vector2(1180, 124 * MatchRules.MAX_PLAYERS - 16));
                ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.viewport = viewport;
                scroll.content = content;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 42;
                int originalCount = rows.arraySize;
                var donor = (SkirmishSlotRowView)rows.GetArrayElementAtIndex(originalCount - 1).objectReferenceValue;
                rows.arraySize = MatchRules.MAX_PLAYERS;
                for (int i = 0; i < rows.arraySize; i++)
                {
                    SkirmishSlotRowView row = i < originalCount
                        ? (SkirmishSlotRowView)rows.GetArrayElementAtIndex(i).objectReferenceValue
                        : UnityEngine.Object.Instantiate(donor, content);
                    row.name = $"SlotRow{i + 1}";
                    var rect = (RectTransform)row.transform;
                    rect.SetParent(content, false);
                    rect.anchoredPosition = new Vector2(0, -124 * i);
                    rows.GetArrayElementAtIndex(i).objectReferenceValue = row;
                    var rowFields = new SerializedObject(row);
                    ((TMP_Text)rowFields.FindProperty("slotIndexText").objectReferenceValue).text = (i + 1).ToString("00");
                }
                RectTransform track = Rect(panel, "PlayerRowsScrollbar", new Vector2(1220, -144), new Vector2(8, 480));
                track.gameObject.AddComponent<Image>().color = new Color32(32, 46, 64, 255);
                RectTransform handle = Rect(track, "Handle", Vector2.zero, Vector2.zero);
                Image handleImage = handle.gameObject.AddComponent<Image>();
                handleImage.color = new Color32(109, 139, 168, 255);
                Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
                scrollbar.handleRect = handle;
                scrollbar.targetGraphic = handleImage;
                scrollbar.direction = Scrollbar.Direction.BottomToTop;
                scroll.verticalScrollbar = scrollbar;
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, SKIRMISH_PATH);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        root = PrefabUtility.LoadPrefabContents(GALLERY_PATH);
        try
        {
            var fields = new SerializedObject(RootView<PlanetSelectionPopupUi>(root));
            fields.FindProperty("mapSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            SerializedProperty cards = fields.FindProperty("cards");
            for (int i = 0; i < cards.arraySize; i++)
            {
                var card = new SerializedObject(cards.GetArrayElementAtIndex(i).objectReferenceValue);
                var preview = (RawImage)card.FindProperty("previewImage").objectReferenceValue;
                preview.uvRect = new Rect(0f, 0f, 1f, 1f);
                preview.rectTransform.anchorMin = preview.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                preview.rectTransform.pivot = new Vector2(0.5f, 1f);
                preview.rectTransform.anchoredPosition = new Vector2(0f, -2f);
                preview.rectTransform.sizeDelta = new Vector2(164f, 164f);
                var label = (TMP_Text)card.FindProperty("nameText").objectReferenceValue;
                label.rectTransform.sizeDelta = new Vector2(label.rectTransform.sizeDelta.x, 62);
                label.enableAutoSizing = true;
                label.fontSizeMin = 6;
                label.fontSizeMax = 20;
            }
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, GALLERY_PATH);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        return "Saved eight scrolling skirmish rows and archetype gallery bindings.";
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static T RootView<T>(GameObject root) where T : Component
    {
        foreach (Component component in root.GetComponents(typeof(Component)))
            if (component is T view) return view;
        throw new InvalidOperationException($"{root.name} has no {typeof(T).Name}.");
    }
}
