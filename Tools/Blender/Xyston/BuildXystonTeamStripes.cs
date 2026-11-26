using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// Xyston team stripes, fitted like the ISD I / Titan stripes: hull faces are clipped to a stripe region, lifted
/// off the surface and drawn with a copy of their own material plus a chipped team mask on the hull UVs.
/// Unlike those straight stripes, the region is an inward offset of the traced deck outline, so the stripe
/// follows the deck recesses. Top deck traces Hull_Armor, bottom deck traces Xyston_Armor. The source red lip
/// band (Hull_Armor_Red) stays; an extra inner line runs over the bow third, where that band is.
/// Run after BuildXyston: unity command script run --file Tools/Blender/Xyston/BuildXystonTeamStripes.cs
///   --entry BuildXystonTeamStripes.Main --timeout 60000 --json
/// </summary>
public static class BuildXystonTeamStripes
{
    private const string MODEL = "Assets/Prefabs/Models/Ships/Empire/Models/Xyston.prefab";
    private const string VIEW = "Assets/Prefabs/Models/Ships/Empire/View/XystonShipView.prefab";
    private const string WRECK = "Assets/Prefabs/Models/Wrecks/XystonWreckView.prefab";
    private const string PLACEMENT = "Assets/Prefabs/Ui/Reinforcement/XystonReinforcementView.prefab";
    private const string MESH_FOLDER = "Assets/Art/Models/EmpireShips/Xyston/";
    private const string MATERIAL_FOLDER = "Assets/Art/Materials/Models/EmpireShips/Xyston/";
    private const string WRECK_MATERIAL_FOLDER = "Assets/Art/Materials/Wrecks/Empire/Xyston/";
    // Hull-UV stripe mask (ISD I Remake stripes): mostly solid paint with small chips. The coarse
    // ImperialDestroyer_ChippedStripeMask is sized for stripe-space UVs and breaks these thin stripes into dashes.
    private const string MASK = "Assets/Art/Textures/Models/EmpireShips/ISDI/ISDI_TeamStripes_TeamMask.png";
    private const string WRECK_SHADER = "Assets/Art/Shaders/Units/ShipWreck.shader";
    private const string SHIP_LIT = "EmpireAtWar/Ship Lit";
    private const string PREFIX = "TeamStripes_";
    private const string BAND = "Hull_Armor_Red";
    private const float SURFACE_OFFSET = 0.03f;
    private const float SAMPLE_STEP = 0.05f;
    private const float SIMPLIFY_TOLERANCE = 0.04f;
    private const float MIN_FACING = 0.3f;
    private const float HEIGHT_TOLERANCE = 0.3f;
    // Model units; the hull is ~252 long. Bow line ends where the source lip band ends (z ~ 64).
    private static readonly Stripe[] STRIPES =
    {
        new Stripe { Inset = 0.7f, Width = 1.3f, MinZ = float.MinValue },
        new Stripe { Inset = 3.0f, Width = 0.9f, MinZ = 64f },
    };
    private static readonly (string Outline, float Facing)[] DECKS = { ("Hull_Armor", 1f), ("Xyston_Armor", -1f) };

    private struct Stripe { public float Inset, Width, MinZ; }
    private struct Vertex { public Vector3 Position; public Vector3 Normal; public Vector2 Uv; }

    public static string Main()
    {
        var model = PrefabUtility.LoadPrefabContents(MODEL);
        var surfaces = new Dictionary<Material, (List<Vector3> V, List<Vector3> N, List<Vector2> Uv, List<int> T)>();
        string report = "";
        try
        {
            foreach (var old in model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith(PREFIX)).ToArray())
                Object.DestroyImmediate(old.gameObject);
            var renderers = model.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.enabled && r.name != BAND && r.sharedMaterials.All(m => m != null && m.shader.name == SHIP_LIT)).ToArray();
            foreach (var (outlineName, facing) in DECKS)
            {
                var outline = renderers.Single(r => r.name == outlineName);
                var deck = Faces(model.transform, outline).Where(f => facing * FaceNormal(f).y > 0.5f).ToList();
                foreach (float side in new[] { -1f, 1f })
                {
                    var trace = TraceOutline(deck, side);
                    float edgeHeight = facing * trace.Min(p => facing * p.Height);
                    var polyline = Simplify(trace.Select(p => p.Point).ToList());
                    report += $"{outlineName} side {side}: {polyline.Count} outline points, edge height {edgeHeight:F2}\n";
                    foreach (var renderer in renderers)
                    {
                        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                        foreach (var face in Faces(model.transform, renderer, slot))
                        {
                            if (facing * FaceNormal(face).y < MIN_FACING) continue;
                            float height = face.Average(v => v.Position.y);
                            if (facing * (height - edgeHeight) < -HEIGHT_TOLERANCE) continue;
                            foreach (var stripe in STRIPES)
                            for (int segment = 0; segment + 1 < polyline.Count; segment++)
                            {
                                var polygon = face;
                                foreach (var plane in Region(polyline, segment, stripe, side))
                                    if ((polygon = Clip(polygon, plane)).Count < 3) break;
                                if (polygon.Count < 3) continue;
                                var material = renderer.sharedMaterials[slot];
                                if (!surfaces.TryGetValue(material, out var target))
                                    surfaces[material] = target = (new List<Vector3>(), new List<Vector3>(), new List<Vector2>(), new List<int>());
                                int offset = target.V.Count;
                                foreach (var vertex in polygon)
                                {
                                    target.V.Add(vertex.Position + vertex.Normal * SURFACE_OFFSET);
                                    target.N.Add(vertex.Normal);
                                    target.Uv.Add(vertex.Uv);
                                }
                                for (int corner = 1; corner < polygon.Count - 1; corner++)
                                    target.T.AddRange(new[] { offset, offset + corner, offset + corner + 1 });
                            }
                        }
                    }
                }
            }
            if (surfaces.Count == 0) throw new InvalidOperationException("No Xyston deck faces intersect the team stripes.");

            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(MASK);
            foreach (var pair in surfaces)
            {
                string name = "Xyston_TeamStripes_" + pair.Key.name;
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_FOLDER + name + ".asset");
                if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, MESH_FOLDER + name + ".asset"); }
                mesh.Clear();
                mesh.SetVertices(pair.Value.V); mesh.SetNormals(pair.Value.N); mesh.SetUVs(0, pair.Value.Uv); mesh.SetTriangles(pair.Value.T, 0);
                mesh.RecalculateTangents(); mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh);
                var paint = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_FOLDER + name + ".mat");
                if (paint == null) { paint = new Material(pair.Key); AssetDatabase.CreateAsset(paint, MATERIAL_FOLDER + name + ".mat"); }
                paint.CopyPropertiesFromMaterial(pair.Key); paint.name = name;
                paint.SetTexture("_TeamMaskMap", mask);
                paint.SetFloat("_TeamMaskStrength", 1f); paint.SetFloat("_TeamRimStrength", 0f); paint.SetFloat("_TeamLiveryStrength", 0f);
                EditorUtility.SetDirty(paint);
                var stripe = new GameObject(PREFIX + pair.Key.name);
                stripe.transform.SetParent(model.transform, false);
                stripe.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = stripe.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = paint;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                report += $"{name}: {pair.Value.T.Count / 3} triangles\n";
            }
            PrefabUtility.SaveAsPrefabAsset(model, MODEL);
        }
        finally { PrefabUtility.UnloadPrefabContents(model); }
        AssetDatabase.SaveAssets();
        WireView();
        HideInPlacement();
        BuildWreck();
        var current = new HashSet<string>(surfaces.Keys.Select(m => "Xyston_TeamStripes_" + m.name));
        foreach (string path in AssetDatabase.FindAssets("Xyston_TeamStripes_", new[] { MESH_FOLDER.TrimEnd('/'), MATERIAL_FOLDER.TrimEnd('/'), WRECK_MATERIAL_FOLDER.TrimEnd('/') })
                     .Select(AssetDatabase.GUIDToAssetPath).ToArray())
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!current.Contains(name.EndsWith("_Wreck") ? name.Substring(0, name.Length - "_Wreck".Length) : name))
            {
                AssetDatabase.DeleteAsset(path);
                report += "removed stale " + path + "\n";
            }
        }
        AssetDatabase.SaveAssets();
        return report;
    }

    // Traces max side*x of the deck at every z step, bow (high z) to the stern corner (widest point),
    // as (outward distance, z) plus the deck height there. Behind the corner the deck narrows to the engine block.
    private static List<(Vector2 Point, float Height)> TraceOutline(List<List<Vertex>> deck, float side)
    {
        float minZ = deck.SelectMany(f => f).Min(v => v.Position.z), maxZ = deck.SelectMany(f => f).Max(v => v.Position.z);
        int count = Mathf.FloorToInt((maxZ - minZ) / SAMPLE_STEP) + 1;
        var reach = Enumerable.Repeat(float.MinValue, count).ToArray();
        var height = new float[count];
        foreach (var face in deck)
        for (int edge = 0; edge < 3; edge++)
        {
            Vector3 a = face[edge].Position, b = face[(edge + 1) % 3].Position;
            if (Mathf.Approximately(a.z, b.z)) continue;
            int first = Mathf.CeilToInt((Mathf.Min(a.z, b.z) - minZ) / SAMPLE_STEP), last = Mathf.FloorToInt((Mathf.Max(a.z, b.z) - minZ) / SAMPLE_STEP);
            for (int i = Mathf.Max(first, 0); i <= Mathf.Min(last, count - 1); i++)
            {
                float z = minZ + i * SAMPLE_STEP;
                var point = Vector3.Lerp(a, b, (z - a.z) / (b.z - a.z));
                if (side * point.x <= reach[i]) continue;
                reach[i] = side * point.x;
                height[i] = point.y;
            }
        }
        int corner = Enumerable.Range(0, count).OrderByDescending(i => reach[i]).First();
        return Enumerable.Range(corner, count - corner).Reverse().Where(i => reach[i] > 0f)
            .Select(i => (new Vector2(reach[i], minZ + i * SAMPLE_STEP), height[i])).ToList();
    }

    private static List<Vector2> Simplify(List<Vector2> points)
    {
        var keep = new bool[points.Count];
        keep[0] = keep[points.Count - 1] = true;
        var stack = new Stack<(int, int)>();
        stack.Push((0, points.Count - 1));
        while (stack.Count > 0)
        {
            var (from, to) = stack.Pop();
            int worst = -1; float worstDistance = SIMPLIFY_TOLERANCE;
            var direction = (points[to] - points[from]).normalized;
            for (int i = from + 1; i < to; i++)
            {
                var offset = points[i] - points[from];
                float distance = Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
                if (distance > worstDistance) { worst = i; worstDistance = distance; }
            }
            if (worst < 0) continue;
            keep[worst] = true;
            stack.Push((from, worst)); stack.Push((worst, to));
        }
        return points.Where((_, i) => keep[i]).ToList();
    }

    // Half-planes (in model space, value >= 0 inside) bounding one outline segment's inward offset band.
    private static IEnumerable<Func<Vertex, float>> Region(List<Vector2> line, int k, Stripe stripe, float side)
    {
        Vector2 Plane(Vertex v) => new Vector2(side * v.Position.x, v.Position.z);
        Vector2 Inward(int i) { var d = (line[i + 1] - line[i]).normalized; return new Vector2(d.y, -d.x); }
        float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        Vector2 start = line[k], end = line[k + 1], dir = (end - start).normalized, inward = Inward(k);
        yield return v => Vector2.Dot(Plane(v) - start, inward) - stripe.Inset;
        yield return v => stripe.Inset + stripe.Width - Vector2.Dot(Plane(v) - start, inward);
        yield return v => Plane(v).x;                                   // centerline: both sides meet in a V at the bow
        yield return v => Plane(v).y - line[line.Count - 1].y;           // stern end
        if (stripe.MinZ > float.MinValue) yield return v => Plane(v).y - stripe.MinZ;
        if (k > 0)
        {
            var miter = (Inward(k - 1) + inward).normalized;
            float sign = Mathf.Sign(Cross(miter, dir));
            yield return v => sign * Cross(miter, Plane(v) - start);
        }
        if (k + 2 < line.Count)
        {
            var miter = (inward + Inward(k + 1)).normalized;
            float sign = Mathf.Sign(Cross(miter, -dir));
            yield return v => sign * Cross(miter, Plane(v) - end);
        }
    }

    private static Vector3 FaceNormal(List<Vertex> face) =>
        Vector3.Cross(face[1].Position - face[0].Position, face[2].Position - face[0].Position).normalized;

    private static IEnumerable<List<Vertex>> Faces(Transform root, MeshRenderer renderer, int slot = -1)
    {
        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        var matrix = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
        var normalMatrix = matrix.inverse.transpose;
        var positions = mesh.vertices; var normals = mesh.normals; var uvs = mesh.uv;
        var indices = slot < 0 ? mesh.triangles : mesh.GetTriangles(slot);
        bool flip = matrix.determinant < 0;
        for (int i = 0; i < indices.Length; i += 3)
        {
            var corners = flip ? new[] { indices[i], indices[i + 2], indices[i + 1] } : new[] { indices[i], indices[i + 1], indices[i + 2] };
            yield return corners.Select(c => new Vertex
            {
                Position = matrix.MultiplyPoint3x4(positions[c]),
                Normal = normalMatrix.MultiplyVector(normals[c]).normalized,
                Uv = uvs[c]
            }).ToList();
        }
    }

    private static List<Vertex> Clip(List<Vertex> polygon, Func<Vertex, float> distance)
    {
        var output = new List<Vertex>();
        for (int index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index];
            var previous = polygon[(index + polygon.Count - 1) % polygon.Count];
            float currentDistance = distance(current), previousDistance = distance(previous);
            if ((currentDistance >= 0) != (previousDistance >= 0))
            {
                float weight = previousDistance / (previousDistance - currentDistance);
                output.Add(new Vertex
                {
                    Position = Vector3.Lerp(previous.Position, current.Position, weight),
                    Normal = Vector3.Lerp(previous.Normal, current.Normal, weight).normalized,
                    Uv = Vector2.Lerp(previous.Uv, current.Uv, weight)
                });
            }
            if (currentDistance >= 0) output.Add(current);
        }
        return output;
    }

    // Team colour, fog and explosion lists of the ship view reference the nested model renderers.
    private static void WireView()
    {
        var root = PrefabUtility.LoadPrefabContents(VIEW);
        try
        {
            var stripes = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith(PREFIX)).Cast<Object>().ToArray();
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string field = component.GetType().Name switch
                {
                    "UnitRenderStateComponent" => "meshRenderers",
                    "FogVisibilityComponent" => "renderers",
                    "Ship" => "explosionHullRenderers",
                    _ => null
                };
                if (field != null) Replace(component, field, stripes);
            }
            PrefabUtility.SaveAsPrefabAsset(root, VIEW);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // The placement hologram shows the hull only; stripes would z-fight with it.
    private static void HideInPlacement()
    {
        var root = PrefabUtility.LoadPrefabContents(PLACEMENT);
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith(PREFIX)))
                renderer.enabled = false;
            PrefabUtility.SaveAsPrefabAsset(root, PLACEMENT);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Adds the stripes to the existing wreck as Ship Wreck copies, placed like ShipWreckBuilder places parts.
    private static void BuildWreck()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL);
        var stripes = model.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith(PREFIX)).ToArray();
        var band = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == BAND && t.GetComponent<MeshRenderer>() != null);
        var wreckShader = AssetDatabase.LoadAssetAtPath<Shader>(WRECK_SHADER);
        var root = PrefabUtility.LoadPrefabContents(WRECK);
        try
        {
            foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith(PREFIX)).ToArray())
                Object.DestroyImmediate(old.gameObject);
            var wreckBand = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == BAND);
            // Wreck parts are model parts re-parented under the wreck root: modelRoot -> wreckRoot.
            var modelToWreck = Matrix4x4.TRS(wreckBand.localPosition, wreckBand.localRotation, wreckBand.localScale)
                               * (model.transform.worldToLocalMatrix * band.localToWorldMatrix).inverse;
            var renderers = new List<Object>(); var filters = new List<Object>();
            foreach (var source in stripes)
            {
                var part = new GameObject(source.name);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = modelToWreck.GetColumn(3);
                part.transform.localRotation = modelToWreck.rotation;
                part.transform.localScale = modelToWreck.lossyScale;
                var filter = part.AddComponent<MeshFilter>();
                filter.sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                string path = WRECK_MATERIAL_FOLDER + source.sharedMaterial.name + "_Wreck.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(source.sharedMaterial); AssetDatabase.CreateAsset(material, path); }
                material.CopyPropertiesFromMaterial(source.sharedMaterial);
                material.shader = wreckShader; material.name = source.sharedMaterial.name + "_Wreck";
                EditorUtility.SetDirty(material);
                renderer.sharedMaterial = material;
                renderers.Add(renderer); filters.Add(filter);
            }
            var view = root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "UnitWreckView");
            Replace(view, "meshRenderers", renderers.ToArray());
            Replace(view, "meshFilters", filters.ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, WRECK);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Drops missing entries (stripes from an earlier run) and appends the current stripes.
    private static void Replace(Object component, string field, Object[] values)
    {
        var serialized = new SerializedObject(component);
        var array = serialized.FindProperty(field);
        for (int i = array.arraySize - 1; i >= 0; i--)
        {
            var value = array.GetArrayElementAtIndex(i).objectReferenceValue;
            if (value == null || value.name.StartsWith(PREFIX)) array.DeleteArrayElementAtIndex(i);
        }
        foreach (var value in values)
        {
            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
