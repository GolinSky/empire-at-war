using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SpawnArea;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using Unity.Collections;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Views.SpawnArea
{
    /// <summary>
    /// Shows reinforcement restrictions over visible terrain on the fog grid. Structure placement
    /// also marks cells where the structure footprint overlaps a reserved site or an obstacle.
    /// </summary>
    public sealed class SpawnAreaOverlay : MonoBehaviour, ISpawnAreaOverlay, IInitializable, ILateDisposable,
        IObserver<BattleMap>
    {
        private const float BYTE_MAX = 255f;
        // Unity's built-in plane is 10 x 10 units.
        private const float PLANE_MESH_SIZE = 10f;

        private static readonly int GRID_SIZE_ID = Shader.PropertyToID("_GridSize");
        private static readonly int GRID_THICKNESS_ID = Shader.PropertyToID("_GridThickness");

        [SerializeField] private MeshRenderer overlayRenderer;
        [SerializeField, Min(16)] private int textureResolution = 256;
        [SerializeField, Min(0.02f)] private float updateInterval = 0.1f;
        [SerializeField] private MeshRenderer fogRenderer;

        private IVisionService _vision;
        private IBattleAreaService _areas;
        private IPlayerRelations _relations;
        private INotifier<BattleMap> _battleMap;
        private StructureSpawnClearance _structureClearance;

        private SpawnAreaGridModel _grid;
        private SpawnAreaGridModel _hazardMask;
        private Bounds _mapBounds;
        private bool _placingStructure;
        private System.Func<float, float, bool> _isObstructed;
        private Texture2D _texture;
        private Material _material;
        private PlayerId _team;
        private float _timer;

        [Inject]
        private void Construct(IVisionService vision, IBattleAreaService areas, IPlayerRelations relations,
            INotifier<BattleMap> battleMap, StructureSpawnClearance structureClearance)
        {
            _vision = vision;
            _areas = areas;
            _relations = relations;
            _battleMap = battleMap;
            _structureClearance = structureClearance;
            _isObstructed = (x, z) => !structureClearance.IsClear(new Vector3(x, 0f, z));
        }

        private void Awake()
        {
            overlayRenderer.enabled = false;
            enabled = false;
        }

        public void Initialize() => _battleMap.AddObserver(this);

        public void LateDispose() => _battleMap.RemoveObserver(this);

        public void UpdateState(BattleMap battleMap)
        {
            Vector2 min = battleMap.Layout.SizeRange.Min;
            Vector2 size = battleMap.Layout.SizeRange.Max - min;
            transform.position = new Vector3(min.x + size.x * 0.5f, fogRenderer.transform.position.y,
                min.y + size.y * 0.5f);
            transform.localScale = new Vector3(size.x / PLANE_MESH_SIZE, 1f, size.y / PLANE_MESH_SIZE);

            _grid = new SpawnAreaGridModel(textureResolution, min.x, min.y, size.x, size.y);
            _mapBounds = new Bounds(new Vector3(min.x + size.x * 0.5f, 0f, min.y + size.y * 0.5f),
                new Vector3(size.x, 0f, size.y));
            _hazardMask = new SpawnAreaGridModel(textureResolution, min.x, min.y, size.x, size.y);
            _hazardMask.RevealAll();
            foreach (HazardShape hazard in battleMap.Layout.Hazards)
            {
                if (!hazard.Data.BlocksReinforcements) continue;
                Bounds bounds = hazard.Bounds;
                _hazardMask.BlockWhere(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z,
                    (x, z) => hazard.Contains(new Vector3(x, 0f, z), 0f, false));
            }
            _texture = new Texture2D(textureResolution, textureResolution, TextureFormat.R8, false);
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.filterMode = FilterMode.Point;
            _material = overlayRenderer.material;
            _material.SetTexture("_MainTex", _texture);
            Material fogMaterial = fogRenderer.sharedMaterial;
            _material.SetFloat(GRID_SIZE_ID, fogMaterial.GetFloat(GRID_SIZE_ID));
            _material.SetFloat(GRID_THICKNESS_ID, fogMaterial.GetFloat(GRID_THICKNESS_ID));
        }

        public void Show(PlayerId team, bool placingStructure)
        {
            _team = team;
            _placingStructure = placingStructure;
            _timer = 0f;
            Redraw();
            overlayRenderer.enabled = true;
            enabled = true;
        }

        public void Hide()
        {
            overlayRenderer.enabled = false;
            enabled = false;
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;
            Redraw();
        }

        // Renderer.material returns an instance this component owns; the texture is created here too.
        private void OnDestroy()
        {
            Destroy(_material);
            Destroy(_texture);
        }

        private void Redraw()
        {
            if (_vision.FogEnabled)
            {
                _grid.Clear();
                foreach (OwnedCircle source in _vision.Sources)
                {
                    if (!_relations.IsAllied(source.Owner, _team)) continue;
                    Vector3 center = source.Transform.position;
                    _grid.Reveal(center.x, center.z, source.Radius);
                }
            }
            else
            {
                _grid.RevealAll();
            }

            foreach (OwnedCircle blocker in _areas.Get(BattleAreaLayer.SpawnBlock))
            {
                if (_relations.IsAllied(blocker.Owner, _team)) continue;
                Vector3 center = blocker.Transform.position;
                _grid.Block(center.x, center.z, blocker.Radius);
            }

            if (_placingStructure) BlockStructureFootprints();

            // The built-in plane's UV origin sits at its +X/+Z corner, so both axes are mirrored.
            NativeArray<byte> pixels = _texture.GetPixelData<byte>(0);
            int last = textureResolution - 1;
            for (int v = 0; v < textureResolution; v++)
            {
                for (int u = 0; u < textureResolution; u++)
                {
                    float state = Mathf.Min(_grid.Get(last - u, last - v), _hazardMask.Get(last - u, last - v));
                    pixels[v * textureResolution + u] = (byte)(state * BYTE_MAX + 0.5f);
                }
            }

            _texture.Apply();
        }

        private void BlockStructureFootprints()
        {
            float radius = _structureClearance.Radius;
            foreach (OwnedCircle exclusion in _areas.Get(BattleAreaLayer.StructureExclusion))
            {
                Vector3 center = exclusion.Transform.position;
                _grid.Block(center.x, center.z, exclusion.Radius + radius);
            }

            foreach (Collider obstacle in _structureClearance.GetObstacles(_mapBounds))
            {
                Bounds bounds = obstacle.bounds;
                bounds.Expand(radius * 2f);
                _grid.BlockWhere(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z, _isObstructed);
            }
        }
    }
}
