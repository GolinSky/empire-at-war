using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RepairRebelStationDomeUv
{
    private const string MODELS = "Assets/Art/Models/SpaceStations/RebelSpaceStation/";
    private const string PREFABS = "Assets/Prefabs/Models/Stations/";

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
            // The source's 90–120 degree dome strip samples an unrelated dark atlas island.
            // Reuse the identical curved plating at +90 degrees; retain the original FBX.
            for (int i = 0; i < uv.Length; i++)
            {
                if (original[i].x <= .169f || original[i].x >= .296f || original[i].y <= -.867f || original[i].y >= -.587f
                    || points[i].y <= 3 || points[i].y >= 5.05f) continue;
                var rotated = new Vector3(-points[i].z, points[i].y, points[i].x);
                var matches = Enumerable.Range(0, points.Length).Where(j => Vector3.Distance(points[j], rotated) < .00003f
                    && original[j].x > .35f && original[j].x < .46f && original[j].y > -.3f && original[j].y < 0).ToArray();
                if (matches.Length == 0 || matches.Any(j => Vector2.Distance(original[j], original[matches[0]]) > .00001f))
                    throw new InvalidOperationException("Dome reference plating changed: " + name + " vertex " + i);
                uv[i] = original[matches[0]];
                changed++;
            }
            if (changed != 15) throw new InvalidOperationException("Expected 15 dome UV vertices: " + name + ", found " + changed);
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
        return "Repaired 15 UV vertices / 16 dome triangles per level in five derived hull meshes. Original FBXs unchanged.";
    }
}
