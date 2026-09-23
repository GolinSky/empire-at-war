using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using EmpireAtWar.Utils;
using Zenject;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.StationFacing;
using System;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMovePresenter : IShipMoveComponent, IMonoComponent, IInitializable,
        ITickable, ILateDisposable, IShipNavigationAgent
    {
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const float HEIGHT_TOLERANCE = 0.5f;

        private readonly ShipMoveModel _model;
        private readonly IShipMoveView _view;
        private ICameraService _cameraService;
        private Vector3 _startPosition;
        private PlayerType _playerType;
        private IMapModelObserver _mapModel;
        private IShipNavigationService _shipNavigationService;
        private IStationFacingService _stationFacingService;
        private IShipMovementMediator _movementMediator;
        private bool _isReleased;
        private readonly List<RadarContact> _navigationContacts =
            new List<RadarContact>();
        private Vector3 _lastBroadsideDirection;
        private bool _hasBroadsideDirection;
        private bool _isNavigationRegistered;

        public string Id => nameof(ShipMovePresenter);
        public Vector3 NavigationPosition => CurrentViewPosition;
        public float NavigationHeight => _model.Height;
        public float NavigationRadius =>
            Mathf.Max(_model.NavigationRadius, MINIMUM_NAVIGATION_RADIUS);
        public float NavigationSpeed => _model.Speed;
        public float NavigationRotationSpeed => _model.RotationSpeed;

        public Vector3 CurrentPosition => CurrentViewPosition;
        public Transform ViewTransform => _view.RootTransform;
        public bool IsMoving => _model.IsNavigating ||
            (!_model.IsHyperSpaceComplete && _model.QueuedDestination.HasValue);
        public bool IsBlocked => _model.IsBlocked;
        public float HyperSpaceDuration => _model.HyperSpaceDuration;

        public ShipMovePresenter(
            ShipMoveModel model,
            IShipMoveView view,
            ICameraService cameraService,
            Vector3 startPosition,
            PlayerType playerType,
            IMapModelObserver mapModel,
            IStationFacingService stationFacingService,
            IShipNavigationService shipNavigationService)
        {
            _model = model;
            _view = view;
            _cameraService = cameraService;
            startPosition.y = _model.Height;
            _startPosition = startPosition;
            _playerType = playerType;
            _mapModel = mapModel;
            _stationFacingService = stationFacingService;
            _shipNavigationService = shipNavigationService;
        }

        public void Initialize()
        {
            _isReleased = false;
            _view.InitializePlayback();
            if (!_shipNavigationService.TryResolveInitialFinalPosition(
                    this,
                    _startPosition,
                    _mapModel.SizeRange,
                    HEIGHT_TOLERANCE,
                    out Vector3 resolvedStartPosition))
            {
                throw new InvalidOperationException(
                    "No clear ship spawn position is available on the map.");
            }

            _startPosition = resolvedStartPosition;
            _model.ConfigureSpawnPose(
                _startPosition.ToNumerics(),
                _stationFacingService.GetRotation(_playerType).ToNumerics(),
                _playerType == PlayerType.Player);

            _view.SetPose(_model.JumpPosition.ToUnity(), _model.StartRotation.ToUnity());
            _shipNavigationService.Register(
                this,
                _model.HyperSpacePosition.ToUnity());
            _isNavigationRegistered = true;
            HyperSpaceJump(_model.HyperSpacePosition.ToUnity());

        }

        public void Tick()
        {
            _view.Tick(Time.deltaTime);
            if (_model.Phase == MovementPhase.Turning &&
                !_view.IsTurningToRoute)
            {
                _model.StartMoving();
            }
        }

        public void LateDispose()
        {
            Release();
        }

        public void Release()
        {
            if (_isReleased)
            {
                return;
            }

            _isReleased = true;
            if (_isNavigationRegistered)
            {
                _shipNavigationService.Unregister(this);
                _isNavigationRegistered = false;
            }

        }

        public void SetMediator(IShipMovementMediator mediator)
        {
            _movementMediator = mediator ??
                throw new ArgumentNullException(nameof(mediator));
        }

        public void MoveToPosition(Vector2 screenPosition)
        {
            Vector3 newPosition = GetWorldCoordinate(screenPosition);
            SetTargetPosition(newPosition);
        }

        private Vector3 SetTargetPosition(Vector3 requestedPosition, bool preserveCourse = false)
        {
            requestedPosition.y = _model.Height;
            Vector3 requestedDestination = ShipAvoidancePlanner.ClampToMap(
                requestedPosition, _mapModel.SizeRange, NavigationRadius);
            Vector3 destination = requestedDestination;
            if (_model.IsSameRequest(requestedDestination.ToNumerics()) &&
                _model.AcceptedDestination.HasValue &&
                _shipNavigationService.IsPositionClear(
                    this,
                    _model.AcceptedDestination.Value.ToUnity(),
                    NavigationRadius))
            {
                destination = _model.AcceptedDestination.Value.ToUnity();
            }

            if (_model.HasTargetPosition(destination.ToNumerics()))
            {
                if (!_model.IsHyperSpaceComplete &&
                    _model.QueuedDestination.HasValue && !_model.IsBlocked)
                {
                    return destination;
                }

                _shipNavigationService.CancelPendingDestination(this);
                if (!_model.IsBlocked)
                {
                    return destination;
                }
            }

            _model.ClearPendingDestinations();
            _shipNavigationService.CancelPendingDestination(this);
            _model.RequestDestination(requestedDestination.ToNumerics());
            UpdateTargetPosition(destination, preserveCourse);
            Vector3 appliedDestination = _model.TargetPosition.ToUnity();
            MovementMediator.OnPositionChanged(appliedDestination);
            return appliedDestination;
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = _cameraService.GetWorldPoint(screenPosition, CurrentViewPosition);
            point.y = _model.Height;

            return point;
        }

        public Vector3 CalculateLookDirection(Vector3 targetPosition)
        {
            targetPosition.y = _model.Height;
            return targetPosition - CurrentViewPosition;
        }

        public void MoveToPosition(Vector3 targetPosition, bool preserveCourse = false)
        {
            targetPosition.y = _model.Height;
            SetTargetPosition(targetPosition, preserveCourse);
        }

        public void MoveToPositionOnScreen(Vector2 targetPosition)
        {
            MoveToPosition(targetPosition);
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            LookAt(targetPosition);
            MovementMediator.OnLookAtTarget(targetPosition);
        }

        public float GetRange(Vector3 targetPosition)
        {
            return Vector3.Distance(CurrentViewPosition, targetPosition);
        }

        public void Stop()
        {
            bool navigationReady = _model.IsHyperSpaceComplete;
            _model.StopAt(CurrentViewPosition.ToNumerics());
            StopAllMovement();
            if (navigationReady)
            {
                _shipNavigationService.Stop(this);
            }

            MovementMediator.OnStopped();
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            bool wasMoving = IsMoving;
            _model.ApplyMoveCoefficient(coefficient);
            if (wasMoving)
            {
                UpdateTargetPosition(_model.TargetPosition.ToUnity());
            }
        }

        public void HandleSelection(bool isSelected)
        {
            _view.SetSelected(isSelected, IsMoving);
        }

        private Vector3 CurrentViewPosition => _view.Position;
        private IShipMovementMediator MovementMediator =>
            _movementMediator ?? throw new InvalidOperationException(
                $"{nameof(ShipMovePresenter)} requires a movement mediator before receiving commands.");

        private void LookAt(Vector3 targetPosition)
        {
            if (IsMoving)
            {
                return;
            }

            Vector3 direction = targetPosition - CurrentViewPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            direction.Normalize();
            Vector3 broadsideDirection = Vector3.Cross(
                Vector3.up,
                direction).normalized;
            if (_hasBroadsideDirection &&
                Mathf.Abs(Vector3.Dot(_view.Forward, broadsideDirection)) <=
                Mathf.Epsilon)
            {
                if (Vector3.Dot(_lastBroadsideDirection, broadsideDirection) < 0f)
                {
                    broadsideDirection = -broadsideDirection;
                }
            }
            else if (Vector3.Dot(_view.Forward, broadsideDirection) < 0f)
            {
                broadsideDirection = -broadsideDirection;
            }

            _lastBroadsideDirection = broadsideDirection;
            _hasBroadsideDirection = true;
            _view.Face(
                broadsideDirection,
                _model.RotationSpeed,
                _model.TurnAcceleration,
                _model.BodyRotationMaxAngle);
        }

        private void StopAllMovement()
        {
            _model.ClearPendingDestinations();
            if (!_model.IsHyperSpaceComplete)
            {
                _shipNavigationService.CancelPendingDestination(this);
                return;
            }

            _view.StopPath();
        }

        private void HyperSpaceJump(Vector3 point)
        {
            _view.PlayHyperSpace(
                point,
                _model.HyperSpaceDuration,
                () =>
                {
                    NumericsVector3? queued = _model.FinishArrival();
                    if (queued.HasValue)
                    {
                        UpdateTargetPosition(queued.Value.ToUnity());
                    }
                    else
                    {
                        _model.Arrive(CurrentViewPosition.ToNumerics());
                        if (!_shipNavigationService.IsPositionClear(
                                this, CurrentViewPosition, NavigationRadius))
                        {
                            UpdateTargetPosition(CurrentViewPosition);
                        }
                    }
                });
        }

        private void UpdateTargetPosition(Vector3 targetPosition, bool preserveCourse = false)
        {
            if (!_model.IsHyperSpaceComplete)
            {
                ShipNavigationPlan plan = _shipNavigationService.Plan(
                    this,
                    _view.Forward,
                    targetPosition,
                    _navigationContacts,
                    HEIGHT_TOLERANCE,
                    NavigationRadius,
                    _mapModel.SizeRange,
                    preserveCourse: true,
                    reserveAsPending: true);
                if (plan.IsStationary)
                {
                    _model.QueueDestination(targetPosition.ToNumerics());
                    _model.BlockDestination(targetPosition.ToNumerics());
                    return;
                }

                _model.QueueDestination(plan.Destination.ToNumerics());
                return;
            }

            targetPosition.y = CurrentViewPosition.y;
            ApplyNavigationPlan(targetPosition, _navigationContacts, preserveCourse);
        }

        public void HandleRadarContacts(IReadOnlyList<RadarContact> contacts)
        {
            if (_isReleased)
            {
                return;
            }

            ReplaceNavigationContacts(contacts);
            NumericsVector3? blocked = _model.TakeBlockedDestination();
            if (blocked.HasValue)
            {
                UpdateTargetPosition(blocked.Value.ToUnity());
            }
        }

        private void ApplyNavigationPlan(
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            bool preserveCourse = false)
        {
            ShipNavigationPlan plan = _shipNavigationService.Plan(
                this,
                _view.CurrentPathTangent ?? _view.Forward,
                requestedDestination,
                obstacleContacts,
                HEIGHT_TOLERANCE,
                NavigationRadius,
                _mapModel.SizeRange,
                preserveCourse && _view.CurrentPathTangent.HasValue);
            if (_view.LogNavigationDecisions)
            {
                Debug.Log(
                    $"[ShipNavigation] Ship={_view.ShipName}, " +
                    $"Detour={plan.Detour.HasValue}, Turn={plan.TurnDuration:F2}s, " +
                    $"Move={plan.MovementDuration:F2}s, Radius={NavigationRadius:F1}, " +
                    $"Speed={NavigationSpeed:F1}, TurnSpeed={NavigationRotationSpeed:F1}, " +
                    $"Blocked={plan.IsStationary}");
            }

            if (plan.IsDeferred)
            {
                _model.DeferDestination(requestedDestination.ToNumerics());
                _model.ReserveDestination(plan.Destination.ToNumerics());
                return;
            }

            if (plan.IsStationary)
            {
                _view.StopPath();
                _model.BlockDestination(requestedDestination.ToNumerics());
                return;
            }

            _model.AcceptDestination(
                plan.Destination.ToNumerics(),
                plan.TurnDuration > Mathf.Epsilon);
            StartPath(plan);
        }

        private void ReplaceNavigationContacts(
            IReadOnlyList<RadarContact> contacts)
        {
            _navigationContacts.Clear();
            for (int i = 0; i < contacts.Count; i++)
            {
                RadarContact contact = contacts[i];
                if (!contact.IsShip)
                {
                    _navigationContacts.Add(contact);
                }
            }
        }

        private void StartPath(ShipNavigationPlan plan)
        {
            _view.PlayPath(
                plan,
                _model.Speed,
                _model.RotationSpeed,
                _model.TurnAcceleration,
                _model.BodyRotationMaxAngle,
                () =>
                {
                    if (_isReleased)
                    {
                        return;
                    }

                    NumericsVector3? deferred =
                        _model.TakeDeferredDestination();
                    if (deferred.HasValue)
                    {
                        SetTargetPosition(deferred.Value.ToUnity());
                        return;
                    }

                    _model.Arrive(CurrentViewPosition.ToNumerics());
                    if (!_shipNavigationService.IsPositionClear(
                            this,
                            plan.Destination,
                            NavigationRadius))
                    {
                        UpdateTargetPosition(plan.Destination);
                    }
                });
        }


    }
}
