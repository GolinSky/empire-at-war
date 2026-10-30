using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Layer;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using System.Collections.Generic;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Screen-space canvas of the core game camera. It sits on the WorldUi layer, so the cinema camera's
    /// culling mask hides the markers as it hid the old per-squadron world canvases. No scaler: one canvas unit
    /// is one screen pixel.
    /// </summary>
    public sealed class SquadronIconLayer : MonoBehaviour, ISquadronIconLayer, ILateTickable
    {
        // Just past the near plane: markers draw over the battle, below overlay HUD canvases.
        private const float PLANE_DISTANCE_PAST_NEAR_CLIP = 0.05f;

        private RectTransform _root;
        private readonly List<SquadronIconComponent> _icons = new List<SquadronIconComponent>();
        private ICameraService _cameraService;
        private CameraData _cameraData;

        [Inject]
        private void Construct(ICameraService cameraService, ILayerService layerService, CameraData cameraData)
        {
            _cameraService = cameraService;
            _cameraData = cameraData;
            gameObject.name = "SquadronIconLayer";
            gameObject.layer = layerService.GetLayer(LayerKey.WorldUi);
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            // UV1: Squadron Frame centre offsets (SquadronFrameMeshEffect); UV1/UV2: MPUIKit shapes.
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                              AdditionalCanvasShaderChannels.TexCoord2;
            canvas.worldCamera = cameraService.SceneCamera;
            canvas.planeDistance = cameraService.SceneCamera.nearClipPlane + PLANE_DISTANCE_PAST_NEAR_CLIP;
            _root = (RectTransform)transform;
        }

        public void Attach(RectTransform icon)
        {
            icon.SetParent(_root, false);
            icon.anchorMin = Vector2.zero;
            icon.anchorMax = Vector2.zero;
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.localRotation = Quaternion.identity;
            icon.localScale = Vector3.one;
        }

        public void Register(SquadronIconComponent icon) => _icons.Add(icon);

        public void Unregister(SquadronIconComponent icon) => _icons.Remove(icon);

        public void LateTick()
        {
            if (_icons.Count == 0) return;
            UnityEngine.Camera camera = _cameraService.SceneCamera;
            Matrix4x4 worldToClip = camera.projectionMatrix * camera.worldToCameraMatrix;
            Vector3 position = _cameraService.CameraPosition;
            Vector3 forward = _cameraService.CameraForward;
            float zoom = Mathf.InverseLerp(_cameraData.ZoomRange.Min, _cameraData.ZoomRange.Max, position.y);
            float zoomScale = Mathf.Lerp(1.5f, 0.5f, zoom);
            int width = Screen.width;
            int height = Screen.height;
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _icons.Count; i++)
                _icons[i].Project(worldToClip, position, forward, width, height, zoomScale, deltaTime);
        }
    }
}
