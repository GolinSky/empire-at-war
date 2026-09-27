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
        private static readonly Color BUTTON_COLOR = new Color32(0x25, 0x63, 0xEB, 0xFF);
        private static readonly Color BUTTON_OUTLINE_COLOR = new Color32(0x60, 0xA5, 0xFA, 0xFF);
        private static readonly Color TRACK_COLOR = new Color32(0x0F, 0x17, 0x2A, 0xD9);
        private static readonly Color TEXT_COLOR = new Color32(0xF8, 0xFA, 0xFC, 0xFF);

        [MenuItem("Tools/Empire At War/Capture Sites/Build Sites")]
        public static void Build()
        {
            AsteroidMiningFacilityAssetBuilder.Build();
            BattleAsteroidAssetBuilder.Build();
            EnsureFolder(PREFAB_FOLDER);
            EnsureFolder(DATA_FOLDER);
            BuildData();
            AddMiniMapIcon();
            BuildSitePrefab(SiteFacilityType.Mining, SITE_PREFAB_PATH,
                parent => AsteroidMiningFacilityAssetBuilder.InstantiateModel(parent, "Machinery", keepRocks: false));
            BuildSitePrefab(SiteFacilityType.BattleAsteroid, BATTLE_SITE_PREFAB_PATH,
                parent => BattleAsteroidAssetBuilder.InstantiateCannons(parent, "Cannons"));

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

        private static GameObject BuildSitePrefab(SiteFacilityType facilityType, string prefabPath,
            System.Func<Transform, GameObject> instantiateFacilityModel)
        {
            GameObject root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
            GameObject rocks = AsteroidMiningFacilityAssetBuilder.InstantiateModel(
                root.transform, "AsteroidRocks", keepRocks: true);
            Transform framework = BuildFramework(root.transform, instantiateFacilityModel);
            MeshRenderer ring = BuildRing(root.transform);
            Canvas canvas = BuildCanvas(root.transform, out Image progress, out TMP_Text status,
                out Button button, out TMP_Text buttonLabel);

            CaptureSiteView view = root.AddComponent<CaptureSiteView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("facilityType").enumValueIndex = (int)facilityType;
            serializedView.FindProperty("radius").floatValue = SITE_RADIUS;
            serializedView.FindProperty("ringRenderer").objectReferenceValue = ring;
            serializedView.FindProperty("constructionFramework").objectReferenceValue = framework;
            serializedView.FindProperty("statusCanvas").objectReferenceValue = canvas;
            serializedView.FindProperty("progressFill").objectReferenceValue = progress;
            serializedView.FindProperty("statusText").objectReferenceValue = status;
            serializedView.FindProperty("buildButton").objectReferenceValue = button;
            serializedView.FindProperty("buildLabel").objectReferenceValue = buttonLabel;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            AddRockObstacles(view, rocks.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform BuildFramework(Transform parent, System.Func<Transform, GameObject> instantiateFacilityModel)
        {
            // Pivot at the facility base so the scaffold rises from the rock as construction progresses.
            GameObject framework = new GameObject("ConstructionFramework");
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
            out Button button, out TMP_Text buttonLabel)
        {
            GameObject canvasObject = new GameObject("StatusCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = UI_LAYER;
            RectTransform canvasTransform = (RectTransform)canvasObject.transform;
            canvasTransform.SetParent(parent, false);
            canvasTransform.sizeDelta = new Vector2(320f, 150f);
            canvasTransform.localPosition = new Vector3(0f, 16f, 0f);
            canvasTransform.localScale = Vector3.one * 0.08f;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

            status = CreateText("Status", canvasTransform, new Vector2(0f, 0.72f), Vector2.one, 30f);
            status.text = "NEUTRAL SITE";

            MPImage track = CreateImage("ProgressTrack", canvasTransform, TRACK_COLOR, 1f);
            SetRect(track.rectTransform, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.68f));
            progress = CreateImage("ProgressFill", track.rectTransform, TEXT_COLOR, 1f);
            SetRect(progress.rectTransform, Vector2.zero, Vector2.one);

            MPImage buttonImage = CreateImage("BuildButton", canvasTransform, BUTTON_COLOR, 10f);
            SetRect(buttonImage.rectTransform, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.44f));
            buttonImage.raycastTarget = true;
            buttonImage.OutlineWidth = 1.5f;
            buttonImage.OutlineColor = BUTTON_OUTLINE_COLOR;
            button = buttonImage.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = buttonImage;
            buttonLabel = CreateText("Label", buttonImage.rectTransform, Vector2.zero, Vector2.one, 22f);
            buttonLabel.text = "BUILD";
            buttonImage.gameObject.SetActive(false);

            canvasObject.SetActive(false);
            return canvas;
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
            float fontSize)
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
            text.color = TEXT_COLOR;
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
