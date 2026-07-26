using DG.Tweening;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Mvc;
using EmpireAtWar.Utils;
using UnityEngine;
using Utilities.ScriptUtils.Dotween;
using Utilities.ScriptUtils.Math;
using ViewComponents;
using Zenject;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Services.ShipNavigation;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : MonoComponent<ShipMoveModel>, IShipMoveComponent, IInitializable,
        ILateDisposable, IShipNavigationAgent
    {
        private const float BODY_ROTATION_DEFAULT_DURATION = 1f;
        private const float AVOIDANCE_CLEARANCE = 8f;
        private const float HEIGHT_TOLERANCE = 0.5f;

        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;

        private ICameraService _cameraService;
        private Vector3 _startPosition;
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private Sequence _translationSequence;
        private Sequence _rotationSequence;
        private Vector3[] _waypoints;
        private Vector3? _pendingTargetPosition;
        private float _duration;
        private bool _canMove;
        private bool _isSelected;
        private IMapModelObserver _mapModel;
        private Vector3? _activeDetour;
        private IShipNavigationService _shipNavigationService;
        private bool _isApplyingNavigationDestination;
        private readonly List<RadarContact> _obstacleContacts = new List<RadarContact>();

        public Vector3 NavigationPosition => CurrentViewPosition;
        public float NavigationHeight => Model.Height;
        public float NavigationRadius => AVOIDANCE_CLEARANCE;

        public bool CanMove => Model.CanMove;
        public Vector3 CurrentPosition => Model.CurrentPosition;
        public Transform ViewTransform => Model.ViewTransform.Value;
        public bool IsMoving => Model.IsMoving;
        public float HyperSpaceDuration => Model.HyperSpaceDuration;

        public event System.Action<Vector3> TargetPositionChanged
        {
            add => Model.TargetPosition.OnChanged += value;
            remove => Model.TargetPosition.OnChanged -= value;
        }

        public event System.Action<Vector3> LookAtTargetChanged
        {
            add => Model.LookAtTarget.OnChanged += value;
            remove => Model.LookAtTarget.OnChanged -= value;
        }

        public event System.Action Stopped
        {
            add => Model.OnStop += value;
            remove => Model.OnStop -= value;
        }


        [Inject]
        private void Construct(
            ShipMoveModel model,
            ICameraService cameraService,
            Vector3 startPosition,
            FogOfWarSystem fogOfWarSystem,
            PlayerType playerType,
            IMapModelObserver mapModel,
            IShipNavigationService shipNavigationService)
        {
            SetModel(model);
            _cameraService = cameraService;
            startPosition.y = Model.Height;
            _startPosition = startPosition;
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
            _mapModel = mapModel;
            _shipNavigationService = shipNavigationService;
            // Model.TargetPosition = startPosition;
        }

        public void Initialize()
        {
            lineRenderer.enabled = false;
            _shipNavigationService.Register(this);
            Model.HyperSpacePosition = _startPosition;

            transform.rotation = Model.StartRotation;
            transform.position = Model.JumpPosition;
            _canMove = false;
            HyperSpaceJump(Model.HyperSpacePosition);

            Model.TargetPosition.OnChanged += UpdateTargetPosition;
            Model.OnStop += StopAllMovement;
            Model.LookAtTarget.OnChanged += LookAt;

            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(Model.ViewTransform.Value, 80f);
            }
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            Model.TargetPosition.OnChanged -= UpdateTargetPosition;
            Model.OnStop -= StopAllMovement;
            Model.LookAtTarget.OnChanged -= LookAt;
            _shipNavigationService.Unregister(this);
            FallDown();
        }

        public void MoveToPosition(Vector2 screenPosition)
        {
            Vector3 newPosition = GetWorldCoordinate(screenPosition);
            SetTargetPosition(newPosition);
        }

        private void SetTargetPosition(Vector3 newPosition)
        {
            newPosition.y = Model.Height;
            newPosition = ShipAvoidancePlanner.ClampToMap(
                newPosition,
                _mapModel.SizeRange,
                AVOIDANCE_CLEARANCE);
            if (!newPosition.IsEqual(Model.TargetPosition.Value))
            {
                Model.TargetPosition.Value = newPosition;
            }
        }
        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = _cameraService.GetWorldPoint(screenPosition, Model.CurrentPosition);
            point.y = Model.Height;

            return point;
        }

        public float MoveAround()
        {
            Vector3 backPosition = Model.CurrentPosition - Model.ViewTransform.Value.forward * Random.Range(30, 50f) + Model.ViewTransform.Value.right * Random.Range(-30, 30);
            SetTargetPosition(backPosition);

            return Vector3.Distance(backPosition, Model.CurrentPosition) / Model.Speed;
        }

        public Vector3 CalculateLookDirection(Vector3 targetPosition)
        {
            targetPosition.y = Model.Height;
            return targetPosition - Model.CurrentPosition;
        }

        public void MoveToPosition(Vector3 targetPosition)
        {
            targetPosition.y = Model.Height;
            SetTargetPosition(targetPosition);
        }

        public void MoveToPositionOnScreen(Vector2 targetPosition)
        {
            MoveToPosition(targetPosition);
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            Model.LookAtTarget.Value = targetPosition;
        }

        public float GetRange(Vector3 targetPosition)
        {
            return Vector3.Distance(Model.CurrentPosition, targetPosition);
        }

        public void Stop()
        {
            Model.Stop();
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            Model.ApplyMoveCoefficient(coefficient);
        }

        public void HandleSelection(bool isSelected)
        {
            _isSelected = isSelected;
            lineRenderer.enabled = isSelected && Model.IsMoving && lineRenderer.positionCount > 1;
        }

        private Vector3 CurrentViewPosition => transform.position;

        private void LookAt(Vector3 targetPosition)
        {
            _rotationSequence.KillExt();
            _rotationSequence = DOTween.Sequence();

            targetPosition.y = CurrentViewPosition.y;
            Quaternion desiredRotation = Quaternion.LookRotation(targetPosition - CurrentViewPosition);
            float angle = Quaternion.Angle(transform.rotation, desiredRotation);
            float safeSpeed = Mathf.Max(Model.RotationSpeed, 0.01f);
            float rotationDuration = Mathf.Clamp(
                angle / safeSpeed,
                Model.MinRotationDuration,
                Model.MaxRotationDuration);

            _rotationSequence.Append(transform.DORotateQuaternion(desiredRotation, rotationDuration).SetEase(lookAtEase));

            float targetZ = GetZRotationOnly(targetPosition);
            Vector3 startEuler = bodyTransform.localEulerAngles;
            Vector3 bodyTargetEuler = new(startEuler.x, startEuler.y, targetZ);

            _rotationSequence.Join(bodyTransform.DOLocalRotate(bodyTargetEuler, rotationDuration).SetEase(lookAtEase));
            _rotationSequence.Append(bodyTransform.DOLocalRotate(
                    new Vector3(startEuler.x, startEuler.y, 0f),
                    BODY_ROTATION_DEFAULT_DURATION)
                .SetEase(lookAtEase));
        }

        private float GetZRotationOnly(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - CurrentViewPosition;
            float direction = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
            return Mathf.Clamp(-direction * 0.2f, -15f, 15f);
        }

        private void StopAllMovement()
        {
            if (!_canMove)
            {
                _pendingTargetPosition = null;
                return;
            }

            _translationSequence.KillExt();
            _activeDetour = null;
            _shipNavigationService.ClearPlan(this);
            ClearRoute();
        }

        private void FallDown()
        {
            Vector3 point = CurrentViewPosition - Model.FallDownDirection;

            _translationSequence.KillIfExist();
            _rotationSequence.KillIfExist();
            _translationSequence = DOTween.Sequence();
            _translationSequence.Append(transform.DOMove(point, Model.FallDownDuration));
            _translationSequence.Join(transform.DOLocalRotate(Model.FallDownRotation.Value, Model.FallDownDuration));
            ClearRoute();
        }

        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - CurrentViewPosition;

            transform.rotation = Quaternion.LookRotation(lookDirection);
            _translationSequence.KillIfExist();
            _translationSequence = DOTween.Sequence();
            _translationSequence.Append(transform.DOMove(point, Model.HyperSpaceDuration).SetEase(hyperSpaceEase));
            _translationSequence.OnComplete(() =>
            {
                _canMove = true;
                if (_pendingTargetPosition.HasValue)
                {
                    Vector3 targetPosition = _pendingTargetPosition.Value;
                    _pendingTargetPosition = null;
                    UpdateTargetPosition(targetPosition);
                }
            });
        }

        private void UpdateTargetPosition(Vector3 targetPosition)
        {
            if (_isApplyingNavigationDestination)
            {
                return;
            }

            if (!_canMove)
            {
                _pendingTargetPosition = targetPosition;
                return;
            }

            targetPosition.y = CurrentViewPosition.y;
            ApplyNavigationPlan(targetPosition, System.Array.Empty<RadarContact>());
        }

        public void HandleRadarContacts(IReadOnlyList<RadarContact> contacts)
        {
            if (!_canMove || !Model.IsMoving || contacts == null || contacts.Count == 0)
            {
                return;
            }

            _obstacleContacts.Clear();
            for (int i = 0; i < contacts.Count; i++)
            {
                if (!contacts[i].IsShip)
                {
                    _obstacleContacts.Add(contacts[i]);
                }
            }

            if (_obstacleContacts.Count == 0)
            {
                return;
            }

            if (_activeDetour.HasValue)
            {
                if (Vector3.Distance(CurrentViewPosition, _activeDetour.Value) >
                    AVOIDANCE_CLEARANCE * 1.5f)
                {
                    return;
                }

                _activeDetour = null;
            }

            Vector3 destination = Model.TargetPosition.Value;
            bool occupiedDestination = ShipAvoidancePlanner.TryResolveDestination(
                destination,
                CurrentViewPosition,
                _obstacleContacts,
                Model.Height,
                HEIGHT_TOLERANCE,
                AVOIDANCE_CLEARANCE,
                _mapModel.SizeRange,
                out _);
            bool obstructedRoute = ShipAvoidancePlanner.TryCalculateDetour(
                CurrentViewPosition,
                destination,
                _obstacleContacts,
                Model.Height,
                HEIGHT_TOLERANCE,
                AVOIDANCE_CLEARANCE,
                _mapModel.SizeRange,
                out _);
            if (occupiedDestination || obstructedRoute)
            {
                ApplyNavigationPlan(destination, _obstacleContacts);
            }
        }

        private void ApplyNavigationPlan(
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts)
        {
            ShipNavigationPlan plan = _shipNavigationService.Plan(
                this,
                transform.forward,
                requestedDestination,
                obstacleContacts,
                HEIGHT_TOLERANCE,
                AVOIDANCE_CLEARANCE,
                _mapModel.SizeRange);
            if (!plan.Destination.IsEqual(Model.TargetPosition.Value))
            {
                _isApplyingNavigationDestination = true;
                Model.TargetPosition.Value = plan.Destination;
                _isApplyingNavigationDestination = false;
            }

            StartPath(plan);
        }

        private void StartPath(ShipNavigationPlan plan)
        {
            _translationSequence.KillExt();
            _translationSequence = DOTween.Sequence();

            _activeDetour = plan.Detour;
            _waypoints = plan.Trajectory;

            float curvedDistance = 0f;
            lineRenderer.positionCount = _waypoints.Length;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                lineRenderer.SetPosition(i, _waypoints[i]);
                if (i < _waypoints.Length - 1)
                {
                    curvedDistance += Vector3.Distance(_waypoints[i], _waypoints[i + 1]);
                }
            }

            _duration = curvedDistance / Model.Speed;
            lineRenderer.enabled = _isSelected && _waypoints.Length > 1;
            _translationSequence.Append(transform.DOPath(
                    _waypoints,
                    _duration,
                    PathType.CatmullRom,
                    PathMode.Full3D,
                    10)
                .SetOptions(false, AxisConstraint.Y, AxisConstraint.X)
                .SetLookAt(0.01f)
                .SetEase(Ease.Linear));
            _translationSequence.OnUpdate(UpdateRouteVisual);
            _translationSequence.OnComplete(() =>
            {
                _activeDetour = null;
                _shipNavigationService.ClearPlan(this);
                ClearRoute();
            });
        }

        private void UpdateRouteVisual()
        {
            if (!_isSelected || _waypoints == null || _waypoints.Length < 2)
            {
                return;
            }

            int nextIndex = _waypoints.Length - 1;
            for (int i = 1; i < _waypoints.Length; i++)
            {
                if (Vector3.Distance(CurrentViewPosition, _waypoints[i]) > AVOIDANCE_CLEARANCE)
                {
                    nextIndex = i;
                    break;
                }
            }

            int count = _waypoints.Length - nextIndex + 1;
            lineRenderer.positionCount = count;
            lineRenderer.SetPosition(0, CurrentViewPosition);
            for (int i = 1; i < count; i++)
            {
                lineRenderer.SetPosition(i, _waypoints[nextIndex + i - 1]);
            }
        }

        private void ClearRoute()
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

    }
}
