using EmpireAtWar.Entities.BaseEntity;
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
    /// The marker lives on the shared <see cref="ISquadronIconLayer"/> canvas; it is hidden while off screen.
    /// </summary>
    public sealed class SquadronIconComponent : MonoComponent<SelectionModel>, ISquadronIconCommand,
        IInitializable, ILateTickable, ILateDisposable, IRespawnable
    {
        private ICameraService _cameraService;
        private IVisionService _visionService;
        private ILocalPlayer _localPlayer;
        private IPlayerColors _playerColors;
        private ISquadronIconLayer _iconLayer;

        [Tooltip("Marker root, moved onto the shared squadron icon canvas when the squadron spawns.")]
        [SerializeField] private RectTransform iconRoot;
        [SerializeField] private Image frameImage;
        [SerializeField] private Image silhouetteImage;
        [SerializeField] private Image iconImage;
        private CameraData _cameraData;

        private PlayerId _owner;
        private Vector3 _anchor;
        private Vector2 _screenPosition;

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
        private bool _isShown;

        [Inject]
        private void Construct(ICameraService cameraService, IVisionService visionService, ILocalPlayer localPlayer,
            SelectionModel model, CameraData cameraData, PlayerId owner, IPlayerColors playerColors,
            ISquadronIconLayer iconLayer)
        {
            SetModel(model);
            _cameraService = cameraService;
            _cameraData = cameraData;
            _visionService = visionService;
            _owner = owner;
            _localPlayer = localPlayer;
            _playerColors = playerColors;
            _iconLayer = iconLayer;
        }

        public void Initialize()
        {
            _iconLayer.Attach(iconRoot);
            iconRoot.sizeDelta = Vector2.one * screenSize;
            _zoomScale = 0f;
            _isShown = true;
            SetShown(false);
            _anchor = transform.position;
            Model.OnSelected += UpdateColor;
            UpdateColor(Model.IsSelected);
            LateTick();
        }

        public void LateDispose() => Release();

        public void Respawn()
        {
            _isReleased = false;
            Initialize();
        }

        public void LateTick()
        {
            if (_isReleased)
            {
                return;
            }

            _anchor = Vector3.Lerp(_anchor, transform.position, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            Vector3 viewport = _cameraService.WorldToViewportPoint(_anchor);
            float zoom = Mathf.InverseLerp(_cameraData.ZoomRange.Min, _cameraData.ZoomRange.Max,
                _cameraService.CameraPosition.y);
            float zoomScale = Mathf.Lerp(1.5f, 0.5f, zoom);
            _screenPosition = new Vector2(viewport.x * Screen.width,
                viewport.y * Screen.height + screenOffset * zoomScale);
            // Off-screen markers are hidden first: the vision query is the costly check.
            float margin = clickSize * zoomScale;
            bool show = viewport.z > 0f &&
                        _screenPosition.x > -margin && _screenPosition.x < Screen.width + margin &&
                        _screenPosition.y > -margin && _screenPosition.y < Screen.height + margin &&
                        (_localPlayer.IsFriendly(_owner) ||
                         _visionService.IsVisible(_localPlayer.Id, transform.position));
            SetShown(show);
            if (!show)
            {
                return;
            }

            if (zoomScale != _zoomScale)
            {
                _zoomScale = zoomScale;
                iconRoot.localScale = Vector3.one * zoomScale;
            }

            iconRoot.anchoredPosition = _screenPosition;
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            if (_isReleased || !_isShown)
            {
                return false;
            }

            Vector2 delta = _screenPosition - screenPoint;
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
            SetShown(false);
        }

        private void SetShown(bool isShown)
        {
            if (_isShown == isShown)
            {
                return;
            }

            _isShown = isShown;
            iconRoot.gameObject.SetActive(isShown);
        }

        // The marker lives under the shared layer, outside this squadron's hierarchy, so it is destroyed
        // with the squadron. During scene unload the layer may already be gone and took the marker with it.
        private void OnDestroy()
        {
            if (iconRoot != null)
            {
                Destroy(iconRoot.gameObject);
            }
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
