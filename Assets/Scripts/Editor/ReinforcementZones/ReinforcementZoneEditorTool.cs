using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Views.ReinforcementZones;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmpireAtWar.Editor.ReinforcementZones
{
    public static class ReinforcementZoneEditorTool
    {
        private const float ZONE_RADIUS = 45f;
        private const string PREFAB_FOLDER = "Assets/Prefabs/View/ReinforcementZones";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/ReinforcementZone.prefab";
        private const string CORUSCANT_PREFAB_PATH = PREFAB_FOLDER + "/CoruscantReinforcementZones.prefab";
        private const string KAMINO_PREFAB_PATH = PREFAB_FOLDER + "/KaminoReinforcementZones.prefab";
        private const string MATERIAL_FOLDER = "Assets/Art/Materials/ReinforcementZones";
        private const string MATERIAL_PATH = MATERIAL_FOLDER + "/ReinforcementZone.mat";

        [MenuItem("Tools/Empire At War/Reinforcement Zones/Setup Active Map")]
        public static void SetupActiveMap()
        {
            Material material = GetOrCreateMaterial();
            GameObject prefab = GetOrCreatePrefab(material);
            Scene scene = SceneManager.GetActiveScene();
            GameObject zonesRoot = GetOrCreateZonesRoot(scene);

            RemoveExistingZones(zonesRoot.transform);
            List<ReinforcementZoneView> views = new List<ReinforcementZoneView>
            {
                CreateZone(prefab, zonesRoot.transform, "PlayerDefaultZone", new Vector3(-180f, 0f, 170f),
                    PlayerType.Player, false),
                CreateZone(prefab, zonesRoot.transform, "EnemyDefaultZone", new Vector3(160f, 0f, -170f),
                    PlayerType.Opponent, false),
                CreateZone(prefab, zonesRoot.transform, "CaptureZoneNorthWest", new Vector3(-65f, 0f, 45f),
                    PlayerType.None, true),
                CreateZone(prefab, zonesRoot.transform, "CaptureZoneSouthEast", new Vector3(55f, 0f, -55f),
                    PlayerType.None, true)
            };
            AssignZoneViews(zonesRoot.GetComponent<ReinforcementZonesSystem>(), views);

            string mapPrefabPath = GetMapPrefabPath(scene.name);
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                zonesRoot,
                mapPrefabPath,
                InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = zonesRoot;
            Debug.Log($"Reinforcement zones configured in {scene.path} with {mapPrefabPath}.");
        }

        private static GameObject GetOrCreateZonesRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "ReinforcementZones")
                {
                    if (root.GetComponent<ReinforcementZonesSystem>() == null)
                    {
                        root.AddComponent<ReinforcementZonesSystem>();
                    }

                    return root;
                }
            }

            GameObject zonesRoot = new GameObject("ReinforcementZones");
            SceneManager.MoveGameObjectToScene(zonesRoot, scene);
            zonesRoot.AddComponent<ReinforcementZonesSystem>();
            return zonesRoot;
        }

        private static void RemoveExistingZones(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponent<ReinforcementZoneView>() != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static ReinforcementZoneView CreateZone(
            GameObject prefab,
            Transform parent,
            string name,
            Vector3 position,
            PlayerType startingOwner,
            bool isCapturable)
        {
            GameObject zone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            zone.name = name;
            zone.transform.position = position;

            ReinforcementZoneView view = zone.GetComponent<ReinforcementZoneView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_startingOwner").enumValueIndex = (int)startingOwner - 1;
            serializedView.FindProperty("_isCapturable").boolValue = isCapturable;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void AssignZoneViews(
            ReinforcementZonesSystem system,
            IReadOnlyList<ReinforcementZoneView> views)
        {
            SerializedObject serializedSystem = new SerializedObject(system);
            SerializedProperty zoneViews = serializedSystem.FindProperty("_zoneViews");
            zoneViews.arraySize = views.Count;
            for (int i = 0; i < views.Count; i++)
            {
                zoneViews.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            }

            serializedSystem.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GetMapPrefabPath(string sceneName)
        {
            return sceneName switch
            {
                "Corusant" => CORUSCANT_PREFAB_PATH,
                "Coruscant" => CORUSCANT_PREFAB_PATH,
                "Kamino" => KAMINO_PREFAB_PATH,
                _ => throw new System.InvalidOperationException(
                    $"No reinforcement-zone prefab is configured for scene '{sceneName}'.")
            };
        }

        private static GameObject GetOrCreatePrefab(Material material)
        {
            EnsureFolder(PREFAB_FOLDER);
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            GameObject root = new GameObject("ReinforcementZone");
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "SphereRenderer";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localScale = new Vector3(ZONE_RADIUS * 2f, 0.2f, ZONE_RADIUS * 2f);
            Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());

            MeshRenderer sphereRenderer = sphere.GetComponent<MeshRenderer>();
            sphereRenderer.sharedMaterial = material;
            sphereRenderer.shadowCastingMode = ShadowCastingMode.Off;
            sphereRenderer.receiveShadows = false;

            Canvas canvas = CreateWorldCanvas(root.transform, out Image progress, out TMP_Text status);
            ReinforcementZoneView view = root.AddComponent<ReinforcementZoneView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("_radius").floatValue = ZONE_RADIUS;
            serializedView.FindProperty("_sphereRenderer").objectReferenceValue = sphereRenderer;
            serializedView.FindProperty("_captureCanvas").objectReferenceValue = canvas;
            serializedView.FindProperty("_captureProgress").objectReferenceValue = progress;
            serializedView.FindProperty("_statusText").objectReferenceValue = status;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Canvas CreateWorldCanvas(Transform parent, out Image progress, out TMP_Text status)
        {
            GameObject canvasObject = new GameObject("WorldUi", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            RectTransform canvasTransform = (RectTransform)canvasObject.transform;
            canvasTransform.sizeDelta = new Vector2(260f, 70f);
            canvasTransform.localPosition = new Vector3(0f, 7f, 0f);
            canvasTransform.localScale = Vector3.one * 0.08f;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;

            Image background = CreateImage("ProgressBackground", canvasTransform, new Color(0f, 0f, 0f, 0.75f));
            SetRect(background.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.42f));

            progress = CreateImage("ProgressFill", background.rectTransform, Color.white);
            SetRect(progress.rectTransform, Vector2.zero, Vector2.one);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillOrigin = 0;
            progress.fillAmount = 0f;

            GameObject textObject = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasTransform, false);
            status = textObject.GetComponent<TextMeshProUGUI>();
            SetRect(status.rectTransform, new Vector2(0f, 0.45f), Vector2.one);
            status.text = "NEUTRAL ZONE";
            status.fontSize = 28f;
            status.alignment = TextAlignmentOptions.Center;
            status.color = Color.white;

            return canvas;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Material GetOrCreateMaterial()
        {
            EnsureFolder(MATERIAL_FOLDER);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new MissingReferenceException("Universal Render Pipeline/Unlit shader was not found.");
            }

            material = new Material(shader)
            {
                name = "ReinforcementZone",
                renderQueue = (int)RenderQueue.Transparent
            };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", new Color(0.7f, 0.7f, 0.7f, 0.25f));
            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            return material;
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
