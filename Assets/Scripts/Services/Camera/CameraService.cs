using DG.Tweening;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.InputService;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraService : IService
    {
        Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position);
        RaycastHit ScreenPointToRay(Vector2 screenPoint);
        Vector3 CameraPosition { get; }
        Transform CameraTransform { get; }
        Vector3 CameraForward { get; }
        float FieldOfView { get; }
        Vector3 WorldToViewportPoint(Vector3 currentPosition);
        Vector2 WorldToScreenPoint(Vector3 position);
        void MoveTo(Vector3 worldPoint);
    }

    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraService : MonoBehaviour, ICameraService, IInitializable, ILateDisposable
    {
        [SerializeField] private Ease _moveEase = Ease.OutExpo;

        private Plane _plane = new();
        private UnityEngine.Camera _camera;
        private CameraData _cameraData;
        private IInputService _inputService;
        private Tween _moveTween;

        public string Id => nameof(CameraService);

        public Vector3 CameraPosition => transform.position;
        public Transform CameraTransform => transform;
        public Vector3 CameraForward => transform.forward;
        public float FieldOfView => _camera.fieldOfView;

        [Inject]
        public void Constructor(CameraData cameraData, IInputService inputService)
        {
            _cameraData = cameraData;
            _inputService = inputService;
        }

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        public void Initialize()
        {
            _inputService.OnSwipe += OnSwipe;
            _inputService.OnZoom += ZoomCamera;
        }

        public void LateDispose()
        {
            _inputService.OnSwipe -= OnSwipe;
            _inputService.OnZoom -= ZoomCamera;
            _moveTween?.Kill();
        }

        public Vector3 WorldToViewportPoint(Vector3 currentPosition)
        {
            return _camera.WorldToViewportPoint(currentPosition);
        }

        public Vector2 WorldToScreenPoint(Vector3 position)
        {
            return _camera.WorldToScreenPoint(position);
        }

        public Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            _plane.SetNormalAndPosition(Vector3.up, Vector3.up * position.y);

            if (_plane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);

            return ScreenPointToRay(screenPoint).point;
        }

        public RaycastHit ScreenPointToRay(Vector2 screenPoint)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            Physics.Raycast(ray, out RaycastHit hit);
            return hit;
        }

        public void MoveTo(Vector3 worldPoint)
        {
            Vector3 targetCameraPosition = worldPoint;

            if (Mathf.Abs(CameraForward.y) > Mathf.Epsilon)
            {
                float distanceToTargetPlane = (worldPoint.y - CameraPosition.y) / CameraForward.y;
                targetCameraPosition = worldPoint - CameraForward * distanceToTargetPlane;
            }

            targetCameraPosition.y = CameraPosition.y;
            SetPosition(ClampPosition(targetCameraPosition), true);
        }

        private void OnSwipe(Vector2 direction)
        {
            Vector3 worldDirection = new(direction.x, 0, direction.y);
            Vector3 move = -worldDirection * _cameraData.PanSpeed * Time.unscaledDeltaTime;
            SetPosition(ClampPosition(CameraPosition + move), true);
        }

        private void ZoomCamera(float scrollDelta)
        {
            scrollDelta = Mathf.Clamp(scrollDelta, -10, 10);
            Vector3 newPosition = CameraPosition - CameraForward * scrollDelta * _cameraData.ZoomSpeed * Time.unscaledDeltaTime;

            if (!_cameraData.ZoomRange.IsInRange(newPosition.y))
                return;

            newPosition.y = _cameraData.ZoomRange.Clamp(newPosition.y);
            SetPosition(ClampPosition(newPosition), false);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            float heightPercentage = Mathf.InverseLerp(
                _cameraData.ZoomRange.Min,
                _cameraData.ZoomRange.Max,
                CameraPosition.y);
            float xMin = Mathf.Lerp(_cameraData.MinMoveRangeX.Min.x, _cameraData.MaxMoveRangeY.Min.x, heightPercentage);
            float xMax = Mathf.Lerp(_cameraData.MinMoveRangeX.Max.x, _cameraData.MaxMoveRangeY.Max.x, heightPercentage);
            float zMin = Mathf.Lerp(_cameraData.MinMoveRangeX.Min.y, _cameraData.MaxMoveRangeY.Min.y, heightPercentage);
            float zMax = Mathf.Lerp(_cameraData.MinMoveRangeX.Max.y, _cameraData.MaxMoveRangeY.Max.y, heightPercentage);

            position.x = Mathf.Clamp(position.x, xMin, xMax);
            position.z = Mathf.Clamp(position.z, zMin, zMax);
            return position;
        }

        private void SetPosition(Vector3 position, bool useTween)
        {
            _moveTween?.Kill();

            if (useTween)
            {
                _moveTween = transform
                    .DOMove(position, _cameraData.TweenSpeed)
                    .SetEase(_moveEase);
                return;
            }

            transform.position = position;
        }
    }
}
