using EmpireAtWar.Entities.Squadrons;
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
    /// Floating icon that stands in for the whole squadron above its centroid. It keeps a constant
    /// on-screen size so tiny fighters stay easy to find and click at any zoom, and it is the squadron's
    /// click target for selection and attack orders.
    /// </summary>
    public sealed class SquadronIconComponent : MonoComponent<SelectionModel>, ISquadronIconCommand,
        IInitializable, ILateTickable, ILateDisposable
    {
        [SerializeField] private Canvas iconCanvas;
        [SerializeField] private Image iconImage;
        [Tooltip("Icon width and height in screen pixels.")]
        [SerializeField, Min(1f)] private float screenSize = 48f;
        [SerializeField] private float heightOffset = 3f;
        [SerializeField] private Color friendlyColor = new Color(0.55f, 1f, 0.55f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.25f);

        private ICameraService _cameraService;
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private Sprite _icon;
        private bool _isReleased;

        private Vector3 IconPosition => transform.position + Vector3.up * heightOffset;

        [Inject]
        private void Construct(SelectionModel model, ICameraService cameraService, FogOfWarSystem fogOfWarSystem,
            PlayerType playerType, FactionsData factionsData, SquadronType squadronType)
        {
            SetModel(model);
            _icon = factionsData.GetSquadronFactionData(squadronType).Icon;
            _cameraService = cameraService;
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
        }

        public void Initialize()
        {
            iconImage.sprite = _icon;
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

            iconCanvas.enabled = _playerType == PlayerType.Player || !_fogOfWarSystem.IsHidden(transform.position);
            if (!iconCanvas.enabled)
            {
                return;
            }

            Vector3 position = IconPosition;
            float distance = Vector3.Dot(position - _cameraService.CameraPosition, _cameraService.CameraForward);
            float worldPerPixel = 2f * distance * Mathf.Tan(_cameraService.FieldOfView * 0.5f * Mathf.Deg2Rad) /
                                  Screen.height;
            iconCanvas.transform.SetPositionAndRotation(position, _cameraService.CameraTransform.rotation);
            iconCanvas.transform.localScale = Vector3.one * (screenSize * worldPerPixel);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_isReleased || !iconCanvas.enabled ||
                _cameraService.WorldToViewportPoint(IconPosition).z <= 0f)
            {
                return false;
            }

            float radius = screenSize * 0.5f;
            return (_cameraService.WorldToScreenPoint(IconPosition) - screenPoint).sqrMagnitude <= radius * radius;
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
            iconImage.color = _playerType != PlayerType.Player ? enemyColor
                : isSelected ? selectedColor
                : friendlyColor;
        }
    }
}
