using EmpireAtWar.Entities.ControlGroups;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Editor.ControlGroups
{
    public static class ControlGroupUiBuilder
    {
        public const string PREFAB_PATH = "Assets/Prefabs/Ui/ControlGroups/ControlGroupUi.prefab";
        private const string FONT_PATH = "Assets/Art/Fonts/CoreGameHud/Rajdhani-SemiBold SDF.asset";
        private const int SLOT_COUNT = 10;
        private const float SLOT_WIDTH = 64f;
        private const float SLOT_STEP = 68f;

        [MenuItem("Tools/UI/Build Squad Preview")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Ui/ControlGroups"))
                AssetDatabase.CreateFolder("Assets/Prefabs/Ui", "ControlGroups");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            var root = new GameObject("ControlGroupUi", typeof(RectTransform));
            try
            {
                RectTransform rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -48f);
                rect.sizeDelta = new Vector2(SLOT_STEP * SLOT_COUNT - 4f, 68f);
                CanvasGroup canvas = root.AddComponent<CanvasGroup>();
                canvas.interactable = false;
                canvas.blocksRaycasts = false;
                ControlGroupUi view = root.AddComponent<ControlGroupUi>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("canvasGroup").objectReferenceValue = canvas;
                SerializedProperty slots = serialized.FindProperty("slots");
                slots.arraySize = SLOT_COUNT;
                for (int index = 0; index < SLOT_COUNT; index++)
                    slots.GetArrayElementAtIndex(index).objectReferenceValue = CreateSlot(rect, font, index);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static ControlGroupSlotView CreateSlot(RectTransform parent, TMP_FontAsset font, int index)
        {
            int digit = (index + 1) % SLOT_COUNT;
            RectTransform rect = Rect("Slot" + digit, parent, new Vector2(index * SLOT_STEP, 0),
                new Vector2(SLOT_WIDTH, 68));
            ControlGroupSlotView slot = rect.gameObject.AddComponent<ControlGroupSlotView>();
            Image background = Line("Background", rect, Vector2.zero, rect.sizeDelta);
            background.color = new Color32(32, 46, 64, 255);
            Label("Number", rect, font, digit.ToString(), new Vector2(5, 46), new Vector2(20, 20), 17);
            TMP_Text count = Label("Count", rect, font, string.Empty, new Vector2(5, 2), new Vector2(54, 17), 13);
            count.alignment = TextAlignmentOptions.Right;
            TMP_Text empty = Label("Empty", rect, font, "+", new Vector2(12, 19), new Vector2(40, 25), 22);
            empty.alignment = TextAlignmentOptions.Center;
            Image first = Icon("FirstIcon", rect, new Vector2(3, 19));
            Image second = Icon("SecondIcon", rect, new Vector2(33, 19));
            Image selection = Line("Selected", rect, new Vector2(28, 63), new Vector2(8, 2));
            selection.color = new Color32(210, 169, 94, 255);
            selection.gameObject.SetActive(false);
            Graphic[] outline =
            {
                Line("Bottom", rect, Vector2.zero, new Vector2(SLOT_WIDTH, 1)),
                Line("Top", rect, new Vector2(0, 67), new Vector2(SLOT_WIDTH, 1)),
                Line("Left", rect, Vector2.zero, new Vector2(1, 68)),
                Line("Right", rect, new Vector2(SLOT_WIDTH - 1, 0), new Vector2(1, 68))
            };
            RectTransform emptyOutline = Rect("EmptyOutline", rect, Vector2.zero, new Vector2(SLOT_WIDTH, 68));
            for (int edge = 0; edge < 4; edge++)
            {
                Line("Bottom" + edge, emptyOutline, new Vector2(edge * (SLOT_WIDTH - 6) / 3, 0), new Vector2(6, 1));
                Line("Top" + edge, emptyOutline, new Vector2(edge * (SLOT_WIDTH - 6) / 3, 67), new Vector2(6, 1));
                Line("Left" + edge, emptyOutline, new Vector2(0, 8 + edge * 16), new Vector2(1, 6));
                Line("Right" + edge, emptyOutline, new Vector2(SLOT_WIDTH - 1, 8 + edge * 16), new Vector2(1, 6));
            }
            foreach (Graphic border in outline) border.enabled = false;
            var serialized = new SerializedObject(slot);
            serialized.FindProperty("countText").objectReferenceValue = count;
            serialized.FindProperty("emptyMark").objectReferenceValue = empty.gameObject;
            serialized.FindProperty("firstIcon").objectReferenceValue = first;
            serialized.FindProperty("secondIcon").objectReferenceValue = second;
            serialized.FindProperty("selectionMark").objectReferenceValue = selection.gameObject;
            serialized.FindProperty("emptyOutline").objectReferenceValue = emptyOutline.gameObject;
            SerializedProperty borders = serialized.FindProperty("outline");
            borders.arraySize = outline.Length;
            for (int i = 0; i < outline.Length; i++) borders.GetArrayElementAtIndex(i).objectReferenceValue = outline[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string text,
            Vector2 position, Vector2 size, float fontSize)
        {
            RectTransform rect = Rect(name, parent, position, size);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = fontSize;
            label.text = text;
            label.color = new Color32(242, 240, 230, 255);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.85f);
            shadow.effectDistance = new Vector2(1, -1);
            return label;
        }

        private static Image Icon(string name, Transform parent, Vector2 position)
        {
            RectTransform rect = Rect(name, parent, position, new Vector2(28, 28));
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static Image Line(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image line = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            line.color = new Color32(109, 139, 168, 255);
            line.raycastTarget = false;
            return line;
        }
    }
}
