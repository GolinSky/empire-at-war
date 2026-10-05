using EmpireAtWar.Models.FogOfWar;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.Vision;
using Unity.Collections;
using UnityEngine;
using Zenject;

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

        private Texture2D _fogTexture;
        private FogVisibilityGridModel _grid;

        private float _timer;
        private Material _fogMaterial;

        public Texture Mask => _fogTexture;
        private ICameraService _cameraService;
        private IVisionService _visionService;
        private ILocalPlayer _localPlayer;

        [Inject]
        private void Construct(ICameraService cameraService, IVisionService visionService, ILocalPlayer localPlayer)
        {
            _cameraService = cameraService;
            _visionService = visionService;
            _localPlayer = localPlayer;
        }

        // The mask exists only after InitializeArea, so the fog stays idle until the map is built.
        private void Awake()
        {
            enabled = false;
        }

        /// <summary>Stretches the fog plane and its mask for the battlefield, then creates the mask.</summary>
        public void InitializeArea(float scale)
        {
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(scale, 1f, scale));
            textureResolution = Mathf.Min(MAX_TEXTURE_RESOLUTION, Mathf.RoundToInt(textureResolution * scale));

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
            WritePixels();
            _fogMaterial.SetTexture("_MainTex", _fogTexture);
            enabled = true;
        }

        /// <summary>Shows the current vision at once instead of fading it in.</summary>
        public void RevealImmediately()
        {
            UpdateFogTargets();
            _grid.Fade(1f);
            WritePixels();
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
                WritePixels();
            }
        }

        private void WritePixels()
        {
            NativeArray<byte> pixels = _fogTexture.GetPixelData<byte>(0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (byte)(_grid.GetVisibility(i) * BYTE_MAX + 0.5f);
            }

            _fogTexture.Apply();
        }

        private void UpdateFogTargets()
        {
            _grid.ResetTargets(keepHistory);

            foreach (OwnedCircle source in _visionService.Sources)
            {
                // Allies share vision, so their sources reveal the local fog too.
                if (!_localPlayer.IsFriendly(source.Owner)) continue;
                Vector2Int pixel = PositionToPixel(ProjectOntoFog(source.Transform.position), out bool inside);
                if (!inside) continue;
                _grid.Reveal(pixel.x, pixel.y, RadiusToPixels(source.Radius), edgeSoftness, 1f);
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

        public Rect GetMaskUvRect(Vector2 worldMin, Vector2 worldMax)
        {
            Vector2 min = WorldToMaskUv(new Vector3(worldMin.x, 0f, worldMin.y));
            Vector2 max = WorldToMaskUv(new Vector3(worldMax.x, 0f, worldMax.y));
            return new Rect(min, max - min);
        }

        private Vector2Int PositionToPixel(Vector3 worldPos, out bool inside)
        {
            Vector2 uv = WorldToMaskUv(worldPos);
            inside = uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
            return new Vector2Int(Mathf.RoundToInt(uv.x * textureResolution),
                Mathf.RoundToInt(uv.y * textureResolution));
        }

        private Vector2 WorldToMaskUv(Vector3 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            float localBoundsExtents = meshFilter.sharedMesh.bounds.extents.x;

            float normalizedX = (localPos.x + localBoundsExtents) / (localBoundsExtents * 2f);
            // The fog is a flat XZ plane: a unit's height never changes the cell it reveals.
            float normalizedZ = (localPos.z + localBoundsExtents) / (localBoundsExtents * 2f);

            if (flipX) normalizedX = 1f - normalizedX;
            if (flipZ) normalizedZ = 1f - normalizedZ;
            return new Vector2(normalizedX, normalizedZ);
        }

        /// <summary>
        /// Units fly at different heights, but the fog is drawn on one plane. The player sees a unit
        /// where the camera ray through it crosses that plane, so the drawn holes use that point.
        /// Drawing only: gameplay visibility in IVisionService ignores height and the camera.
        /// </summary>
        private Vector3 ProjectOntoFog(Vector3 worldPos)
        {
            Vector3 cameraPosition = _cameraService.CameraPosition;
            float fogHeight = transform.position.y;
            // A camera level with or below the unit (cinematic shots) has no ray down to the fog plane.
            if (cameraPosition.y <= worldPos.y || cameraPosition.y <= fogHeight) return worldPos;

            float rayFraction = (cameraPosition.y - fogHeight) / (cameraPosition.y - worldPos.y);
            return cameraPosition + (worldPos - cameraPosition) * rayFraction;
        }
    }
}
