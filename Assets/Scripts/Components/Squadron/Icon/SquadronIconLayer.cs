using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Layer;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Screen-space canvas of the core game camera. It sits on the WorldUi layer, so the cinema camera's
    /// culling mask hides the markers as it hid the old per-squadron world canvases. No scaler: one canvas unit
    /// is one screen pixel.
    /// </summary>
    public sealed class SquadronIconLayer : MonoBehaviour, ISquadronIconLayer
    {
        // Just past the near plane: markers draw over the battle, below overlay HUD canvases.
        private const float PLANE_DISTANCE_PAST_NEAR_CLIP = 0.05f;

        private RectTransform _root;

        [Inject]
        private void Construct(ICameraService cameraService, ILayerService layerService)
        {
            gameObject.name = "SquadronIconLayer";
            gameObject.layer = layerService.GetLayer(LayerKey.WorldUi);
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
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
    }
}
