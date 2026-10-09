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
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;
using MPUIKIT;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EmpireAtWar.Editor.Squadrons
{
    /// <summary>Generates the "{SquadronType}SquadronView" prefabs from a fighter model.</summary>
    [EmpireAtWar.Editor.EditorToolInfo("Rebuilds squadron view prefabs from the builder specifications. Build Squadron Views covers Delta-7 and Belbullab-22; A-Wing has a separate command.")]
    public static class SquadronViewPrefabBuilder
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Models/Squadrons";
        private const string EXPLOSION_PATH = "Assets/Prefabs/Vfx/FighterExplosionVfx.prefab";
        private const string TRAIL_MATERIAL_PATH = "Assets/Art/Materials/Vfx/Engines.mat";
        private const string SELECTION_SOURCE_PATH = "Assets/Prefabs/Models/Ships/ArquitensShipView.prefab";

        private const float SELECTION_RING_SIZE = 8f;
        private const float SELECTION_RING_HEIGHT = -1f;
        private const float GUN_HALF_ARC = 12f;
        private const float EDITOR_SLOT_SPACING = 2f;
        private const float TRAIL_TIME = 0.6f;
        private const float TRAIL_WIDTH = 0.16f;
        private const float ICON_SIZE = 72f;
        private const float ICON_FRAME_STROKE = 1.5f;
        private const float ICON_FRAME_CORNER_RADIUS = 3f;
        private const float ICON_SILHOUETTE_PADDING = 6.75f;
        private const float ICON_SHADOW_OFFSET = -1.5f;
        private const float ICON_PADDING = 9f;

        [MenuItem("Tools/Empire At War/Units/Squadrons/Build Squadron Views")]
        public static void BuildAll()
        {
            Build(new SquadronViewSpec(type: SquadronType.Delta7,
                modelPath: "Assets/Art/Models/RepublicShips/Delta7/Delta7.obj", memberCount: 5, modelScale: 0.306f,
                modelEuler: Vector3.zero, modelOffset: new Vector3(0f, -0.551f, 0f), gunForward: 1.98f, colliderRadius: 1.5f,
                trailOffsets: new[] { new Vector3(-0.72f, 0f, -1.62f), new Vector3(0.72f, 0f, -1.62f) },
                trailColor: new Color(0.55f, 0.75f, 1f)));
            Build(new SquadronViewSpec(type: SquadronType.Belbullab22,
                modelPath: "Assets/Art/Models/SeparatistShips/Belbullab22/Belbullab22.obj", memberCount: 4, modelScale: 0.244f,
                modelEuler: new Vector3(0f, 180f, 0f), modelOffset: new Vector3(0f, -0.095f, -0.196f), gunForward: 1.96f, colliderRadius: 1.5f,
                trailOffsets: new[] { new Vector3(-0.812f, 0f, -1.68f), new Vector3(0.812f, 0f, -1.68f) },
                trailColor: new Color(1f, 0.62f, 0.3f)));
            BuildAWing();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Empire At War/Units/Squadrons/Build A-Wing Squadron View")]
        public static void BuildAWing()
        {
            Build(new SquadronViewSpec(type: SquadronType.AWing,
                modelPath: "Assets/Art/Models/RepublicShips/AWing/AWing.dae", memberCount: 6, modelScale: 0.00015f,
                modelEuler: new Vector3(0f, 270f, 0f), modelOffset: new Vector3(0.036f, 0.041f, -0.309f), gunForward: 1.6f, colliderRadius: 1.5f,
                trailOffsets: new[] { new Vector3(-0.65f, 0f, -1.5f), new Vector3(0.65f, 0f, -1.5f) },
                trailColor: new Color(0.55f, 0.75f, 1f)));
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
            SetObjectList(weaponComponent, "hardPoints", guns);
            AddSelectionRing(root.transform, selection);
            Sprite unitIcon = AssetDatabase.LoadAssetAtPath<FactionCatalog>(
                "Assets/Settings/Data/Factions/Shared/FactionCatalog.asset").GetSquadronFactionData(spec.Type).Icon;
            AddWorldIcon(root, unitIcon);
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

        /// <summary>Adds the floating squadron marker with a full-color icon above its team-colored silhouette shadow.</summary>
        public static void AddWorldIcon(GameObject root, Sprite unitIcon)
        {
            SquadronIconComponent icon = root.AddComponent<SquadronIconComponent>();
            GameObject canvasObject = new GameObject("IconCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            RectTransform canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = Vector2.one * ICON_SIZE;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            MPImage background = CreateIconImage<MPImage>("Background", canvasObject.transform, 0f);
            background.DrawShape = DrawShape.Rectangle;
            background.color = new Color(0f, 0f, 0f, 0.5f);
            background.FalloffDistance = 1f;
            Rectangle backgroundRectangle = background.Rectangle;
            backgroundRectangle.CornerRadius = Vector4.one * ICON_FRAME_CORNER_RADIUS;
            background.Rectangle = backgroundRectangle;

            MPImage frame = CreateIconImage<MPImage>("Frame", canvasObject.transform, 0f);
            frame.DrawShape = DrawShape.Rectangle;
            frame.StrokeWidth = ICON_FRAME_STROKE;
            frame.FalloffDistance = 1f;
            Rectangle rectangle = frame.Rectangle;
            rectangle.CornerRadius = Vector4.one * ICON_FRAME_CORNER_RADIUS;
            frame.Rectangle = rectangle;

            UnityEngine.UI.Image silhouetteImage =
                CreateIconImage<UnityEngine.UI.Image>("Silhouette", canvasObject.transform, ICON_SILHOUETTE_PADDING);
            silhouetteImage.sprite = unitIcon;
            silhouetteImage.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Ui/IconShadow.mat");
            silhouetteImage.preserveAspect = true;
            silhouetteImage.rectTransform.anchoredPosition = new Vector2(0f, ICON_SHADOW_OFFSET);

            UnityEngine.UI.Image iconImage =
                CreateIconImage<UnityEngine.UI.Image>("Icon", canvasObject.transform, ICON_PADDING);
            iconImage.sprite = unitIcon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;

            SerializedObject iconObject = new SerializedObject(icon);
            iconObject.FindProperty("iconCanvas").objectReferenceValue = canvas;
            iconObject.FindProperty("frameImage").objectReferenceValue = frame;
            iconObject.FindProperty("silhouetteImage").objectReferenceValue = silhouetteImage;
            iconObject.FindProperty("iconImage").objectReferenceValue = iconImage;
            iconObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateIconImage<T>(string name, Transform parent, float padding)
            where T : UnityEngine.UI.Image
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
            imageObject.transform.SetParent(parent, false);
            RectTransform imageRect = (RectTransform)imageObject.transform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.sizeDelta = Vector2.one * (-2f * padding);
            T image = imageObject.GetComponent<T>();
            image.raycastTarget = false;
            return image;
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
