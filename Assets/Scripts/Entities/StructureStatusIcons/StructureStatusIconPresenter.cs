using System;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.StructureStatusIcons
{
    public sealed class StructureStatusIconPresenter : ILateTickable, ILateDisposable
    {
        private const float WORLD_SIZE = 48f;
        private const float MINIMAP_SIZE = 26f;
        private const float WORLD_VERTICAL_OFFSET = 42f;
        private readonly StructureStatusIconSources _sources;
        private readonly MiniMapUiController _miniMap;
        private readonly MiniMapData _data;
        private readonly BattleMarkerLayer _layer;
        private readonly ICinematicCameraModelObserver _cinematic;
        private readonly IHudVisibilityObserver _hud;
        private readonly IInputLock _inputLock;
        private readonly UnityEngine.Camera _camera;
        private StructureStatusIconView[] _world = Array.Empty<StructureStatusIconView>();
        private StructureStatusIconView[] _map = Array.Empty<StructureStatusIconView>();
        private Vector3[] _anchors = Array.Empty<Vector3>();
        private Vector2[] _projected = Array.Empty<Vector2>();
        private bool[] _inside = Array.Empty<bool>();
        private bool[] _worldDisplayed = Array.Empty<bool>();
        private bool[] _mapDisplayed = Array.Empty<bool>();
        private int[] _revisions = Array.Empty<int>();
        private int[] _previous = Array.Empty<int>();
        private int _previousCount;
        private int _version = -1;
        private Matrix4x4 _cameraMatrix;
        private Rect _viewport;
        private RectTransform _worldParent;
        private bool _disposed;

        public StructureStatusIconPresenter(StructureStatusIconSources sources, MiniMapUiController miniMap,
            MiniMapData data, BattleMarkerLayer layer, ICinematicCameraModelObserver cinematic,
            ICameraService camera, IHudVisibilityObserver hud, IInputLock inputLock)
        {
            _sources = sources;
            _miniMap = miniMap;
            _data = data;
            _layer = layer;
            _cinematic = cinematic;
            _camera = camera.SceneCamera;
            _hud = hud;
            _inputLock = inputLock;
        }

        public void LateTick() => Advance(Time.unscaledDeltaTime);

        private void Advance(float seconds)
        {
            if (_disposed || _miniMap.View == null) return;
            MiniMapUi view = _miniMap.View;
            bool rebuilt = _version != _sources.Version;
            if (rebuilt) Prewarm(view);
            StructureStatusIconModel model = _sources.Model;
            for (int i = 0; i < _previousCount; i++)
            {
                int source = _previous[i];
                StructureStatusIconRecord record = model.Get(source);
                if (!record.WorldActive) _world[source].Display(false, 0f, default);
                if (!record.MinimapActive) _map[source].Display(false, 0f, default);
            }
            _previousCount = 0;
            if (model.ActiveCount == 0) return;
            Matrix4x4 matrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
            bool reproject = rebuilt || matrix != _cameraMatrix || _viewport != _camera.pixelRect;
            _cameraMatrix = matrix;
            _viewport = _camera.pixelRect;
            bool worldDisplayed = _hud.IsHudVisible && !_cinematic.IsActive && !_inputLock.IsLocked;
            bool mapDisplayed = worldDisplayed && view.IsVisible && view.gameObject.activeInHierarchy;
            for (int i = model.ActiveCount - 1; i >= 0; i--)
            {
                int source = model.GetActiveSource(i);
                Vector3 anchor = _sources.GetPosition(source);
                bool sameTransition = _revisions[source] == _sources.GetRevision(source);
                if (reproject || !sameTransition || _anchors[source] != anchor)
                {
                    _anchors[source] = anchor;
                    Project(source, anchor);
                }
                StructureStatusIconRecord record = model.Get(source);
                _world[source].SetAppearance(_data.StructureStatusIcons, record.Kind, record.State);
                _map[source].SetAppearance(_data.StructureStatusIcons, record.Kind, record.State);
                model.Advance(source, seconds,
                    sameTransition && _worldDisplayed[source] && worldDisplayed && _inside[source],
                    sameTransition && _mapDisplayed[source] && mapDisplayed);
                record = model.Get(source);
                _worldDisplayed[source] = worldDisplayed && record.WorldActive && _inside[source];
                _mapDisplayed[source] = mapDisplayed && record.MinimapActive;
                _revisions[source] = _sources.GetRevision(source);
                float fadeSeconds = sameTransition ? seconds : 0f;
                _world[source].Display(_worldDisplayed[source], record.WorldAlpha, _projected[source], fadeSeconds);
                _map[source].Display(_mapDisplayed[source], record.MinimapAlpha, view.GetPosition(anchor), fadeSeconds);
                _previous[_previousCount++] = source;
            }
        }

        private void Project(int source, Vector3 anchor)
        {
            Vector3 point = _camera.WorldToScreenPoint(anchor);
            point.y += WORLD_VERTICAL_OFFSET;
            float halfSize = WORLD_SIZE * 0.6f;
            _inside[source] = point.z > _camera.nearClipPlane && point.z < _camera.farClipPlane &&
                point.x >= _viewport.xMin + halfSize && point.x <= _viewport.xMax - halfSize &&
                point.y >= _viewport.yMin + halfSize && point.y <= _viewport.yMax - halfSize;
            _projected[source] = (Vector2)point - _viewport.min;
        }

        private void Prewarm(MiniMapUi view)
        {
            ReleaseViews();
            int count = _sources.Count;
            _world = new StructureStatusIconView[count];
            _map = new StructureStatusIconView[count];
            _anchors = new Vector3[count];
            _projected = new Vector2[count];
            _inside = new bool[count];
            _worldDisplayed = new bool[count];
            _mapDisplayed = new bool[count];
            _revisions = new int[count];
            _previous = new int[count];
            _worldParent = (RectTransform)_layer.transform;
            for (int i = 0; i < count; i++)
            {
                _world[i] = StructureStatusIconView.Create(_worldParent, WORLD_SIZE, _layer.gameObject.layer);
                _layer.Attach(_world[i].Root);
                _map[i] = StructureStatusIconView.Create(view.StructureIconParent, MINIMAP_SIZE, view.gameObject.layer);
                _anchors[i] = new Vector3(float.NaN, float.NaN, float.NaN);
            }
            _version = _sources.Version;
        }

        private void ReleaseViews()
        {
            for (int i = 0; i < _world.Length; i++) { _world[i].Release(); _map[i].Release(); }
            _world = Array.Empty<StructureStatusIconView>();
            _map = Array.Empty<StructureStatusIconView>();
            _previousCount = 0;
        }

        public void LateDispose()
        {
            _disposed = true;
            ReleaseViews();
        }
    }
}
