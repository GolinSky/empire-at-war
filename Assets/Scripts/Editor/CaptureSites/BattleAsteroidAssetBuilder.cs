using System.Collections.Generic;
using EmpireAtWar.Editor.Rendering;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Editor.CaptureSites
{
    /// <summary>
    /// Builds the battle asteroid entity assets: the XQ6 platform's components on the asteroid mine's machinery, with
    /// turbolaser turrets on the site rocks around it.
    /// </summary>
    public static class BattleAsteroidAssetBuilder
    {
        private const string CANNON_MODEL_PATH =
            "Assets/Art/Models/Cannons/HeavyTurbolaserCannon/HeavyTurbolaserCannon.fbx";
        private const string SOURCE_VIEW_PATH = "Assets/Prefabs/Models/DefendStation/DefendPlatformView.prefab";
        private const string VIEW_PATH = "Assets/Prefabs/Models/DefendStation/AsteroidDefendPlatformView.prefab";
        private const string INSTALLER_PATH = "Assets/Prefabs/View/AsteroidDefendPlatformInstaller.prefab";
        private const string SOURCE_DATA_PATH = "Assets/Settings/Data/Models/DefendPlatform/DefendPlatformData.asset";
        private const string DATA_PATH = "Assets/Settings/Data/Models/DefendPlatform/AsteroidDefendPlatformData.asset";

        private const float CANNON_SCALE = 3f;
        private const float HEIGHT_SAMPLE_RADIUS = 0.8f;
        private const float SURFACE_HEIGHT_PERCENTILE = 0.85f;
        // Sinks each cannon base slightly into the uneven rock so no edge floats.
        private const float MOUNT_SINK = 0.1f;
        private const float HULL = 3500f;
        private const float SHIELDS = 1800f;

        // Site-local XZ mount points on bare rock clear of the machinery: one on each small rock, four around the
        // large one's rim.
        private static readonly Vector2[] MOUNT_POINTS =
        {
            new Vector2(-8.6f, -2.5f),
            new Vector2(-6.4f, 3.1f),
            new Vector2(0.2f, 5.8f),
            new Vector2(7.6f, 5.4f),
            new Vector2(8.6f, 2.2f),
            new Vector2(6.6f, -0.6f),
        };

        // Muzzle of the unscaled cannon model, whose barrels point along -X.
        private static readonly Vector3 MUZZLE_OFFSET = new Vector3(-0.2f, 0.09f, 0f);

        [MenuItem("Tools/Empire At War/Capture Sites/Build Battle Asteroid Assets")]
        public static void Build()
        {
            BuildView();
            BuildInstaller();
            BuildData();
            AsteroidMiningFacilityAssetBuilder.AddAddressable(VIEW_PATH, "View");
            AsteroidMiningFacilityAssetBuilder.AddAddressable(INSTALLER_PATH, "Installers");
            AsteroidMiningFacilityAssetBuilder.AddAddressable(DATA_PATH, "Model");
            AssetDatabase.SaveAssets();
        }

        /// <summary>The asteroid mine's machinery with the turbolaser turrets around it.</summary>
        public static GameObject InstantiateFacility(Transform parent, string name)
        {
            GameObject facility = new GameObject(name);
            facility.layer = parent.gameObject.layer;
            facility.transform.SetParent(parent, false);
            AsteroidMiningFacilityAssetBuilder.InstantiateModel(facility.transform, "Machinery", keepRocks: false);
            InstantiateCannons(facility.transform, "Cannons");
            return facility;
        }

        /// <summary>Mounts the turbolasers on the site rocks, each facing away from the site centre.</summary>
        public static GameObject InstantiateCannons(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.layer = parent.gameObject.layer;
            group.transform.SetParent(parent, false);
            Vector3[] mounts = GetMountPositions();
            GameObject cannonModel = AssetDatabase.LoadAssetAtPath<GameObject>(CANNON_MODEL_PATH);
            for (int i = 0; i < mounts.Length; i++)
            {
                GameObject cannon = (GameObject)PrefabUtility.InstantiatePrefab(cannonModel, group.transform);
                PrefabUtility.UnpackPrefabInstance(cannon, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                cannon.name = "Cannon" + (i + 1);
                cannon.transform.localPosition = mounts[i];
                cannon.transform.localRotation = GetFacing(mounts[i]);
                cannon.transform.localScale = Vector3.one * CANNON_SCALE;
                foreach (Transform part in cannon.GetComponentsInChildren<Transform>())
                {
                    part.gameObject.layer = parent.gameObject.layer;
                }
            }

            return group;
        }

        private static void BuildView()
        {
            AssetDatabase.DeleteAsset(VIEW_PATH);
            AssetDatabase.CopyAsset(SOURCE_VIEW_PATH, VIEW_PATH);
            GameObject root = PrefabUtility.LoadPrefabContents(VIEW_PATH);
            root.name = "AsteroidDefendPlatformView";

            // The XQ6 hull and its always-visible shell give way to the mine's machinery, turrets and a ship-style,
            // impact-only shield.
            Transform platformModel = root.transform.Find("default");
            Bounds platformBounds = AsteroidMiningFacilityAssetBuilder.GetLocalBounds(
                root.transform, new[] { platformModel.GetComponent<Renderer>() });
            Object.DestroyImmediate(platformModel.gameObject);
            Object.DestroyImmediate(root.transform.Find("ShieldView").gameObject);

            GameObject facility = InstantiateFacility(root.transform, "Facility");
            Renderer[] facilityRenderers = facility.GetComponentsInChildren<Renderer>();
            Bounds bounds = AsteroidMiningFacilityAssetBuilder.GetLocalBounds(root.transform, facilityRenderers);
            Shield shield = AsteroidMiningFacilityAssetBuilder.BuildShield(root.transform, bounds);

            BoxCollider collider = root.GetComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            Renderer[] fogRenderers = new Renderer[facilityRenderers.Length + 1];
            facilityRenderers.CopyTo(fogRenderers, 0);
            fogRenderers[facilityRenderers.Length] = shield.GetComponent<Renderer>();
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

            MountHardPoints(root.transform, facility.transform.Find("Cannons"));

            float selectionScale = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Max(platformBounds.extents.x, platformBounds.extents.z);
            root.transform.Find("SelectedCanvas").localScale *= selectionScale;
            root.transform.Find("Components/ShieldGenerator").localPosition =
                new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            // The model was re-instantiated with its source materials; swap in the converted unit copies.
            ShipLitSetupTool.UseShipLitMaterialCopies(root);
            // The hierarchy was rebuilt, so the team color view must list the new renderers.
            ShipLitSetupTool.AssignTeamColorRenderers(root);

            PrefabUtility.SaveAsPrefabAsset(root, VIEW_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void MountHardPoints(Transform root, Transform cannons)
        {
            // Each of the XQ6's weapon hard points moves to the muzzle of one cannon.
            Transform weaponComponent = root.Find("Components/WeaponComponent");
            weaponComponent.localPosition = Vector3.zero;
            SerializedObject weapon = new SerializedObject(weaponComponent.GetComponent<WeaponComponent>());
            SerializedProperty hardPoints = weapon.FindProperty("hardPoints");
            for (int i = 0; i < hardPoints.arraySize; i++)
            {
                Transform cannon = cannons.GetChild(i % cannons.childCount);
                Transform hardPoint = ((Component)hardPoints.GetArrayElementAtIndex(i).objectReferenceValue).transform;
                hardPoint.position = cannon.TransformPoint(MUZZLE_OFFSET);
                hardPoint.rotation = cannon.rotation;
            }
        }

        private static Vector3[] GetMountPositions()
        {
            // Samples the rock meshes from the site's own model so cannons sit on the rock surface.
            GameObject sampleRoot = new GameObject("RockSample");
            GameObject rocks = AsteroidMiningFacilityAssetBuilder.InstantiateModel(
                sampleRoot.transform, "AsteroidRocks", keepRocks: true);
            MeshFilter[] rockMeshes = rocks.GetComponentsInChildren<MeshFilter>();
            Vector3[] mounts = new Vector3[MOUNT_POINTS.Length];
            for (int i = 0; i < MOUNT_POINTS.Length; i++)
            {
                float height = GetSurfaceHeight(rockMeshes, MOUNT_POINTS[i]);
                mounts[i] = new Vector3(MOUNT_POINTS[i].x, height - MOUNT_SINK, MOUNT_POINTS[i].y);
            }

            Object.DestroyImmediate(sampleRoot);
            return mounts;
        }

        private static float GetSurfaceHeight(MeshFilter[] rockMeshes, Vector2 point)
        {
            // The rocks are spiky: the highest vertex under a mount floats on a spike tip and the mean buries the
            // cannon, so the mount uses a high percentile of the upper-surface vertices.
            List<float> heights = new List<float>();
            foreach (MeshFilter rock in rockMeshes)
            {
                float rockCenterHeight = rock.transform.TransformPoint(rock.sharedMesh.bounds.center).y;
                foreach (Vector3 vertex in rock.sharedMesh.vertices)
                {
                    Vector3 world = rock.transform.TransformPoint(vertex);
                    if (world.y > rockCenterHeight &&
                        new Vector2(world.x - point.x, world.z - point.y).sqrMagnitude <=
                        HEIGHT_SAMPLE_RADIUS * HEIGHT_SAMPLE_RADIUS)
                    {
                        heights.Add(world.y);
                    }
                }
            }

            heights.Sort();
            return heights[Mathf.FloorToInt((heights.Count - 1) * SURFACE_HEIGHT_PERCENTILE)];
        }

        private static Quaternion GetFacing(Vector3 mount)
        {
            Vector3 outward = new Vector3(mount.x, 0f, mount.z);
            return Quaternion.FromToRotation(Vector3.left, outward.normalized);
        }

        private static void BuildInstaller()
        {
            GameObject root = new GameObject("AsteroidDefendPlatformInstaller");
            GameObjectContext context = root.AddComponent<GameObjectContext>();
            AsteroidDefendPlatformInstaller installer = root.AddComponent<AsteroidDefendPlatformInstaller>();
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
            SerializedProperty componentData = serializedData.FindProperty("<ComponentData>k__BackingField");
            componentData.FindPropertyRelative("<Hull>k__BackingField").floatValue = HULL;
            componentData.FindPropertyRelative("<Shields>k__BackingField").floatValue = SHIELDS;
            serializedData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
    }
}
