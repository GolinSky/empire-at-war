using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Player;
using UnityEngine;
using UnityEngine.UI;
using EmpireAtWar.Services.Vision;
using Zenject;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Floating marker that stands in for the whole squadron: a team-colored frame and shadow behind the fighter icon,
    /// drawn slightly above the smoothed squadron centroid. Its screen size follows camera zoom so tiny fighters
    /// stay easy to find and click, and it is the squadron's click target for selection and attack orders.
    /// </summary>
    public sealed class SquadronIconComponent : MonoComponent<SelectionModel>, ISquadronIconCommand,
        IInitializable, ILateTickable, ILateDisposable
    {
        private ICameraService _cameraService;
        private IVisionService _visionService;
        private ILocalPlayer _localPlayer;
        private IPlayerColors _playerColors;

        [SerializeField] private Canvas iconCanvas;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image silhouetteImage;
        [SerializeField] private Image iconImage;
        private CameraData _cameraData;

        private PlayerId _owner;
        private Vector3 _anchor;
        private Vector3 _iconPosition;

        [Tooltip("Visible marker width and height in screen pixels at the midpoint of camera zoom.")]
        [SerializeField, Min(1f)] private float screenSize = 72f;
        [Tooltip("Clickable square width and height in screen pixels at the midpoint of camera zoom.")]
        [SerializeField, Min(1f)] private float clickSize = 132f;
        [Tooltip("Screen pixels the marker sits above the squadron centroid at the midpoint of camera zoom.")]
        [SerializeField] private float screenOffset = 60f;
        [Tooltip("How fast the marker catches up with the centroid; higher follows fighters more tightly.")]
        [SerializeField, Min(0.01f)] private float followSharpness = 12f;
        private float _zoomScale;

        private bool _isReleased;

        [Inject]
        private void Construct(ICameraService cameraService, IVisionService visionService, ILocalPlayer localPlayer,
            SelectionModel model, CameraData cameraData, PlayerId owner, IPlayerColors playerColors)
        {
            SetModel(model);
            _cameraService = cameraService;
            _cameraData = cameraData;
            _visionService = visionService;
            _owner = owner;
            _localPlayer = localPlayer;
            _playerColors = playerColors;
        }

        public void Initialize()
        {
            ((RectTransform)iconCanvas.transform).sizeDelta = Vector2.one * screenSize;
            _anchor = transform.position;
            Model.OnSelected += UpdateColor;
            UpdateColor(Model.IsSelected);
            LateTick();
        }

        public void LateDispose() => Release();

        public void LateTick()
        {
            if (_isReleased)
            {
                return;
            }

            _anchor = Vector3.Lerp(_anchor, transform.position, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            iconCanvas.enabled = _localPlayer.IsFriendly(_owner) || _visionService.IsVisible(_localPlayer.Id, transform.position);
            if (!iconCanvas.enabled)
            {
                return;
            }

            Transform cameraTransform = _cameraService.CameraTransform;
            float distance = Vector3.Dot(_anchor - _cameraService.CameraPosition, _cameraService.CameraForward);
            float worldPerPixel = 2f * distance * Mathf.Tan(_cameraService.FieldOfView * 0.5f * Mathf.Deg2Rad) /
                                  Screen.height;
            float zoom = Mathf.InverseLerp(_cameraData.ZoomRange.Min, _cameraData.ZoomRange.Max,
                _cameraService.CameraPosition.y);
            _zoomScale = Mathf.Lerp(1.5f, 0.5f, zoom);
            _iconPosition = _anchor + cameraTransform.up * (screenOffset * _zoomScale * worldPerPixel);
            iconCanvas.transform.SetPositionAndRotation(_iconPosition, cameraTransform.rotation);
            iconCanvas.transform.localScale = Vector3.one * (worldPerPixel * _zoomScale);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_isReleased || !iconCanvas.enabled ||
                _cameraService.WorldToViewportPoint(_iconPosition).z <= 0f)
            {
                return false;
            }

            Vector2 delta = _cameraService.WorldToScreenPoint(_iconPosition) - screenPoint;
            float halfSize = clickSize * _zoomScale * 0.5f;
            return Mathf.Abs(delta.x) <= halfSize && Mathf.Abs(delta.y) <= halfSize;
        }

        public override void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            Model.OnSelected -= UpdateColor;
            iconCanvas.enabled = false;
        }

        private void UpdateColor(bool isSelected)
        {
            Color color = _playerColors.GetColor(_owner);
            frameImage.color = color;
            color.a = isSelected ? 0.35f : 0.2f;
            silhouetteImage.color = color;
            iconImage.color = Color.white;
        }
    }
}
