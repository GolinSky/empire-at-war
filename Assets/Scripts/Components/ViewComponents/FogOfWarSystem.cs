using System.Collections.Generic;
using EmpireAtWar.Models.FogOfWar;
using UnityEngine;

namespace ViewComponents
{
    public class FogOfWarSystem : MonoBehaviour
    {
        private const float HISTORIC_VISIBILITY = 0.35f;
        private const int MAX_TEXTURE_RESOLUTION = 512;

        
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
        private Color[] _fogPixels;
        private Color[] _targetPixels;

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
            _fogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            _fogTexture.wrapMode = TextureWrapMode.Clamp;
            _fogTexture.filterMode = FilterMode.Bilinear;

            int totalPixels = textureResolution * textureResolution;
            _fogPixels = new Color[totalPixels];
            _targetPixels = new Color[totalPixels];
            for (int i = 0; i < totalPixels; i++)
            {
                _fogPixels[i] = Color.black;
                _targetPixels[i] = Color.black;
            }

            _fogTexture.SetPixels(_fogPixels);
            _fogTexture.Apply();
            _fogMaterial.SetTexture("_MainTex", _fogTexture);
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

            // Smoothly interpolate current pixels to target pixels
            bool changed = false;
            for (int i = 0; i < _fogPixels.Length; i++)
            {
                if (_fogPixels[i].r != _targetPixels[i].r)
                {
                    _fogPixels[i].r = Mathf.MoveTowards(_fogPixels[i].r, _targetPixels[i].r, fadeSpeed * Time.deltaTime);
                    changed = true;
                }
            }

            if (changed)
            {
                _fogTexture.SetPixels(_fogPixels);
                _fogTexture.Apply();
            }
        }

        private void UpdateFogTargets()
        {
            float baseVis = keepHistory ? HISTORIC_VISIBILITY : 0f;

            // Reset targets to base visibility (either totally unrevealed or historically revealed)
            for (int i = 0; i < _targetPixels.Length; i++)
            {
                _targetPixels[i].r = _fogPixels[i].r > 0 ? baseVis : 0f;
            }

            // Cleanup destroyed objects automatically
            _activeSources.RemoveAll(s => s.transform == null);

            // Paint circles for each active vision source in the target pixel array
            foreach (var source in _activeSources)
            {
                Vector2Int pixel = PositionToPixel(source.transform.position, out bool inside);
                if (!inside) continue;
                int px = pixel.x;
                int py = pixel.y;

                // Determine radius length in pixels along each axis
                // Prevent division by zero
                float safeMapX = Mathf.Max(mapWorldSize.x, 0.1f);
                float safeMapY = Mathf.Max(mapWorldSize.y, 0.1f);

                float rNormX = source.radius / safeMapX;
                float rNormY = source.radius / safeMapY;
                int radiusPxX = Mathf.RoundToInt(rNormX * textureResolution);
                int radiusPxY = Mathf.RoundToInt(rNormY * textureResolution);

                // We use the larger pixel radius length to simplify distance checks (assume square aspect ratio conceptually)
                int radiusPx = Mathf.Max(radiusPxX, radiusPxY);
                // Ensure a minimum of at least 1 pixel radius if there is supposed to be a hole
                if (radiusPx < 1) radiusPx = 1;

                int outerRadiusPx = Mathf.CeilToInt(
                    radiusPx * (1f + edgeSoftness));

                // Build bounds around the full feathered edge in pixel space.
                int minX = Mathf.Clamp(px - outerRadiusPx, 0, textureResolution - 1);
                int maxX = Mathf.Clamp(px + outerRadiusPx, 0, textureResolution - 1);
                int minY = Mathf.Clamp(py - outerRadiusPx, 0, textureResolution - 1);
                int maxY = Mathf.Clamp(py + outerRadiusPx, 0, textureResolution - 1);

                float sqrOuterRadiusPx = outerRadiusPx * outerRadiusPx;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        // Calculate square distance in pixels
                        float distSqr = (x - px) * (x - px) + (y - py) * (y - py);

                        if (distSqr <= sqrOuterRadiusPx)
                        {
                            int index = y * textureResolution + x;

                            // The registered radius is the middle of the
                            // feather, so its border is exactly half visible.
                            float targetVis =
                                FogVisibilityModel.CalculateSoftVisibility(
                                    Mathf.Sqrt(distSqr),
                                    radiusPx,
                                    edgeSoftness) *
                                source.intensity;

                            // Apply to all RGB channels equally
                            _targetPixels[index].r = Mathf.Max(_targetPixels[index].r, targetVis);
                            _targetPixels[index].g = _targetPixels[index].r;
                            _targetPixels[index].b = _targetPixels[index].r;
                        }
                    }
                }
            }
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
            int px = Mathf.Clamp(pixel.x, 0, textureResolution - 1);
            int py = Mathf.Clamp(pixel.y, 0, textureResolution - 1);

            return _fogPixels[py * textureResolution + px].r;
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
