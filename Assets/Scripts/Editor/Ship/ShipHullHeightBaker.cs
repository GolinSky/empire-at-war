using System.Text;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Ship.Data;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    /// <summary>Measures each ship view's visible hull across its full bank range and caches the
    /// vertical extent in its ShipData. Disabled renderers (shield bubbles) and inactive objects
    /// are excluded.</summary>
    [EmpireAtWar.Editor.EditorToolInfo("Bake hull heights from current ship prefabs into ship data.")]
    public static class ShipHullHeightBaker
    {
        private const string SHIP_DATA_FOLDER = "Assets/Settings/Data/Ship";
        private const string SHIP_VIEW_FOLDER = "Assets/Prefabs/Models/Ships";
        private const string DATA_SUFFIX = "ShipData";
        private const string VIEW_SUFFIX = "ShipView";
        private const string HULL_BOTTOM_PROPERTY = "<HullBottom>k__BackingField";
        private const string HULL_TOP_PROPERTY = "<HullTop>k__BackingField";
        private const string BODY_TRANSFORM_PROPERTY = "bodyTransform";

        private const float BANK_SAMPLE_STEP = 1f;

        [MenuItem("Tools/Empire At War/Units/Ships/Bake Hull Heights")]
        public static void BakeAll()
        {
            StringBuilder report = new StringBuilder("Ship hull heights baked:\n");
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShipData), new[] { SHIP_DATA_FOLDER }))
            {
                ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                Bounds hull = MeasureHull(LoadView(data), data.BodyRotationMaxAngle);
                SerializedObject serializedData = new SerializedObject(data);
                serializedData.FindProperty(HULL_BOTTOM_PROPERTY).floatValue = hull.min.y;
                serializedData.FindProperty(HULL_TOP_PROPERTY).floatValue = hull.max.y;
                serializedData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                report.AppendLine($"{data.name}: {hull.min.y:F1} .. {hull.max.y:F1}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        private static GameObject LoadView(ShipData data)
        {
            string viewName = data.name.Substring(0, data.name.Length - DATA_SUFFIX.Length) + VIEW_SUFFIX;
            string viewPath = $"{SHIP_VIEW_FOLDER}/{viewName}.prefab";
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
            if (view == null)
                throw new MissingReferenceException($"{data.name} has no ship view at {viewPath}.");
            return view;
        }

        private static Bounds MeasureHull(GameObject viewPrefab, float maxBankAngle)
        {
            GameObject view = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab);
            try
            {
                view.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                // Editor-only measurement: the baked prefab is inspected, not wired, so lookups are required.
                Transform body = (Transform)new SerializedObject(view.GetComponentInChildren<ShipMoveComponent>())
                    .FindProperty(BODY_TRANSFORM_PROPERTY).objectReferenceValue;
                Quaternion restRotation = body.localRotation;
                Renderer[] renderers = view.GetComponentsInChildren<Renderer>();
                // Same roll ShipMovementTweenPlayer applies to the body while turning.
                Bounds hull = MeasureRenderers(renderers, viewPrefab.name);
                for (float bank = -maxBankAngle; bank <= maxBankAngle; bank += BANK_SAMPLE_STEP)
                {
                    body.localRotation = restRotation * Quaternion.Euler(0f, 0f, bank);
                    hull.Encapsulate(MeasureRenderers(renderers, viewPrefab.name));
                }

                return hull;
            }
            finally
            {
                Object.DestroyImmediate(view);
            }
        }

        private static Bounds MeasureRenderers(Renderer[] renderers, string viewName)
        {
            bool hasHull = false;
            Bounds hull = default;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                    continue;

                if (hasHull)
                {
                    hull.Encapsulate(renderer.bounds);
                }
                else
                {
                    hull = renderer.bounds;
                    hasHull = true;
                }
            }

            if (!hasHull)
                throw new MissingComponentException($"{viewName} has no visible hull renderer.");
            return hull;
        }
    }
}
