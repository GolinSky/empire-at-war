using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Settings;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Utils;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.Camera
{
    public interface ICameraService : IService
    {
        Vector3 CameraPosition { get; }
        Transform CameraTransform { get; }
        Vector3 CameraForward { get; }
        float FieldOfView { get; }
        UnityEngine.Camera SceneCamera { get; }

        Vector3 GetWorldPoint(Vector2 screenPoint, Vector3 position);

        RaycastHit ScreenPointToRay(Vector2 screenPoint);

        int ScreenPointRaycastAll(Vector2 screenPoint, RaycastHit[] results);

        Vector3 WorldToViewportPoint(Vector3 currentPosition);

        Vector2 WorldToScreenPoint(Vector3 position);

        IReadOnlyList<Vector3> GetGroundFootprint(Vector2 mapMin, Vector2 mapMax);

        void MoveTo(Vector3 worldPoint);

        void SetPose(Vector3 position, Quaternion rotation);

        void SetCullingMask(LayerMask mask);
    }

    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraService : MonoBehaviour, ICameraService, IInitializable, ILateDisposable, ITickable,
        IObserver<BattleState>
    {
        // Load hitches would otherwise turn one frame of held input into a long jump.
        private const float MAX_FRAME_DELTA = 0.05f;
        // Keeps the view-ray to height conversion finite if a pose ever looks near the horizon.
        private const float MIN_PITCH_SINE = 0.1f;

        private IMapModelObserver _mapModel;
        private INotifier<BattleState> _battleState;
        private ICameraInput _cameraInput;
        private ICameraPreferences _preferences;
        private IInputLock _inputLock;

        [SerializeField] private UnityEngine.Camera sceneCamera;
        private CameraData _cameraData;
        private readonly CameraFrustumProjection _frustumProjection = new CameraFrustumProjection();
        private readonly CameraZoom _zoom = new CameraZoom();

        private Plane _plane = new();
        private Vector2 _keyboardInput;
        private Vector2 _keyboardVelocity;
        // The map bounds exist once the battle runs; the camera stays movable while paused.
        private bool _canMove;

        public string Id => nameof(CameraService);

        private float PanSpeed => _cameraData.PanSpeed * _preferences.PanSpeedMultiplier;
        private static float FrameDelta => Mathf.Min(Time.unscaledDeltaTime, MAX_FRAME_DELTA);
        private float PitchSine => Mathf.Max(-CameraForward.y, MIN_PITCH_SINE);

        public Vector3 CameraPosition => transform.position;
        public Transform CameraTransform => transform;
        public Vector3 CameraForward => transform.forward;
        public float FieldOfView => sceneCamera.fieldOfView;
        public UnityEngine.Camera SceneCamera => sceneCamera;

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
            _zoom.Reset(CameraPosition.y);
            _cameraInput.Zoomed += ZoomCamera;
            _cameraInput.ZoomScrolled += ScrollZoom;
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
                _zoom.Reset(CameraPosition.y);
            }
        }

        public void LateDispose()
        {
            _cameraInput.Zoomed -= ZoomCamera;
            _cameraInput.ZoomScrolled -= ScrollZoom;
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

        public int ScreenPointRaycastAll(Vector2 screenPoint, RaycastHit[] results)
        {
            Ray ray = sceneCamera.ScreenPointToRay(screenPoint);
            return Physics.RaycastNonAlloc(ray, results);
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
            _zoom.Reset(position.y);
        }

        public void SetCullingMask(LayerMask mask)
        {
            sceneCamera.cullingMask = mask;
        }

        public void Tick()
        {
            if (!_canMove || _inputLock.IsLocked)
            {
                return;
            }

            TickZoom();
            TickKeyboardPan();
        }

        private void TickKeyboardPan()
        {
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
                // A lock can hand the camera to another owner; drop any unfinished zoom.
                _zoom.Reset(CameraPosition.y);
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

        private void ZoomCamera(float axis)
        {
            if (!_canMove)
            {
                return;
            }

            DollyZoomTarget(Mathf.Clamp(axis, -1f, 1f) * _cameraData.ZoomSpeed * FrameDelta);
        }

        private void ScrollZoom(float notches)
        {
            if (!_canMove)
            {
                return;
            }

            DollyZoomTarget(notches * _cameraData.ZoomStepDistance);
        }

        private void DollyZoomTarget(float viewDistance)
        {
            // Tick does not run while locked, so a target set now would replay on unlock.
            if (_inputLock.IsLocked)
            {
                return;
            }

            if (_preferences.InvertZoom)
            {
                viewDistance = -viewDistance;
            }

            _zoom.Dolly(
                viewDistance * _preferences.ZoomSpeedMultiplier,
                PitchSine,
                _cameraData.ZoomRange.Min,
                _cameraData.ZoomRange.Max);
        }

        private void TickZoom()
        {
            float height = CameraPosition.y;
            float heightDelta = _zoom.Step(height, _cameraData.ZoomSmoothTime, FrameDelta) - height;
            if (Mathf.Abs(heightDelta) <= Mathf.Epsilon)
            {
                return;
            }

            // Moving along the view ray keeps the ground point under the screen centre fixed, as in EaW.
            SetPosition(ClampPosition(CameraPosition - CameraForward * (heightDelta / PitchSine)));
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            // Bound the point under the screen centre, not the elevated camera transform.
            Vector3 groundOffset = CameraForward * (position.y / PitchSine);
            Vector2 groundPoint = new Vector2(position.x + groundOffset.x, position.z + groundOffset.z);
            Vector2 clampedPoint = _mapModel.SizeRange.Clamp(groundPoint);
            position.x += clampedPoint.x - groundPoint.x;
            position.z += clampedPoint.y - groundPoint.y;
            return position;
        }

        private void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
