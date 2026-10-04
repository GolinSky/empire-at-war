using System.Collections.Generic;
using System.Text;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.ViewComponents.Health;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Editor
{
    /// <summary>Wraps each shield tightly around its hull: a convex shell of the visible renderers plus a margin,
    /// saved as a per-view mesh and as the planes <see cref="Shield"/> clips shots against.</summary>
    public static class ShieldHullBaker
    {
        private const string VIEW_FOLDER = "Assets/Prefabs/Models";
        private const string MESH_PARENT_FOLDER = "Assets/Art/Models";
        private const string MESH_FOLDER_NAME = "Shields";
        private const string SPHERE_MESH_PATH = "Assets/Art/Models/ShieldSurface.asset";
        private const string HULL_PLANES_PROPERTY = "hullPlanes";
        private const string BODY_TRANSFORM_PROPERTY = "bodyTransform";

        private const int PLANE_COUNT = 1024;
        // Gap between hull and shield: a fraction of the hull's largest half-extent, at least MIN_MARGIN world units.
        private const float MARGIN_RATIO = 0.03f;
        private const float MIN_MARGIN = 0.75f;

        [MenuItem("Tools/Ships/Bake Shield Hulls")]
        public static void BakeAll()
        {
            StringBuilder report = new StringBuilder("Shield hulls baked:\n");
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { VIEW_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<Shield>(true) == null)
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Bounds hull = Bake(root.transform, root.GetComponentInChildren<Shield>(true));
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    report.AppendLine($"{root.name}: shell size {hull.size}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(report.ToString());
        }

        /// <returns>The baked shell's local bounds.</returns>
        public static Bounds Bake(Transform root, Shield shield)
        {
            Transform surface = shield.transform;
            // Ships bank their body while turning; the shell rides on the body so the hull never leaves it.
            ShipMoveComponent move = root.GetComponentInChildren<ShipMoveComponent>(true);
            if (move != null)
            {
                Transform body = (Transform)new SerializedObject(move)
                    .FindProperty(BODY_TRANSFORM_PROPERTY).objectReferenceValue;
                surface.SetParent(body, false);
            }

            Transform parent = surface.parent;
            Vector3[] points = CollectHullPoints(root, surface, parent.worldToLocalMatrix, out Bounds hull);
            Vector3 center = hull.center;
            for (int i = 0; i < points.Length; i++)
                points[i] -= center;

            float largestExtent = Mathf.Max(hull.extents.x, Mathf.Max(hull.extents.y, hull.extents.z));
            float margin = Mathf.Max(MIN_MARGIN / parent.lossyScale.x, MARGIN_RATIO * largestExtent);
            Vector4[] planes = BuildPlanes(points, margin);
            Mesh mesh = SaveMesh(BuildShellMesh(planes, root.name + "Shield"), root.name);

            surface.localPosition = center;
            surface.localRotation = Quaternion.identity;
            surface.localScale = Vector3.one;
            shield.GetComponent<MeshFilter>().sharedMesh = mesh;
            SerializedObject serializedShield = new SerializedObject(shield);
            SerializedProperty planesProperty = serializedShield.FindProperty(HULL_PLANES_PROPERTY);
            planesProperty.arraySize = planes.Length;
            for (int i = 0; i < planes.Length; i++)
                planesProperty.GetArrayElementAtIndex(i).vector4Value = planes[i];
            serializedShield.ApplyModifiedPropertiesWithoutUndo();
            return mesh.bounds;
        }

        private static Vector3[] CollectHullPoints(Transform root, Transform surface, Matrix4x4 toParent,
            out Bounds hull)
        {
            HashSet<Vector3> points = new HashSet<Vector3>();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.enabled || renderer.transform == surface) continue;
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Matrix4x4 toLocal = toParent * renderer.localToWorldMatrix;
                foreach (Vector3 vertex in mesh.vertices)
                    points.Add(toLocal.MultiplyPoint3x4(vertex));
            }

            if (points.Count == 0)
                throw new MissingComponentException($"{root.name} has no visible hull renderer.");

            Vector3[] result = new Vector3[points.Count];
            points.CopyTo(result);
            hull = new Bounds(result[0], Vector3.zero);
            foreach (Vector3 point in result)
                hull.Encapsulate(point);
            return result;
        }

        // Supporting planes along evenly spread directions: their intersection is a convex shell around the hull.
        private static Vector4[] BuildPlanes(Vector3[] points, float margin)
        {
            Vector4[] planes = new Vector4[PLANE_COUNT];
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < PLANE_COUNT; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / PLANE_COUNT;
                float ring = Mathf.Sqrt(1f - y * y);
                Vector3 normal = new Vector3(Mathf.Cos(goldenAngle * i) * ring, y, Mathf.Sin(goldenAngle * i) * ring);
                float reach = float.NegativeInfinity;
                foreach (Vector3 point in points)
                    reach = Mathf.Max(reach, Vector3.Dot(normal, point));
                planes[i] = new Vector4(normal.x, normal.y, normal.z, reach + margin);
            }

            return planes;
        }

        // Pushes each unit-sphere vertex out along its direction until it meets the shell.
        private static Mesh BuildShellMesh(Vector4[] planes, string name)
        {
            Mesh sphere = AssetDatabase.LoadAssetAtPath<Mesh>(SPHERE_MESH_PATH);
            Vector3[] vertices = sphere.vertices;
            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 direction = vertices[v].normalized;
                float radius = float.PositiveInfinity;
                foreach (Vector4 plane in planes)
                {
                    float facing = Vector3.Dot(plane, direction);
                    if (facing > 0f) radius = Mathf.Min(radius, plane.w / facing);
                }

                vertices[v] = direction * radius;
            }

            Mesh mesh = new Mesh { name = name, indexFormat = sphere.indexFormat };
            mesh.vertices = vertices;
            mesh.triangles = sphere.triangles;
            mesh.RecalculateNormals();
            mesh.normals = WeldNormals(vertices, mesh.normals);
            mesh.RecalculateBounds();
            return mesh;
        }

        // The sphere's seam duplicates vertices; averaging their normals keeps the displaced shell closed.
        private static Vector3[] WeldNormals(Vector3[] vertices, Vector3[] normals)
        {
            Dictionary<Vector3, Vector3> sums = new Dictionary<Vector3, Vector3>();
            for (int v = 0; v < vertices.Length; v++)
            {
                sums.TryGetValue(vertices[v], out Vector3 sum);
                sums[vertices[v]] = sum + normals[v];
            }

            for (int v = 0; v < vertices.Length; v++)
                normals[v] = sums[vertices[v]].normalized;
            return normals;
        }

        private static Mesh SaveMesh(Mesh mesh, string viewName)
        {
            string folder = $"{MESH_PARENT_FOLDER}/{MESH_FOLDER_NAME}";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(MESH_PARENT_FOLDER, MESH_FOLDER_NAME);

            string path = $"{folder}/{viewName}Shield.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            // Overwrite in place so the asset keeps its GUID and prefab references.
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
