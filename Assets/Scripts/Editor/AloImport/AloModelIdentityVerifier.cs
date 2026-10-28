using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public static class AloModelIdentityVerifier
    {
        private const float TOLERANCE = 0.00002f;
        private const float UV_TOLERANCE = 0.0000001f;

        public static void Verify(string baselinePath, string candidatePath, string inventoryPath)
        {
            Dictionary<long, Object> baseline = Objects(baselinePath);
            Dictionary<long, Object> candidate = Objects(candidatePath);
            Directory.CreateDirectory(Path.GetDirectoryName(inventoryPath));
            File.WriteAllText(inventoryPath, new JObject { ["baselinePath"] = baselinePath, ["candidatePath"] = candidatePath,
                ["baseline"] = Inventory(baseline), ["candidate"] = Inventory(candidate) }.ToString());
            if (!baseline.Keys.OrderBy(id => id).SequenceEqual(candidate.Keys.OrderBy(id => id))) Fail("persistent local-fileID set");
            foreach (var pair in baseline)
            {
                Object original = pair.Value;
                Object replacement = candidate[pair.Key];
                if (original.GetType() != replacement.GetType() || original.name != replacement.name) Fail("type/name for fileID " + pair.Key);
                if (original is Transform transform)
                {
                    var other = (Transform)replacement;
                    if (Id(transform.parent) != Id(other.parent) || Vector3.Distance(transform.localPosition, other.localPosition) > TOLERANCE ||
                        Vector3.Distance(transform.localScale, other.localScale) > TOLERANCE || Quaternion.Angle(transform.localRotation, other.localRotation) > TOLERANCE)
                        Fail("hierarchy/transform " + transform.name);
                }
                if (original is Renderer renderer)
                {
                    var other = (Renderer)replacement;
                    if (renderer.enabled != other.enabled || !renderer.sharedMaterials.Select(MaterialId).SequenceEqual(other.sharedMaterials.Select(MaterialId)))
                        Fail("material slots/visibility " + renderer.name);
                }
                if (original is MeshFilter filter && Id(filter.sharedMesh) != Id(((MeshFilter)replacement).sharedMesh)) Fail("mesh binding " + filter.name);
                if (original is Mesh mesh) VerifyMesh(mesh, (Mesh)replacement);
                if (original is SkinnedMeshRenderer skin)
                {
                    var other = (SkinnedMeshRenderer)replacement;
                    if (Id(skin.sharedMesh) != Id(other.sharedMesh) || Id(skin.rootBone) != Id(other.rootBone) || !skin.bones.Select(Id).SequenceEqual(other.bones.Select(Id)))
                        Fail("skin/bone binding " + skin.name);
                }
            }
        }

        private static JArray Inventory(Dictionary<long, Object> objects)
        {
            var result = new JArray();
            foreach (var pair in objects.OrderBy(item => item.Key))
            {
                var item = new JObject { ["fileId"] = pair.Key, ["type"] = pair.Value.GetType().FullName, ["name"] = pair.Value.name };
                if (pair.Value is Transform transform) item["parentFileId"] = Id(transform.parent);
                if (pair.Value is Renderer renderer) item["materialSlots"] = new JArray(renderer.sharedMaterials.Select(MaterialId));
                if (pair.Value is Mesh mesh) { item["vertices"] = mesh.vertexCount; item["subMeshes"] = mesh.subMeshCount; }
                result.Add(item);
            }
            return result;
        }

        private static void VerifyMesh(Mesh baseline, Mesh candidate)
        {
            if (baseline.subMeshCount != candidate.subMeshCount || baseline.vertexCount != candidate.vertexCount) Fail("mesh layout " + baseline.name);
            Vector3[] vertices = baseline.vertices;
            Vector3[] replacement = candidate.vertices;
            Vector3[] normals = baseline.normals;
            Vector3[] otherNormals = candidate.normals;
            Vector2[] uvs = baseline.uv;
            Vector2[] otherUvs = candidate.uv;
            if (normals.Length != otherNormals.Length || uvs.Length != otherUvs.Length) Fail("normal/UV layout " + baseline.name);
            for (int index = 0; index < vertices.Length; index++)
                if (Vector3.Distance(vertices[index], replacement[index]) > TOLERANCE) Fail("geometry " + baseline.name);
            for (int index = 0; index < normals.Length; index++)
                if (Vector3.Distance(normals[index], otherNormals[index]) > 0.001f) Fail("normals " + baseline.name);
            for (int index = 0; index < uvs.Length; index++)
                if (Vector2.Distance(uvs[index], otherUvs[index]) > UV_TOLERANCE) Fail("UV " + baseline.name);
            if (!baseline.colors32.SequenceEqual(candidate.colors32)) Fail("vertex colors " + baseline.name);
            Matrix4x4[] poses = baseline.bindposes;
            Matrix4x4[] otherPoses = candidate.bindposes;
            if (poses.Length != otherPoses.Length) Fail("bind pose count " + baseline.name);
            for (int index = 0; index < poses.Length; index++)
                for (int element = 0; element < 16; element++)
                    if (Mathf.Abs(poses[index][element] - otherPoses[index][element]) > TOLERANCE) Fail("bind pose " + baseline.name);
            if (!baseline.boneWeights.SequenceEqual(candidate.boneWeights)) Fail("bone weights " + baseline.name);
            Vector4[] tangents = baseline.tangents;
            Vector4[] otherTangents = candidate.tangents;
            if (tangents.Length != otherTangents.Length) Fail("tangent count " + baseline.name);
            for (int index = 0; index < tangents.Length; index++)
                if (Vector4.Distance(tangents[index], otherTangents[index]) > 0.001f) Fail("tangents " + baseline.name);
            for (int channel = 1; channel < 8; channel++)
            {
                var originalUvs = new List<Vector4>();
                var replacementUvs = new List<Vector4>();
                baseline.GetUVs(channel, originalUvs);
                candidate.GetUVs(channel, replacementUvs);
                if (originalUvs.Count != replacementUvs.Count) Fail("UV channel count " + baseline.name);
                for (int index = 0; index < originalUvs.Count; index++)
                    if (Vector4.Distance(originalUvs[index], replacementUvs[index]) > UV_TOLERANCE) Fail("UV channel " + baseline.name);
            }
            for (int slot = 0; slot < baseline.subMeshCount; slot++)
                if (baseline.GetTopology(slot) != candidate.GetTopology(slot) || !baseline.GetIndices(slot).SequenceEqual(candidate.GetIndices(slot))) Fail("slot topology " + baseline.name);
        }

        private static Dictionary<long, Object> Objects(string path)
        {
            var objects = new Dictionary<long, Object>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path)) Add(asset, objects);
            Collect(AssetDatabase.LoadAssetAtPath<GameObject>(path).transform, objects);
            return objects;
        }

        private static void Collect(Transform transform, Dictionary<long, Object> objects)
        {
            Add(transform.gameObject, objects);
            SerializedProperty components = new SerializedObject(transform.gameObject).FindProperty("m_Component");
            for (int index = 0; index < components.arraySize; index++)
                Add(components.GetArrayElementAtIndex(index).FindPropertyRelative("component").objectReferenceValue, objects);
            for (int child = 0; child < transform.childCount; child++) Collect(transform.GetChild(child), objects);
        }

        private static void Add(Object asset, Dictionary<long, Object> objects)
        {
            if (asset == null) Fail("missing imported object");
            long id = Id(asset);
            if (objects.TryGetValue(id, out Object existing) && existing != asset) Fail("duplicate imported local-fileID");
            objects[id] = asset;
        }

        private static long Id(Object asset)
        {
            if (asset == null) return 0;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long id)) Fail("nonpersistent imported object");
            return id;
        }

        private static string MaterialId(Material material)
        {
            if (material == null) Fail("missing material");
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material)) + ":" + Id(material);
        }

        private static void Fail(string detail) => throw new InvalidOperationException("UNSUPPORTED_CAPABILITY: existing FBX identity differs (" + detail + "); an explicit source-identity rebind adapter is required before publication.");
    }
}
