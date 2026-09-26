using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.MiningFacility;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Editor.CaptureSites
{
    /// <summary>Builds the asteroid mine entity assets from the Space Mining Facility model.</summary>
    public static class AsteroidMiningFacilityAssetBuilder
    {
        public const string MODEL_PATH =
            "Assets/Art/Models/Station/space-station-asteroid-mining-facility/Space Mining Facility.dae";
        // Recentres the model's XZ footprint on the site pivot; shared by the site's rocks and scaffold.
        public static readonly Vector3 MODEL_OFFSET = new Vector3(-7f, 0f, -3.5f);

        private const string SOURCE_VIEW_PATH = "Assets/Prefabs/Models/MiningFacilities/MiningFacilityView.prefab";
        private const string VIEW_PATH = "Assets/Prefabs/Models/MiningFacilities/AsteroidMiningFacilityView.prefab";
        private const string INSTALLER_PATH = "Assets/Prefabs/View/AsteroidMiningFacilityInstaller.prefab";
        private const string SOURCE_DATA_PATH = "Assets/Settings/Data/Models/MiningFacilities/MiningFacilityData.asset";
        private const string DATA_PATH = "Assets/Settings/Data/Models/MiningFacilities/AsteroidMiningFacilityData.asset";
        private const string SHIELD_MESH_PATH = "Assets/Art/Models/ShieldSurface.asset";
        private const string SHIELD_MATERIAL_PATH = "Assets/Art/Materials/ShipShield.mat";
        private const float SHIELD_VISIBILITY_RADIUS = 6f;
        private const float SHIELD_WAVE_SPEED = 10f;
        private const float SHIELD_WAVE_WIDTH = 1.2f;
        private const float SHIELD_DISPLACEMENT = 0.25f;
        private const float SELECTION_SCALE = 2.1f;
        private const float INCOME = 20f;
        private const float HULL = 2500f;
        private const float SHIELDS = 1200f;

        [MenuItem("Tools/Empire At War/Capture Sites/Build Asteroid Mine Assets")]
        public static void Build()
        {
            BuildView();
            BuildInstaller();
            BuildData();
            AddAddressable(VIEW_PATH, "View");
            AddAddressable(INSTALLER_PATH, "Installers");
            AddAddressable(DATA_PATH, "Model");
            AssetDatabase.SaveAssets();
        }

        public static bool IsAsteroidRock(string partName)
        {
            return partName.StartsWith("Sphere");
        }

        public static void AddAddressable(string assetPath, string groupName)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(
                AssetDatabase.AssetPathToGUID(assetPath), settings.FindGroup(groupName));
            entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
        }

        private static void BuildView()
        {
            AssetDatabase.DeleteAsset(VIEW_PATH);
            AssetDatabase.CopyAsset(SOURCE_VIEW_PATH, VIEW_PATH);
            GameObject root = PrefabUtility.LoadPrefabContents(VIEW_PATH);
            root.name = "AsteroidMiningFacilityView";
            root.transform.localPosition = Vector3.zero;

            // The source's always-visible sphere is replaced by a ship-style, impact-only shield.
            Object.DestroyImmediate(root.transform.Find("Sphere").gameObject);

            GameObject model = InstantiateModel(root.transform, "Machinery", keepRocks: false);
            Renderer[] machinery = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = GetLocalBounds(root.transform, machinery);

            Shield shield = BuildShield(root.transform, bounds);

            BoxCollider collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            Renderer[] fogRenderers = new Renderer[machinery.Length + 1];
            machinery.CopyTo(fogRenderers, 0);
            fogRenderers[machinery.Length] = shield.GetComponent<Renderer>();
            SerializedObject fog = new SerializedObject(root.GetComponent<FogVisibilityComponent>());
            SerializedProperty renderers = fog.FindProperty("renderers");
            renderers.arraySize = fogRenderers.Length;
            for (int i = 0; i < fogRenderers.Length; i++)
            {
                renderers.GetArrayElementAtIndex(i).objectReferenceValue = fogRenderers[i];
            }
            fog.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject health = new SerializedObject(root.GetComponent<HealthComponent>());
            health.FindProperty("ionFieldBounds").boundsValue = bounds;
            health.FindProperty("shieldView").objectReferenceValue = shield;
            health.ApplyModifiedPropertiesWithoutUndo();

            root.transform.Find("SelectedCanvas").localScale *= SELECTION_SCALE;
            root.transform.Find("ShieldGenerator").localPosition = new Vector3(0f, bounds.max.y, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, VIEW_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static Shield BuildShield(Transform root, Bounds bounds)
        {
            // Unit-sphere mesh: a uniform scale of the machinery's corner distance gives a round shell around it.
            GameObject surface = new GameObject("ShieldSurface");
            surface.layer = root.gameObject.layer;
            surface.transform.SetParent(root, false);
            surface.transform.localPosition = bounds.center;
            surface.transform.localScale = Vector3.one * bounds.extents.magnitude;
            surface.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SHIELD_MESH_PATH);
            MeshRenderer renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SHIELD_MATERIAL_PATH);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Hidden until an impact arrives; Shield enables it only while hits fade out.
            renderer.enabled = false;

            Shield shield = surface.AddComponent<Shield>();
            SerializedObject serializedShield = new SerializedObject(shield);
            serializedShield.FindProperty("shieldRenderer").objectReferenceValue = renderer;
            serializedShield.FindProperty("visibilityRadius").floatValue = SHIELD_VISIBILITY_RADIUS;
            serializedShield.FindProperty("waveSpeed").floatValue = SHIELD_WAVE_SPEED;
            serializedShield.FindProperty("waveWidth").floatValue = SHIELD_WAVE_WIDTH;
            serializedShield.FindProperty("displacementStrength").floatValue = SHIELD_DISPLACEMENT;
            serializedShield.ApplyModifiedPropertiesWithoutUndo();
            return shield;
        }

        private static void BuildInstaller()
        {
            GameObject root = new GameObject("AsteroidMiningFacilityInstaller");
            GameObjectContext context = root.AddComponent<GameObjectContext>();
            AsteroidMiningFacilityInstaller installer = root.AddComponent<AsteroidMiningFacilityInstaller>();
            SerializedObject serializedContext = new SerializedObject(context);
            SerializedProperty installers = serializedContext.FindProperty("_monoInstallers");
            installers.arraySize = 1;
            installers.GetArrayElementAtIndex(0).objectReferenceValue = installer;
            serializedContext.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, INSTALLER_PATH);
            Object.DestroyImmediate(root);
        }

        private static void BuildData()
        {
            AssetDatabase.DeleteAsset(DATA_PATH);
            AssetDatabase.CopyAsset(SOURCE_DATA_PATH, DATA_PATH);
            ScriptableObject data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA_PATH);
            SerializedObject serializedData = new SerializedObject(data);
            serializedData.FindProperty("<Income>k__BackingField").floatValue = INCOME;
            SerializedProperty componentData = serializedData.FindProperty("<ComponentData>k__BackingField");
            componentData.FindPropertyRelative("<Hull>k__BackingField").floatValue = HULL;
            componentData.FindPropertyRelative("<Shields>k__BackingField").floatValue = SHIELDS;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        public static GameObject InstantiateModel(Transform parent, string name, bool keepRocks)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH), parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = name;
            model.transform.localPosition = MODEL_OFFSET;
            for (int i = model.transform.childCount - 1; i >= 0; i--)
            {
                Transform part = model.transform.GetChild(i);
                if (IsAsteroidRock(part.name) != keepRocks)
                {
                    Object.DestroyImmediate(part.gameObject);
                    continue;
                }

                part.gameObject.layer = parent.gameObject.layer;
            }

            model.layer = parent.gameObject.layer;
            return model;
        }

        public static Bounds GetLocalBounds(Transform space, Renderer[] renderers)
        {
            Bounds bounds = new Bounds(space.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(space.InverseTransformPoint(renderer.bounds.min));
                bounds.Encapsulate(space.InverseTransformPoint(renderer.bounds.max));
            }

            return bounds;
        }
    }
}
