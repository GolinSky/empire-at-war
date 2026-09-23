using System;
using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Utils;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using ViewComponents;
using Zenject;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : MonoComponent<ShipMoveModel>, IShipMoveComponent,
        IShipNavigationAgent, IInitializable, ITickable, ILateDisposable
    {
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const float HEIGHT_TOLERANCE = 0.5f;

        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;
        [SerializeField] private bool logNavigationDecisions;

        private ICameraService _cameraService;
        private Vector3 _startPosition;
        private PlayerType _playerType;
        private IMapModelObserver _mapModel;
        private IStationFacingService _stationFacingService;
        private IShipNavigationService _shipNavigationService;
        private FogOfWarSystem _fogOfWarSystem;
        private IRadarModelObserver _radarModel;
        private IShipMovementMediator _movementMediator;
        private ShipMovementTweenPlayer _motion;
        private readonly List<RadarContact> _navigationContacts = new List<RadarContact>();
        private Vector3 _lastBroadsideDirection;
        private bool _hasBroadsideDirection;
        private bool _isNavigationRegistered;
        private bool _isReleased;

        public Vector3 NavigationPosition => transform.position;
        public float NavigationHeight => Model.Height;
        public float NavigationRadius => Mathf.Max(Model.NavigationRadius, MINIMUM_NAVIGATION_RADIUS);
        public float NavigationSpeed => Model.Speed;
        public float NavigationRotationSpeed => Model.RotationSpeed;
        public Vector3 CurrentPosition => transform.position;
        public bool IsMoving => Model.IsMoving;
        public bool IsBlocked => Model.IsBlocked;
        public float HyperSpaceDuration => Model.HyperSpaceDuration;

        [Inject]
        private void Construct(ShipMoveModel model, ICameraService cameraService,
            Vector3 startPosition, PlayerType playerType, IMapModelObserver mapModel,
            IStationFacingService stationFacingService,
            IShipNavigationService shipNavigationService, FogOfWarSystem fogOfWarSystem,
            IRadarModelObserver radarModel)
        {
            SetModel(model);
            _cameraService = cameraService;
            startPosition.y = Model.Height;
            _startPosition = startPosition;
            _playerType = playerType;
            _mapModel = mapModel;
            _stationFacingService = stationFacingService;
            _shipNavigationService = shipNavigationService;
            _fogOfWarSystem = fogOfWarSystem;
            _radarModel = radarModel;
        }

        public void Initialize()
        {
            _isReleased = false;
            if (bodyTransform == null || lineRenderer == null)
                throw new InvalidOperationException($"{name} has missing ship movement references.");
            _motion = new ShipMovementTweenPlayer(transform, bodyTransform,
                lineRenderer, hyperSpaceEase);
            if (!_shipNavigationService.TryResolveInitialFinalPosition(this,
                    _startPosition, _mapModel.SizeRange, HEIGHT_TOLERANCE,
                    out Vector3 resolvedPosition))
                throw new InvalidOperationException("No clear ship spawn position is available on the map.");
            _startPosition = resolvedPosition;
            Model.ConfigureSpawnPose(_startPosition.ToNumerics(),
                _stationFacingService.GetRotation(_playerType).ToNumerics(),
                _playerType == PlayerType.Player);
            transform.SetPositionAndRotation(Model.JumpPosition.ToUnity(),
                Model.StartRotation.ToUnity());
            _shipNavigationService.Register(this, Model.HyperSpacePosition.ToUnity());
            _isNavigationRegistered = true;
            _motion.PlayHyperSpace(Model.HyperSpacePosition.ToUnity(),
                Model.HyperSpaceDuration, FinishHyperSpaceJump);
            if (_playerType == PlayerType.Player)
                _fogOfWarSystem.RegisterVisionSource(transform, _radarModel.Range);
        }

        public void Tick() => _motion.Tick(Time.deltaTime);
        public void LateDispose() => Release();

        public override void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            if (_isNavigationRegistered)
            {
                _shipNavigationService.Unregister(this);
                _isNavigationRegistered = false;
            }
            if (_playerType == PlayerType.Player)
                _fogOfWarSystem.UnregisterVisionSource(transform);
            _motion.Release();
        }

        public void SetMediator(IShipMovementMediator mediator) => _movementMediator = mediator;

        public void MoveToPositionOnScreen(Vector2 screenPosition)
        {
            Vector3 position = _cameraService.GetWorldPoint(screenPosition, transform.position);
            position.y = Model.Height;
            SetTargetPosition(position);
        }

        public void MoveToPosition(Vector3 position, bool preserveCourse = false) =>
            SetTargetPosition(position, preserveCourse);

        private Vector3 SetTargetPosition(Vector3 requestedPosition, bool preserveCourse = false)
        {
            requestedPosition.y = Model.Height;
            Vector3 requested = ShipAvoidancePlanner.ClampToMap(requestedPosition,
                _mapModel.SizeRange, NavigationRadius);
            Vector3 destination = requested;
            if (Model.IsSameRequest(requested.ToNumerics()) &&
                _shipNavigationService.IsPositionClear(this,
                    Model.Destination.ToUnity(), NavigationRadius))
                destination = Model.Destination.ToUnity();
            if (Model.HasDestination(destination.ToNumerics()))
            {
                if (Model.Phase == MovementPhase.Arriving &&
                    Model.PendingDestination.HasValue)
                    return destination;
                _shipNavigationService.CancelPendingDestination(this);
                if (!Model.IsBlocked && !Model.PendingDestination.HasValue)
                    return destination;
            }
            _shipNavigationService.CancelPendingDestination(this);
            Model.Request(requested.ToNumerics());
            Model.TakePending();
            Plan(destination, preserveCourse);
            Vector3 applied = Model.Destination.ToUnity();
            _movementMediator.OnPositionChanged(applied);
            return applied;
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            if (!IsMoving)
            {
                Vector3 direction = targetPosition - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > Mathf.Epsilon)
                {
                    Vector3 broadside = Vector3.Cross(Vector3.up, direction.normalized);
                    if (_hasBroadsideDirection &&
                        Mathf.Abs(Vector3.Dot(transform.forward, broadside)) <= Mathf.Epsilon)
                    {
                        if (Vector3.Dot(_lastBroadsideDirection, broadside) < 0f)
                            broadside = -broadside;
                    }
                    else if (Vector3.Dot(transform.forward, broadside) < 0f)
                        broadside = -broadside;
                    _lastBroadsideDirection = broadside;
                    _hasBroadsideDirection = true;
                    _motion.PlayLookAt(broadside, Model.RotationSpeed,
                        Model.TurnAcceleration, Model.BodyRotationMaxAngle);
                }
            }
            _movementMediator.OnLookAtTarget(targetPosition);
        }

        public float GetRange(Vector3 targetPosition) =>
            Vector3.Distance(transform.position, targetPosition);

        public void Stop()
        {
            bool ready = Model.Phase != MovementPhase.Arriving;
            Model.StopAt(transform.position.ToNumerics());
            if (ready)
            {
                _motion.StopPath();
                _shipNavigationService.Stop(this);
            }
            else _shipNavigationService.CancelPendingDestination(this);
            _movementMediator.OnStopped();
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            bool wasMoving = IsMoving;
            Model.ApplyMoveCoefficient(coefficient);
            if (wasMoving) Plan(Model.Destination.ToUnity());
        }

        public void HandleSelection(bool isSelected) =>
            _motion.SetSelected(isSelected, IsMoving);

        public void HandleRadarContacts(IReadOnlyList<RadarContact> contacts)
        {
            if (_isReleased) return;
            _navigationContacts.Clear();
            for (int i = 0; i < contacts.Count; i++)
                if (!contacts[i].IsShip) _navigationContacts.Add(contacts[i]);
            if (Model.IsBlocked)
            {
                NumericsVector3? blocked = Model.TakePending();
                if (blocked.HasValue) Plan(blocked.Value.ToUnity());
            }
        }

        private void FinishHyperSpaceJump()
        {
            NumericsVector3? queued = Model.FinishArrival();
            if (queued.HasValue) Plan(queued.Value.ToUnity());
            else
            {
                Model.Arrive(transform.position.ToNumerics());
                if (!_shipNavigationService.IsPositionClear(this,
                        transform.position, NavigationRadius))
                    Plan(transform.position);
            }
        }

        private void Plan(Vector3 destination, bool preserveCourse = false)
        {
            if (Model.Phase == MovementPhase.Arriving)
            {
                ShipNavigationPlan pendingPlan = _shipNavigationService.Plan(this,
                    transform.forward, destination, _navigationContacts, HEIGHT_TOLERANCE,
                    NavigationRadius, _mapModel.SizeRange,
                    preserveCourse: true, reserveAsPending: true);
                if (pendingPlan.IsStationary) Model.Block(destination.ToNumerics());
                else Model.Queue(pendingPlan.Destination.ToNumerics());
                return;
            }
            destination.y = transform.position.y;
            ShipNavigationPlan plan = _shipNavigationService.Plan(this,
                _motion.CurrentPathTangent ?? transform.forward,
                destination, _navigationContacts, HEIGHT_TOLERANCE,
                NavigationRadius, _mapModel.SizeRange,
                preserveCourse && _motion.CurrentPathTangent.HasValue);
            if (logNavigationDecisions)
                Debug.Log($"[ShipNavigation] Ship={name}, Detour={plan.Detour.HasValue}, " +
                    $"Turn={plan.TurnDuration:F2}s, Move={plan.MovementDuration:F2}s, " +
                    $"Radius={NavigationRadius:F1}, Speed={NavigationSpeed:F1}, " +
                    $"TurnSpeed={NavigationRotationSpeed:F1}, Blocked={plan.IsStationary}");
            if (plan.IsDeferred)
            {
                Model.Defer(destination.ToNumerics());
                return;
            }
            if (plan.IsStationary)
            {
                _motion.StopPath();
                Model.Block(destination.ToNumerics());
                return;
            }
            Model.Accept(plan.Destination.ToNumerics());
            _motion.PlayPath(plan, Model.Speed, Model.RotationSpeed,
                Model.TurnAcceleration, Model.BodyRotationMaxAngle, () =>
                {
                    if (_isReleased) return;
                    NumericsVector3? deferred = Model.TakePending();
                    if (deferred.HasValue)
                    {
                        Plan(deferred.Value.ToUnity());
                        _movementMediator.OnPositionChanged(Model.Destination.ToUnity());
                        return;
                    }
                    Model.Arrive(transform.position.ToNumerics());
                    if (!_shipNavigationService.IsPositionClear(this,
                            plan.Destination, NavigationRadius))
                        Plan(plan.Destination);
                });
        }
    }
}
