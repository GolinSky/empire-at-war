using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Editor.AloImport
{
    public sealed class AloGeometrySource : IDisposable
    {
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        private readonly List<MeshFilter> _filters = new List<MeshFilter>();
        public GameObject Root { get; }
        public IReadOnlyList<MeshRenderer> Renderers => _renderers;
        public IReadOnlyList<MeshFilter> Filters => _filters;
        public Bounds Bounds { get; private set; }

        public AloGeometrySource(JObject recipe, Scene scene)
        {
            JObject generation = (JObject)recipe["derivatives"]["generation"];
            var objects = new AloAssetObjectMap(recipe, new[] { (string)generation["sourceRole"] });
            var sourceRoot = objects.Resolve(generation["root"]) as Transform;
            if (sourceRoot == null) throw new InvalidOperationException("Derivative root must identify a saved Transform.");
            if ((string)generation["root"]["role"] != (string)generation["sourceRole"]) throw new InvalidOperationException("Derivative source/root roles differ.");
            Matrix4x4 reconstructed = Matrix4x4.TRS(sourceRoot.position, sourceRoot.rotation, sourceRoot.lossyScale);
            for (int index = 0; index < 16; index++)
                if (Mathf.Abs(reconstructed[index] - sourceRoot.localToWorldMatrix[index]) > 0.00002f) throw new InvalidOperationException("Derivative source root has nonrepresentable shear.");
            Root = new GameObject((string)recipe["modelId"] + "Geometry");
            SceneManager.MoveGameObjectToScene(Root, scene);
            try
            {
                var visual = new GameObject("VisualRoot").transform;
                visual.SetParent(Root.transform, false);
                visual.localScale = sourceRoot.lossyScale;
                visual.gameObject.SetActive(sourceRoot.gameObject.activeSelf);
                var transforms = new Dictionary<Transform, Transform> { [sourceRoot] = visual };
                var seen = new HashSet<Renderer>();
                foreach (JToken reference in generation["renderers"])
                {
                    if ((string)reference["role"] != (string)generation["sourceRole"]) throw new InvalidOperationException("Derivative renderers must belong to the declared source role.");
                    var source = objects.Resolve(reference) as MeshRenderer;
                    if (source == null) throw new InvalidOperationException("Static derivative source must identify a MeshRenderer.");
                    if (!seen.Add(source)) throw new InvalidOperationException("Duplicate derivative renderer.");
                    Transform node = CloneTransform(source.transform, transforms);
                    var components = new SerializedObject(source.gameObject).FindProperty("m_Component");
                    MeshFilter sourceFilter = null;
                    for (int index = 0; index < components.arraySize; index++)
                    {
                        Object component = components.GetArrayElementAtIndex(index).FindPropertyRelative("component").objectReferenceValue;
                        if (component is MeshFilter filter)
                        {
                            if (sourceFilter != null) throw new InvalidOperationException("Ambiguous source MeshFilter.");
                            sourceFilter = filter;
                        }
                    }
                    if (sourceFilter == null || sourceFilter.sharedMesh == null) throw new InvalidOperationException("Missing derivative source mesh.");
                    if (source.sharedMaterials.Length != sourceFilter.sharedMesh.subMeshCount || source.sharedMaterials.Any(material => material == null))
                        throw new InvalidOperationException("Derivative source material slots do not match its mesh.");
                    var meshFilter = node.gameObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = sourceFilter.sharedMesh;
                    var renderer = node.gameObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = source.sharedMaterials;
                    renderer.enabled = source.enabled;
                    renderer.shadowCastingMode = source.shadowCastingMode;
                    renderer.receiveShadows = source.receiveShadows;
                    _filters.Add(meshFilter);
                    _renderers.Add(renderer);
                }
                var points = new List<Vector3>();
                for (int index = 0; index < _renderers.Count; index++)
                    if (_renderers[index].enabled && _renderers[index].gameObject.activeInHierarchy)
                        foreach (Vector3 vertex in _filters[index].sharedMesh.vertices) points.Add(_renderers[index].transform.TransformPoint(vertex));
                if (points.Count == 0) throw new InvalidOperationException("Derivative source contains no enabled geometry.");
                var bounds = new Bounds(points[0], Vector3.zero);
                foreach (Vector3 point in points) bounds.Encapsulate(point);
                Bounds = bounds;
            }
            catch
            {
                Object.DestroyImmediate(Root);
                throw;
            }
        }

        private static Transform CloneTransform(Transform source, Dictionary<Transform, Transform> transforms)
        {
            if (transforms.TryGetValue(source, out Transform existing)) return existing;
            if (source.parent == null) throw new InvalidOperationException("Derivative renderer is outside its declared root.");
            Transform parent = CloneTransform(source.parent, transforms);
            var clone = new GameObject(source.name).transform;
            clone.SetParent(parent, false);
            clone.localPosition = source.localPosition;
            clone.localRotation = source.localRotation;
            clone.localScale = source.localScale;
            clone.gameObject.SetActive(source.gameObject.activeSelf);
            transforms.Add(source, clone);
            return clone;
        }

        public void Dispose() => Object.DestroyImmediate(Root);
    }
}
