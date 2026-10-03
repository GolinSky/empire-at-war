using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using UnityEngine;
using UnityEngine.UI;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.Squadrons.Icon
{
    /// <summary>
    /// Floating marker that stands in for the whole squadron: a hollow frame with the fighter silhouette,
    /// drawn slightly above the smoothed squadron centroid. Its screen size follows camera zoom so tiny fighters
    /// stay easy to find and click, and it is the squadron's click target for selection and attack orders.
    /// </summary>
    public sealed class SquadronIconComponent : MonoComponent<SelectionModel>, ISquadronIconCommand,
        IInitializable, ILateTickable, ILateDisposable
    {
        private ICameraService _cameraService;
        private IFogOfWarSystem _fogOfWarSystem;
        private ILocalPlayer _localPlayer;

        [SerializeField] private Canvas iconCanvas;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image silhouetteImage;
        private CameraData _cameraData;

        [SerializeField] private Color friendlyColor = new Color(0.55f, 1f, 0.55f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private Color allyColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.25f);
        private PlayerId _owner;
        private Vector3 _anchor;
        private Vector3 _iconPosition;

        [Tooltip("Visible marker width and height in screen pixels at the midpoint of camera zoom.")]
        [SerializeField, Min(1f)] private float screenSize = 96f;
        [Tooltip("Clickable square width and height in screen pixels at the midpoint of camera zoom.")]
        [SerializeField, Min(1f)] private float clickSize = 132f;
        [Tooltip("Screen pixels the marker sits above the squadron centroid at the midpoint of camera zoom.")]
        [SerializeField] private float screenOffset = 60f;
        [Tooltip("How fast the marker catches up with the centroid; higher follows fighters more tightly.")]
        [SerializeField, Min(0.01f)] private float followSharpness = 12f;
        private float _zoomScale;

        private bool _isReleased;

        [Inject]
        private void Construct(ICameraService cameraService, IFogOfWarSystem fogOfWarSystem, ILocalPlayer localPlayer,
            SelectionModel model, CameraData cameraData, PlayerId owner)
        {
            SetModel(model);
            _cameraService = cameraService;
            _cameraData = cameraData;
            _fogOfWarSystem = fogOfWarSystem;
            _owner = owner;
            _localPlayer = localPlayer;
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
            iconCanvas.enabled = _localPlayer.IsFriendly(_owner) || !_fogOfWarSystem.IsHidden(transform.position);
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
            Color color = !_localPlayer.IsFriendly(_owner) ? enemyColor
                : !_localPlayer.IsLocal(_owner) ? allyColor
                : isSelected ? selectedColor
                : friendlyColor;
            frameImage.color = color;
            silhouetteImage.color = Color.white;
        }
    }
}
