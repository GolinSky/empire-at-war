using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.Ship.Data;
using UnityEditor;
using UnityEngine;
using ShipView = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Editor
{
    /// <summary>
    /// Fits a convex pointer collider to the bound hull renderers of battlecruiser and dreadnought views. Their
    /// bounding boxes enclose huge empty volumes that caught taps aimed at open space or at ships flying above.
    /// The root box stays on the view, disabled: spawn clearance still reads it as the hull bounds.
    /// </summary>
    [EditorToolInfo("Fit convex pointer colliders to battlecruiser and dreadnought hulls.")]
    public static class ShipPickHullBaker
    {
        private const string SHIP_DATA_FOLDER = "Assets/Settings/Data/Ship";
        private const string SHIP_DATA_SUFFIX = "ShipData";
        private const string HULL_RENDERERS_PROPERTY = "explosionHullRenderers";
        private const string MESH_SUFFIX = "_PickHull";
        // PhysX caps a convex hull at 255 polygons; n points give at most 2n - 4 triangles.
        private const int SUPPORT_DIRECTIONS = 128;
        private static readonly Vector3[] AXES =
            { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

        public static bool NeedsPickHull(ShipClass shipClass) =>
            shipClass == ShipClass.BattleCruiser || shipClass == ShipClass.Dreadnought;

        [MenuItem("Tools/Empire At War/Units/Ships/Bake Pick Hulls")]
        public static void BakeAll()
        {
            StringBuilder report = new StringBuilder("Ship pick hulls baked:\n");
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ShipData), new[] { SHIP_DATA_FOLDER }))
            {
                ShipData data = AssetDatabase.LoadAssetAtPath<ShipData>(AssetDatabase.GUIDToAssetPath(guid));
                if (!NeedsPickHull(data.ShipClass)) continue;
                string viewPath = AssetDatabase.GetAssetPath(WeaponLoadoutBaker.LoadShipView(data));
                Mesh mesh = Bake(data, viewPath);
                report.AppendLine($"{data.name}: {mesh.vertexCount} hull points -> {AssetDatabase.GetAssetPath(mesh)}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <summary>Root-local bounds of the bound hull renderers; a current pick hull spans exactly these.</summary>
        public static Bounds MeasureHull(GameObject view)
        {
            List<Vector3> points = CollectPoints(view, out _);
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points) bounds.Encapsulate(point);
            return bounds;
        }

        private static Mesh Bake(ShipData data, string viewPath)
        {
            GameObject view = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
            List<Vector3> points = CollectPoints(view, out string meshFolder);
            string unitName = data.name.Substring(0, data.name.Length - SHIP_DATA_SUFFIX.Length);
            Mesh mesh = SaveMesh(SelectSupportPoints(points), $"{meshFolder}/{unitName}{MESH_SUFFIX}.asset",
                unitName + MESH_SUFFIX);

            GameObject root = PrefabUtility.LoadPrefabContents(viewPath);
            try
            {
                // Editor baking of the authored root colliders; runtime never searches for them.
                BoxCollider box = root.GetComponent<BoxCollider>();
                if (box == null)
                    throw new MissingComponentException($"{view.name} has no root hull box to keep as bounds data.");
                box.enabled = false;
                MeshCollider collider = root.GetComponent<MeshCollider>();
                if (collider == null) collider = root.AddComponent<MeshCollider>();
                // Convex first: assigning the point mesh to a triangle-mesh collider cooks its fan triangles.
                collider.convex = true;
                collider.isTrigger = false;
                collider.sharedMesh = mesh;
                PrefabUtility.SaveAsPrefabAsset(root, viewPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return mesh;
        }

        private static List<Vector3> CollectPoints(GameObject view, out string meshFolder)
        {
            // Editor inspection of the explicit hull binding, the same renderers the vision hull bakes from.
            ShipView ship = view.GetComponent<ShipView>();
            SerializedProperty renderers = new SerializedObject(ship).FindProperty(HULL_RENDERERS_PROPERTY);
            Matrix4x4 toRoot = view.transform.worldToLocalMatrix;
            List<Vector3> points = new List<Vector3>();
            meshFolder = null;
            for (int i = 0; i < renderers.arraySize; i++)
            {
                Renderer renderer = (Renderer)renderers.GetArrayElementAtIndex(i).objectReferenceValue;
                if (renderer == null)
                    throw new MissingReferenceException($"{view.name} has a missing hull renderer at index {i}.");
                if (!(renderer is MeshRenderer) || !renderer.enabled || !IsAuthoredActive(renderer.transform, view.transform))
                    continue;
                Mesh source = renderer.GetComponent<MeshFilter>().sharedMesh;
                meshFolder ??= Path.GetDirectoryName(AssetDatabase.GetAssetPath(source))?.Replace('\\', '/');
                Matrix4x4 toLocal = toRoot * renderer.localToWorldMatrix;
                foreach (Vector3 vertex in source.vertices) points.Add(toLocal.MultiplyPoint3x4(vertex));
            }

            if (points.Count < 4)
                throw new InvalidOperationException($"{view.name} has no bound visible hull geometry to bake.");
            if (string.IsNullOrEmpty(meshFolder) || !meshFolder.StartsWith("Assets/Art/Models/", StringComparison.Ordinal))
                throw new InvalidOperationException($"{view.name} hull mesh is outside Assets/Art/Models: {meshFolder}.");
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

        /// <summary>
        /// Keeps the hull's extreme point for the six axes and evenly spread directions. Points are compared in the
        /// unit box of the hull's bounds, so a long, flat hull keeps its bow tip and wing corners as well as its
        /// broad faces, and the axis extremes make the pick hull span the full hull bounds.
        /// </summary>
        private static Vector3[] SelectSupportPoints(List<Vector3> points)
        {
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points) bounds.Encapsulate(point);
            Vector3 extents = Vector3.Max(bounds.extents, Vector3.one * 0.001f);
            Vector3 toUnit = new Vector3(1f / extents.x, 1f / extents.y, 1f / extents.z);
            Vector3[] normalized = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++) normalized[i] = Vector3.Scale(points[i] - bounds.center, toUnit);
            HashSet<int> selected = new HashSet<int>();
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            int spread = SUPPORT_DIRECTIONS - AXES.Length;
            for (int d = 0; d < SUPPORT_DIRECTIONS; d++)
            {
                Vector3 direction;
                if (d < AXES.Length) direction = AXES[d];
                else
                {
                    int s = d - AXES.Length;
                    float y = 1f - 2f * (s + 0.5f) / spread;
                    float radius = Mathf.Sqrt(1f - y * y);
                    float angle = goldenAngle * s;
                    direction = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                }

                int best = 0;
                float bestDot = float.MinValue;
                for (int i = 0; i < normalized.Length; i++)
                {
                    float dot = Vector3.Dot(normalized[i], direction);
                    if (dot <= bestDot) continue;
                    bestDot = dot;
                    best = i;
                }

                selected.Add(best);
            }

            Vector3[] support = new Vector3[selected.Count];
            int index = 0;
            foreach (int i in selected) support[index++] = points[i];
            return support;
        }

        private static Mesh SaveMesh(Vector3[] vertices, string path, string name)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear();
            mesh.name = name;
            mesh.vertices = vertices;
            // Convex cooking reads only the points; the fan just gives the mesh valid indices.
            int[] triangles = new int[(vertices.Length - 2) * 3];
            for (int i = 0; i < vertices.Length - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
