using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.ShipUi;
using EmpireAtWar.Views.MiniMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Editor
{
    public static class SpaceHazardUiBuilder
    {
        private const string ICON_FOLDER = "Assets/Art/Textures/Ui/Icons/MiniMap/";

        public static void Build()
        {
            Sprite nebula = ImportIcon("NebulaWhite.png");
            Sprite ion = ImportIcon("IonStormWhite.png");
            foreach (string guid in AssetDatabase.FindAssets("t:SpaceHazardData", new[] { "Assets/Settings" }))
            {
                var data = AssetDatabase.LoadAssetAtPath<SpaceHazardData>(AssetDatabase.GUIDToAssetPath(guid));
                bool isIon = data.HazardType == HazardType.IonStorm;
                var preset = new SerializedObject(data);
                preset.FindProperty("<DisplayName>k__BackingField").stringValue = isIon ? "Ion Storm" : "Nebula";
                preset.FindProperty("<MinimapColor>k__BackingField").colorValue = isIon ? new Color(0.28f, 0.8f, 0.95f) : new Color(0.72f, 0.38f, 0.87f);
                preset.FindProperty("<MinimapIcon>k__BackingField").objectReferenceValue = isIon ? ion : nebula;
                preset.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }
            BuildMiniMap();
            BuildShipStatus("Assets/Prefabs/Ui/Ship/ShipUi.prefab");
            BuildShipStatus("Assets/Prefabs/Ui/Ship/ShipGroupEntryUi.prefab");
            AssetDatabase.SaveAssets();
        }

        private static Sprite ImportIcon(string name)
        {
            string path = ICON_FOLDER + name;
            var reference = (TextureImporter)AssetImporter.GetAtPath(ICON_FOLDER + "AsteroidWhite.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            reference.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);
            importer.SetPlatformTextureSettings(reference.GetDefaultPlatformTextureSettings());
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BuildMiniMap()
        {
            const string path = "Assets/Prefabs/Ui/MiniMap/MiniMapUi.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var serialized = new SerializedObject(root.GetComponent<MiniMapUi>());
                var obstacles = (MiniMapObstacleView)serialized.FindProperty("obstacleView").objectReferenceValue;
                var hazard = (MiniMapHazardView)serialized.FindProperty("hazardView").objectReferenceValue;
                if (hazard == null)
                {
                    var child = new GameObject("Hazards", typeof(RectTransform), typeof(CanvasRenderer));
                    child.layer = obstacles.gameObject.layer;
                    hazard = child.AddComponent<MiniMapHazardView>();
                    hazard.rectTransform.SetParent(obstacles.transform.parent, false);
                    hazard.rectTransform.anchorMin = Vector2.zero;
                    hazard.rectTransform.anchorMax = Vector2.one;
                    hazard.rectTransform.sizeDelta = Vector2.zero;
                    hazard.transform.SetSiblingIndex(obstacles.transform.GetSiblingIndex());
                }
                hazard.raycastTarget = false;
                serialized.FindProperty("hazardView").objectReferenceValue = hazard;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void BuildShipStatus(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var serialized = new SerializedObject(root.GetComponent<ShipUi>());
                var label = (TMP_Text)serialized.FindProperty("hazardStatus").objectReferenceValue;
                if (label == null)
                {
                    var child = new GameObject("HazardStatus", typeof(RectTransform));
                    child.layer = root.layer;
                    label = child.AddComponent<TextMeshProUGUI>();
                    label.rectTransform.SetParent(root.transform, false);
                }
                label.rectTransform.anchorMin = new Vector2(0f, 1f);
                label.rectTransform.anchorMax = new Vector2(0f, 1f);
                label.rectTransform.pivot = new Vector2(0f, 1f);
                label.rectTransform.anchoredPosition = new Vector2(4f, -108f);
                label.rectTransform.sizeDelta = new Vector2(72f, 20f);
                label.font = TMP_Settings.defaultFontAsset;
                label.fontSize = 10f;
                label.enableAutoSizing = true;
                label.fontSizeMin = 6f;
                label.fontSizeMax = 10f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(0.56f, 0.82f, 1f);
                label.raycastTarget = false;
                label.text = "";
                serialized.FindProperty("hazardStatus").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
