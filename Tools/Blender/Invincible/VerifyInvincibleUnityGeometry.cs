using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyInvincibleUnityGeometry
{
    const string MODEL = "Assets/Art/Models/SeparatistShips/Invincible/Invincible.fbx";
    const string MATERIALS = "Assets/Art/Materials/Models/SeparatistShips/Invincible/";
    const string TASK = "Temp/InvincibleImport/";
    const float TOLERANCE = .00001f;
    const float SHADOW_UV_TOLERANCE = .001f;
    const float POSITION_CELL_SIZE = .001f;

    public static string Main()
    {
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var expected = (JObject)report["before"];
        var geometry = JObject.Parse(File.ReadAllText(TASK + "SourceGeometry.json"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL);
        var transforms = Hierarchy(model.transform).ToArray();
        var filters = transforms.SelectMany(t => Attached<MeshFilter>(t.gameObject)).ToArray();
        var meshes = (JObject)expected["meshes"];
        var bones = (JObject)expected["bones"];
        Check(filters.Length == meshes.Count && filters.Length == 7, "Transport FBX contains seven source meshes.");
        Check(bones.Count == 85, "Source contains 85 bones including the binary-verified Root.");
        Check(Mathf.Abs(((ModelImporter)AssetImporter.GetAtPath(MODEL)).globalScale - .02f) < TOLERANCE, "Expected .02 source import scale.");
        var rows = new JArray();
        foreach (var source in meshes.Properties())
        {
            var filter = filters.Single(f => f.name == (string)source.Value["exported_name"]);
            var mesh = filter.sharedMesh;
            var materials = Attached<MeshRenderer>(filter.gameObject).Single().sharedMaterials;
            var names = source.Value["materials"].Values<string>().ToArray();
            Check(materials.Select(m => m.name).SequenceEqual(names), "Material slot names: " + source.Name);
            Check(mesh.subMeshCount == names.Length, "Material slot count: " + source.Name);
            for (int i = 0; i < names.Length; i++)
            {
                Check(AssetDatabase.GetAssetPath(materials[i]) == MATERIALS + names[i] + ".mat", "External material binding: " + source.Name);
                Check(mesh.GetTriangles(i).Length / 3 == (int)source.Value["slot_triangles"][i], "Per-slot triangle count: " + source.Name);
            }
            int triangles = mesh.triangles.Length / 3;
            Check(triangles == (int)source.Value["triangles"], "Triangle count: " + source.Name);
            Check(mesh.uv.Length == mesh.vertexCount, "UV channel: " + source.Name);
            var corners = (JArray)geometry[source.Name];
            Check(corners.Count == triangles * 3, "Source records every triangle corner: " + source.Name);
            var error = GeometryError(filter, corners);
            float uvTolerance = (string)source.Value["shader"] == "MeshShadowVolume.fx" ? SHADOW_UV_TOLERANCE : TOLERANCE;
            Check(error.x <= TOLERANCE && error.y <= uvTolerance, "Source geometry/UV displacement: " + source.Name + " " + error);
            rows.Add(new JObject { ["name"] = source.Name, ["vertices"] = mesh.vertexCount, ["triangles"] = triangles,
                ["sourceCorners"] = corners.Count, ["checkedCorners"] = corners.Count, ["runtimeVisible"] = !(bool)source.Value["hidden"],
                ["maximumPositionError"] = error.x, ["maximumUvError"] = error.y, ["uvTolerance"] = uvTolerance,
                ["uvMatchesSource"] = error.y <= TOLERANCE, ["uvRemappedCorners"] = (int)error.z, ["materials"] = new JArray(names) });
        }
        float boneError = 0;
        foreach (var source in bones.Properties())
        {
            var matches = transforms.Where(t => t.name == source.Name && !Attached<MeshFilter>(t.gameObject).Any()).ToArray();
            Check(matches.Length == 1, "Unique source bone: " + source.Name);
            var bone = matches[0];
            boneError = Mathf.Max(boneError, Vector3.Distance(bone.position, Position(source.Value["head"])));
            string parent = (string)source.Value["parent"];
            Check(parent == null || bone.parent.name == parent, "Source bone parent: " + source.Name);
        }
        Check(boneError <= TOLERANCE, "Maximum bone displacement: " + boneError);
        Check(rows.Sum(row => (int)row["triangles"]) == 21421, "Transport FBX includes 21421 triangles including processed helpers.");
        var result = new JObject { ["success"] = true, ["meshCount"] = filters.Length, ["bones"] = bones.Count,
            ["maximumBoneError"] = boneError, ["tolerance"] = TOLERANCE, ["sourceCornerSampleStride"] = 1,
            ["visibleUvsMatchSource"] = rows.Where(row => (bool)row["runtimeVisible"]).All(row => (bool)row["uvMatchesSource"]),
            ["helperUvDifference"] = "Shadow-helper UVs change during Unity FBX import; measured differences are reported per mesh. This helper is absent from runtime visual prefabs.",
            ["meshes"] = rows, ["totalTriangles"] = rows.Sum(row => (int)row["triangles"]) };
        File.WriteAllText(TASK + "UnityGeometryReport.json", result.ToString());
        return result.ToString();
    }

    static Vector3 GeometryError(MeshFilter filter, JArray corners)
    {
        var vertices = filter.sharedMesh.vertices.Select(v => filter.transform.TransformPoint(v)).ToArray();
        var uv = filter.sharedMesh.uv;
        var lookup = new Dictionary<Vector3Int, List<int>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            var key = Cell(vertices[i]);
            if (!lookup.TryGetValue(key, out var list)) lookup[key] = list = new List<int>();
            list.Add(i);
        }
        var error = Vector3.zero;
        foreach (var corner in corners)
        {
            var position = Position(corner);
            var expectedUv = new Vector2((float)corner[3], (float)corner[4]);
            var cell = Cell(position);
            float bestUv = float.PositiveInfinity, bestPosition = float.PositiveInfinity;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                if (!lookup.TryGetValue(cell + new Vector3Int(x, y, z), out var candidates)) continue;
                foreach (int candidate in candidates)
                {
                    float positionError = Vector3.Distance(position, vertices[candidate]);
                    if (positionError > TOLERANCE) continue;
                    float uvError = Vector2.Distance(expectedUv, uv[candidate]);
                    if (uvError < bestUv || uvError == bestUv && positionError < bestPosition)
                    { bestUv = uvError; bestPosition = positionError; }
                }
            }
            Check(!float.IsPositiveInfinity(bestUv), "Missing source geometry corner: " + filter.name);
            error.x = Mathf.Max(error.x, bestPosition);
            error.y = Mathf.Max(error.y, bestUv);
            if (bestUv > TOLERANCE) error.z++;
        }
        return error;
    }

    static IEnumerable<Transform> Hierarchy(Transform root)
    {
        yield return root;
        foreach (Transform child in root)
            foreach (var descendant in Hierarchy(child)) yield return descendant;
    }

    static IEnumerable<T> Attached<T>(GameObject owner) where T : Component
    {
        var array = new SerializedObject(owner).FindProperty("m_Component");
        for (int i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue is T component) yield return component;
    }

    static Vector3 Position(JToken p) => new Vector3(-(float)p[0], (float)p[2], -(float)p[1]) * .02f;
    static Vector3Int Cell(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x / POSITION_CELL_SIZE), Mathf.FloorToInt(p.y / POSITION_CELL_SIZE), Mathf.FloorToInt(p.z / POSITION_CELL_SIZE));
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
