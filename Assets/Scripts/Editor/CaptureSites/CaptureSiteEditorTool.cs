using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.MiniMap;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace EmpireAtWar.Editor.CaptureSites
{
    /// <summary>Builds the capture-site prefabs and data; maps spawn the sites at generated positions.</summary>
    public static class CaptureSiteEditorTool
    {
        private const float SITE_RADIUS = 40f;
        private const float SITE_SCALE = 5f;
        private const int UI_LAYER = 5;
        private const int OBSTACLE_LAYER = 9;
        private const string MINI_MAP_DATA_PATH = "Assets/Settings/Data/Models/MiniMap/MiniMapData.asset";
        private const string PREFAB_FOLDER = "Assets/Prefabs/View/CaptureSites";
        private const string SITE_PREFAB_PATH = PREFAB_FOLDER + "/CaptureSite.prefab";
        private const string BATTLE_SITE_PREFAB_PATH = PREFAB_FOLDER + "/BattleAsteroidCaptureSite.prefab";
        private const string DATA_FOLDER = "Assets/Settings/Data/Models/CaptureSites";
        private const string DATA_PATH = DATA_FOLDER + "/CaptureSiteData.asset";
        private const string RING_MATERIAL_PATH = "Assets/Art/Materials/ReinforcementZones/ReinforcementZone.mat";
        private const string HOLOGRAM_MATERIAL_PATH = "Assets/Art/Materials/Hologram.mat";
        private static readonly Color PANEL_COLOR = new Color32(0x08, 0x0C, 0x14, 0xF5);
        private static readonly Color PANEL_OUTLINE_COLOR = new Color32(0x1F, 0x87, 0xE6, 0xD9);
        private static readonly Color CARD_COLOR = new Color32(0x0F, 0x17, 0x2A, 0xFF);
        private static readonly Color CARD_OUTLINE_COLOR = new Color32(0x60, 0xA5, 0xFA, 0xFF);
        private static readonly Color CARD_DISABLED_COLOR = new Color32(0x80, 0x80, 0x80, 0x80);
        private static readonly Color TRACK_COLOR = new Color32(0x18, 0x22, 0x32, 0xF2);
        private static readonly Color TEXT_COLOR = new Color32(0xF8, 0xFA, 0xFC, 0xFF);
        private static readonly Color LABEL_COLOR = new Color32(0x94, 0xA3, 0xB8, 0xFF);
        private static readonly Color VALUE_COLOR = new Color32(0x38, 0xBD, 0xF8, 0xFF);

        [MenuItem("Tools/Empire At War/Capture Sites/Build Sites")]
        public static void Build()
        {
            AsteroidMiningFacilityAssetBuilder.Build();
            BattleAsteroidAssetBuilder.Build();
            EnsureFolder(PREFAB_FOLDER);
            EnsureFolder(DATA_FOLDER);
            BuildData();
            AddMiniMapIcon();
            BuildSitePrefabs();
        }

        /// <summary>
        /// Rebuilds only the site prefabs. Both map slots share one layout: the owner picks the facility after capture.
        /// </summary>
        [MenuItem("Tools/Empire At War/Capture Sites/Rebuild Site Prefabs")]
        public static void BuildSitePrefabs()
        {
            EnsureFolder(PREFAB_FOLDER);
            BuildSitePrefab(SITE_PREFAB_PATH);
            BuildSitePrefab(BATTLE_SITE_PREFAB_PATH);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Applies rock obstacles and the minimap icon to the existing site assets without rebuilding them.</summary>
        [MenuItem("Tools/Empire At War/Capture Sites/Apply Obstacles And Mini Map Icon")]
        public static void ApplyObstaclesAndMiniMapIcon()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SITE_PREFAB_PATH);
            AddRockObstacles(root.GetComponent<CaptureSiteView>(), root.transform.Find("AsteroidRocks"));
            PrefabUtility.SaveAsPrefabAsset(root, SITE_PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
            AddMiniMapIcon();
            AssetDatabase.SaveAssets();
        }

        private static void AddRockObstacles(CaptureSiteView site, Transform rocks)
        {
            // Registered MapObstacles steer ship navigation and draw on the minimap.
            SerializedObject serializedSite = new SerializedObject(site);
            SerializedProperty siteObstacles = serializedSite.FindProperty("rockObstacles");
            siteObstacles.arraySize = 0;
            foreach (Transform rock in rocks)
            {
                MapObstacle obstacle = rock.GetComponent<MapObstacle>();
                if (obstacle == null)
                {
                    rock.gameObject.layer = OBSTACLE_LAYER;
                    MeshCollider collider = rock.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = rock.GetComponent<MeshFilter>().sharedMesh;
                    obstacle = rock.gameObject.AddComponent<MapObstacle>();
                    SerializedObject serializedObstacle = new SerializedObject(obstacle);
                    serializedObstacle.FindProperty("_obstacleCollider").objectReferenceValue = collider;
                    serializedObstacle.ApplyModifiedPropertiesWithoutUndo();
                }

                siteObstacles.arraySize++;
                siteObstacles.GetArrayElementAtIndex(siteObstacles.arraySize - 1).objectReferenceValue = obstacle;
            }

            serializedSite.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddMiniMapIcon()
        {
            // Sites reuse the mining facility icon, tinted by owner like other markers.
            ScriptableObject data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(MINI_MAP_DATA_PATH);
            SerializedObject serializedData = new SerializedObject(data);
            SerializedProperty entries = serializedData.FindProperty("<MarkWrapper>k__BackingField")
                .FindPropertyRelative("keyValue");
            Object icon = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                int key = entry.FindPropertyRelative("key").intValue;
                if (key == (int)MarkType.CaptureSite)
                {
                    return;
                }

                if (key == (int)MarkType.MiningFacility)
                {
                    icon = entry.FindPropertyRelative("value").objectReferenceValue;
                }
            }

            entries.arraySize++;
            SerializedProperty siteEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            siteEntry.FindPropertyRelative("key").intValue = (int)MarkType.CaptureSite;
            siteEntry.FindPropertyRelative("value").objectReferenceValue = icon;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void BuildData()
        {
            CaptureSiteData data = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(DATA_PATH);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CaptureSiteData>();
                AssetDatabase.CreateAsset(data, DATA_PATH);
            }

            SerializedObject serializedData = new SerializedObject(data);
            SerializedProperty entries = serializedData.FindProperty("facilityCosts").FindPropertyRelative("keyValue");
            entries.arraySize = 2;
            SetCost(entries.GetArrayElementAtIndex(0), SiteFacilityType.Mining, "Asteroid Mine", 600f, 25f);
            SetCost(entries.GetArrayElementAtIndex(1), SiteFacilityType.BattleAsteroid, "Battle Asteroid", 800f, 30f);
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AsteroidMiningFacilityAssetBuilder.AddAddressable(DATA_PATH, "Model");
        }

        private static void SetCost(SerializedProperty entry, SiteFacilityType facilityType, string name, float price,
            float buildTime)
        {
            entry.FindPropertyRelative("key").enumValueIndex = (int)facilityType;
            SerializedProperty cost = entry.FindPropertyRelative("value");
            cost.FindPropertyRelative("<Name>k__BackingField").stringValue = name;
            cost.FindPropertyRelative("<Price>k__BackingField").floatValue = price;
            cost.FindPropertyRelative("<BuildTime>k__BackingField").floatValue = buildTime;
        }

        private static GameObject BuildSitePrefab(string prefabPath)
        {
            GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            root.transform.localScale = Vector3.one * SITE_SCALE;
            GameObject rocks = AsteroidMiningFacilityAssetBuilder.InstantiateModel(
                root.transform, "AsteroidRocks", keepRocks: true);
            Transform miningFramework = BuildFramework(root.transform, "MiningFramework",
                parent => AsteroidMiningFacilityAssetBuilder.InstantiateModel(parent, "Machinery", keepRocks: false));
            Transform battleFramework = BuildFramework(root.transform, "BattleAsteroidFramework",
                parent => BattleAsteroidAssetBuilder.InstantiateCannons(parent, "Cannons"));
            MeshRenderer ring = BuildRing(root.transform);
            Canvas canvas = BuildCanvas(root.transform, out Image progress, out TMP_Text status,
                out GameObject buildOptions, out SiteFacilityOptionView[] options);

            CaptureSiteView view = root.AddComponent<CaptureSiteView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("radius").floatValue = SITE_RADIUS;
            serializedView.FindProperty("ringRenderer").objectReferenceValue = ring;
            SerializedProperty frameworks = serializedView.FindProperty("constructionFrameworks")
                .FindPropertyRelative("keyValue");
            frameworks.arraySize = 2;
            SetFramework(frameworks.GetArrayElementAtIndex(0), SiteFacilityType.Mining, miningFramework);
            SetFramework(frameworks.GetArrayElementAtIndex(1), SiteFacilityType.BattleAsteroid, battleFramework);
            serializedView.FindProperty("statusCanvas").objectReferenceValue = canvas;
            serializedView.FindProperty("progressFill").objectReferenceValue = progress;
            serializedView.FindProperty("statusText").objectReferenceValue = status;
            serializedView.FindProperty("buildOptions").objectReferenceValue = buildOptions;
            SerializedProperty optionsProperty = serializedView.FindProperty("facilityOptions");
            optionsProperty.arraySize = options.Length;
            for (int i = 0; i < options.Length; i++)
            {
                optionsProperty.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
            }

            serializedView.ApplyModifiedPropertiesWithoutUndo();
            AddRockObstacles(view, rocks.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void SetFramework(SerializedProperty entry, SiteFacilityType facilityType, Transform framework)
        {
            entry.FindPropertyRelative("key").enumValueIndex = (int)facilityType;
            entry.FindPropertyRelative("value").objectReferenceValue = framework;
        }

        private static Transform BuildFramework(Transform parent, string name,
            System.Func<Transform, GameObject> instantiateFacilityModel)
        {
            // Pivot at the facility base so the scaffold rises from the rock as construction progresses.
            GameObject framework = new GameObject(name);
            framework.transform.SetParent(parent, false);
            GameObject facilityModel = instantiateFacilityModel(framework.transform);
            Renderer[] renderers = facilityModel.GetComponentsInChildren<Renderer>();
            float baseHeight = AsteroidMiningFacilityAssetBuilder.GetLocalBounds(parent, renderers).min.y;
            framework.transform.localPosition = new Vector3(0f, baseHeight, 0f);
            facilityModel.transform.localPosition += new Vector3(0f, -baseHeight, 0f);

            Material hologram = AssetDatabase.LoadAssetAtPath<Material>(HOLOGRAM_MATERIAL_PATH);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = hologram;
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            framework.SetActive(false);
            return framework.transform;
        }

        private static MeshRenderer BuildRing(Transform parent)
        {
            GameObject ring = new GameObject("CaptureRing");
            ring.transform.SetParent(parent, false);
            ring.transform.localPosition = new Vector3(0f, -2f, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(SITE_RADIUS * 2f, SITE_RADIUS * 2f, 1f);
            ring.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(RING_MATERIAL_PATH);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Canvas BuildCanvas(Transform parent, out Image progress, out TMP_Text status,
            out GameObject buildOptions, out SiteFacilityOptionView[] options)
        {
            GameObject canvasObject = new GameObject("StatusCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = UI_LAYER;
            RectTransform canvasTransform = (RectTransform)canvasObject.transform;
            canvasTransform.SetParent(parent, false);
            // Bottom pivot: the view scales the panel to a constant screen size, growing up from above the rock.
            canvasTransform.pivot = new Vector2(0.5f, 0f);
            canvasTransform.sizeDelta = new Vector2(560f, 330f);
            canvasTransform.localPosition = new Vector3(0f, 16f, 0f);
            canvasTransform.localScale = Vector3.one * (0.08f / SITE_SCALE);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

            MPImage statusPanel = CreatePanel("StatusPanel", canvasTransform, PANEL_COLOR, PANEL_OUTLINE_COLOR, 16f);
            SetRect(statusPanel.rectTransform, new Vector2(0f, 0.72f), Vector2.one);
            status = CreateText("Status", statusPanel.rectTransform, new Vector2(0.04f, 0.36f),
                new Vector2(0.96f, 0.96f), 40f, TEXT_COLOR);
            status.text = "NEUTRAL SITE";

            MPImage track = CreateImage("ProgressTrack", statusPanel.rectTransform, TRACK_COLOR, 1f);
            SetRect(track.rectTransform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.3f));
            progress = CreateImage("ProgressFill", track.rectTransform, TEXT_COLOR, 1f);
            SetRect(progress.rectTransform, Vector2.zero, Vector2.one);

            MPImage optionsPanel = CreatePanel("BuildOptions", canvasTransform, PANEL_COLOR, PANEL_OUTLINE_COLOR, 16f);
            SetRect(optionsPanel.rectTransform, Vector2.zero, new Vector2(1f, 0.69f));
            options = new[]
            {
                BuildOptionCard(optionsPanel.rectTransform, SiteFacilityType.Mining, "+ CREDIT INCOME",
                    new Vector2(0.03f, 0.05f), new Vector2(0.485f, 0.95f)),
                BuildOptionCard(optionsPanel.rectTransform, SiteFacilityType.BattleAsteroid, "DEFENSE PLATFORM",
                    new Vector2(0.515f, 0.05f), new Vector2(0.97f, 0.95f)),
            };
            buildOptions = optionsPanel.gameObject;
            buildOptions.SetActive(false);

            canvasObject.SetActive(false);
            return canvas;
        }

        private static SiteFacilityOptionView BuildOptionCard(Transform parent, SiteFacilityType facilityType,
            string role, Vector2 anchorMin, Vector2 anchorMax)
        {
            MPImage card = CreatePanel($"{facilityType}Option", parent, CARD_COLOR, CARD_OUTLINE_COLOR, 10f);
            SetRect(card.rectTransform, anchorMin, anchorMax);
            card.raycastTarget = true;
            Button button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = card;
            ColorBlock colors = button.colors;
            colors.disabledColor = CARD_DISABLED_COLOR;
            button.colors = colors;

            TMP_Text nameText = CreateText("Name", card.rectTransform, new Vector2(0.06f, 0.6f),
                new Vector2(0.94f, 0.94f), 34f, TEXT_COLOR);
            TMP_Text roleText = CreateText("Role", card.rectTransform, new Vector2(0.06f, 0.38f),
                new Vector2(0.94f, 0.58f), 24f, LABEL_COLOR);
            roleText.text = role;
            TMP_Text costText = CreateText("Cost", card.rectTransform, new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.36f), 30f, VALUE_COLOR);

            SiteFacilityOptionView option = card.gameObject.AddComponent<SiteFacilityOptionView>();
            SerializedObject serializedOption = new SerializedObject(option);
            serializedOption.FindProperty("facilityType").enumValueIndex = (int)facilityType;
            serializedOption.FindProperty("button").objectReferenceValue = button;
            serializedOption.FindProperty("nameText").objectReferenceValue = nameText;
            serializedOption.FindProperty("costText").objectReferenceValue = costText;
            serializedOption.ApplyModifiedPropertiesWithoutUndo();
            return option;
        }

        private static MPImage CreatePanel(string name, Transform parent, Color color, Color outlineColor,
            float cornerRadius)
        {
            MPImage panel = CreateImage(name, parent, color, cornerRadius);
            panel.OutlineWidth = 1.5f;
            panel.OutlineColor = outlineColor;
            return panel;
        }

        private static MPImage CreateImage(string name, Transform parent, Color color, float cornerRadius)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.layer = UI_LAYER;
            imageObject.transform.SetParent(parent, false);
            MPImage image = imageObject.AddComponent<MPImage>();
            image.raycastTarget = false;
            image.color = color;
            image.DrawShape = DrawShape.Rectangle;
            image.FalloffDistance = 1f;
            Rectangle rectangle = image.Rectangle;
            rectangle.CornerRadius = Vector4.one * cornerRadius;
            image.Rectangle = rectangle;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = UI_LAYER;
            textObject.transform.SetParent(parent, false);
            TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
            SetRect(text.rectTransform, anchorMin, anchorMax);
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 6f;
            text.fontSizeMax = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
