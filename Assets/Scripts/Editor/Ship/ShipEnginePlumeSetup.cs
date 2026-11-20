using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.Ship.Movement;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor.Ship
{
    /// <summary>Places shared engine plume instances on a ship view prefab. Rerunning replaces the
    /// previous plumes, so new numbers move or rescale them.</summary>
    public static class ShipEnginePlumeSetup
    {
        public const string PLUME_PREFAB_PATH = "Assets/Prefabs/Vfx/Ships/ShipEnginePlume.prefab";
        private const string PLUME_NAME = "Engine Plume";
        private const string ENGINE_PLUMES_PROPERTY = "enginePlumes";
        private const string BODY_TRANSFORM_PROPERTY = "bodyTransform";
        private const string FOG_RENDERERS_PROPERTY = "renderers";

        /// <param name="rootSpaceNozzles">Nozzle exit centers in ship-root space.</param>
        /// <param name="widths">Plume width per nozzle in ship-root units; length is 14 × width.</param>
        public static void Apply(string prefabPath, Vector3[] rootSpaceNozzles, float[] widths)
        {
            GameObject plumePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PLUME_PREFAB_PATH);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SerializedObject movement = new SerializedObject(root.GetComponent<ShipMoveComponent>());
                SerializedObject fog = new SerializedObject(root.GetComponent<FogVisibilityComponent>());
                Transform body = (Transform)movement.FindProperty(BODY_TRANSFORM_PROPERTY).objectReferenceValue;

                var removed = new HashSet<Renderer>();
                SerializedProperty plumes = movement.FindProperty(ENGINE_PLUMES_PROPERTY);
                for (int i = 0; i < plumes.arraySize; i++)
                    removed.Add((Renderer)plumes.GetArrayElementAtIndex(i).objectReferenceValue);
                foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true))
                    removed.Add(trail);
                foreach (Renderer renderer in removed.Where(renderer => renderer != null))
                    Object.DestroyImmediate(renderer.gameObject);

                var created = new LineRenderer[rootSpaceNozzles.Length];
                for (int i = 0; i < created.Length; i++)
                {
                    var plume = (GameObject)PrefabUtility.InstantiatePrefab(plumePrefab, body);
                    plume.name = created.Length == 1 ? PLUME_NAME : $"{PLUME_NAME} {i + 1}";
                    plume.layer = body.gameObject.layer;
                    plume.transform.SetPositionAndRotation(root.transform.TransformPoint(rootSpaceNozzles[i]),
                        root.transform.rotation);
                    // Scale stretches the local-space length; View-aligned width ignores scale.
                    plume.transform.localScale = Vector3.one * (widths[i] / body.lossyScale.x);
                    created[i] = plume.GetComponent<LineRenderer>();
                    created[i].widthMultiplier = widths[i];
                }

                plumes.arraySize = created.Length;
                for (int i = 0; i < created.Length; i++)
                    plumes.GetArrayElementAtIndex(i).objectReferenceValue = created[i];
                movement.ApplyModifiedPropertiesWithoutUndo();

                SerializedProperty fogRenderers = fog.FindProperty(FOG_RENDERERS_PROPERTY);
                var kept = new List<Object>();
                for (int i = 0; i < fogRenderers.arraySize; i++)
                {
                    Object renderer = fogRenderers.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (renderer != null && !removed.Contains(renderer as Renderer)) kept.Add(renderer);
                }
                kept.AddRange(created);
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
