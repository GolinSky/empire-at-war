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
using Random = UnityEngine.Random;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : MonoComponent<ShipMoveModel>, IShipMoveComponent, IInitializable,
        ILateDisposable, IShipNavigationAgent
    {
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const float HEIGHT_TOLERANCE = 0.5f;
        private const float MOVE_AROUND_MINIMUM_BACKWARD_DISTANCE = 30f;
        private const float MOVE_AROUND_MAXIMUM_BACKWARD_DISTANCE = 50f;
        private const float MOVE_AROUND_MAXIMUM_LATERAL_DISTANCE = 30f;

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

        public Vector3 NavigationPosition => CurrentViewPosition;
        public float NavigationHeight => Model.Height;
        public float NavigationRadius =>
            Mathf.Max(Model.NavigationRadius, MINIMUM_NAVIGATION_RADIUS);
        public float NavigationSpeed => Model.Speed;
        public float NavigationRotationSpeed => Model.RotationSpeed;

        public Vector3 CurrentPosition => Model.CurrentPosition;
        public Transform ViewTransform => Model.ViewTransform.Value;
        public bool IsMoving => Model.IsMoving;
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
            _shipNavigationService.Register(this);
            Model.ConfigureSpawnPose(
                _startPosition,
                _stationFacingService.GetRotation(_playerType),
                _playerType == PlayerType.Player);

            transform.rotation = Model.StartRotation;
            transform.position = Model.JumpPosition;
            _isNavigationReady = false;
            HyperSpaceJump(Model.HyperSpacePosition);

            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(
                    Model.ViewTransform.Value,
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
            _shipNavigationService.ClearPlan(this);
            _shipNavigationService.Unregister(this);
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

        private Vector3 SetTargetPosition(Vector3 requestedPosition)
        {
            requestedPosition.y = Model.Height;
            Vector3 destination = ShipAvoidancePlanner.ClampToMap(
                requestedPosition,
                _mapModel.SizeRange,
                NavigationRadius);
            if (Model.HasTargetPosition(destination))
            {
                return destination;
            }

            Model.SetTargetPosition(destination);
            UpdateTargetPosition(destination);
            Vector3 appliedDestination = Model.TargetPosition;
            MovementMediator.OnPositionChanged(appliedDestination);
            return appliedDestination;
        }

        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = _cameraService.GetWorldPoint(screenPosition, Model.CurrentPosition);
            point.y = Model.Height;

            return point;
        }

        public float MoveAround()
        {
            float backwardDistance = Random.Range(
                MOVE_AROUND_MINIMUM_BACKWARD_DISTANCE,
                MOVE_AROUND_MAXIMUM_BACKWARD_DISTANCE);
            float lateralDistance = Random.Range(
                -MOVE_AROUND_MAXIMUM_LATERAL_DISTANCE,
                MOVE_AROUND_MAXIMUM_LATERAL_DISTANCE);
            Vector3 requestedPosition =
                Model.CurrentPosition -
                Model.ViewTransform.Value.forward * backwardDistance +
                Model.ViewTransform.Value.right * lateralDistance;
            Vector3 destination = SetTargetPosition(requestedPosition);

            return Vector3.Distance(destination, Model.CurrentPosition) /
                   Mathf.Max(Model.Speed, Mathf.Epsilon);
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
            LookAt(targetPosition);
            MovementMediator.OnLookAtTarget(targetPosition);
        }

        public float GetRange(Vector3 targetPosition)
        {
            return Vector3.Distance(Model.CurrentPosition, targetPosition);
        }

        public void Stop()
        {
            Model.SetTargetPosition(CurrentViewPosition);
            StopAllMovement();
            MovementMediator.OnStopped();
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            Model.ApplyMoveCoefficient(coefficient);
            Stop();
        }

        public void HandleSelection(bool isSelected)
        {
            _tweenPlayer.SetSelected(isSelected, Model.IsMoving);
        }

        private Vector3 CurrentViewPosition => transform.position;
        private IShipMovementMediator MovementMediator =>
            _movementMediator ?? throw new InvalidOperationException(
                $"{nameof(ShipMoveComponent)} requires a movement mediator before receiving commands.");

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

            _tweenPlayer.PlayLookAt(
                direction,
                Model.RotationSpeed,
                Model.BodyRotationMaxAngle);
        }

        private void StopAllMovement()
        {
            if (!_isNavigationReady)
            {
                _pendingTargetPosition = null;
                return;
            }

            _tweenPlayer.StopPath();
            _shipNavigationService.ClearPlan(this);
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
            });
        }

        private void UpdateTargetPosition(Vector3 targetPosition)
        {
            if (!_isNavigationReady)
            {
                _pendingTargetPosition = targetPosition;
                return;
            }

            targetPosition.y = CurrentViewPosition.y;
            ApplyNavigationPlan(targetPosition, _navigationContacts);
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
            if (_logNavigationDecisions)
            {
                Debug.Log(
                    $"[ShipNavigation] Ship={name}, " +
                    $"Detour={plan.Detour.HasValue}, Turn={plan.TurnDuration:F2}s, " +
                    $"Wait={plan.WaitDuration:F2}s, " +
                    $"Move={plan.MovementDuration:F2}s, Radius={NavigationRadius:F1}, " +
                    $"Speed={NavigationSpeed:F1}, TurnSpeed={NavigationRotationSpeed:F1}, " +
                    $"TrafficChecks={plan.TrafficConflictChecks}",
                    this);
            }

            if (!Model.HasTargetPosition(plan.Destination))
            {
                Model.SetTargetPosition(plan.Destination);
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
                if (!contact.IsShip ||
                    Mathf.Abs(contact.Position.y - NavigationHeight) <=
                    HEIGHT_TOLERANCE)
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
                () => _shipNavigationService.ClearPlan(this));
        }

    }
}
