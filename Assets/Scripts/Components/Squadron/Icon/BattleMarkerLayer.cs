using System.Collections.Generic;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Layer;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public sealed class BattleMarkerLayer : MonoBehaviour, IBattleMarkerLayer, ITickable, ILateTickable, ILateDisposable
    {
        private const float SNAPSHOT_INTERVAL = 0.1f;
        // Below every HUD overlay (health/hardpoint overlays use -1, prefabs down to -10).
        private const int MARKER_SORTING_ORDER = -20;
        private const float SHIP_MARKER_SCALE = 1.848f;
        private const float SHIP_FRAME_VERTICAL_INSET = 24f;
        private static readonly ProfilerMarker SnapshotSampler = new ProfilerMarker("Battle.Markers.Snapshot");
        private static readonly ProfilerMarker VisionSampler = new ProfilerMarker("Battle.Markers.Vision");
        private static readonly ProfilerMarker CompletionSampler = new ProfilerMarker("Battle.Markers.Consume");
        private static readonly ProfilerMarker LayoutSampler = new ProfilerMarker("Battle.Markers.Layout");
        private readonly List<IBattleMarker> _markers = new List<IBattleMarker>();
        private readonly List<IBattleHull> _hulls = new List<IBattleHull>();
        private readonly Dictionary<IBattleMarker, int> _indices = new Dictionary<IBattleMarker, int>();
        private readonly SquadronIconGrouping _grouping = new SquadronIconGrouping();
        private readonly BattleMarkerProjection _projection = new BattleMarkerProjection();
        private ICameraService _cameraService;
        private CameraData _cameraData;
        private ICinematicCameraModelObserver _cinematic;
        private RectTransform _squadronRoot;
        private RectTransform _shipRoot;
        private Material _iconMaterial;
        private Material _frameMaterial;
        private float _elapsed;
        private int _nextDrawOrder;
        private bool _dirty = true;
        private bool _disposed;

        public IReadOnlyList<IBattleMarker> Markers => _markers;

        [Inject]
        private void Construct(ICameraService cameraService, ILayerService layerService, CameraData cameraData,
            ICinematicCameraModelObserver cinematic)
        {
            _cameraService = cameraService;
            _cameraData = cameraData;
            _cinematic = cinematic;
            gameObject.layer = layerService.GetLayer(LayerKey.WorldUi);
            Canvas canvas = gameObject.AddComponent<Canvas>();
            // Overlay keeps vertices in screen pixels. A camera canvas at the near plane (29.9° FOV) puts
            // one pixel at ~0.0002 world units, below float precision at map coordinates → icon/frame wobble.
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = MARKER_SORTING_ORDER;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                              AdditionalCanvasShaderChannels.TexCoord2;
            // Squadron and ship markers move independently; separate canvases keep one from rebuilding the other's batches.
            _shipRoot = CreateSubCanvas("ShipMarkers", canvas);
            _squadronRoot = CreateSubCanvas("SquadronMarkers", canvas);
        }

        private RectTransform CreateSubCanvas(string name, Canvas parent)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = gameObject.layer;
            var rect = (RectTransform)child.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Canvas canvas = child.AddComponent<Canvas>();
            canvas.additionalShaderChannels = parent.additionalShaderChannels;
            return rect;
        }

        internal void SetMaterials(Material iconMaterial, Material frameMaterial)
        {
            _iconMaterial = iconMaterial;
            _frameMaterial = frameMaterial;
        }

        public void Attach(RectTransform icon, bool isSquadron)
        {
            icon.SetParent(isSquadron ? _squadronRoot : _shipRoot, false);
            icon.SetAsLastSibling();
            icon.anchorMin = Vector2.zero;
            icon.anchorMax = Vector2.zero;
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.localRotation = Quaternion.identity;
            icon.localScale = Vector3.one;
        }

        public BattleMarker CreateShipMarker(Sprite sprite, Transform anchor)
        {
            GameObject root = new GameObject("ShipBattleMarker", typeof(RectTransform));
            root.layer = gameObject.layer;
            root.SetActive(false);
            var rect = (RectTransform)root.transform;
            Image frame = CreateImage("Frame", rect, 0f);
            frame.rectTransform.sizeDelta = new Vector2(0f, -SHIP_FRAME_VERTICAL_INSET);
            frame.material = _frameMaterial;
            frame.gameObject.AddComponent<SquadronFrameMeshEffect>().UseTeamFrame = true;
            Image icon = CreateImage("Icon", rect, 5.5f * SHIP_MARKER_SCALE);
            icon.material = _iconMaterial;
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            return new BattleMarker(this, rect, frame, icon, 60f * SHIP_MARKER_SCALE, screenOffset: 80f)
                { AnchorTransform = anchor };
        }

        private Image CreateImage(string name, Transform parent, float padding)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.layer = gameObject.layer;
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.sizeDelta = Vector2.one * (-2f * padding);
            return image;
        }

        public void Register(IBattleMarker marker)
        {
            ((BattleMarker)marker).DrawOrder = _nextDrawOrder++;
            _indices.Add(marker, _markers.Count);
            _markers.Add(marker);
            _dirty = true;
        }

        public void Unregister(IBattleMarker marker)
        {
            if (!_indices.Remove(marker, out int index)) return;
            int last = _markers.Count - 1;
            if (index != last)
            {
                IBattleMarker moved = _markers[last];
                _markers[index] = moved;
                _indices[moved] = index;
            }

            _markers.RemoveAt(last);
            InvalidateMembership(marker.EntityId);
        }

        public void RegisterHull(IBattleHull hull) => _hulls.Add(hull);

        public void UnregisterHull(IBattleHull hull) => _hulls.Remove(hull);

        public void InvalidateMembership(long entityId)
        {
            if (_disposed) return;
            _grouping.Forget(entityId);
            _dirty = true;
        }

        public void Tick()
        {
            if (_disposed) return;
            _elapsed += Time.deltaTime;
            if (_grouping.HasPendingSnapshot || !_dirty && _elapsed < SNAPSHOT_INTERVAL) return;

            using (VisionSampler.Auto())
            {
                for (int i = 0; i < _markers.Count; i++)
                {
                    var marker = (BattleMarker)_markers[i];
                    if (marker.IsSquadron) marker.SampleVisibility();
                }
            }

            using (SnapshotSampler.Auto())
            {
                float zoom = CurrentZoomScale();
                _projection.Prepare(_cameraService.SceneCamera);
                _grouping.BeginSnapshot(_markers.Count);
                for (int i = 0; i < _markers.Count; i++)
                {
                    var marker = (BattleMarker)_markers[i];
                    if (!marker.IsSquadron || !marker.IsVisible ||
                        !_projection.TryProject(marker.Position, out Vector2 screen)) continue;
                    _grouping.Add(marker.EntityId, marker.Owner.GetHashCode(), screen);
                }

                _grouping.Schedule(_elapsed, BattleMarkerProjection.JoinPixels(zoom),
                    BattleMarkerProjection.LeavePixels(zoom), BattleMarkerProjection.Pitch(zoom));
            }
            _elapsed = 0f;
            _dirty = false;
        }

        public void LateTick()
        {
            if (_disposed) return;
            using (CompletionSampler.Auto()) _grouping.TryComplete();
            if (_markers.Count == 0) return;
            if (_cinematic.IsActive)
            {
                for (int i = 0; i < _markers.Count; i++) ((BattleMarker)_markers[i]).Hide();
                return;
            }

            using (LayoutSampler.Auto())
                _projection.Draw(_markers, _hulls, _grouping, _cameraService.SceneCamera, CurrentZoomScale(),
                    Time.unscaledDeltaTime);
        }

        private float CurrentZoomScale()
        {
            float zoom = Mathf.InverseLerp(_cameraData.ZoomRange.Min, _cameraData.ZoomRange.Max,
                _cameraService.CameraPosition.y);
            return Mathf.Lerp(1.5f, 0.5f, zoom);
        }

        public void LateDispose()
        {
            if (_disposed) return;
            _disposed = true;
            _grouping.Dispose();
        }

        private void OnDestroy() => LateDispose();
    }
}
