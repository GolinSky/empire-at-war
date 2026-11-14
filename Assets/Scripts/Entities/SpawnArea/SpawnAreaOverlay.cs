using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SpawnArea;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Views.SpawnArea
{
    /// <summary>
    /// Samples the selected reinforcement's restrictions at fog-grid cell centres.
    /// </summary>
    public sealed class SpawnAreaOverlay : MonoBehaviour, ISpawnAreaOverlay, IInitializable, ILateDisposable,
        IObserver<BattleMap>
    {
        private const float BYTE_MAX = 255f;
        // Unity's built-in plane is 10 x 10 units.
        private const float PLANE_MESH_SIZE = 10f;
        private const float REACHABILITY_INTERVAL = 1f;

        private static readonly ProfilerMarker REDRAW_MARKER = new ProfilerMarker("Battle.SpawnArea.Redraw");

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
        private ShipSpawnClearance _shipClearance;
        private IShipSpawnReachability _shipReachability;

        private SpawnAreaGridModel _grid;
        private SpawnAreaGridModel _hazardMask;
        private Bounds _mapBounds;
        private bool _placingStructure;
        private ShipUnitRequest _shipRequest;
        private Func<Vector3, bool> _isPositionValid;
        private Func<float, float, bool> _isObstructed;
        private Vector3[] _cellCenters;
        private bool[] _reachable;
        private float _reachabilityTimer;
        private bool _reachabilityPending;
        private bool _forceReachabilityRefresh;
        private Texture2D _texture;
        private Material _material;
        private PlayerId _team;
        private float _timer;

        [Inject]
        private void Construct(IVisionService vision, IBattleAreaService areas, IPlayerRelations relations,
            INotifier<BattleMap> battleMap, StructureSpawnClearance structureClearance,
            ShipSpawnClearance shipClearance, IShipSpawnReachability shipReachability)
        {
            _vision = vision;
            _areas = areas;
            _relations = relations;
            _battleMap = battleMap;
            _structureClearance = structureClearance;
            _shipClearance = shipClearance;
            _shipReachability = shipReachability;
            _isObstructed = (x, z) => !_isPositionValid(new Vector3(x, 0f, z));
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
            _cellCenters = new Vector3[textureResolution * textureResolution];
            _reachable = new bool[_cellCenters.Length];
            for (int row = 0; row < textureResolution; row++)
                for (int column = 0; column < textureResolution; column++)
                    _cellCenters[row * textureResolution + column] =
                        new Vector3(_grid.GetCenterX(column), 0f, _grid.GetCenterZ(row));
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

        public void Show(PlayerId team, UnitRequest request, Func<Vector3, bool> isPositionValid)
        {
            _team = team;
            _placingStructure = !ReinforcementModel.UsesCapacity(request);
            _shipRequest = request as ShipUnitRequest;
            _isPositionValid = isPositionValid;
            _reachabilityTimer = REACHABILITY_INTERVAL;
            _reachabilityPending = false;
            _forceReachabilityRefresh = true;
            // Never display a previous selection's mask while this ship's first batch is pending.
            Array.Clear(_reachable, 0, _reachable.Length);
            _timer = 0f;
            Redraw();
            overlayRenderer.enabled = true;
            enabled = true;
        }

        public void Hide()
        {
            overlayRenderer.enabled = false;
            enabled = false;
            _shipRequest = null;
            _isPositionValid = null;
        }

        public Vector3 GetPlacementPosition(Vector3 position)
        {
            // Keep out-of-map positions invalid instead of clamping them onto an edge cell.
            if (_shipRequest == null || position.x < _mapBounds.min.x || position.x > _mapBounds.max.x ||
                position.z < _mapBounds.min.z || position.z > _mapBounds.max.z) return position;
            return new Vector3(_grid.SnapX(position.x), position.y, _grid.SnapZ(position.z));
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            _reachabilityTimer += Time.unscaledDeltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;
            Redraw();
        }

        public float GetPlacementSpacing(float minimum) =>
            _shipRequest == null ? minimum : _grid.GetPlacementSpacing(minimum);

        // Renderer.material returns an instance this component owns; the texture is created here too.
        private void OnDestroy()
        {
            Destroy(_material);
            Destroy(_texture);
        }

        private void Redraw()
        {
            using var redraw = REDRAW_MARKER.Auto();
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
            if (_shipRequest != null)
            {
                if (_reachabilityPending || _reachabilityTimer >= REACHABILITY_INTERVAL)
                {
                    _reachabilityPending = !_shipReachability.TryFillReachability(
                        _team, _shipRequest.Key, _cellCenters, _reachable, _forceReachabilityRefresh);
                    _forceReachabilityRefresh = false;
                    _reachabilityTimer = 0f;
                }
                _grid.BlockUnreachable(_reachable);
                _shipClearance.BlockObstructedCells(_team, _shipRequest.Key, _grid, _mapBounds);
            }

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
