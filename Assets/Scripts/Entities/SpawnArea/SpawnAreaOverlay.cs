using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SpawnArea;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using Unity.Collections;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Views.SpawnArea
{
    /// <summary>
    /// Second fog layer, drawn only during reinforcement placement: open cells are tinted, cells the team
    /// sees but a hostile blocker covers are marked blocked. Flat on the XZ plane, like the rule it shows.
    /// </summary>
    public sealed class SpawnAreaOverlay : MonoBehaviour, ISpawnAreaOverlay, IInitializable, ILateDisposable,
        IObserver<BattleMap>
    {
        private const float BYTE_MAX = 255f;
        // Unity's built-in plane is 10 x 10 units.
        private const float PLANE_MESH_SIZE = 10f;

        [SerializeField] private MeshRenderer overlayRenderer;
        [SerializeField, Min(16)] private int textureResolution = 256;
        [SerializeField, Min(0.02f)] private float updateInterval = 0.1f;
        [SerializeField] private float height = -35f;

        private IVisionService _vision;
        private ISpawnBlockerService _blockers;
        private IPlayerRelations _relations;
        private INotifier<BattleMap> _battleMap;

        private SpawnAreaGridModel _grid;
        private Texture2D _texture;
        private Material _material;
        private PlayerId _team;
        private float _timer;

        [Inject]
        private void Construct(IVisionService vision, ISpawnBlockerService blockers, IPlayerRelations relations,
            INotifier<BattleMap> battleMap)
        {
            _vision = vision;
            _blockers = blockers;
            _relations = relations;
            _battleMap = battleMap;
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
            transform.position = new Vector3(min.x + size.x * 0.5f, height, min.y + size.y * 0.5f);
            transform.localScale = new Vector3(size.x / PLANE_MESH_SIZE, 1f, size.y / PLANE_MESH_SIZE);

            _grid = new SpawnAreaGridModel(textureResolution, min.x, min.y, size.x, size.y);
            _texture = new Texture2D(textureResolution, textureResolution, TextureFormat.R8, false);
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.filterMode = FilterMode.Bilinear;
            _material = overlayRenderer.material;
            _material.SetTexture("_MainTex", _texture);
        }

        public void Show(PlayerId team)
        {
            _team = team;
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
            _grid.Clear();
            foreach (OwnedCircle source in _vision.Sources)
            {
                if (!_relations.IsAllied(source.Owner, _team)) continue;
                Vector3 center = source.Transform.position;
                _grid.Reveal(center.x, center.z, source.Radius);
            }

            foreach (OwnedCircle blocker in _blockers.Blockers)
            {
                if (_relations.IsAllied(blocker.Owner, _team)) continue;
                Vector3 center = blocker.Transform.position;
                _grid.Block(center.x, center.z, blocker.Radius);
            }

            // The built-in plane's UV origin sits at its +X/+Z corner, so both axes are mirrored.
            NativeArray<byte> pixels = _texture.GetPixelData<byte>(0);
            int last = textureResolution - 1;
            for (int v = 0; v < textureResolution; v++)
            {
                for (int u = 0; u < textureResolution; u++)
                {
                    pixels[v * textureResolution + u] = (byte)(_grid.Get(last - u, last - v) * BYTE_MAX + 0.5f);
                }
            }

            _texture.Apply();
        }
    }
}
