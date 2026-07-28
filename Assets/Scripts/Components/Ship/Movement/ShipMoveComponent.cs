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
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const float HEIGHT_TOLERANCE = 0.5f;

        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;
        [SerializeField] private bool logNavigationDecisions;

        private ICameraService _cameraService;
        private Vector3 _startPosition;
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private Sequence _translationSequence;
        private Sequence _rotationSequence;
        private Vector3[] _waypoints;
        private Vector3? _pendingTargetPosition;
        private bool _canMove;
        private bool _isSelected;
        private IMapModelObserver _mapModel;
        private IShipNavigationService _shipNavigationService;
        private bool _isApplyingNavigationDestination;
        private Quaternion _bodyRestRotation;
        private bool _isReleased;

        public Vector3 NavigationPosition => CurrentViewPosition;
        public float NavigationHeight => Model.Height;
        public float NavigationRadius =>
            Mathf.Max(Model.NavigationRadius, MINIMUM_NAVIGATION_RADIUS);
        public float NavigationSpeed => Model.Speed;
        public float NavigationRotationSpeed => Model.RotationSpeed;

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
            _isReleased = false;
            lineRenderer.enabled = false;
            _bodyRestRotation = bodyTransform.localRotation;
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
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
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
                NavigationRadius);
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
            if (Model.IsMoving)
            {
                return;
            }

            Vector3 direction = targetPosition - CurrentViewPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            _rotationSequence.KillExt();
            _rotationSequence = DOTween.Sequence();

            Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            float rotationDuration = ShipRotationKinematics.CalculateTurnDuration(
                transform.rotation,
                direction,
                Mathf.Max(Model.RotationSpeed, Mathf.Epsilon));

            _rotationSequence.Append(transform
                .DORotateQuaternion(desiredRotation, rotationDuration)
                .SetEase(Ease.Linear));

            float targetZ = GetZRotationOnly(targetPosition);
            Quaternion bodyTargetRotation =
                _bodyRestRotation * Quaternion.Euler(0f, 0f, targetZ);

            _rotationSequence.Join(bodyTransform
                .DOLocalRotateQuaternion(bodyTargetRotation, rotationDuration)
                .SetEase(lookAtEase));
            _rotationSequence.Append(bodyTransform
                .DOLocalRotateQuaternion(
                    _bodyRestRotation,
                    BODY_ROTATION_DEFAULT_DURATION)
                .SetEase(lookAtEase));
        }

        private float GetZRotationOnly(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - CurrentViewPosition;
            float direction = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
            return Mathf.Clamp(
                -direction * 0.2f,
                -Model.BodyRotationMaxAngle,
                Model.BodyRotationMaxAngle);
        }

        private void StopAllMovement()
        {
            if (!_canMove)
            {
                _pendingTargetPosition = null;
                return;
            }

            _translationSequence.KillExt();
            _shipNavigationService.ClearPlan(this);
            StraightenBody();
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

            if (lookDirection.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }

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
            // Avoidance is intentionally disabled while movement is evaluated.
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
                NavigationRadius,
                _mapModel.SizeRange);
            if (logNavigationDecisions)
            {
                Debug.Log(
                    $"[ShipNavigation] Ship={name}, " +
                    $"Detour={plan.Detour.HasValue}, Wait={plan.WaitDuration:F2}s, " +
                    $"Move={plan.MovementDuration:F2}s, Radius={NavigationRadius:F1}, " +
                    $"Speed={NavigationSpeed:F1}, TurnSpeed={NavigationRotationSpeed:F1}, " +
                    $"TrafficChecks={plan.TrafficConflictChecks}",
                    this);
            }

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

            _waypoints = plan.Trajectory;

            lineRenderer.positionCount = _waypoints.Length;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                lineRenderer.SetPosition(i, _waypoints[i]);
            }

            lineRenderer.enabled = _isSelected && _waypoints.Length > 1;
            if (plan.WaitDuration > Mathf.Epsilon)
            {
                _translationSequence.Append(DOVirtual.Float(
                    0f,
                    1f,
                    plan.WaitDuration,
                    _ => { }));
            }

            _translationSequence.Append(DOVirtual.Float(
                    0f,
                    1f,
                    plan.MovementDuration,
                    progress => ApplyRouteProgress(plan.Route, progress))
                .SetEase(Ease.Linear));
            _translationSequence.OnComplete(() =>
            {
                ApplyRouteProgress(plan.Route, 1f);
                _shipNavigationService.ClearPlan(this);
                StraightenBody();
                ClearRoute();
            });
        }

        private void ApplyRouteProgress(ShipBezierRoute route, float progress)
        {
            Vector3 position = route.EvaluateNormalizedDistance(
                progress,
                out Vector3 tangent);
            transform.position = position;
            RotateAlongRoute(tangent);
        }

        private void RotateAlongRoute(Vector3 tangent)
        {
            Quaternion previousRotation = transform.rotation;
            float rotationSpeed = Mathf.Max(
                Model.RotationSpeed,
                Mathf.Epsilon);
            float bank = ShipRotationKinematics.CalculateBankAngle(
                previousRotation,
                tangent,
                rotationSpeed,
                Time.deltaTime,
                Model.BodyRotationMaxAngle);
            transform.rotation = ShipRotationKinematics.Step(
                previousRotation,
                tangent,
                rotationSpeed,
                Time.deltaTime);

            Quaternion targetBodyRotation =
                _bodyRestRotation * Quaternion.Euler(0f, 0f, bank);
            bodyTransform.localRotation = Quaternion.RotateTowards(
                bodyTransform.localRotation,
                targetBodyRotation,
                rotationSpeed * Time.deltaTime);
        }

        private void StraightenBody()
        {
            bodyTransform.localRotation = _bodyRestRotation;
        }

        private void ClearRoute()
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

    }
}
