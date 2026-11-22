using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes the Viscount team masks: one worn paint circle on each hull side (port and starboard), nothing else.
/// Wear: ragged edge, paint chipped off by noise (more toward the rim), and darker grime in the albedo
/// thins the paint.
/// The source textures carry no team alpha, so Prepare.py's masks are all white (whole hull recolored).
/// Each texel takes the value of the hull point it covers, so the circle lands on the hull regardless of
/// how the UV islands are laid out; mirrored halves that share UVs agree.
/// Run after BuildPrototype/BuildDreadnought:
///   unity command script run --file Tools/Blender/Viscount/TeamMask.cs --entry ViscountTeamMask.Bake
/// </summary>
public static class ViscountTeamMask
{
    private const string MODELS = "Assets/Prefabs/Models/Ships/Rebellion/Models/";
    private const string TEXTURES = "Assets/Art/Textures/Models/RebellionShips/";
    private const string HULL_TOKEN = "_Viscount";
    private const float CIRCLE_RADIUS_HEIGHTS = 0.125f; // circle radius as a share of hull height
    private const float CIRCLE_UP_HEIGHTS = 0.15f;      // circle centre above mid-hull, share of height
    private const float CIRCLE_FORWARD_LENGTHS = 0.05f; // circle centre ahead of mid-hull, share of length
    private const float SIDE_MIN_FACING = 0.3f;         // |normal.x| a surface needs to count as hull side
    private const float EDGE_FRACTION = 0.08f;          // soft edge width, share of the radius
    private const int DILATE_PIXELS = 4;                // grow painted texels past UV seams
    private const float EDGE_RAGGED_RADII = 0.1f;       // edge wobble, share of the radius
    private const float CHIP_SCALE = 5f;                // chip patches across one radius
    private const float SPECK_SCALE = 22f;              // fine specks across one radius
    private const float CHIP_CENTRE = 0.25f;            // chip threshold at the circle centre (higher = more bare metal)
    private const float CHIP_RIM = 0.52f;               // chip threshold at the rim
    private const float CHIP_SOFTNESS = 0.05f;
    private const float GRIME_FLOOR = 0.6f;             // paint left on the darkest albedo texels
    private const float GRIME_CLEAN_LUMINANCE = 0.65f;  // albedo luminance treated as clean

    public static string Bake()
    {
        var report = new StringBuilder();
        foreach (string variant in new[] { "ViscountPrototype", "ViscountDreadnought" })
            BakeVariant(variant, report);
        return report.ToString();
    }

    private static void BakeVariant(string variant, StringBuilder report)
    {
        var targets = new Dictionary<string, (float[] value, bool[] written, int w, int h)>();
        foreach (string guid in AssetDatabase.FindAssets("_TeamMask t:Texture2D", new[] { TEXTURES + variant }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Source PNG size, not the imported size, so texels line up with the file.
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(path));
            targets[path] = (new float[source.width * source.height], new bool[source.width * source.height], source.width, source.height);
            Object.DestroyImmediate(source);
        }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + variant + ".prefab"));
        try
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
            Bounds hull = new Bounds(root.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
            foreach (MeshRenderer r in renderers)
                foreach (Vector3 v in r.GetComponent<MeshFilter>().sharedMesh.vertices)
                    hull.Encapsulate(root.transform.InverseTransformPoint(r.transform.TransformPoint(v)));
            var circle = new Circle
            {
                Centre = new Vector2(hull.size.y * CIRCLE_UP_HEIGHTS, hull.size.z * CIRCLE_FORWARD_LENGTHS),
                Radius = hull.size.y * CIRCLE_RADIUS_HEIGHTS,
            };

            foreach (MeshRenderer r in renderers)
            {
                Mesh mesh = r.GetComponent<MeshFilter>().sharedMesh;
                Vector2[] uv = mesh.uv;
                Vector3[] local = mesh.vertices.Select(v => root.transform.InverseTransformPoint(r.transform.TransformPoint(v)) - hull.center).ToArray();
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    Material material = r.sharedMaterials[s];
                    Texture mask = material.HasProperty("_TeamMaskMap") ? material.GetTexture("_TeamMaskMap") : null;
                    if (mask == null || !mask.name.Contains(HULL_TOKEN) || mask.name.Contains("Greeble")) continue;
                    var t = targets[AssetDatabase.GetAssetPath(mask)];
                    int[] tris = mesh.GetTriangles(s);
                    for (int i = 0; i < tris.Length; i += 3)
                        Raster(uv, local, tris[i], tris[i + 1], tris[i + 2], circle, t.value, t.written, t.w, t.h);
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        foreach (var (path, t) in targets.Select(kv => (kv.Key, kv.Value)))
        {
            Dilate(t.value, t.written, t.w, t.h);
            ApplyGrime(path, t.value);
            var pixels = t.value.Select(m => (byte)Mathf.RoundToInt(m * 255f)).ToArray();
            var output = new Texture2D(t.w, t.h, TextureFormat.R8, false);
            output.SetPixelData(pixels, 0);
            File.WriteAllBytes(path, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            report.AppendLine($"{Path.GetFileName(path)}: coverage {t.value.Average() * 100:F2}%");
        }
    }

    private struct Circle
    {
        public Vector2 Centre;  // (y, z) in hull space, seen from the side
        public float Radius;

        public float Value(Vector3 p)
        {
            // Circle-local coordinates in radii; x is ignored, so both sides wear the same way.
            Vector2 q = (new Vector2(p.y, p.z) - Centre) / Radius;
            float r = q.magnitude;
            if (r > 1.5f) return 0f;
            float ragged = r + (Fbm(q * 3f + new Vector2(17.3f, 4.1f)) - 0.5f) * 2f * EDGE_RAGGED_RADII;
            float shape = Mathf.Clamp01((1f - ragged) / EDGE_FRACTION + 0.5f);
            float threshold = Mathf.Lerp(CHIP_CENTRE, CHIP_RIM, Mathf.Clamp01(r * r));
            float chips = Fbm(q * CHIP_SCALE) * 0.75f + Noise(q * SPECK_SCALE) * 0.25f;
            float kept = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(threshold - CHIP_SOFTNESS, threshold + CHIP_SOFTNESS, chips));
            float density = 0.75f + 0.25f * Noise(q * SPECK_SCALE * 0.5f + new Vector2(3.7f, 9.2f));
            return shape * kept * density;
        }
    }

    // Darker albedo texels (grime, panel gaps) keep less paint.
    private static void ApplyGrime(string maskPath, float[] value)
    {
        var albedo = new Texture2D(2, 2);
        albedo.LoadImage(File.ReadAllBytes(maskPath.Replace("_TeamMask", "")));
        Color32[] rgb = albedo.GetPixels32();
        Object.DestroyImmediate(albedo);
        if (rgb.Length != value.Length) return;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] <= 0f) continue;
            Color c = rgb[i];
            float luminance = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            value[i] *= Mathf.Lerp(GRIME_FLOOR, 1f, Mathf.Clamp01(luminance / GRIME_CLEAN_LUMINANCE));
        }
    }

    private static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }

    private static float Noise(Vector2 p)
    {
        int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
        float fx = p.x - x, fy = p.y - y;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(Hash(x, y), Hash(x + 1, y), fx), Mathf.Lerp(Hash(x, y + 1), Hash(x + 1, y + 1), fx), fy);
    }

    private static float Fbm(Vector2 p) => Noise(p) * 0.57f + Noise(p * 2.03f + new Vector2(5.2f, 1.3f)) * 0.29f + Noise(p * 4.01f + new Vector2(2.9f, 8.1f)) * 0.14f;

    private static void Raster(Vector2[] uv, Vector3[] pos, int a, int b, int c, Circle circle, float[] value, bool[] written, int w, int h)
    {
        // Tiled UVs (v in -1..0 or 3..4) share one texture tile.
        Vector2 tile = new Vector2(Mathf.Floor(Mathf.Min(uv[a].x, uv[b].x, uv[c].x)), Mathf.Floor(Mathf.Min(uv[a].y, uv[b].y, uv[c].y)));
        Vector2 size = new Vector2(w, h);
        Vector2 pa = Vector2.Scale(uv[a] - tile, size), pb = Vector2.Scale(uv[b] - tile, size), pc = Vector2.Scale(uv[c] - tile, size);
        float area = (pb.x - pa.x) * (pc.y - pa.y) - (pc.x - pa.x) * (pb.y - pa.y);
        if (Mathf.Abs(area) < 1e-8f) return;
        // Only surfaces facing outboard, so the circle stays on the hull side and off the deck and keel.
        Vector3 normal = Vector3.Cross(pos[b] - pos[a], pos[c] - pos[a]).normalized;
        Vector3 centroid = (pos[a] + pos[b] + pos[c]) / 3f;
        bool side = Mathf.Abs(normal.x) >= SIDE_MIN_FACING && Mathf.Sign(normal.x) == Mathf.Sign(centroid.x);
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, pb.x, pc.x))), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(pa.x, pb.x, pc.x)));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, pb.y, pc.y))), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(pa.y, pb.y, pc.y)));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float wa = ((pb.x - p.x) * (pc.y - p.y) - (pc.x - p.x) * (pb.y - p.y)) / area;
                float wb = ((pc.x - p.x) * (pa.y - p.y) - (pa.x - p.x) * (pc.y - p.y)) / area;
                float wc = 1f - wa - wb;
                if (wa < -1e-4f || wb < -1e-4f || wc < -1e-4f) continue;
                int i = y * w + x;
                value[i] = Mathf.Max(written[i] ? value[i] : 0f, side ? circle.Value(pos[a] * wa + pos[b] * wb + pos[c] * wc) : 0f);
                written[i] = true;
            }
    }

    private static void Dilate(float[] value, bool[] written, int w, int h)
    {
        for (int pass = 0; pass < DILATE_PIXELS; pass++)
        {
            var next = (bool[])written.Clone();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (written[i]) continue;
                    foreach (int j in new[] { x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1 })
                        if (j >= 0 && written[j]) { value[i] = value[j]; next[i] = true; break; }
                }
            System.Array.Copy(next, written, written.Length);
        }
    }
}
