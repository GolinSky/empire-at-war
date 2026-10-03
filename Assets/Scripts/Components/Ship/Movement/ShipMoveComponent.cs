using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.ShipSpawning;
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

        private IMapModelObserver _mapModel;
        private IStationFacingService _stationFacingService;
        private IShipNavigationService _shipNavigationService;
        private IShipSpawnClearance _shipSpawnClearance;
        private IFogOfWarSystem _fogOfWarSystem;
        private IRadarModelObserver _radarModel;
        private IWeaponFacing _weaponFacing;
        private ILocalPlayer _localPlayer;

        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;
        private CombatModifiers _modifiers;
        private ShipMovementTweenPlayer _motion;
        private readonly List<RadarContact> _navigationContacts = new List<RadarContact>();

        [SerializeField] private Ease hyperSpaceEase;
        private Vector3 _startPosition;
        private PlayerId _owner;
        private ShipType _shipType;

        [SerializeField] private bool logNavigationDecisions;
        private bool _sharesLocalVision;
        private bool _isNavigationRegistered;
        private bool _isReleased;

        public event Action<Vector3> DestinationChanged;

        public event Action<Vector3> LookingAt;

        public event Action Stopped;

        public event Action HyperSpaceCompleted;

        public Vector3 NavigationPosition => transform.position;
        public float NavigationHeight => Model.Height;
        public ShipHullSpan NavigationHullSpan =>
            new ShipHullSpan(Model.Height + Model.HullBottom, Model.Height + Model.HullTop);
        public float NavigationRadius => Mathf.Max(Model.NavigationRadius, MINIMUM_NAVIGATION_RADIUS);
        public float NavigationSpeed => Model.Speed;
        public float NavigationRotationSpeed => Model.RotationSpeed;
        public Vector3 CurrentPosition => transform.position;
        public bool IsMoving => Model.IsMoving;

        [Inject]
        private void Construct(IMapModelObserver mapModel,
            IStationFacingService stationFacingService, IShipNavigationService shipNavigationService,
            IShipSpawnClearance shipSpawnClearance, IFogOfWarSystem fogOfWarSystem,
            IRadarModelObserver radarModel,
            IWeaponFacing weaponFacing, ILocalPlayer localPlayer,
            ShipMoveModel model, CombatModifiers modifiers, ShipType shipType, Vector3 startPosition,
            PlayerId owner)
        {
            _weaponFacing = weaponFacing;
            SetModel(model);
            _modifiers = modifiers;
            startPosition.y = Model.Height;
            _startPosition = startPosition;
            _owner = owner;
            _shipType = shipType;
            _mapModel = mapModel;
            _stationFacingService = stationFacingService;
            _shipNavigationService = shipNavigationService;
            _shipSpawnClearance = shipSpawnClearance;
            _fogOfWarSystem = fogOfWarSystem;
            _radarModel = radarModel;
            _localPlayer = localPlayer;
        }

        public void Initialize()
        {
            _isReleased = false;
            if (bodyTransform == null || lineRenderer == null)
                throw new InvalidOperationException($"{name} has missing ship movement references.");
            _motion = new ShipMovementTweenPlayer(transform, bodyTransform,
                lineRenderer, hyperSpaceEase);
            _modifiers.Changed += UpdateRouteSpeed;
            // The spawner already validated this point; the ship lands exactly where it was placed.
            Model.ConfigureSpawnPose(_startPosition.ToNumerics(),
                _stationFacingService.GetRotation(_owner).ToNumerics());
            transform.SetPositionAndRotation(Model.JumpPosition.ToUnity(),
                Model.StartRotation.ToUnity());
            _shipNavigationService.Register(this, Model.HyperSpacePosition.ToUnity());
            _isNavigationRegistered = true;
            // Holds the landing spot against other spawns until the hull physically arrives.
            _shipSpawnClearance.ReserveLanding(this, _owner, _shipType, _startPosition);
            _motion.PlayHyperSpace(Model.HyperSpacePosition.ToUnity(),
                Model.HyperSpaceDuration, FinishHyperSpaceJump);
            // Allies share vision, so their ships reveal the local fog too.
            _sharesLocalVision = _localPlayer.IsFriendly(_owner);
            if (_sharesLocalVision)
                _fogOfWarSystem.RegisterVisionSource(transform, _radarModel.Range);
        }

        public void LateDispose() => Release();

        public void Tick()
        {
            if (!_modifiers.IsIonDisabled) _motion.Tick(Time.deltaTime);
        }

        public override void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            _modifiers.Changed -= UpdateRouteSpeed;
            _shipSpawnClearance.ReleaseLanding(this);
            if (_isNavigationRegistered)
            {
                _shipNavigationService.Unregister(this);
                _isNavigationRegistered = false;
            }
            if (_sharesLocalVision)
                _fogOfWarSystem.UnregisterVisionSource(transform);
            _motion.Release();
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
                if (!Model.PendingDestination.HasValue)
                    return destination;
            }
            _shipNavigationService.CancelPendingDestination(this);
            Model.Request(requested.ToNumerics());
            Model.TakePending();
            Plan(destination, preserveCourse);
            Vector3 applied = Model.Destination.ToUnity();
            DestinationChanged?.Invoke(applied);
            return applied;
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            if (_modifiers.IsIonDisabled) return;
            if (!IsMoving)
            {
                float turn = _weaponFacing.GetFiringTurnAngle(targetPosition);
                Vector3 facing = Quaternion.AngleAxis(turn, Vector3.up) * transform.forward;
                _motion.PlayLookAt(facing, Model.RotationSpeed,
                    Model.TurnAcceleration, Model.BodyRotationMaxAngle);
            }
            LookingAt?.Invoke(targetPosition);
        }

        public float GetRange(Vector3 targetPosition) =>
            PlanarGeometry.Distance(transform.position, targetPosition);

        public void LookInDirection(Vector3 direction)
        {
            if (!IsMoving && !_modifiers.IsIonDisabled)
                _motion.PlayLookAt(direction, Model.RotationSpeed,
                    Model.TurnAcceleration, Model.BodyRotationMaxAngle);
        }

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
            Stopped?.Invoke();
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
        }

        private void FinishHyperSpaceJump()
        {
            _shipSpawnClearance.ReleaseLanding(this);
            HyperSpaceCompleted?.Invoke();
            NumericsVector3? queued = Model.FinishArrival();
            if (queued.HasValue) Plan(queued.Value.ToUnity());
            else Model.Arrive(transform.position.ToNumerics());
        }

        private void Plan(Vector3 destination, bool preserveCourse = false)
        {
            if (Model.Phase == MovementPhase.Arriving)
            {
                ShipNavigationPlan pendingPlan = _shipNavigationService.Plan(this,
                    transform.forward, destination, _navigationContacts, HEIGHT_TOLERANCE,
                    NavigationRadius, _mapModel.SizeRange,
                    preserveCourse: true, reserveAsPending: true);
                // Nothing is reachable from the jump point yet; plan again once the jump lands.
                Model.Queue((pendingPlan.IsStationary ? destination : pendingPlan.Destination)
                    .ToNumerics());
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
                    $"TurnSpeed={NavigationRotationSpeed:F1}, Stationary={plan.IsStationary}");
            if (plan.IsDeferred)
            {
                Model.Defer(destination.ToNumerics());
                return;
            }
            if (plan.IsStationary)
            {
                // Nowhere reachable to go: the ship holds position instead of retrying.
                _motion.StopPath();
                Model.Arrive(transform.position.ToNumerics());
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
                        DestinationChanged?.Invoke(Model.Destination.ToUnity());
                        return;
                    }
                    Model.Arrive(transform.position.ToNumerics());
                    if (!_shipNavigationService.IsPositionClear(this,
                            plan.Destination, NavigationRadius))
                        Plan(plan.Destination);
                });
        }

        private void UpdateRouteSpeed() => _motion.SetRouteSpeed(Model.Speed);
    }
}
