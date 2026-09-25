using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Editor.Squadrons
{
    /// <summary>Generates the "{SquadronType}SquadronView" prefabs from a fighter model.</summary>
    public static class SquadronViewPrefabBuilder
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Models/Squadrons";
        private const string EXPLOSION_PATH = "Assets/Prefabs/Vfx/FighterExplosionVfx.prefab";
        private const string TRAIL_MATERIAL_PATH = "Assets/Art/Materials/Engines.mat";
        private const string SELECTION_SOURCE_PATH = "Assets/Prefabs/Models/Ships/ArquitensShipView.prefab";
        private const float SELECTION_RING_SIZE = 8f;
        private const float SELECTION_RING_HEIGHT = -1f;
        private const float GUN_HALF_ARC = 12f;
        private const float EDITOR_SLOT_SPACING = 2f;
        private const float TRAIL_TIME = 0.6f;
        private const float TRAIL_WIDTH = 0.16f;

        [MenuItem("Tools/Squadrons/Build Squadron Views")]
        public static void BuildAll()
        {
            Build(new SquadronViewSpec(SquadronType.Delta7,
                "Assets/Art/Models/RepublicModels/Delta7/Delta7.obj", 5, 0.085f,
                Vector3.zero, new Vector3(0f, -0.153f, 0f), 0.55f, 0.8f,
                new[] { new Vector3(-0.2f, 0f, -0.45f), new Vector3(0.2f, 0f, -0.45f) },
                new Color(0.55f, 0.75f, 1f)));
            Build(new SquadronViewSpec(SquadronType.Belbullab22,
                "Assets/Art/Models/SeparatistShip/belbullab/B22_whole.obj", 4, 0.087f,
                new Vector3(0f, 180f, 0f), new Vector3(0f, -0.034f, -0.07f), 0.7f, 0.9f,
                new[] { new Vector3(-0.29f, 0f, -0.6f), new Vector3(0.29f, 0f, -0.6f) },
                new Color(1f, 0.62f, 0.3f)));
            AssetDatabase.SaveAssets();
        }

        public static void Build(SquadronViewSpec spec)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject root = new GameObject($"{spec.Type}SquadronView");
            SceneManager.MoveGameObjectToScene(root, scene);
            try
            {
                Populate(root, spec);
                if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
                {
                    AssetDatabase.CreateFolder("Assets/Prefabs/Models", "Squadrons");
                }

                PrefabUtility.SaveAsPrefabAsset(root, $"{PREFAB_FOLDER}/{root.name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Populate(GameObject root, SquadronViewSpec spec)
        {
            root.AddComponent<Squadron>();
            SquadronFlightComponent flight = root.AddComponent<SquadronFlightComponent>();
            SquadronHealthComponent health = root.AddComponent<SquadronHealthComponent>();
            root.AddComponent<RadarComponent>();
            SelectionComponent selection = root.AddComponent<SelectionComponent>();

            GameObject weapons = new GameObject("Weapons");
            weapons.transform.SetParent(root.transform, false);
            WeaponComponent weaponComponent = weapons.AddComponent<WeaponComponent>();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath);
            GameObject explosion = AssetDatabase.LoadAssetAtPath<GameObject>(EXPLOSION_PATH);
            Material trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(TRAIL_MATERIAL_PATH);
            List<FighterView> fighters = new List<FighterView>();
            List<WeaponHardPoint> guns = new List<WeaponHardPoint>();
            for (int i = 0; i < spec.MemberCount; i++)
            {
                FighterView fighter = CreateFighter(root.transform, i, spec, model, explosion, trailMaterial);
                fighters.Add(fighter);
                guns.Add(fighter.Gun);
            }

            SetObjectList(flight, "fighters", fighters);
            SetObjectList(health, "fighters", fighters);
            SerializedObject weaponObject = new SerializedObject(weaponComponent);
            weaponObject.FindProperty("useWeaponDamageRange").boolValue = true;
            weaponObject.ApplyModifiedPropertiesWithoutUndo();
            SetObjectList(weaponComponent, "hardPoints", guns);
            AddSelectionRing(root.transform, selection);
            AddWorldIcon(root);
        }

        private static FighterView CreateFighter(Transform parent, int index, SquadronViewSpec spec,
            GameObject model, GameObject explosion, Material trailMaterial)
        {
            GameObject fighter = new GameObject($"Fighter{index}");
            fighter.transform.SetParent(parent, false);
            fighter.transform.localPosition = SquadronFormation.GetSlot(index, EDITOR_SLOT_SPACING).ToUnity();

            GameObject body = new GameObject("Body");
            body.transform.SetParent(fighter.transform, false);
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model, body.transform);
            modelInstance.transform.localPosition = spec.ModelOffset;
            modelInstance.transform.localRotation = Quaternion.Euler(spec.ModelEuler);
            modelInstance.transform.localScale = Vector3.one * spec.ModelScale;
            foreach (Renderer renderer in modelInstance.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            SphereCollider collider = fighter.AddComponent<SphereCollider>();
            collider.radius = spec.ColliderRadius;

            GameObject gunObject = new GameObject("Gun");
            gunObject.transform.SetParent(fighter.transform, false);
            gunObject.transform.localPosition = new Vector3(0f, 0f, spec.GunForward);
            WeaponHardPoint gun = gunObject.AddComponent<WeaponHardPoint>();
            SerializedObject gunObjectData = new SerializedObject(gun);
            gunObjectData.FindProperty("<WeaponType>k__BackingField").intValue = (int)WeaponType.FighterLaser;
            gunObjectData.FindProperty("<HardPointType>k__BackingField").intValue = (int)HardPointType.Weapon;
            gunObjectData.FindProperty("<Id>k__BackingField").intValue = index;
            gunObjectData.FindProperty("spawnDestroyedExplosion").boolValue = false;
            gunObjectData.FindProperty("yAxisRange.<Min>k__BackingField").floatValue = -GUN_HALF_ARC;
            gunObjectData.FindProperty("yAxisRange.<Max>k__BackingField").floatValue = GUN_HALF_ARC;
            gunObjectData.FindProperty("prewarmEffects").intValue = 2;
            gunObjectData.ApplyModifiedPropertiesWithoutUndo();

            GameObject explosionInstance = (GameObject)PrefabUtility.InstantiatePrefab(explosion, fighter.transform);
            explosionInstance.transform.localPosition = Vector3.zero;

            TrailRenderer[] trails = new TrailRenderer[spec.TrailOffsets.Length];
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i] = CreateTrail(fighter.transform, spec.TrailOffsets[i], spec.TrailColor, trailMaterial);
            }

            FighterView view = fighter.AddComponent<FighterView>();
            view.SetEditorReferences(index, body.transform, collider, gun,
                explosionInstance.GetComponent<ParticleSystem>(), trails);
            return view;
        }

        private static TrailRenderer CreateTrail(Transform parent, Vector3 localPosition, Color color,
            Material material)
        {
            GameObject trailObject = new GameObject("EngineTrail");
            trailObject.transform.SetParent(parent, false);
            trailObject.transform.localPosition = localPosition;
            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = TRAIL_TIME;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = TRAIL_WIDTH;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.numCapVertices = 2;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.25f),
                    new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            return trail;
        }

        private static void AddSelectionRing(Transform root, SelectionComponent selection)
        {
            Transform source = AssetDatabase.LoadAssetAtPath<GameObject>(SELECTION_SOURCE_PATH)
                .transform.Find("SelectedCanvas");
            GameObject canvas = Object.Instantiate(source.gameObject, root, false);
            canvas.name = source.name;
            canvas.transform.localPosition = new Vector3(0f, SELECTION_RING_HEIGHT, 0f);
            RectTransform image = (RectTransform)canvas.transform.Find("SelectedImage");
            image.sizeDelta = Vector2.one * SELECTION_RING_SIZE;

            SerializedObject selectionObject = new SerializedObject(selection);
            selectionObject.FindProperty("selectionType").intValue = (int)SelectionType.Ship;
            selectionObject.FindProperty("selectedCanvas").objectReferenceValue = canvas.GetComponent<Canvas>();
            selectionObject.FindProperty("selectedImage").objectReferenceValue =
                image.GetComponent<UnityEngine.UI.Image>();
            selectionObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Adds the floating squadron icon; its sprite comes from the squadron's faction data at runtime.</summary>
        public static void AddWorldIcon(GameObject root)
        {
            SquadronIconComponent icon = root.AddComponent<SquadronIconComponent>();
            GameObject canvasObject = new GameObject("IconCanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root.transform, false);
            RectTransform canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = Vector2.one;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            GameObject imageObject = new GameObject("IconImage", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.zero;
            UnityEngine.UI.Image image = imageObject.GetComponent<UnityEngine.UI.Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            SerializedObject iconObject = new SerializedObject(icon);
            iconObject.FindProperty("iconCanvas").objectReferenceValue = canvas;
            iconObject.FindProperty("iconImage").objectReferenceValue = image;
            iconObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectList<T>(Object target, string propertyName, IReadOnlyList<T> values)
            where T : Object
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty list = serializedObject.FindProperty(propertyName);
            list.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
