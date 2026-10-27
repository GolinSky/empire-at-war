using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyRecalcitrantUnityGeometry
{
    const string TASK = "Temp/RecalcitrantImport/";
    const string MODELS = "Assets/Art/Models/SeparatistShips/Recalcitrant/";
    const string MATERIALS = "Assets/Art/Materials/Models/SeparatistShips/Recalcitrant/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Recalcitrant.prefab";
    const float TOLERANCE = .00002f;
    const float HIDDEN_SHADOW_UV_TOLERANCE = .001f;
    const float POSITION_CELL_SIZE = .001f;

    public static string Main()
    {
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var sourceGeometry = JObject.Parse(File.ReadAllText(TASK + "SourceGeometry.json"));
        var fbxGeometry = JObject.Parse(File.ReadAllText(TASK + "FbxGeometry.json"));
        var results = new JObject();
        foreach (var model in ((JObject)report["models"]).Properties())
            results[model.Name] = VerifyModel(model.Name, (JObject)model.Value,
                (JObject)sourceGeometry[model.Name], (JObject)fbxGeometry[model.Name]);
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        Check(visual != null, "Visual prefab exists.");
        Check(visual.transform.localPosition == Vector3.zero && visual.transform.localRotation == Quaternion.identity
            && visual.transform.localScale == Vector3.one, "Visual root has identity transform.");
        var transforms = Hierarchy(visual.transform).ToArray();
        var visualRows = new JArray();
        foreach (var model in ((JObject)report["models"]).Properties())
            visualRows.Add(VerifyVisualModel(transforms.Single(t => t != visual.transform && t.name == model.Name), model.Name, (JObject)model.Value));
        var visible = transforms.SelectMany(t => Attached<MeshRenderer>(t.gameObject)).ToArray();
        var expectedNames = ((JObject)report["models"]).Properties().SelectMany(model =>
            ((JObject)model.Value["before"]["meshes"]).Properties().Where(mesh =>
                !(bool)mesh.Value["hidden"] && !mesh.Value["materials"].Values<string>().Any(IsHelperMaterial))
                .Select(mesh => mesh.Name)).OrderBy(name => name).ToArray();
        Check(visible.All(renderer => renderer.enabled), "Visual contains only enabled source surfaces.");
        Check(visible.Select(renderer => renderer.name).OrderBy(name => name).SequenceEqual(expectedNames),
            "Visual surface names/counts match source hull and eight turrets.");
        for (int index = 1; index <= 8; index++)
        {
            string name = "RecalcitrantTurret" + index.ToString("00");
            var turret = transforms.Single(t => t.name == name);
            Check(turret.parent.name == "TL_" + index.ToString("00") && turret.localPosition == Vector3.zero,
                "Turret is attached at its source socket: " + name);
        }
        var result = new JObject { ["success"] = true, ["models"] = results,
            ["visibleMeshes"] = visible.Length, ["visualRootIdentity"] = true,
            ["visualBakeChecks"] = visualRows,
            ["sourceCornerSampleStride"] = 1 };
        File.WriteAllText(TASK + "UnityGeometryReport.json", result.ToString());
        return result.ToString();
    }

    static JObject VerifyModel(string name, JObject conversion, JObject sourceGeometry, JObject fbxGeometry)
    {
        string path = MODELS + name + ".fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Check(model != null, "FBX exists: " + name);
        Check(Mathf.Abs(((ModelImporter)AssetImporter.GetAtPath(path)).globalScale - .02f) < TOLERANCE,
            "FBX source scale: " + name);
        var transforms = Hierarchy(model.transform).ToArray();
        var renderers = transforms.SelectMany(t => Attached<Renderer>(t.gameObject))
            .Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
        var meshes = (JObject)conversion["before"]["meshes"];
        var exportedMeshes = (JObject)conversion["after"]["meshes"];
        Check(renderers.Length == meshes.Count, "FBX mesh count: " + name);
        var rows = new JArray();
        foreach (var source in meshes.Properties())
        {
            var renderer = renderers.Single(r => r.name == (string)source.Value["exported_name"]);
            var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                : Attached<MeshFilter>(renderer.gameObject).Single().sharedMesh;
            var materials = source.Value["materials"].Values<string>().ToArray();
            bool collisionException = name == "Recalcitrant" && source.Name == "Col";
            var expectedMesh = collisionException && mesh.triangles.Length / 3 == (int)source.Value["triangles"]
                ? source.Value : exportedMeshes[source.Name];
            Check(renderer.sharedMaterials.All(material => material != null), "No missing material remaps: " + name + "/" + source.Name);
            Check(renderer.sharedMaterials.Select(m => m.name).SequenceEqual(materials), "Material slots: " + name + "/" + source.Name);
            Check(mesh.subMeshCount == materials.Length, "Submesh count: " + name + "/" + source.Name);
            for (int slot = 0; slot < materials.Length; slot++)
            {
                Check(AssetDatabase.GetAssetPath(renderer.sharedMaterials[slot]) == MATERIALS + materials[slot] + ".mat",
                    "External material remap: " + name + "/" + source.Name);
                Check(mesh.GetTriangles(slot).Length / 3 == (int)expectedMesh["slot_triangles"][slot],
                    "FBX per-slot triangles: " + name + "/" + source.Name + " expected=" + expectedMesh["slot_triangles"][slot] + " actual=" + mesh.GetTriangles(slot).Length / 3);
            }
            int triangles = mesh.triangles.Length / 3;
            Check(triangles == (int)expectedMesh["triangles"], "FBX triangle count: " + name + "/" + source.Name);
            Check(mesh.uv.Length == mesh.vertexCount, "UV channel: " + name + "/" + source.Name);
            bool retessellated = collisionException && triangles != (int)source.Value["triangles"];
            var corners = (JArray)(retessellated ? fbxGeometry[source.Name] : sourceGeometry[source.Name]);
            Check(corners.Count == triangles * 3, "Every triangle corner recorded: " + name + "/" + source.Name);
            var error = GeometryError(renderer.transform, mesh, corners);
            float uvTolerance = (bool)source.Value["hidden"] && (string)source.Value["shader"] == "MeshShadowVolume.fx"
                ? HIDDEN_SHADOW_UV_TOLERANCE : TOLERANCE;
            Check(error.x <= TOLERANCE && error.y <= uvTolerance,
                "Geometry/UV displacement: " + name + "/" + source.Name + " position=" + error.x.ToString("R") + " uv=" + error.y.ToString("R"));
            rows.Add(new JObject { ["name"] = source.Name, ["triangles"] = triangles, ["vertices"] = mesh.vertexCount,
                ["checkedCorners"] = corners.Count, ["maximumPositionError"] = error.x, ["maximumUvError"] = error.y,
                ["uvTolerance"] = uvTolerance, ["uvMatchesSource"] = error.y <= TOLERANCE,
                ["materials"] = new JArray(materials), ["geometryBasis"] = retessellated
                    ? "FBX round trip; documented hidden collision retessellation" : "Original imported ALO" });
        }
        float boneError = 0;
        var bones = (JObject)conversion["before"]["bones"];
        Check(conversion["source_bone_names"].Values<string>().OrderBy(b => b).SequenceEqual(bones.Properties().Select(b => b.Name).OrderBy(b => b)),
            "All original binary bone names retained: " + name);
        foreach (var source in bones.Properties())
        {
            var bone = transforms.Single(t => t.name == source.Name && !Attached<Renderer>(t.gameObject).Any());
            boneError = Mathf.Max(boneError, Vector3.Distance(bone.position, Position(source.Value["head"])));
            string parent = (string)source.Value["parent"];
            Check(parent == null || bone.parent.name == parent, "Bone parent: " + name + "/" + source.Name);
            Check(parent != null || bones.Property(bone.parent.name) == null, "Source root remains outside bone hierarchy: " + name + "/" + source.Name);
        }
        Check(boneError <= TOLERANCE, "Bone displacement: " + name + " " + boneError);
        return new JObject { ["meshes"] = rows, ["meshCount"] = rows.Count, ["bones"] = bones.Count,
            ["maximumBoneError"] = boneError, ["tolerance"] = TOLERANCE,
            ["totalTriangles"] = rows.Sum(row => (int)row["triangles"]) };
    }

    static JObject VerifyVisualModel(Transform visualRoot, string name, JObject conversion)
    {
        var rawRoot = AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + name + ".fbx").transform;
        var rawRenderers = Hierarchy(rawRoot).SelectMany(t => Attached<Renderer>(t.gameObject)).ToArray();
        var visualRenderers = Hierarchy(visualRoot).SelectMany(t => Attached<MeshRenderer>(t.gameObject)).ToArray();
        var rows = new JArray();
        foreach (var source in ((JObject)conversion["before"]["meshes"]).Properties().Where(mesh =>
            !(bool)mesh.Value["hidden"] && !mesh.Value["materials"].Values<string>().Any(IsHelperMaterial)))
        {
            var raw = rawRenderers.Single(r => r.name == (string)source.Value["exported_name"]);
            var actual = visualRenderers.Single(r => r.name == source.Name);
            var rawMesh = raw is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                : Attached<MeshFilter>(raw.gameObject).Single().sharedMesh;
            var actualMesh = Attached<MeshFilter>(actual.gameObject).Single().sharedMesh;
            var expectedVertices = rawMesh.vertices.Select(v => rawRoot.InverseTransformPoint(raw.transform.TransformPoint(v))).ToArray();
            var actualVertices = actualMesh.vertices.Select(v => visualRoot.InverseTransformPoint(actual.transform.TransformPoint(v))).ToArray();
            Check(expectedVertices.Length == actualVertices.Length, "Visual baked vertex count: " + name + "/" + source.Name);
            float error = expectedVertices.Select((v, index) => Vector3.Distance(v, actualVertices[index])).Max();
            Check(error <= TOLERANCE, "Visual bake displacement: " + name + "/" + source.Name + " position=" + error.ToString("R"));
            Check(rawMesh.uv.SequenceEqual(actualMesh.uv), "Visual baked UVs: " + name + "/" + source.Name);
            Check(rawMesh.triangles.SequenceEqual(actualMesh.triangles), "Visual baked triangles: " + name + "/" + source.Name);
            rows.Add(new JObject { ["name"] = source.Name, ["checkedVertices"] = actualVertices.Length, ["maximumBakeDisplacement"] = error });
        }
        return new JObject { ["name"] = name, ["meshes"] = rows };
    }

    static Vector2 GeometryError(Transform transform, Mesh mesh, JArray corners)
    {
        var vertices = mesh.vertices.Select(transform.TransformPoint).ToArray();
        var uv = mesh.uv;
        var lookup = new Dictionary<Vector3Int, List<int>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            var key = Cell(vertices[i]);
            if (!lookup.TryGetValue(key, out var list)) lookup[key] = list = new List<int>();
            list.Add(i);
        }
        var error = Vector2.zero;
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
            Check(!float.IsPositiveInfinity(bestUv), "Missing source geometry corner: " + transform.name);
            error.x = Mathf.Max(error.x, bestPosition);
            error.y = Mathf.Max(error.y, bestUv);
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

    static bool IsHelperMaterial(string name) => name.Contains("collision") || name.Contains("shadow");
    static Vector3 Position(JToken p) => new Vector3(-(float)p[0], (float)p[2], -(float)p[1]) * .02f;
    static Vector3Int Cell(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x / POSITION_CELL_SIZE), Mathf.FloorToInt(p.y / POSITION_CELL_SIZE), Mathf.FloorToInt(p.z / POSITION_CELL_SIZE));
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
