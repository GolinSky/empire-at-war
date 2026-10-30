using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using UnityEngine;

namespace EmpireAtWar.Components.Environment
{
    public sealed class HazardCloudView : MonoBehaviour
    {
        private static readonly int DISTANCE = Shader.PropertyToID("_Distance");
        private static readonly int FLOW_DIRECTION = Shader.PropertyToID("_FlowDirection");
        private static readonly int NOISE_SCALE = Shader.PropertyToID("_NoiseScale");
        private static readonly int LAYER_OPACITY = Shader.PropertyToID("_LayerOpacity");
        private static readonly int NOISE_OFFSET = Shader.PropertyToID("_NoiseOffset");
        private static readonly int INSET = Shader.PropertyToID("_Inset");
        private static readonly int SOFTNESS = Shader.PropertyToID("_Softness");
        private static readonly int FLASH = Shader.PropertyToID("_Flash");
        // The top sheet keeps the old cloud height; deeper sheets stack below it so ships at the usual
        // tiers stay readable above the gas while the cloud gains depth.
        private const float SHEET_HEIGHT = -60f;
        private const float SHEET_SPACING = 25f;
        // Deeper sheets shrink and soften so the cloud thins toward its bottom like a volume.
        private const float SHEET_INSET = 14f;
        private const float WISP_REACH = 90f;
        private const float CORE_DEPTH = 40f;
        private const int ARC_POINTS = 20;
        private const float NOISE_SIZE_SHARE = 1.4f;
        [SerializeField] private MeshRenderer[] layers;
        [SerializeField] private MeshFilter[] filters;
        [SerializeField] private LineRenderer arc;
        private readonly List<Vector3> _corePoints = new List<Vector3>();
        private MaterialPropertyBlock[] _properties;
        private Mesh _mesh;
        private Texture2D _distance;
        private bool _isIon;
        private bool _flashing;
        private float _nextFlash;
        private float _flashEnd;
        private float _height;
        private int _qualityLevel = -1;

        public void Configure(HazardShape shape, float floor, float ceiling)
        {
            Vector3 center = shape.Bounds.center;
            transform.SetPositionAndRotation(center, Quaternion.identity);
            _isIon = shape.Data.HazardType == HazardType.IonStorm;
            _height = Mathf.Clamp(SHEET_HEIGHT, floor, ceiling);
            ReleaseResources();
            _distance = CreateDistanceTexture(shape);
            _mesh = CreateSheet(shape, center, WISP_REACH);
            CollectCorePoints(shape);
            Vector3 flow = shape.Spot.Frame.Along;
            _properties = new MaterialPropertyBlock[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                float height = Mathf.Clamp(_height - i * SHEET_SPACING, floor, ceiling);
                layers[i].transform.localPosition = new Vector3(0f, height - center.y, 0f);
                layers[i].transform.localScale = Vector3.one;
                layers[i].sharedMaterial = shape.Data.VisualMaterial;
                filters[i].sharedMesh = _mesh;
                var properties = new MaterialPropertyBlock();
                properties.SetTexture(DISTANCE, _distance);
                properties.SetVector(FLOW_DIRECTION, new Vector4(flow.x, flow.z, 0f, 0f));
                properties.SetFloat(NOISE_SCALE, 1f / Mathf.Max(1f, shape.Spot.Roughness * NOISE_SIZE_SHARE));
                // The top sheet is the full gameplay outline; the rest add depth without moving the edge.
                properties.SetFloat(NOISE_OFFSET, i * 17.31f);
                properties.SetFloat(INSET, i * SHEET_INSET);
                properties.SetFloat(SOFTNESS, 1f + i * 0.75f);
                properties.SetVector(FLASH, Vector4.zero);
                layers[i].SetPropertyBlock(properties);
                _properties[i] = properties;
            }
            ApplyQuality();
            arc.enabled = false;
            _flashing = false;
            _flashEnd = 0f;
            _nextFlash = Time.time + Random.Range(1.5f, 3f);
        }

        public static Texture2D CreateDistanceTexture(HazardShape shape)
        {
            var texture = new Texture2D(shape.Columns, shape.Rows, TextureFormat.RHalf, false, true)
            {
                name = "Hazard distance",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var halves = new ushort[shape.Columns * shape.Rows];
            for (int row = 0; row < shape.Rows; row++)
                for (int column = 0; column < shape.Columns; column++)
                    halves[row * shape.Columns + column] = Mathf.FloatToHalf(shape.GetDistance(column, row));
            texture.SetPixelData(halves, 0);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>Only covers cells that can contribute wisps; UVs retain the distance-grid mapping.</summary>
        public static Mesh CreateSheet(HazardShape shape, Vector3 origin, float reach)
        {
            Bounds bounds = shape.Bounds;
            bounds.Expand(new Vector3(reach * 2f, 0f, reach * 2f));
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int row = 0; row < shape.Rows - 1; row++)
                for (int column = 0; column < shape.Columns - 1; column++)
                {
                    // Match RHalf storage: bilinear filtering cannot fall below all four texels.
                    float minimum = Mathf.Min(Mathf.Min(
                        Mathf.HalfToFloat(Mathf.FloatToHalf(shape.GetDistance(column, row))),
                        Mathf.HalfToFloat(Mathf.FloatToHalf(shape.GetDistance(column + 1, row)))), Mathf.Min(
                        Mathf.HalfToFloat(Mathf.FloatToHalf(shape.GetDistance(column, row + 1))),
                        Mathf.HalfToFloat(Mathf.FloatToHalf(shape.GetDistance(column + 1, row + 1)))));
                    if (minimum > reach) continue;
                    Vector3 sample = shape.GetSamplePosition(column, row);
                    float minX = Mathf.Max(sample.x, bounds.min.x);
                    float minZ = Mathf.Max(sample.z, bounds.min.z);
                    float maxX = Mathf.Min(sample.x + shape.CellSize, bounds.max.x);
                    float maxZ = Mathf.Min(sample.z + shape.CellSize, bounds.max.z);
                    if (minX >= maxX || minZ >= maxZ) continue;
                    int first = vertices.Count;
                    vertices.Add(new Vector3(minX - origin.x, 0f, minZ - origin.z));
                    vertices.Add(new Vector3(minX - origin.x, 0f, maxZ - origin.z));
                    vertices.Add(new Vector3(maxX - origin.x, 0f, maxZ - origin.z));
                    vertices.Add(new Vector3(maxX - origin.x, 0f, minZ - origin.z));
                    for (int i = first; i < first + 4; i++)
                    {
                        Vector3 position = vertices[i] + origin;
                        uv.Add(new Vector2(((position.x - shape.GridOrigin.x) / shape.CellSize + 0.5f) / shape.Columns,
                            ((position.z - shape.GridOrigin.z) / shape.CellSize + 0.5f) / shape.Rows));
                    }
                    triangles.Add(first);
                    triangles.Add(first + 1);
                    triangles.Add(first + 2);
                    triangles.Add(first);
                    triangles.Add(first + 2);
                    triangles.Add(first + 3);
                }
            var mesh = new Mesh
            {
                name = "Hazard sheet",
                indexFormat = vertices.Count > ushort.MaxValue
                    ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CollectCorePoints(HazardShape shape)
        {
            _corePoints.Clear();
            for (int row = 0; row < shape.Rows; row += 3)
                for (int column = 0; column < shape.Columns; column += 3)
                    if (shape.GetDistance(column, row) < -CORE_DEPTH)
                        _corePoints.Add(shape.GetSamplePosition(column, row));
        }

        private void Update()
        {
            if (!_isIon || _corePoints.Count == 0) return;
            if (Time.time >= _nextFlash)
            {
                _nextFlash = Time.time + Random.Range(1.5f, 3f);
                _flashEnd = Time.time + Random.Range(0.15f, 0.4f);
                StartFlash();
            }
            bool flash = Time.time < _flashEnd;
            if (flash == _flashing) return;
            _flashing = flash;
            arc.enabled = flash;
            if (flash) return;
            for (int i = 0; i < layers.Length; i++)
            {
                _properties[i].SetVector(FLASH, Vector4.zero);
                layers[i].SetPropertyBlock(_properties[i]);
            }
        }

        private void LateUpdate()
        {
            if (_properties != null && _qualityLevel != QualitySettings.GetQualityLevel()) ApplyQuality();
        }

        private void ApplyQuality()
        {
            _qualityLevel = QualitySettings.GetQualityLevel();
            int count = Mathf.Min(layers.Length, _qualityLevel <= 1 ? 2 : _qualityLevel == 2 ? 3 : 5);
            float totalOpacity = 0f;
            float activeOpacity = 0f;
            for (int i = 0; i < layers.Length; i++)
            {
                float opacity = Mathf.Lerp(0.6f, 0.3f, i / Mathf.Max(1f, layers.Length - 1f));
                totalOpacity += opacity;
                if (i < count) activeOpacity += opacity;
            }
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].enabled = i < count;
                float opacity = Mathf.Lerp(0.6f, 0.3f, i / Mathf.Max(1f, layers.Length - 1f));
                _properties[i].SetFloat(LAYER_OPACITY, opacity * totalOpacity / activeOpacity);
                layers[i].SetPropertyBlock(_properties[i]);
            }
        }

        // A flash lights one region and a jagged bolt crosses it, instead of the whole storm blinking.
        private void StartFlash()
        {
            Vector3 start = _corePoints[Random.Range(0, _corePoints.Count)];
            Vector3 end = _corePoints[Random.Range(0, _corePoints.Count)];
            Vector3 span = end - start;
            float length = Random.Range(150f, 320f);
            if (span.sqrMagnitude > length * length) end = start + span.normalized * length;
            Vector3 normal = new Vector3(-(end - start).z, 0f, (end - start).x).normalized;
            arc.positionCount = ARC_POINTS;
            for (int i = 0; i < ARC_POINTS; i++)
            {
                float t = i / (ARC_POINTS - 1f);
                float jitter = i == 0 || i == ARC_POINTS - 1 ? 0f : Random.Range(-1f, 1f) * 22f * Mathf.Sin(t * Mathf.PI);
                Vector3 position = Vector3.Lerp(start, end, t) + normal * jitter - transform.position;
                position.y = _height - transform.position.y;
                arc.SetPosition(i, position);
            }
            Vector3 middle = (start + end) * 0.5f;
            Vector4 flash = new Vector4(middle.x, middle.z, Random.Range(200f, 400f), 0.9f);
            for (int i = 0; i < layers.Length; i++)
            {
                _properties[i].SetVector(FLASH, flash);
                layers[i].SetPropertyBlock(_properties[i]);
            }
        }

        private void OnDestroy() => ReleaseResources();

        private void ReleaseResources()
        {
            Release(_mesh);
            Release(_distance);
            _mesh = null;
            _distance = null;
        }

        private static void Release(Object resource)
        {
            if (resource == null) return;
            if (Application.isPlaying) Destroy(resource);
            else DestroyImmediate(resource);
        }
    }
}
