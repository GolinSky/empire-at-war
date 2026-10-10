using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Utils;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraService : IService
    {
        Vector3 CameraPosition { get; }
        Transform CameraTransform { get; }
        Vector3 CameraForward { get; }
        float FieldOfView { get; }

        Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position);

        RaycastHit ScreenPointToRay(Vector2 screenPoint);

        Vector3 WorldToViewportPoint(Vector3 currentPosition);

        Vector2 WorldToScreenPoint(Vector3 position);

        IReadOnlyList<Vector3> GetGroundFootprint(Vector2 mapMin, Vector2 mapMax);

        void MoveTo(Vector3 worldPoint);

        void SetPose(Vector3 position, Quaternion rotation);
    }

    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraService : MonoBehaviour, ICameraService, IInitializable, ILateDisposable, ITickable,
        IObserver<BattleState>
    {
        // Load hitches would otherwise turn one frame of held input into a long jump.
        private const float MAX_FRAME_DELTA = 0.05f;

        private IMapModelObserver _mapModel;
        private INotifier<BattleState> _battleState;
        private ICameraInput _cameraInput;
        private ICameraPreferences _preferences;
        private IInputLock _inputLock;

        [SerializeField] private UnityEngine.Camera sceneCamera;
        private CameraData _cameraData;
        private readonly CameraFrustumProjection _frustumProjection = new CameraFrustumProjection();

        private Plane _plane = new();
        private Vector2 _keyboardInput;
        private Vector2 _keyboardVelocity;
        // The map bounds exist once the battle runs; the camera stays movable while paused.
        private bool _canMove;

        public string Id => nameof(CameraService);

        private float PanSpeed => _cameraData.PanSpeed * _preferences.PanSpeedMultiplier;
        private static float FrameDelta => Mathf.Min(Time.unscaledDeltaTime, MAX_FRAME_DELTA);

        public Vector3 CameraPosition => transform.position;
        public Transform CameraTransform => transform;
        public Vector3 CameraForward => transform.forward;
        public float FieldOfView => sceneCamera.fieldOfView;

        [Inject]
        public void Constructor(
            ICameraInput cameraInput,
            IMapModelObserver mapModel,
            ICameraPreferences preferences,
            IInputLock inputLock,
            INotifier<BattleState> battleState,
            CameraData cameraData)
        {
            _battleState = battleState;
            _inputLock = inputLock;
            _preferences = preferences;
            _cameraData = cameraData;
            _mapModel = mapModel;
            _cameraInput = cameraInput;
        }

        public void Initialize()
        {
            _keyboardInput = Vector2.zero;
            _keyboardVelocity = Vector2.zero;
            _cameraInput.Zoomed += ZoomCamera;
            _cameraInput.Panned += PanCamera;
            _inputLock.LockChanged += OnLockChanged;
            _battleState.AddObserver(this);
        }

        public void UpdateState(BattleState state)
        {
            _canMove = state == BattleState.Running || state == BattleState.Paused;
            if (!_canMove)
            {
                _keyboardVelocity = Vector2.zero;
            }
        }

        public void LateDispose()
        {
            _cameraInput.Zoomed -= ZoomCamera;
            _cameraInput.Panned -= PanCamera;
            _inputLock.LockChanged -= OnLockChanged;
            _battleState.RemoveObserver(this);
            _keyboardInput = Vector2.zero;
            _keyboardVelocity = Vector2.zero;
        }

        public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 mapMin, Vector2 mapMax)
        {
            return _frustumProjection.Project(sceneCamera, mapMin, mapMax);
        }

        public Vector3 WorldToViewportPoint(Vector3 currentPosition)
        {
            return sceneCamera.WorldToViewportPoint(currentPosition);
        }

        public Vector2 WorldToScreenPoint(Vector3 position)
        {
            return sceneCamera.WorldToScreenPoint(position);
        }

        public Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position)
        {
            Ray ray = sceneCamera.ScreenPointToRay(screenPoint);
            _plane.SetNormalAndPosition(Vector3.up, Vector3.up * position.y);

            if (_plane.Raycast(ray, out float distance))
                return ray.GetPoint(distance);

            return ScreenPointToRay(screenPoint).point;
        }

        public RaycastHit ScreenPointToRay(Vector2 screenPoint)
        {
            Ray ray = sceneCamera.ScreenPointToRay(screenPoint);
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
            SetPosition(ClampPosition(targetCameraPosition));
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            _keyboardVelocity = Vector2.zero;
            transform.SetPositionAndRotation(position, rotation);
        }

        public void Tick()
        {
            if (!_canMove || _inputLock.IsLocked)
            {
                return;
            }

            _keyboardInput = _cameraInput.Move;
            _keyboardVelocity = VelocitySmoothing.MoveTowardsTarget(
                _keyboardVelocity,
                _keyboardInput,
                PanSpeed,
                _cameraData.PanAcceleration,
                _cameraData.PanDeceleration,
                FrameDelta);
            if (_keyboardVelocity.sqrMagnitude <= Mathf.Epsilon)
            {
                _keyboardVelocity = Vector2.zero;
                return;
            }

            Vector3 move = GetPlanarDirection(_keyboardVelocity) * FrameDelta;
            SetPosition(ClampPosition(CameraPosition + move));
        }

        private void PanCamera(Vector2 direction)
        {
            if (!_canMove)
            {
                return;
            }

            Vector2 normalizedDirection = Vector2.ClampMagnitude(direction, 1f);
            Vector3 move = GetPlanarDirection(normalizedDirection) *
                PanSpeed *
                FrameDelta;
            SetPosition(ClampPosition(CameraPosition + move));
        }

        private void OnLockChanged(bool isLocked)
        {
            if (isLocked)
            {
                _keyboardVelocity = Vector2.zero;
            }
        }

        private Vector3 GetPlanarDirection(Vector2 input)
        {
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            return right * input.x + forward * input.y;
        }

        private void ZoomCamera(float scrollDelta)
        {
            if (!_canMove)
            {
                return;
            }

            scrollDelta = Mathf.Clamp(scrollDelta, -10, 10);
            if (_preferences.InvertZoom)
            {
                scrollDelta = -scrollDelta;
            }

            float zoomSpeed = _cameraData.ZoomSpeed * _preferences.ZoomSpeedMultiplier;
            Vector3 newPosition = CameraPosition - CameraForward * scrollDelta * zoomSpeed * FrameDelta;

            if (!_cameraData.ZoomRange.IsInRange(newPosition.y))
                return;

            newPosition.y = _cameraData.ZoomRange.Clamp(newPosition.y);
            SetPosition(ClampPosition(newPosition));
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            float heightPercentage = Mathf.InverseLerp(
                _cameraData.ZoomRange.Min,
                _cameraData.ZoomRange.Max,
                position.y);
            Vector2 min = _mapModel.SizeRange.Min + Vector2.Lerp(
                _cameraData.MinZoomPadding.Min, _cameraData.MaxZoomPadding.Min, heightPercentage);
            Vector2 max = _mapModel.SizeRange.Max + Vector2.Lerp(
                _cameraData.MinZoomPadding.Max, _cameraData.MaxZoomPadding.Max, heightPercentage);

            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.z = Mathf.Clamp(position.z, min.y, max.y);
            return position;
        }

        private void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
