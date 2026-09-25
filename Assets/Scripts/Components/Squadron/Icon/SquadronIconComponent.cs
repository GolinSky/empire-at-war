using EmpireAtWar.Models.Factions;
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
    /// drawn slightly above the smoothed squadron centroid. It keeps a constant on-screen size so tiny fighters
    /// stay easy to find and click at any zoom, and it is the squadron's click target for selection and attack orders.
    /// </summary>
    public sealed class SquadronIconComponent : MonoComponent<SelectionModel>, ISquadronIconCommand,
        IInitializable, ILateTickable, ILateDisposable
    {
        [SerializeField] private Canvas iconCanvas;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image silhouetteImage;
        [Tooltip("Visible marker width and height in screen pixels.")]
        [SerializeField, Min(1f)] private float screenSize = 32f;
        [Tooltip("Clickable square width and height in screen pixels.")]
        [SerializeField, Min(1f)] private float clickSize = 44f;
        [Tooltip("Screen pixels the marker sits above the squadron centroid.")]
        [SerializeField] private float screenOffset = 20f;
        [Tooltip("How fast the marker catches up with the centroid; higher follows fighters more tightly.")]
        [SerializeField, Min(0.01f)] private float followSharpness = 12f;
        [SerializeField] private Color friendlyColor = new Color(0.55f, 1f, 0.55f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.25f);

        private ICameraService _cameraService;
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private Vector3 _anchor;
        private Vector3 _iconPosition;
        private bool _isReleased;

        [Inject]
        private void Construct(SelectionModel model, ICameraService cameraService, FogOfWarSystem fogOfWarSystem,
            PlayerType playerType)
        {
            SetModel(model);
            _cameraService = cameraService;
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
        }

        public void Initialize()
        {
            ((RectTransform)iconCanvas.transform).sizeDelta = Vector2.one * screenSize;
            _anchor = transform.position;
            Model.OnSelected += UpdateColor;
            UpdateColor(Model.IsSelected);
            LateTick();
        }

        public void LateTick()
        {
            if (_isReleased)
            {
                return;
            }

            _anchor = Vector3.Lerp(_anchor, transform.position, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            iconCanvas.enabled = _playerType == PlayerType.Player || !_fogOfWarSystem.IsHidden(transform.position);
            if (!iconCanvas.enabled)
            {
                return;
            }

            Transform cameraTransform = _cameraService.CameraTransform;
            float distance = Vector3.Dot(_anchor - _cameraService.CameraPosition, _cameraService.CameraForward);
            float worldPerPixel = 2f * distance * Mathf.Tan(_cameraService.FieldOfView * 0.5f * Mathf.Deg2Rad) /
                                  Screen.height;
            _iconPosition = _anchor + cameraTransform.up * (screenOffset * worldPerPixel);
            iconCanvas.transform.SetPositionAndRotation(_iconPosition, cameraTransform.rotation);
            iconCanvas.transform.localScale = Vector3.one * worldPerPixel;
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_isReleased || !iconCanvas.enabled ||
                _cameraService.WorldToViewportPoint(_iconPosition).z <= 0f)
            {
                return false;
            }

            Vector2 delta = _cameraService.WorldToScreenPoint(_iconPosition) - screenPoint;
            float halfSize = clickSize * 0.5f;
            return Mathf.Abs(delta.x) <= halfSize && Mathf.Abs(delta.y) <= halfSize;
        }

        public void LateDispose() => Release();

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
            Color color = _playerType != PlayerType.Player ? enemyColor
                : isSelected ? selectedColor
                : friendlyColor;
            frameImage.color = color;
            silhouetteImage.color = color;
        }
    }
}
