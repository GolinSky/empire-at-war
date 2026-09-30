using System.Collections.Generic;
using EmpireAtWar.Models.FogOfWar;
using Unity.Collections;
using UnityEngine;

namespace ViewComponents
{
    public class FogOfWarSystem : MonoBehaviour, IFogOfWarSystem
    {
        private const int MAX_TEXTURE_RESOLUTION = 512;
        private const float BYTE_MAX = 255f;

        
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private Renderer fogRenderer;
        [Header("Fog Map Settings")]
        [Tooltip("Resolution of the dynamic mask texture")]
        public int textureResolution = 256;

        [Tooltip("Automatically match Map Size and Center to the Renderer's bounds on start.")]
        public bool autoDetectBounds = true;

        [Tooltip("Total size of the map in world units (if auto-detect is off)")]
        public Vector2 mapWorldSize = new Vector2(200f, 200f);
        [Tooltip("Center of the map in world units (if auto-detect is off)")]
        public Vector3 mapCenter = Vector3.zero;

        [Header("UV Orientation")]
        [Tooltip("Flip X axis mapping. Unity's default Plane mesh needs this ON.")]
        public bool flipX = true;
        [Tooltip("Flip Z axis mapping. Unity's default Plane mesh needs this ON.")]
        public bool flipZ = true;

        [Header("Update Settings")]
        public float updateInterval = 0.1f;
        public float fadeSpeed = 3f;
        [Range(0f, 1f)]
        [Tooltip("Width of the feathered border relative to vision radius. The configured radius remains 50% visible.")]
        public float edgeSoftness = 0.25f;
        [Tooltip("If true, already visited areas will remain dimly visible. If false, fog completely returns when sources leave.")]
        public bool keepHistory;

        // Represents a single area of vision
        public class VisionSource
        {
            public Transform transform;
            public float radius;
            public float intensity;
        }

        private Texture2D _fogTexture;
        private FogVisibilityGridModel _grid;

        private List<VisionSource> _activeSources = new List<VisionSource>();
        private float _timer;
        private Material _fogMaterial;

        /// <summary>Stretches the fog plane and its mask for a larger battlefield; call before Start.</summary>
        public void ScaleArea(float scale)
        {
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(scale, 1f, scale));
            textureResolution = Mathf.Min(MAX_TEXTURE_RESOLUTION, Mathf.RoundToInt(textureResolution * scale));
        }

        // Base visibility for areas we've already explored (if keepHistory is true)

        private void Start()
        {
            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            if (autoDetectBounds)
            {
                mapCenter = fogRenderer.bounds.center;
                mapWorldSize = new Vector2(
                    meshBounds.size.x * Mathf.Abs(transform.lossyScale.x),
                    meshBounds.size.z * Mathf.Abs(transform.lossyScale.z));
                if (mapWorldSize.y < 0.1f)
                    mapWorldSize.y = meshBounds.size.y * Mathf.Abs(transform.lossyScale.y);
            }

            _fogMaterial = fogRenderer.material;
            // The shader reads only .r; a one-byte mask written in place avoids building
            // and copying a full Color[] of the map every frame the fog fades.
            _fogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.R8, false);
            _fogTexture.wrapMode = TextureWrapMode.Clamp;
            _fogTexture.filterMode = FilterMode.Bilinear;

            _grid = new FogVisibilityGridModel(textureResolution);
            NativeArray<byte> pixels = _fogTexture.GetPixelData<byte>(0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = 0;
            }

            _fogTexture.Apply();
            _fogMaterial.SetTexture("_MainTex", _fogTexture);
        }

        // Renderer.material returns an instance this component owns; the mask texture is created here too.
        private void OnDestroy()
        {
            Destroy(_fogMaterial);
            Destroy(_fogTexture);
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            // Re-calculate the target pixels at fixed intervals for performance
            if (_timer >= updateInterval)
            {
                _timer = 0;
                UpdateFogTargets();
            }

            if (_grid.Fade(fadeSpeed * Time.deltaTime))
            {
                NativeArray<byte> pixels = _fogTexture.GetPixelData<byte>(0);
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = (byte)(_grid.GetVisibility(i) * BYTE_MAX + 0.5f);
                }

                _fogTexture.Apply();
            }
        }

        private void UpdateFogTargets()
        {
            _grid.ResetTargets(keepHistory);

            // Cleanup destroyed objects automatically
            _activeSources.RemoveAll(s => s.transform == null);

            foreach (var source in _activeSources)
            {
                Vector2Int pixel = PositionToPixel(source.transform.position, out bool inside);
                if (!inside) continue;
                _grid.Reveal(pixel.x, pixel.y, RadiusToPixels(source.radius), edgeSoftness, source.intensity);
            }
        }

        private int RadiusToPixels(float radius)
        {
            // Prevent division by zero
            float safeMapX = Mathf.Max(mapWorldSize.x, 0.1f);
            float safeMapY = Mathf.Max(mapWorldSize.y, 0.1f);
            int radiusPxX = Mathf.RoundToInt(radius / safeMapX * textureResolution);
            int radiusPxY = Mathf.RoundToInt(radius / safeMapY * textureResolution);

            // Ensure a minimum of at least 1 pixel radius if there is supposed to be a hole
            return Mathf.Max(1, Mathf.Max(radiusPxX, radiusPxY));
        }

        // ===================================
        // PUBLIC API
        // ===================================

        /// <summary>
        /// Registers a transform as a continuous vision source. 
        /// You only need to call this ONCE per ship/unit when it spawns.
        /// </summary>
        public void RegisterVisionSource(Transform targetTransform, float radius, float intensity = 1.0f)
        {
            if (targetTransform == null) return;

            // Check if already registered
            if (_activeSources.Exists(s => s.transform == targetTransform)) return;

            _activeSources.Add(new VisionSource { transform = targetTransform, radius = radius, intensity = intensity });
        }

        /// <summary>
        /// Manually unregister a vision source. This happens automatically if the Transform is destroyed.
        /// </summary>
        public void UnregisterVisionSource(Transform targetTransform)
        {
            if (targetTransform == null) return;
            _activeSources.RemoveAll(s => s.transform == targetTransform);
        }

        private Vector2Int PositionToPixel(Vector3 worldPos, out bool inside)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            float localBoundsExtents = meshFilter.sharedMesh.bounds.extents.x;

            float normalizedX = (localPos.x + localBoundsExtents) / (localBoundsExtents * 2f);
            float localDepth = (Mathf.Abs(localPos.z) < 0.001f && Mathf.Abs(localPos.y) > 0.001f) ? localPos.y : localPos.z;
            float normalizedZ = (localDepth + localBoundsExtents) / (localBoundsExtents * 2f);

            if (flipX) normalizedX = 1f - normalizedX;
            if (flipZ) normalizedZ = 1f - normalizedZ;

            inside = normalizedX >= 0f && normalizedX <= 1f && normalizedZ >= 0f && normalizedZ <= 1f;
            return new Vector2Int(Mathf.RoundToInt(normalizedX * textureResolution),
                Mathf.RoundToInt(normalizedZ * textureResolution));
        }

        /// <summary>
        /// Checks the current fog density at a specific world coordinate.
        /// Useful for disabling the rendering of enemy ships that fall into unseen fog areas!
        /// Returns 1.0 if fully revealed, 0.0 if not.
        /// </summary>
        public float GetVisibilityAtPosition(Vector3 worldPos)
        {
            Vector2Int pixel = PositionToPixel(worldPos, out _);
            return _grid.GetVisibility(pixel.x, pixel.y);
        }

        /// <summary>
        /// Helper to quickly query if an object is hidden by fog.
        /// </summary>
        public bool IsHidden(Vector3 worldPos, float threshold = 0.1f)
        {
            return GetVisibilityAtPosition(worldPos) < threshold;
        }
    }
}
