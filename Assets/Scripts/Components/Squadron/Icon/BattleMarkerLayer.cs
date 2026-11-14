using System.Collections.Generic;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Layer;
using MPUIKIT;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    public sealed class BattleMarkerLayer : MonoBehaviour, IBattleMarkerLayer, ITickable, ILateTickable, ILateDisposable
    {
        private const float SNAPSHOT_INTERVAL = 0.1f;
        private const float PLANE_DISTANCE_PAST_NEAR_CLIP = 0.05f;
        private const float SHIP_MARKER_SCALE = 1.848f;
        private static readonly ProfilerMarker SnapshotSampler = new ProfilerMarker("Battle.Markers.Snapshot");
        private static readonly ProfilerMarker VisionSampler = new ProfilerMarker("Battle.Markers.Vision");
        private static readonly ProfilerMarker CompletionSampler = new ProfilerMarker("Battle.Markers.Consume");
        private static readonly ProfilerMarker LayoutSampler = new ProfilerMarker("Battle.Markers.Layout");
        private readonly List<IBattleMarker> _markers = new List<IBattleMarker>();
        private readonly Dictionary<IBattleMarker, int> _indices = new Dictionary<IBattleMarker, int>();
        private readonly SquadronIconGrouping _grouping = new SquadronIconGrouping();
        private readonly BattleMarkerProjection _projection = new BattleMarkerProjection();
        private ICameraService _cameraService;
        private CameraData _cameraData;
        private ICinematicCameraModelObserver _cinematic;
        private RectTransform _root;
        private Material _iconMaterial;
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
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                              AdditionalCanvasShaderChannels.TexCoord2;
            canvas.worldCamera = cameraService.SceneCamera;
            canvas.planeDistance = cameraService.SceneCamera.nearClipPlane + PLANE_DISTANCE_PAST_NEAR_CLIP;
            _root = (RectTransform)transform;
        }

        internal void SetIconMaterial(Material material) => _iconMaterial = material;

        public void Attach(RectTransform icon)
        {
            icon.SetParent(_root, false);
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
            CreateShape("Background", rect, new Color(0f, 0f, 0f, 0.5f), 0f);
            MPImage frame = CreateShape("Frame", rect, Color.white, 1.5f);
            Image icon = CreateImage<Image>("Icon", rect, 6.75f * SHIP_MARKER_SCALE);
            icon.material = _iconMaterial;
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.rectTransform.anchoredPosition = new Vector2(0f, -1.5f * SHIP_MARKER_SCALE);
            return new BattleMarker(this, rect, frame, icon, 60f * SHIP_MARKER_SCALE, 120.96f, screenOffset: 80f)
                { AnchorTransform = anchor };
        }

        private MPImage CreateShape(string name, Transform parent, Color color, float stroke)
        {
            MPImage image = CreateImage<MPImage>(name, parent, 0f);
            image.DrawShape = DrawShape.Rectangle;
            image.color = color;
            image.StrokeWidth = stroke;
            image.FalloffDistance = 1f;
            Rectangle corners = image.Rectangle;
            corners.CornerRadius = Vector4.one * 3f;
            image.Rectangle = corners;
            return image;
        }

        private T CreateImage<T>(string name, Transform parent, float padding) where T : Image
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.layer = gameObject.layer;
            imageObject.transform.SetParent(parent, false);
            T image = imageObject.AddComponent<T>();
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
            if (marker.IsSquadron) _dirty = true;
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
            if (marker.IsSquadron) InvalidateMembership(marker.EntityId);
        }

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
                _grouping.BeginSnapshot(_markers.Count);
                for (int i = 0; i < _markers.Count; i++)
                {
                    var marker = (BattleMarker)_markers[i];
                    if (!marker.IsSquadron || !marker.IsVisible) continue;
                    Vector3 position = marker.Position;
                    Vector3 velocity = marker.Velocity;
                    _grouping.Add(marker.EntityId, marker.Owner.GetHashCode(),
                        new Vector2(position.x, position.z), new Vector2(velocity.x, velocity.z));
                }

                _grouping.Schedule(_elapsed);
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

            UnityEngine.Camera camera = _cameraService.SceneCamera;
            Vector3 position = _cameraService.CameraPosition;
            float zoom = Mathf.InverseLerp(_cameraData.ZoomRange.Min, _cameraData.ZoomRange.Max, position.y);
            using (LayoutSampler.Auto())
                _projection.Draw(_markers, _grouping, camera, Mathf.Lerp(1.5f, 0.5f, zoom), Time.deltaTime);
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
