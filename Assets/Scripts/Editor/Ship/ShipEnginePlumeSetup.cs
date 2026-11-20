using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Ship.Movement;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship
{
    /// <summary>Places shared engine trail instances on a ship view prefab. Rerunning replaces the
    /// previous ones, so new numbers move or resize them.</summary>
    public static class ShipEnginePlumeSetup
    {
        public const string PLUME_PREFAB_PATH = "Assets/Prefabs/Vfx/Ships/ShipEnginePlume.prefab";
        private const string PLUME_NAME = "Engine Plume";
        private const string ENGINE_PLUMES_PROPERTY = "enginePlumes";
        private const string BODY_TRANSFORM_PROPERTY = "bodyTransform";
        private const string FOG_RENDERERS_PROPERTY = "renderers";

        /// <param name="rootSpaceNozzles">Nozzle exit centers in ship-root space.</param>
        /// <param name="widths">Trail width per nozzle in ship-root units; full-pace length is 14 × width.</param>
        public static void Apply(string prefabPath, Vector3[] rootSpaceNozzles, float[] widths)
        {
            GameObject plumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLUME_PREFAB_PATH);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SerializedObject movement = new SerializedObject(root.GetComponent<ShipMoveComponent>());
                SerializedObject fog = new SerializedObject(root.GetComponent<FogVisibilityComponent>());
                Transform body = (Transform)movement.FindProperty(BODY_TRANSFORM_PROPERTY).objectReferenceValue;

                // Earlier plume instances (any component version) and hand-made trails.
                var removed = new HashSet<GameObject>(root.GetComponentsInChildren<Transform>(true)
                    .Select(transform => transform.gameObject)
                    .Where(gameObject => PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) &&
                        PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject) == plumePrefab));
                foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true))
                    removed.Add(trail.gameObject);
                foreach (GameObject gameObject in removed) Object.DestroyImmediate(gameObject);

                var created = new TrailRenderer[rootSpaceNozzles.Length];
                for (int i = 0; i < created.Length; i++)
                {
                    var plume = (GameObject)PrefabUtility.InstantiatePrefab(plumePrefab, body);
                    plume.name = created.Length == 1 ? PLUME_NAME : $"{PLUME_NAME} {i + 1}";
                    plume.layer = body.gameObject.layer;
                    plume.transform.SetPositionAndRotation(root.transform.TransformPoint(rootSpaceNozzles[i]),
                        root.transform.rotation);
                    created[i] = plume.GetComponent<TrailRenderer>();
                    created[i].widthMultiplier = widths[i];
                }

                SerializedProperty plumes = movement.FindProperty(ENGINE_PLUMES_PROPERTY);
                plumes.arraySize = created.Length;
                for (int i = 0; i < created.Length; i++)
                    plumes.GetArrayElementAtIndex(i).objectReferenceValue = created[i];
                movement.ApplyModifiedPropertiesWithoutUndo();

                // Destroyed instances read back as null here.
                SerializedProperty fogRenderers = fog.FindProperty(FOG_RENDERERS_PROPERTY);
                var kept = Enumerable.Range(0, fogRenderers.arraySize)
                    .Select(i => fogRenderers.GetArrayElementAtIndex(i).objectReferenceValue)
                    .Where(renderer => renderer != null)
                    .Concat(created)
                    .ToList();
                fogRenderers.arraySize = kept.Count;
                for (int i = 0; i < kept.Count; i++)
                    fogRenderers.GetArrayElementAtIndex(i).objectReferenceValue = kept[i];
                fog.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
