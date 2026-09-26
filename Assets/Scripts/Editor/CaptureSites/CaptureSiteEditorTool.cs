using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.CaptureSites;
using MPUIKIT;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmpireAtWar.Editor.CaptureSites
{
    /// <summary>Builds the capture-site prefab and places the hand-authored sites into each planet scene.</summary>
    public static class CaptureSiteEditorTool
    {
        private const float SITE_RADIUS = 40f;
        private const int UI_LAYER = 5;
        private const string PREFAB_FOLDER = "Assets/Prefabs/View/CaptureSites";
        private const string SITE_PREFAB_PATH = PREFAB_FOLDER + "/CaptureSite.prefab";
        private const string DATA_FOLDER = "Assets/Settings/Data/Models/CaptureSites";
        private const string DATA_PATH = DATA_FOLDER + "/CaptureSiteData.asset";
        private const string RING_MATERIAL_PATH = "Assets/Art/Materials/ReinforcementZones/ReinforcementZone.mat";
        private const string HOLOGRAM_MATERIAL_PATH = "Assets/Art/Materials/Hologram.mat";
        private static readonly Color BUTTON_COLOR = new Color32(0x25, 0x63, 0xEB, 0xFF);
        private static readonly Color BUTTON_OUTLINE_COLOR = new Color32(0x60, 0xA5, 0xFA, 0xFF);
        private static readonly Color TRACK_COLOR = new Color32(0x0F, 0x17, 0x2A, 0xD9);
        private static readonly Color TEXT_COLOR = new Color32(0xF8, 0xFA, 0xFC, 0xFF);

        private static readonly (string Scene, string Prefab)[] MAPS =
        {
            ("Assets/Scenes/Planets/Kamino/Kamino.unity", PREFAB_FOLDER + "/KaminoCaptureSites.prefab"),
            ("Assets/Scenes/Planets/Corusant/Corusant.unity", PREFAB_FOLDER + "/CoruscantCaptureSites.prefab"),
        };

        // Empty diagonal corners, roughly equidistant from both stations.
        private static readonly (string Name, Vector3 Position)[] SITES =
        {
            ("AsteroidSiteNorthEast", new Vector3(170f, 0f, 170f)),
            ("AsteroidSiteSouthWest", new Vector3(-170f, 0f, -170f)),
        };

        [MenuItem("Tools/Empire At War/Capture Sites/Build Sites And Place In Maps")]
        public static void Build()
        {
            AsteroidMiningFacilityAssetBuilder.Build();
            EnsureFolder(PREFAB_FOLDER);
            EnsureFolder(DATA_FOLDER);
            BuildData();
            GameObject sitePrefab = BuildSitePrefab();
            foreach ((string scenePath, string prefabPath) in MAPS)
            {
                PlaceInScene(scenePath, BuildMapPrefab(sitePrefab, prefabPath));
            }

            AssetDatabase.SaveAssets();
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
            entries.arraySize = 1;
            SerializedProperty mining = entries.GetArrayElementAtIndex(0);
            mining.FindPropertyRelative("key").enumValueIndex = (int)SiteFacilityType.Mining;
            SerializedProperty cost = mining.FindPropertyRelative("value");
            cost.FindPropertyRelative("<Name>k__BackingField").stringValue = "Asteroid Mine";
            cost.FindPropertyRelative("<Price>k__BackingField").floatValue = 600f;
            cost.FindPropertyRelative("<BuildTime>k__BackingField").floatValue = 25f;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AsteroidMiningFacilityAssetBuilder.AddAddressable(DATA_PATH, "Model");
        }

        private static GameObject BuildSitePrefab()
        {
            GameObject root = new GameObject("CaptureSite");
            AsteroidMiningFacilityAssetBuilder.InstantiateModel(root.transform, "AsteroidRocks", keepRocks: true);
            Transform framework = BuildFramework(root.transform);
            MeshRenderer ring = BuildRing(root.transform);
            Canvas canvas = BuildCanvas(root.transform, out Image progress, out TMP_Text status,
                out Button button, out TMP_Text buttonLabel);

            CaptureSiteView view = root.AddComponent<CaptureSiteView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("radius").floatValue = SITE_RADIUS;
            serializedView.FindProperty("ringRenderer").objectReferenceValue = ring;
            serializedView.FindProperty("constructionFramework").objectReferenceValue = framework;
            serializedView.FindProperty("statusCanvas").objectReferenceValue = canvas;
            serializedView.FindProperty("progressFill").objectReferenceValue = progress;
            serializedView.FindProperty("statusText").objectReferenceValue = status;
            serializedView.FindProperty("buildButton").objectReferenceValue = button;
            serializedView.FindProperty("buildLabel").objectReferenceValue = buttonLabel;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SITE_PREFAB_PATH);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform BuildFramework(Transform parent)
        {
            // Pivot at the machinery base so the scaffold rises from the rock as construction progresses.
            GameObject framework = new GameObject("ConstructionFramework");
            framework.transform.SetParent(parent, false);
            GameObject machinery = AsteroidMiningFacilityAssetBuilder.InstantiateModel(
                framework.transform, "Machinery", keepRocks: false);
            Renderer[] renderers = machinery.GetComponentsInChildren<Renderer>();
            float baseHeight = AsteroidMiningFacilityAssetBuilder.GetLocalBounds(parent, renderers).min.y;
            framework.transform.localPosition = new Vector3(0f, baseHeight, 0f);
            machinery.transform.localPosition = AsteroidMiningFacilityAssetBuilder.MODEL_OFFSET +
                new Vector3(0f, -baseHeight, 0f);

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

        private static GameObject BuildMapPrefab(GameObject sitePrefab, string prefabPath)
        {
            GameObject root = new GameObject("CaptureSites");
            CaptureSitesSystem system = root.AddComponent<CaptureSitesSystem>();
            SerializedObject serializedSystem = new SerializedObject(system);
            SerializedProperty siteViews = serializedSystem.FindProperty("siteViews");
            siteViews.arraySize = SITES.Length;
            for (int i = 0; i < SITES.Length; i++)
            {
                GameObject site = (GameObject)PrefabUtility.InstantiatePrefab(sitePrefab, root.transform);
                site.name = SITES[i].Name;
                site.transform.position = SITES[i].Position;
                siteViews.GetArrayElementAtIndex(i).objectReferenceValue = site.GetComponent<CaptureSiteView>();
            }

            serializedSystem.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void PlaceInScene(string scenePath, GameObject mapPrefab)
        {
            // Additive open leaves every other open scene, and its unsaved state, untouched.
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            foreach (GameObject existing in scene.GetRootGameObjects())
            {
                if (existing.GetComponent<CaptureSitesSystem>() != null)
                {
                    Object.DestroyImmediate(existing);
                }
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, scene);
            instance.name = "CaptureSites";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
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
