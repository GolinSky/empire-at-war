using System;
using EmpireAtWar.Models.Players;
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
using EmpireAtWar.Services.Timing;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Utils;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using VContainer;
using EmpireAtWar.Lifecycle;
using NumericsVector3 = System.Numerics.Vector3;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : MonoComponent<ShipMoveModel>, IShipMoveComponent,
        IShipNavigationAgent, IShipPlanRequester, IInitializable, ITickable, ILateDisposable
    {
        private const float MINIMUM_NAVIGATION_RADIUS = 1f;
        private const float HEIGHT_TOLERANCE = 0.5f;
        // Solving the firing turn reads every hardpoint transform. A stationary ship keeps its solved facing
        // until the bearing to the target swings this far or the solve ages out (lost hardpoints change the arcs).
        private const float FACING_RESOLVE_ANGLE = 2f;
        private const float FACING_RESOLVE_INTERVAL = 1f;
        // A ship chasing a moving target re-plans its course at most this often; new orders plan at once.
        private const float REPATH_INTERVAL = 0.5f;

        private IMapModelObserver _mapModel;
        private IStationFacingService _stationFacingService;
        private IShipNavigationService _shipNavigationService;
        private IShipSpawnClearance _shipSpawnClearance;
        private IVisionService _visionService;
        private IRadarModelObserver _radarModel;
        private IWeaponFacing _weaponFacing;

        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;
        private CombatModifiers _modifiers;
        private ShipMovementTweenPlayer _motion;

        [SerializeField] private Ease hyperSpaceEase;
        private Vector3 _startPosition;
        private PlayerId _owner;
        private ShipType _shipType;
        private ShipSpawnPose _spawnPose;

        [SerializeField] private bool logNavigationDecisions;
        private bool _isNavigationRegistered;
        private bool _isReleased;
        private bool _hasSolvedFacing;
        // Orders wait in the navigation service's per-frame plan budget; the newest destination is planned.
        private bool _hasQueuedPlan;
        private bool _isInPlanQueue;
        private bool _queuedPreserveCourse;
        private Vector3 _queuedDestination;
        private Vector3? _failedDestination;
        private bool _hasArrivalCorrection;
        private float _lastPlanTime = float.NegativeInfinity;
        private Vector3 _solvedFacing;
        private Vector3 _solvedBearing;
        private float _solvedFacingTime;

        public event Action<Vector3> DestinationChanged;

        public event Action<Vector3> LookingAt;

        public event Action HyperSpaceCompleted;

        public Vector3 NavigationPosition => transform.position;
        public float NavigationHeight => Model.Height;
        public ShipHullSpan NavigationHullSpan =>
            new ShipHullSpan(Model.Height + Model.HullBottom, Model.Height + Model.HullTop);
        public float NavigationRadius => Mathf.Max(Model.NavigationRadius, MINIMUM_NAVIGATION_RADIUS);
        public float NavigationSpeed => Model.Speed;
        public float NavigationRotationSpeed => Model.RotationSpeed;
        public Vector3 CurrentPosition => transform.position;
        // A ship waiting for its plan counts as moving, so a move order does not complete before it starts.
        public bool IsMoving => (_isNavigationRegistered && Model.Phase == MovementPhase.Arriving) ||
                                Model.IsMoving || _hasQueuedPlan;

        [Inject]
        private void Construct(IMapModelObserver mapModel,
            IStationFacingService stationFacingService, IShipNavigationService shipNavigationService,
            IShipSpawnClearance shipSpawnClearance, IVisionService visionService,
            IRadarModelObserver radarModel,
            IWeaponFacing weaponFacing,
            ShipMoveModel model, CombatModifiers modifiers, ShipType shipType, Vector3 startPosition,
            PlayerId owner, ShipSpawnPose spawnPose)
        {
            _spawnPose = spawnPose;
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
            _visionService = visionService;
            _radarModel = radarModel;
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
            Quaternion rotation = _spawnPose.IsRestored ? _spawnPose.Rotation : _stationFacingService.GetRotation(_owner);
            Model.ConfigureSpawnPose(_startPosition.ToNumerics(), rotation.ToNumerics());
            if (_spawnPose.IsRestored)
            {
                // A loaded ship is already in place: no jump, no landing reservation, no arrival sound.
                transform.SetPositionAndRotation(Model.HyperSpacePosition.ToUnity(), Model.StartRotation.ToUnity());
                _shipNavigationService.Register(this, Model.HyperSpacePosition.ToUnity());
                _isNavigationRegistered = true;
                Model.Arrive(Model.HyperSpacePosition);
            }
            else
            {
                transform.SetPositionAndRotation(Model.JumpPosition.ToUnity(),
                    Model.StartRotation.ToUnity());
                _shipNavigationService.Register(this, Model.HyperSpacePosition.ToUnity());
                _isNavigationRegistered = true;
                // Holds the landing spot against other spawns until the hull physically arrives.
                _shipSpawnClearance.ReserveLanding(this, _owner, _shipType, _startPosition);
                _motion.PlayHyperSpace(Model.HyperSpacePosition.ToUnity(),
                    Model.HyperSpaceDuration, FinishHyperSpaceJump);
            }
            _visionService.Register(_owner, transform, _radarModel.Range);
        }

        public void LateDispose() => Release();

        public void Tick()
        {
            if (!_modifiers.IsIonDisabled) _motion.Tick(Time.deltaTime);
            if (_hasQueuedPlan && !_isInPlanQueue && !_motion.IsBraking && IsRepathDue()) EnqueuePlan();
        }

        public override void Release()
        {
            if (_isReleased) return;
            _isReleased = true;
            _hasQueuedPlan = false;
            _modifiers.Changed -= UpdateRouteSpeed;
            _shipSpawnClearance.ReleaseLanding(this);
            if (_isNavigationRegistered)
            {
                _shipNavigationService.Unregister(this);
                _isNavigationRegistered = false;
            }
            _visionService.Unregister(transform);
            _motion.Release();
        }

        public void MoveToPosition(Vector3 position, bool preserveCourse = false) =>
            SetTargetPosition(position, preserveCourse);

        private void SetTargetPosition(Vector3 requestedPosition, bool preserveCourse = false)
        {
            requestedPosition.y = Model.Height;
            Vector3 requested = ShipAvoidancePlanner.ClampToMap(requestedPosition,
                _mapModel.SizeRange, NavigationRadius);
            if (preserveCourse && _failedDestination.HasValue &&
                (requested - _failedDestination.Value).sqrMagnitude < NavigationRadius * NavigationRadius)
                return;
            bool retryFailed = _failedDestination.HasValue;
            _failedDestination = null;
            Vector3 destination = requested;
            if (!retryFailed && Model.IsSameRequest(requested.ToNumerics()) &&
                _shipNavigationService.IsPositionClear(this,
                    Model.Destination.ToUnity(), NavigationRadius))
                destination = Model.Destination.ToUnity();
            if (!retryFailed && Model.HasDestination(destination.ToNumerics()))
            {
                // Already heading there: a plan still waiting for another point is dropped.
                _hasQueuedPlan = false;
                if (Model.Phase == MovementPhase.Arriving &&
                    Model.PendingDestination.HasValue)
                    return;
                _shipNavigationService.CancelPendingDestination(this);
                if (!Model.PendingDestination.HasValue)
                    return;
            }
            _shipNavigationService.CancelPendingDestination(this);
            Model.Request(requested.ToNumerics());
            Model.TakePending();
            _hasArrivalCorrection = false;
            QueuePlan(destination, preserveCourse);
        }

        private void QueuePlan(Vector3 destination, bool preserveCourse)
        {
            _queuedDestination = destination;
            _queuedPreserveCourse = preserveCourse;
            _hasQueuedPlan = true;
            // A course correction while under way waits out the interval; Tick queues it once due.
            if (preserveCourse && Model.IsMoving && !IsRepathDue()) return;
            EnqueuePlan();
        }

        private bool IsRepathDue() => Time.time - _lastPlanTime >= REPATH_INTERVAL;

        private void EnqueuePlan()
        {
            if (_isInPlanQueue || _motion.IsBraking) return;
            _isInPlanQueue = true;
            _shipNavigationService.RequestPlan(this);
        }

        public void ExecuteQueuedPlan()
        {
            _isInPlanQueue = false;
            if (!_hasQueuedPlan || _isReleased || _motion.IsBraking) return;
            _hasQueuedPlan = false;
            _lastPlanTime = Time.time;
            Plan(_queuedDestination, _queuedPreserveCourse);
            DestinationChanged?.Invoke(Model.Destination.ToUnity());
        }

        public void LookAtTarget(Vector3 targetPosition)
        {
            if (_modifiers.IsIonDisabled || Model.Phase == MovementPhase.Arriving) return;
            if (!IsMoving)
            {
                Vector3 bearing = targetPosition - transform.position;
                bearing.y = 0f;
                if (!_hasSolvedFacing || Time.time - _solvedFacingTime > FACING_RESOLVE_INTERVAL ||
                    Vector3.Angle(bearing, _solvedBearing) > FACING_RESOLVE_ANGLE)
                {
                    float turn;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    using (BattleProfilerMarkers.ShipFacingSolve.Auto())
#endif
                    turn = _weaponFacing.GetFiringTurnAngle(targetPosition);
                    _solvedFacing = Quaternion.AngleAxis(turn, Vector3.up) * transform.forward;
                    _solvedBearing = bearing;
                    _solvedFacingTime = Time.time;
                    _hasSolvedFacing = true;
                }

                _motion.PlayLookAt(_solvedFacing, Model.RotationSpeed,
                    Model.TurnAcceleration, Model.BodyRotationMaxAngle);
            }
            else _hasSolvedFacing = false;
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
            _hasQueuedPlan = false;
            _failedDestination = null;
            _hasArrivalCorrection = false;
            bool ready = Model.Phase != MovementPhase.Arriving;
            Model.StopAt(transform.position.ToNumerics());
            if (ready)
            {
                _motion.StopPath();
                _shipNavigationService.Stop(this);
            }
            else _shipNavigationService.CancelPendingDestination(this);
        }

        // Engine damage only changes speed; the current route stays valid.
        public void ApplyMoveCoefficient(float coefficient)
        {
            Model.ApplyMoveCoefficient(coefficient);
            _motion.SetRouteSpeed(Model.Speed);
        }

        public void HandleSelection(bool isSelected) =>
            _motion.SetSelected(isSelected, IsMoving);

        private void FinishHyperSpaceJump()
        {
            _shipSpawnClearance.ReleaseLanding(this);
            HyperSpaceCompleted?.Invoke();
            NumericsVector3? queued = Model.FinishArrival();
            if (queued.HasValue) QueuePlan(queued.Value.ToUnity(), false);
            else Model.Arrive(transform.position.ToNumerics());
        }

        private void Plan(Vector3 destination, bool preserveCourse = false)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var scope = (Model.Phase == MovementPhase.Arriving
                ? BattleProfilerMarkers.NavigationArrival
                : preserveCourse ? BattleProfilerMarkers.NavigationPreserveCourse
                : BattleProfilerMarkers.NavigationNewCourse).Auto();
#endif
            if (Model.Phase == MovementPhase.Arriving)
            {
                ShipNavigationPlan pendingPlan = _shipNavigationService.Plan(this,
                    transform.forward, destination, Array.Empty<RadarContact>(), HEIGHT_TOLERANCE,
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
                destination, Array.Empty<RadarContact>(), HEIGHT_TOLERANCE,
                NavigationRadius, _mapModel.SizeRange,
                preserveCourse && _motion.CurrentPathTangent.HasValue);
            if (logNavigationDecisions)
                Debug.Log($"[ShipNavigation] Ship={name}, Detour={plan.Detour.HasValue}, " +
                    $"Turn={plan.TurnDuration:F2}s, Move={plan.MovementDuration:F2}s, " +
                    $"Radius={NavigationRadius:F1}, Speed={NavigationSpeed:F1}, " +
                    $"TurnSpeed={NavigationRotationSpeed:F1}, Stationary={plan.IsStationary}");
            if (plan.IsStationary)
            {
                // Autonomous pursuit waits for a changed destination or a fresh order.
                _failedDestination = destination;
                _motion.StopPath();
                _shipNavigationService.Stop(this);
                Model.TakePending();
                Model.Arrive(transform.position.ToNumerics());
                return;
            }
            if (plan.IsDeferred)
            {
                Model.Defer(destination.ToNumerics());
                return;
            }
            if (plan.TurnDuration > Mathf.Epsilon && _motion.HasMomentum)
            {
                // Brake on the validated route, then plan the turn from the actual stop point.
                _motion.BrakeForReplan();
                QueuePlan(destination, false);
                return;
            }
            Model.Accept(plan.Destination.ToNumerics());
            _motion.PlayPath(plan, Model.Speed, Model.RotationSpeed,
                Model.TurnAcceleration, Model.BodyRotationMaxAngle, () =>
                {
                    if (_isReleased) return;
                    if (_hasQueuedPlan)
                    {
                        Model.Arrive(transform.position.ToNumerics());
                        return;
                    }
                    NumericsVector3? deferred = Model.TakePending();
                    if (deferred.HasValue)
                    {
                        QueuePlan(deferred.Value.ToUnity(), false);
                        return;
                    }
                    Model.Arrive(transform.position.ToNumerics());
                    if (!_shipNavigationService.IsPositionClear(this,
                            plan.Destination, NavigationRadius))
                    {
                        if (_hasArrivalCorrection)
                        {
                            _failedDestination = Model.LastRequest.Value.ToUnity();
                            return;
                        }
                        _hasArrivalCorrection = true;
                        QueuePlan(plan.Destination, false);
                    }
                });
        }

        private void UpdateRouteSpeed()
        {
            _motion.SetRouteSpeed(Model.Speed);
            _visionService.Register(_owner, transform, _radarModel.Range);
        }
    }
}
