using System;
using System.Collections.Generic;
using System.Text;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.Ship.Data;
using UnityEditor;
using UnityEngine;
using ShipView = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Editor
{
    [EditorToolInfo("Bake planar fog visibility hulls from explicitly bound ship hull renderers.")]
    public static class ShipVisionHullBaker
    {
        private const string SHIP_DATA_FOLDER = "Assets/Settings/Data/Ship";
        private const string HULL_RENDERERS_PROPERTY = "explosionHullRenderers";
        private const string VISION_HULL_PROPERTY = "visionHull";

        [MenuItem("Tools/Empire At War/Units/Ships/Bake Vision Hulls")]
        public static void BakeAll()
        {
            StringBuilder report = new StringBuilder("Ship vision hulls baked:\n");
            var baked = new List<(ShipData Data, Vector2[] Hull)>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShipData), new[] { SHIP_DATA_FOLDER }))
            {
                ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                Vector2[] hull = MeasureHull(WeaponLoadoutBaker.LoadShipView(data));
                baked.Add((data, hull));
            }

            // Resolve every mapping and hull first so missing source geometry cannot leave a partial bake.
            foreach ((ShipData data, Vector2[] hull) in baked)
            {
                SerializedObject serializedData = new SerializedObject(data);
                SerializedProperty property = serializedData.FindProperty(VISION_HULL_PROPERTY);
                property.arraySize = hull.Length;
                for (int i = 0; i < hull.Length; i++)
                    property.GetArrayElementAtIndex(i).vector2Value = hull[i];
                serializedData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                report.AppendLine($"{data.name}: {hull.Length} vertices");
            }

            AssetDatabase.SaveAssets();
            report.AppendLine($"Total: {baked.Count} ship data assets.");
            Debug.Log(report.ToString());
        }

        public static Vector2[] MeasureHull(GameObject prefab) => BuildHull(CollectPoints(prefab));

        private static List<Vector2> CollectPoints(GameObject prefab)
        {
            // Editor inspection of the explicit hull binding; runtime never searches components or mesh vertices.
            ShipView ship = prefab.GetComponent<ShipView>();
            SerializedProperty renderers = new SerializedObject(ship).FindProperty(HULL_RENDERERS_PROPERTY);
            List<Vector2> points = new List<Vector2>();
            Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;
            for (int i = 0; i < renderers.arraySize; i++)
            {
                Renderer renderer = (Renderer)renderers.GetArrayElementAtIndex(i).objectReferenceValue;
                if (renderer == null)
                    throw new MissingReferenceException($"{prefab.name} has a missing hull renderer at index {i}.");
                if (!(renderer is MeshRenderer) || !renderer.enabled || !IsAuthoredActive(renderer.transform, prefab.transform))
                    continue;
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Matrix4x4 toLocal = toRoot * renderer.localToWorldMatrix;
                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 point = toLocal.MultiplyPoint3x4(vertex);
                    points.Add(new Vector2(point.x, point.z));
                }
            }

            if (points.Count < 3)
                throw new InvalidOperationException($"{prefab.name} has no bound visible hull geometry to bake.");
            return points;
        }

        private static bool IsAuthoredActive(Transform transform, Transform root)
        {
            // Persistent prefab assets are not active in a Scene, even when every authored ancestor is enabled.
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf) return false;
                if (current == root) return true;
            }
            throw new InvalidOperationException($"Hull renderer {transform.name} is outside {root.name}.");
        }

        private static Vector2[] BuildHull(List<Vector2> points)
        {
            points.Sort((first, second) => first.x != second.x
                ? first.x.CompareTo(second.x) : first.y.CompareTo(second.y));
            List<Vector2> hull = new List<Vector2>();
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];
                while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }

            int lowerCount = hull.Count;
            for (int i = points.Count - 2; i >= 0; i--)
            {
                Vector2 point = points[i];
                while (hull.Count > lowerCount && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0f)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            if (hull.Count < 3)
                throw new InvalidOperationException("Ship vision hull has fewer than three non-collinear vertices.");
            return hull.ToArray();
        }

        private static float Cross(Vector2 first, Vector2 second, Vector2 third)
        {
            Vector2 edge = second - first;
            Vector2 offset = third - first;
            return edge.x * offset.y - edge.y * offset.x;
        }
    }
}
