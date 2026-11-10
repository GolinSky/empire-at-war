using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RepairRebelStationDomeUv
{
    private const string MODELS = "Assets/Art/Models/SpaceStations/RebelSpaceStation/";
    private const string PREFABS = "Assets/Prefabs/Models/Stations/Rebellion/";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station UV repair requires Edit Mode.");
        for (int level = 1; level <= 5; level++)
        {
            string name = "RebelSpaceStationLevel" + level;
            string folder = MODELS + "Level" + level + "/";
            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(folder + name + ".fbx");
            var renderer = raw.GetComponentsInChildren<MeshRenderer>(true).Single(r => r.name == "Level_0" + level);
            var source = renderer.GetComponent<MeshFilter>().sharedMesh;
            var points = source.vertices.Select(renderer.transform.TransformPoint).ToArray();
            var original = source.uv;
            var uv = source.uv;
            int changed = 0;
            // Map the entire dome to the same mirrored quadrant. Copying one strip
            // leaves its panel bands discontinuous with the surrounding UV islands.
            float[] heights = { 5.03991f, 4.66590f, 4.14595f, 3.46180f, 3.02394f };
            float[] radii = { 1.25130f, 2.15927f, 3.03669f, 3.65367f, 3.75972f };
            var onShell = points.Select(p => Enumerable.Range(0, heights.Length).Any(k =>
                Mathf.Abs(p.y - heights[k]) < .00003f &&
                Mathf.Abs(new Vector2(p.x, p.z).magnitude - radii[k]) < .00003f)).ToArray();
            var triangles = source.triangles;
            var dome = new System.Collections.Generic.HashSet<int>();
            var reference = new System.Collections.Generic.HashSet<int>();
            int domeTriangles = 0;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                if (!onShell[a] || !onShell[b] || !onShell[c] ||
                    Mathf.Max(points[a].y, points[b].y, points[c].y) - Mathf.Min(points[a].y, points[b].y, points[c].y) < .00003f) continue;
                dome.UnionWith(new[] { a, b, c });
                if (new[] { a, b, c }.All(j => points[j].x >= -.00001f && points[j].z >= -.00001f))
                    reference.UnionWith(new[] { a, b, c });
                domeTriangles++;
            }
            if (domeTriangles < 180) throw new InvalidOperationException("Dome shell not found: " + name);
            // Higher levels have cutouts/bevels in the same UV islands. Include their
            // connected vertices so a repair does not stop at a cutout triangle.
            bool expanded;
            do
            {
                expanded = false;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    var face = new[] { triangles[t], triangles[t + 1], triangles[t + 2] };
                    if (!face.Any(dome.Contains)) continue;
                    foreach (int index in face) expanded |= dome.Add(index);
                }
            } while (expanded);
            foreach (int i in dome)
            {
                var mirrored = new Vector3(Mathf.Abs(points[i].x), points[i].y, Mathf.Abs(points[i].z));
                if (mirrored.y < heights[4] - .00003f || mirrored.y > heights[0] + .00003f)
                    throw new InvalidOperationException("Dome island extends outside shell: " + name + " vertex " + i);
                float sector = Mathf.Atan2(mirrored.z, mirrored.x) * Mathf.Rad2Deg / 15f;
                int column = Mathf.Min(5, Mathf.FloorToInt(sector));
                int row = 0;
                while (row < 3 && mirrored.y < heights[row + 1]) row++;
                Vector2 Sample(int ring, int step)
                {
                    float angle = step * 15f * Mathf.Deg2Rad;
                    var point = new Vector3(radii[ring] * Mathf.Cos(angle), heights[ring], radii[ring] * Mathf.Sin(angle));
                    var matches = reference.Where(j => Vector3.Distance(points[j], point) < .00003f).OrderBy(j => j).ToArray();
                    if (matches.Length == 0 || matches.Any(j => Vector2.Distance(original[j], original[matches[0]]) > .00001f))
                        throw new InvalidOperationException("Dome reference plating changed: " + name + " ring " + ring + " column " + step);
                    return original[matches[0]];
                }
                uv[i] = Vector2.Lerp(Vector2.Lerp(Sample(row, column), Sample(row, column + 1), sector - column),
                    Vector2.Lerp(Sample(row + 1, column), Sample(row + 1, column + 1), sector - column),
                    Mathf.InverseLerp(heights[row], heights[row + 1], mirrored.y));
                if (uv[i] != original[i]) changed++;
            }
            if (changed == 0) throw new InvalidOperationException("No dome UV changes: " + name);
            var repaired = UnityEngine.Object.Instantiate(source);
            repaired.name = name + "Hull";
            repaired.uv = uv;
            repaired.RecalculateTangents();
            string meshPath = folder + repaired.name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) { AssetDatabase.CreateAsset(repaired, meshPath); saved = repaired; }
            else { EditorUtility.CopySerialized(repaired, saved); UnityEngine.Object.DestroyImmediate(repaired); EditorUtility.SetDirty(saved); }
            string path = PREFABS + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var filter = root.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "Level_0" + level);
                filter.sharedMesh = saved;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return "Applied continuous mirrored dome UVs to five derived hull meshes, including higher-level cutouts. Original FBXs unchanged.";
    }
}
