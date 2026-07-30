using System.Collections.Generic;
using EmpireAtWar.Models.FogOfWar;
using UnityEngine;

namespace ViewComponents
{
    public class FogOfWarSystem : MonoBehaviour
    {
        private const float HISTORIC_VISIBILITY = 0.35f;

        
        [SerializeField] private MeshFilter meshFilter;
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

        // Base visibility for areas we've already explored (if keepHistory is true)

        private void Start()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                if (autoDetectBounds)
                {
                    // Use local bounds instead of world bounds
                    // This way we calculate map boundaries relative to the plane regardless of rotation/scale
                    mapCenter = r.bounds.center;

                    // We measure local size using the attached object's scale and base mesh bounds
                    if (r is MeshRenderer meshRenderer && meshRenderer.GetComponent<MeshFilter>() != null)
                    {
                        Bounds meshBounds = meshRenderer.GetComponent<MeshFilter>().mesh.bounds;
                        mapWorldSize = new Vector2(
                            meshBounds.size.x * Mathf.Abs(transform.lossyScale.x),
                            meshBounds.size.z * Mathf.Abs(transform.lossyScale.z));

                        if (mapWorldSize.y < 0.1f)
                            mapWorldSize.y = meshBounds.size.y * Mathf.Abs(transform.lossyScale.y);
                    }
                    else
                    {
                        // Fallback
                        float sizeX = Mathf.Abs(transform.lossyScale.x) * 10f;
                        float sizeZ = Mathf.Abs(transform.lossyScale.z) * 10f;
                        mapWorldSize = new Vector2(sizeX, sizeZ);
                    }

                    Debug.Log($"[FogOfWarSystem] Auto-detected relative: Center {mapCenter}, Size {mapWorldSize}");
                }

                _fogMaterial = r.material;
            }
            else
            {
                Debug.LogWarning("FogOfWarSystem needs to be on an object with a Renderer.");
            }

            // Using RGBA32 is fully compatible everywhere compared to R8
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

            if (_fogMaterial != null)
            {
                _fogMaterial.SetTexture("_MainTex", _fogTexture);
            }
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
                // Transform the world-space target into the LOCAL space of the Fog Grid
                // This completely solves negative scale and rotated plane mapping issues.
                Vector3 localPos = transform.InverseTransformPoint(source.transform.position);

                // Local space plane (Assuming standard Unity Plane/Quad where XY or XZ are the surface bounds [-5, 5])
                // We map local.x to U and local.z (or y, if rotated) to V.
                // Unity default planes are 10x10 in local space, Quads are 1x1.
                // We normalize this to a 0.0 to 1.0 coordinate system mapped on the grid.

                // Assuming standard center pivot (0,0) and normalized width/height (-0.5 to 0.5)
                // We need to account for Mesh bounds difference (Plane = 10, Quad = 1)
                float localBoundsExtents = 5f; // For Unity Plane. (Quad is 0.5f)
                if (meshFilter != null && meshFilter.mesh != null)
                {
                    localBoundsExtents = meshFilter.mesh.bounds.extents.x;
                }

                // Map local position to 0-1 range. Extents are e.g. [-5, 5], so we add 5 and divide by 10.
                float normalizedX = (localPos.x + localBoundsExtents) / (localBoundsExtents * 2f);

                // If it's a Quad facing forward, local Z might be 0, so we use local Y instead.
                float localDepth = (Mathf.Abs(localPos.z) < 0.001f && Mathf.Abs(localPos.y) > 0.001f) ? localPos.y : localPos.z;
                float normalizedZ = (localDepth + localBoundsExtents) / (localBoundsExtents * 2f);

                // Unity's default Plane mesh has UVs inverted relative to local space.
                // Flip axes to match the shader's UV sampling.
                if (flipX) normalizedX = 1f - normalizedX;
                if (flipZ) normalizedZ = 1f - normalizedZ;

                Vector2 normalizedPos = new Vector2(normalizedX, normalizedZ);

                // Discard rendering outside the texture boundaries to save processing
                if (normalizedPos.x < 0f || normalizedPos.x > 1f || normalizedPos.y < 0f || normalizedPos.y > 1f)
                    continue;


                // Get the center pixel based on current resolution
                int px = Mathf.RoundToInt(normalizedPos.x * textureResolution);
                int py = Mathf.RoundToInt(normalizedPos.y * textureResolution);

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

        /// <summary>
        /// Checks the current fog density at a specific world coordinate.
        /// Useful for disabling the rendering of enemy ships that fall into unseen fog areas!
        /// Returns 1.0 if fully revealed, 0.0 if not.
        /// </summary>
        public float GetVisibilityAtPosition(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            float localBoundsExtents = 5f;
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.mesh != null)
            {
                localBoundsExtents = mf.mesh.bounds.extents.x;
            }

            float normalizedX = (localPos.x + localBoundsExtents) / (localBoundsExtents * 2f);
            float localDepth = (Mathf.Abs(localPos.z) < 0.001f && Mathf.Abs(localPos.y) > 0.001f) ? localPos.y : localPos.z;
            float normalizedZ = (localDepth + localBoundsExtents) / (localBoundsExtents * 2f);

            if (flipX) normalizedX = 1f - normalizedX;
            if (flipZ) normalizedZ = 1f - normalizedZ;

            int px = Mathf.Clamp(Mathf.RoundToInt(normalizedX * textureResolution), 0, textureResolution - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * textureResolution), 0, textureResolution - 1);

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
