using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Components.Environment
{
    public sealed class NebulaImpostor : IDisposable
    {
        private const int BAKE_WIDTH = 4096;
        private const int BAKE_HEIGHT = 2048;
        private const int BAKE_LAYER = 31;
        private readonly MeshRenderer _volume;
        private readonly GameObject _quad;
        private readonly MeshRenderer _renderer;
        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly RenderTexture _texture;

        public bool IsBaking { get; private set; }
        public Vector3 Forward => _quad.transform.forward;

        public NebulaImpostor(MeshRenderer volume, Shader shader)
        {
            _volume = volume;
            _texture = new RenderTexture(BAKE_WIDTH, BAKE_HEIGHT, 24, RenderTextureFormat.ARGBHalf)
            {
                name = "Match Nebula Bake", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            _texture.Create();
            _material = new Material(shader) { name = "Match Nebula Impostor", mainTexture = _texture };
            _mesh = new Mesh { name = "Match Nebula Quad" };
            _mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            _mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            _mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _mesh.RecalculateBounds();
            _quad = new GameObject("Nebula Impostor");
            _quad.transform.SetParent(volume.transform.parent, false);
            _quad.layer = volume.gameObject.layer;
            _quad.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _quad.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.enabled = false;
        }

        public void Bake()
        {
            var timer = Stopwatch.StartNew();
            Transform source = _volume.transform;
            Quaternion rotation = source.rotation;
            Bounds bounds = _volume.localBounds;
            // Project the actual transformed box into the fixed battle-camera basis.
            Bounds projected = new Bounds(Vector3.zero, Vector3.zero);
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector3 center = source.TransformPoint(bounds.center);
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                projected.Encapsulate(inverse * (source.TransformPoint(corner) - center));
            }
            float halfHeight = Mathf.Max(projected.extents.y, projected.extents.x * BAKE_HEIGHT / BAKE_WIDTH);
            _quad.transform.SetPositionAndRotation(center, rotation);
            _quad.transform.localScale = new Vector3(halfHeight * 2f * BAKE_WIDTH / BAKE_HEIGHT, halfHeight * 2f, 1);
            var cameraObject = new GameObject("Nebula Bake Camera") { hideFlags = HideFlags.HideAndDontSave };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.scene = _volume.gameObject.scene;
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = halfHeight;
            camera.aspect = (float)BAKE_WIDTH / BAKE_HEIGHT;
            camera.nearClipPlane = 1;
            camera.farClipPlane = projected.size.z + 4;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            // Background also contains stars; isolate the volume for this synchronous request.
            int originalLayer = _volume.gameObject.layer;
            _volume.gameObject.layer = BAKE_LAYER;
            camera.cullingMask = 1 << BAKE_LAYER;
            camera.allowHDR = true;
            camera.allowMSAA = false;
            camera.transform.SetPositionAndRotation(center - Forward * (projected.extents.z + 2), rotation);
            UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            IsBaking = true;
            _renderer.enabled = false;
            _volume.enabled = true;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest
                {
                    destination = _texture
                });
            }
            finally
            {
                _volume.enabled = false;
                _volume.gameObject.layer = originalLayer;
                IsBaking = false;
                DestroyOwned(cameraObject);
            }
            UnityEngine.Debug.Log($"Nebula impostor baked at {BAKE_WIDTH}x{BAKE_HEIGHT}, 128 samples in {timer.Elapsed.TotalMilliseconds:F1} ms (CPU submission).");
        }

        public void SetVisible(bool visible) => _renderer.enabled = visible;

        public void Dispose()
        {
            _texture.Release();
            DestroyOwned(_texture);
            DestroyOwned(_quad);
            DestroyOwned(_mesh);
            DestroyOwned(_material);
        }

        private static void DestroyOwned(Object owned)
        {
            if (Application.isPlaying) Object.Destroy(owned);
            else Object.DestroyImmediate(owned);
        }
    }
}
