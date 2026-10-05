using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.FogOfWar;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.Vision;
using Unity.Collections;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.MiniMap
{
    /// <summary>
    /// Flat fog for the minimap, built from the local team's vision sources at their true XZ positions.
    /// The 3D fog projects its holes along camera rays, so it cannot be reused here: its holes move with the camera.
    /// </summary>
    public sealed class MiniMapFogMask : IMiniMapFogMask, ITickable, IInitializable, ILateDisposable,
        IObserver<BattleMap>
    {
        private const int RESOLUTION = 128;
        private const float BYTE_MAX = 255f;
        // Same pacing and feathering as the 3D fog so both layers reveal together.
        private const float UPDATE_INTERVAL = 0.1f;
        private const float FADE_SPEED = 3f;
        private const float EDGE_SOFTNESS = 0.25f;

        private readonly IVisionService _vision;
        private readonly ILocalPlayer _localPlayer;
        private readonly INotifier<BattleMap> _battleMap;

        private readonly FogVisibilityGridModel _grid = new FogVisibilityGridModel(RESOLUTION);
        private readonly Texture2D _texture;
        private Vector2 _mapMin;
        private Vector2 _mapSize;
        private bool _hasMap;
        private float _timer;

        public Texture Mask => _texture;

        public MiniMapFogMask(IVisionService vision, ILocalPlayer localPlayer, INotifier<BattleMap> battleMap)
        {
            _vision = vision;
            _localPlayer = localPlayer;
            _battleMap = battleMap;
            _texture = new Texture2D(RESOLUTION, RESOLUTION, TextureFormat.R8, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            WritePixels();
        }

        public void Initialize() => _battleMap.AddObserver(this);

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            Object.Destroy(_texture);
        }

        public void UpdateState(BattleMap battleMap)
        {
            _mapMin = battleMap.Layout.SizeRange.Min;
            _mapSize = battleMap.Layout.SizeRange.Max - _mapMin;
            _hasMap = true;
        }

        public void Tick()
        {
            if (!_hasMap) return;

            _timer += Time.deltaTime;
            if (_timer >= UPDATE_INTERVAL)
            {
                _timer = 0f;
                UpdateTargets();
            }

            if (_grid.Fade(FADE_SPEED * Time.deltaTime)) WritePixels();
        }

        private void UpdateTargets()
        {
            _grid.ResetTargets(false);
            foreach (OwnedCircle source in _vision.Sources)
            {
                if (!_localPlayer.IsFriendly(source.Owner)) continue;
                Vector3 center = source.Transform.position;
                int column = Mathf.RoundToInt((center.x - _mapMin.x) / _mapSize.x * RESOLUTION);
                int row = Mathf.RoundToInt((center.z - _mapMin.y) / _mapSize.y * RESOLUTION);
                int radius = Mathf.Max(1, Mathf.RoundToInt(source.Radius / Mathf.Max(_mapSize.x, _mapSize.y) * RESOLUTION));
                _grid.Reveal(column, row, radius, EDGE_SOFTNESS, 1f);
            }
        }

        private void WritePixels()
        {
            NativeArray<byte> pixels = _texture.GetPixelData<byte>(0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (byte)(_grid.GetVisibility(i) * BYTE_MAX + 0.5f);
            }

            _texture.Apply();
        }
    }
}
