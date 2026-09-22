using DG.Tweening;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Mvc;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities.ScriptUtils.Math;
using ViewComponents;
using Zenject;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.StationFacing;
using System;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : MonoComponent<ShipMoveModel>, IShipMoveComponent, IInitializable,
        ILateDisposable, IShipNavigationAgent
    {
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const float HEIGHT_TOLERANCE = 0.5f;

        [FormerlySerializedAs("lookAtEase")]
        [SerializeField] private Ease _lookAtEase;
        [FormerlySerializedAs("hyperSpaceEase")]
        [SerializeField] private Ease _hyperSpaceEase;
        [FormerlySerializedAs("lineRenderer")]
        [SerializeField] private LineRenderer _lineRenderer;
        [FormerlySerializedAs("bodyTransform")]
        [SerializeField] private Transform _bodyTransform;
        [FormerlySerializedAs("logNavigationDecisions")]
        [SerializeField] private bool _logNavigationDecisions;

        private ICameraService _cameraService;
        private Vector3 _startPosition;
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private Vector3? _pendingTargetPosition;
        private bool _isNavigationReady;
        private IMapModelObserver _mapModel;
        private IShipNavigationService _shipNavigationService;
        private IStationFacingService _stationFacingService;
        private IShipMovementMediator _movementMediator;
        private IRadarModelObserver _radarModel;
        private bool _isReleased;
        private ShipMovementTweenPlayer _tweenPlayer;
        private readonly List<RadarContact> _navigationContacts =
            new List<RadarContact>();
        private Vector3 _lastBroadsideDirection;
        private bool _hasBroadsideDirection;
        private Vector3? _deferredTargetPosition;
        private Vector3? _blockedTargetPosition;
        private bool _isNavigationRegistered;
        private Vector3? _lastRequestedDestination;
        private Vector3? _lastAcceptedDestination;

        public Vector3 NavigationPosition => CurrentViewPosition;
        public float NavigationHeight => Model.Height;
        public float NavigationRadius =>
            Mathf.Max(Model.NavigationRadius, MINIMUM_NAVIGATION_RADIUS);
        public float NavigationSpeed => Model.Speed;
        public float NavigationRotationSpeed => Model.RotationSpeed;

        public Vector3 CurrentPosition => CurrentViewPosition;
        public Transform ViewTransform => transform;
        public bool IsMoving => Model.IsMoving(ToNumerics(CurrentViewPosition));
        public bool IsBlocked => _blockedTargetPosition.HasValue;
        public float HyperSpaceDuration => Model.HyperSpaceDuration;

        [Inject]
        private void Construct(
            ShipMoveModel model,
            ICameraService cameraService,
            Vector3 startPosition,
            FogOfWarSystem fogOfWarSystem,
            PlayerType playerType,
            IMapModelObserver mapModel,
            IStationFacingService stationFacingService,
            IShipNavigationService shipNavigationService,
            IRadarModelObserver radarModel)
        {
            SetModel(model);
            _cameraService = cameraService;
            startPosition.y = Model.Height;
            _startPosition = startPosition;
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
            _mapModel = mapModel;
            _stationFacingService = stationFacingService;
            _shipNavigationService = shipNavigationService;
            _radarModel = radarModel;
        }

        public void Initialize()
        {
            if (_lineRenderer == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ShipMoveComponent)} requires a serialized line renderer.");
            }

            if (_bodyTransform == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ShipMoveComponent)} requires a serialized body transform.");
            }

            _isReleased = false;
            _tweenPlayer = new ShipMovementTweenPlayer(
                transform,
                _bodyTransform,
                _lineRenderer,
                _lookAtEase,
                _hyperSpaceEase);
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
            Model.ConfigureSpawnPose(
                ToNumerics(_startPosition),
                ToNumerics(_stationFacingService.GetRotation(_playerType)),
                _playerType == PlayerType.Player);

            transform.rotation = ToUnity(Model.StartRotation);
            transform.position = ToUnity(Model.JumpPosition);
            _shipNavigationService.Register(
                this,
                ToUnity(Model.HyperSpacePosition));
            _isNavigationRegistered = true;
            _isNavigationReady = false;
            HyperSpaceJump(ToUnity(Model.HyperSpacePosition));

            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(
                    ViewTransform,
                    _radarModel.Range);
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
            if (_isNavigationRegistered)
            {
                _shipNavigationService.Unregister(this);
                _isNavigationRegistered = false;
            }

            if (_tweenPlayer != null)
            {
                _tweenPlayer.Release();
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
            requestedPosition.y = Model.Height;
            Vector3 requestedDestination = ShipAvoidancePlanner.ClampToMap(
                requestedPosition, _mapModel.SizeRange, NavigationRadius);
            Vector3 destination = requestedDestination;
            if (_lastRequestedDestination.HasValue &&
                _lastAcceptedDestination.HasValue &&
                _lastRequestedDestination.Value == requestedDestination &&
                _shipNavigationService.IsPositionClear(
                    this,
                    _lastAcceptedDestination.Value,
                    NavigationRadius))
            {
                destination = _lastAcceptedDestination.Value;
            }

            if (Model.HasTargetPosition(ToNumerics(destination)))
            {
                if (!_isNavigationReady && _pendingTargetPosition.HasValue &&
                    !_blockedTargetPosition.HasValue)
                {
                    return destination;
                }

                _deferredTargetPosition = null;
                _shipNavigationService.CancelPendingDestination(this);
                if (!_blockedTargetPosition.HasValue)
                {
                    return destination;
                }
            }

            _deferredTargetPosition = null;
            _blockedTargetPosition = null;
            _shipNavigationService.CancelPendingDestination(this);

            _lastRequestedDestination = requestedDestination;
            UpdateTargetPosition(destination, preserveCourse);
            Vector3 appliedDestination = ToUnity(Model.TargetPosition);
            MovementMediator.OnPositionChanged(appliedDestination);
            return appliedDestination;
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = _cameraService.GetWorldPoint(screenPosition, CurrentViewPosition);
            point.y = Model.Height;

            return point;
        }

        public Vector3 CalculateLookDirection(Vector3 targetPosition)
        {
            targetPosition.y = Model.Height;
            return targetPosition - CurrentViewPosition;
        }

        public void MoveToPosition(Vector3 targetPosition, bool preserveCourse = false)
        {
            targetPosition.y = Model.Height;
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
            Model.SetTargetPosition(ToNumerics(CurrentViewPosition));
            StopAllMovement();
            if (_isNavigationReady)
            {
                _shipNavigationService.Stop(this);
            }

            MovementMediator.OnStopped();
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            bool wasMoving = IsMoving;
            Model.ApplyMoveCoefficient(coefficient);
            if (wasMoving)
            {
                UpdateTargetPosition(ToUnity(Model.TargetPosition));
            }
        }

        public void HandleSelection(bool isSelected)
        {
            _tweenPlayer.SetSelected(isSelected, IsMoving);
        }

        private Vector3 CurrentViewPosition => transform.position;
        private IShipMovementMediator MovementMediator =>
            _movementMediator ?? throw new InvalidOperationException(
                $"{nameof(ShipMoveComponent)} requires a movement mediator before receiving commands.");

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
                Mathf.Abs(Vector3.Dot(transform.forward, broadsideDirection)) <=
                Mathf.Epsilon)
            {
                if (Vector3.Dot(_lastBroadsideDirection, broadsideDirection) < 0f)
                {
                    broadsideDirection = -broadsideDirection;
                }
            }
            else if (Vector3.Dot(transform.forward, broadsideDirection) < 0f)
            {
                broadsideDirection = -broadsideDirection;
            }

            _lastBroadsideDirection = broadsideDirection;
            _hasBroadsideDirection = true;
            _tweenPlayer.PlayLookAt(
                broadsideDirection,
                Model.RotationSpeed,
                Model.BodyRotationMaxAngle);
        }

        private void StopAllMovement()
        {
            if (!_isNavigationReady)
            {
                _pendingTargetPosition = null;
                _deferredTargetPosition = null;
                _blockedTargetPosition = null;
                _shipNavigationService.CancelPendingDestination(this);
                return;
            }

            _tweenPlayer.StopPath();
            _deferredTargetPosition = null;
            _blockedTargetPosition = null;
        }

        private void HyperSpaceJump(Vector3 point)
        {
            _tweenPlayer.PlayHyperSpace(
                point,
                Model.HyperSpaceDuration,
                () =>
            {
                _isNavigationReady = true;
                if (_pendingTargetPosition.HasValue)
                {
                    Vector3 targetPosition = _pendingTargetPosition.Value;
                    _pendingTargetPosition = null;
                    UpdateTargetPosition(targetPosition);
                }
                else
                {
                    Model.SetTargetPosition(ToNumerics(CurrentViewPosition));
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
            if (!_isNavigationReady)
            {
                ShipNavigationPlan plan = _shipNavigationService.Plan(
                    this,
                    transform.forward,
                    targetPosition,
                    _navigationContacts,
                    HEIGHT_TOLERANCE,
                    NavigationRadius,
                    _mapModel.SizeRange,
                    preserveCourse: true,
                    reserveAsPending: true);
                if (plan.IsStationary)
                {
                    _pendingTargetPosition = targetPosition;
                    _blockedTargetPosition = targetPosition;
                    return;
                }

                _pendingTargetPosition = plan.Destination;
                _lastAcceptedDestination = plan.Destination;
                Model.SetTargetPosition(ToNumerics(plan.Destination));
                return;
            }

            targetPosition.y = CurrentViewPosition.y;
            ApplyNavigationPlan(targetPosition, _navigationContacts, preserveCourse);
        }

        public void HandleRadarContacts(IReadOnlyList<RadarContact> contacts)
        {
            if (contacts == null)
            {
                throw new ArgumentNullException(nameof(contacts));
            }

            if (_isReleased)
            {
                return;
            }

            ReplaceNavigationContacts(contacts);
            if (_blockedTargetPosition.HasValue)
            {
                Vector3 blockedDestination = _blockedTargetPosition.Value;
                _blockedTargetPosition = null;
                UpdateTargetPosition(blockedDestination);
            }
        }

        private void ApplyNavigationPlan(
            Vector3 requestedDestination,
            IReadOnlyList<RadarContact> obstacleContacts,
            bool preserveCourse = false)
        {
            ShipNavigationPlan plan = _shipNavigationService.Plan(
                this,
                _tweenPlayer.CurrentPathTangent ?? transform.forward,
                requestedDestination,
                obstacleContacts,
                HEIGHT_TOLERANCE,
                NavigationRadius,
                _mapModel.SizeRange,
                preserveCourse && _tweenPlayer.CurrentPathTangent.HasValue);
            if (_logNavigationDecisions)
            {
                Debug.Log(
                    $"[ShipNavigation] Ship={name}, " +
                    $"Detour={plan.Detour.HasValue}, Turn={plan.TurnDuration:F2}s, " +
                    $"Move={plan.MovementDuration:F2}s, Radius={NavigationRadius:F1}, " +
                    $"Speed={NavigationSpeed:F1}, TurnSpeed={NavigationRotationSpeed:F1}, " +
                    $"Blocked={plan.IsStationary}",
                    this);
            }

            if (plan.IsDeferred)
            {
                _deferredTargetPosition = requestedDestination;
                _lastAcceptedDestination = plan.Destination;
                return;
            }

            if (plan.IsStationary)
            {
                _tweenPlayer.StopPath();
                Model.SetTargetPosition(ToNumerics(CurrentViewPosition));
                _blockedTargetPosition = requestedDestination;
                return;
            }

            _blockedTargetPosition = null;
            _deferredTargetPosition = null;
            _lastAcceptedDestination = plan.Destination;

            if (!Model.HasTargetPosition(ToNumerics(plan.Destination)))
            {
                Model.SetTargetPosition(ToNumerics(plan.Destination));
            }

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
            _tweenPlayer.PlayPath(
                plan,
                Model.RotationSpeed,
                Model.BodyRotationMaxAngle,
                () =>
                {
                    if (_isReleased)
                    {
                        return;
                    }

                    if (_deferredTargetPosition.HasValue)
                    {
                        Vector3 deferredTargetPosition =
                            _deferredTargetPosition.Value;
                        _deferredTargetPosition = null;
                        SetTargetPosition(deferredTargetPosition);
                        return;
                    }

                    if (!_shipNavigationService.IsPositionClear(
                            this,
                            plan.Destination,
                            NavigationRadius))
                    {
                        UpdateTargetPosition(plan.Destination);
                    }
                });
        }

        private static NumericsVector3 ToNumerics(Vector3 value)
        {
            return new NumericsVector3(value.x, value.y, value.z);
        }

        private static NumericsQuaternion ToNumerics(Quaternion value)
        {
            return new NumericsQuaternion(value.x, value.y, value.z, value.w);
        }

        private static Vector3 ToUnity(NumericsVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static Quaternion ToUnity(NumericsQuaternion value)
        {
            return new Quaternion(value.X, value.Y, value.Z, value.W);
        }

    }
}
