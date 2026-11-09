using System;
using DG.Tweening;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    internal sealed class ShipMovementTweenPlayer
    {
        private const float BANK_SMOOTH_TIME = 0.6f;
        private const float ROUTE_HEADING_TOLERANCE = 1f;
        private const float MINIMUM_TANGENT_STEP = 0.05f;
        // Seconds to reach full route speed from rest; braking uses the same rate.
        private const float SPEED_RAMP_TIME = 1.5f;
        // Floor so braking never stalls the hull just short of the route end.
        private const float MINIMUM_SPEED_FACTOR = 0.1f;

        private const int ROUTE_STEP_SEARCH_ITERATIONS = 8;
        // Heading lag the hull may keep behind the route tangent, so it can hold its turn rate through a bend.
        private const float MAXIMUM_TURN_LEAD = 10f;
        // Bends are looked for as far ahead as the hull travels while turning this much at full speed.
        private const float CORNER_LOOK_AHEAD_TURN = 90f;
        private const int CORNER_SAMPLE_COUNT = 8;

        private readonly Transform _rootTransform;
        private readonly Transform _bodyTransform;
        private readonly LineRenderer _lineRenderer;
        private Sequence _translationSequence;
        private ShipBezierRoute _route;
        private Action _pathCompleted;

        private readonly Ease _hyperSpaceEase;
        private readonly Quaternion _bodyRestRotation;
        private Vector3 _lookDirection;
        private Vector3? _currentPathTangent;

        private float _routeProgress;
        private float _routeSpeed;
        private float _currentSpeed;
        private float _rotationSpeed;
        private float _turnAcceleration;
        private float _maximumBankAngle;
        private float _turnLead;
        private float _cornerTurnRate;
        private float _angularVelocity;
        private float _bankVelocity;

        private bool _isSelected;
        private bool _hasLookDirection;
        private bool _isTurningToRoute;
        private bool _isBraking;
        private bool _isReplicaRoute;
        private Vector3[] _displayedRoute = Array.Empty<Vector3>();

        /// <summary>The route the line shows; empty when there is none.</summary>
        public Vector3[] DisplayedRoute => _displayedRoute;
        public event Action RouteDisplayChanged;

        public Vector3? CurrentPathTangent => _currentPathTangent;
        public bool IsBraking => _isBraking;
        public bool HasMomentum => _route != null && _currentSpeed > Mathf.Epsilon;

        public ShipMovementTweenPlayer(
            Transform rootTransform,
            Transform bodyTransform,
            LineRenderer lineRenderer,
            Ease hyperSpaceEase)
        {
            _rootTransform = rootTransform;
            _bodyTransform = bodyTransform;
            _lineRenderer = lineRenderer;
            _hyperSpaceEase = hyperSpaceEase;
            _bodyRestRotation = _bodyTransform.localRotation;
            ClearRoute();
        }

        public void SetRouteSpeed(float speed)
        {
            _routeSpeed = speed;
            _currentSpeed = Mathf.Min(_currentSpeed, speed);
        }

        public void BrakeForReplan() => _isBraking = true;

        public void SetSelected(bool isSelected, bool isMoving)
        {
            _isSelected = isSelected;
            _lineRenderer.enabled =
                isSelected && (isMoving || _isReplicaRoute) && _lineRenderer.positionCount > 1;
        }

        /// <summary>Online client: shows the host's route without moving the hull, which follows replication.</summary>
        public void ShowReplicaRoute(Vector3[] waypoints)
        {
            _isReplicaRoute = waypoints.Length > 1;
            if (_isReplicaRoute) DisplayRoute(waypoints);
            else ClearRoute();
        }

        public void PlayLookAt(
            Vector3 targetDirection,
            float rotationSpeed,
            float turnAcceleration,
            float maximumBankAngle)
        {
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            _lookDirection = targetDirection.normalized;
            _hasLookDirection = true;
            _rotationSpeed = rotationSpeed;
            _turnAcceleration = turnAcceleration;
            _maximumBankAngle = maximumBankAngle;
        }

        public void PlayHyperSpace(
            Vector3 destination,
            float duration,
            Action completed)
        {
            Vector3 lookDirection = destination - _rootTransform.position;
            if (lookDirection.sqrMagnitude > Mathf.Epsilon)
            {
                _rootTransform.rotation = Quaternion.LookRotation(
                    lookDirection,
                    Vector3.up);
            }

            StopPath();
            _hasLookDirection = false;
            _angularVelocity = 0f;
            _currentSpeed = 0f;
            _translationSequence.KillExt();
            _translationSequence = DOTween.Sequence();
            _translationSequence.Append(_rootTransform
                .DOMove(destination, duration)
                .SetEase(_hyperSpaceEase));
            if (completed != null)
            {
                _translationSequence.OnComplete(completed.Invoke);
            }
        }

        public void PlayPath(
            ShipNavigationPlan plan,
            float speed,
            float rotationSpeed,
            float turnAcceleration,
            float maximumBankAngle,
            Action completed)
        {
            _translationSequence.KillExt();
            _hasLookDirection = false;
            _route = plan.Route;
            _pathCompleted = completed;
            _routeProgress = 0f;
            _routeSpeed = speed;
            _rotationSpeed = rotationSpeed;
            _turnAcceleration = turnAcceleration;
            _maximumBankAngle = maximumBankAngle;
            // StepYaw holds a yaw rate w only while the target leads by w^2 / (2 * acceleration).
            _turnLead = Mathf.Min(MAXIMUM_TURN_LEAD,
                rotationSpeed * rotationSpeed / (2f * turnAcceleration));
            _cornerTurnRate = Mathf.Max(Mathf.Min(rotationSpeed,
                Mathf.Sqrt(2f * turnAcceleration * (ROUTE_HEADING_TOLERANCE + _turnLead))), Mathf.Epsilon);
            _isTurningToRoute = plan.TurnDuration > Mathf.Epsilon;
            if (_isTurningToRoute)
            {
                // The hull turns in place before it moves, so it starts the route from rest.
                _currentSpeed = 0f;
            }
            _currentPathTangent = null;
            DisplayRoute(plan.Trajectory);
        }

        public void StopPath()
        {
            _route = null;
            _currentSpeed = 0f;
            _pathCompleted = null;
            _currentPathTangent = null;
            _isTurningToRoute = false;
            _isBraking = false;
            ClearRoute();
        }

        public void Release()
        {
            StopPath();
            _hasLookDirection = false;
            _translationSequence.KillExt();
        }

public void Tick(float deltaTime)
        {
            if (_route != null)
            {
                TickRoute(deltaTime);
            }
            else if (_hasLookDirection)
            {
                StepRotation(_lookDirection, deltaTime);
            }
            else
            {
                _angularVelocity = Mathf.MoveTowards(
                    _angularVelocity,
                    0f,
                    _turnAcceleration * deltaTime);
                SmoothBank(deltaTime);
            }
        }

        private void TickRoute(float deltaTime)
        {
            if (_isTurningToRoute)
            {
                StepRotation(_route.InitialTangent, deltaTime);
                if (Vector3.Angle(
                        _rootTransform.forward,
                        _route.InitialTangent) > ROUTE_HEADING_TOLERANCE)
                {
                    return;
                }

                _isTurningToRoute = false;
            }

            // A re-plan keeps the current speed, so pursuit re-paths do not restart from rest.
            float acceleration = _routeSpeed / SPEED_RAMP_TIME;
            float remainingDistance = (1f - _routeProgress) * _route.Length;
            float targetSpeed = _isBraking ? 0f : Mathf.Max(
                Mathf.Min(_routeSpeed, Mathf.Sqrt(2f * acceleration * remainingDistance),
                    GetCornerSpeedLimit(remainingDistance)),
                _routeSpeed * MINIMUM_SPEED_FACTOR);
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * deltaTime);
            if (_isBraking && _currentSpeed <= Mathf.Epsilon)
            {
                StopPath();
                return;
            }
            float requestedStep = _currentSpeed * deltaTime /
                Mathf.Max(_route.Length, Mathf.Epsilon);
            float nextProgress = Mathf.Min(1f, _routeProgress + requestedStep);
            float allowedTurn = Mathf.Max(
                MINIMUM_TANGENT_STEP,
                Mathf.Abs(_angularVelocity) * deltaTime +
                0.5f * _turnAcceleration * deltaTime * deltaTime) + _turnLead;
            float low = _routeProgress;
            float high = nextProgress;
            _route.EvaluateNormalizedDistance(high, out Vector3 nextTangent);
            if (Vector3.Angle(_rootTransform.forward, nextTangent) <=
                allowedTurn + ROUTE_HEADING_TOLERANCE)
            {
                low = high;
            }
            else
            {
                for (int i = 0; i < ROUTE_STEP_SEARCH_ITERATIONS; i++)
                {
                    float middle = (low + high) * 0.5f;
                    _route.EvaluateNormalizedDistance(middle, out Vector3 tangent);
                    if (Vector3.Angle(_rootTransform.forward, tangent) <=
                        allowedTurn + ROUTE_HEADING_TOLERANCE)
                    {
                        low = middle;
                    }
                    else
                    {
                        high = middle;
                    }
                }
            }

            // When the turn rate limits the step (e.g. at a sharp waypoint corner)
            // progress cannot pass the bend until the hull faces the upcoming
            // tangent, so steer towards it instead of the current one.
            bool isTurnLimited = low < nextProgress;
            _routeProgress = low;
            _rootTransform.position = _route.EvaluateNormalizedDistance(
                _routeProgress,
                out Vector3 routeTangent);
            _currentPathTangent = routeTangent;
            StepRotation(isTurnLimited ? nextTangent : routeTangent, deltaTime);
            if (_routeProgress < 1f - Mathf.Epsilon)
            {
                return;
            }

            Action completed = _pathCompleted;
            StopPath();
            completed?.Invoke();
        }

        // Highest speed at which the hull still turns onto every tangent within the look-ahead before reaching it.
        private float GetCornerSpeedLimit(float remainingDistance)
        {
            // Time lost ramping the yaw rate from its current value up to the corner rate.
            float rampDeficit = Mathf.Max(0f, _cornerTurnRate - Mathf.Abs(_angularVelocity));
            float rampTime = rampDeficit * rampDeficit / (2f * _turnAcceleration * _cornerTurnRate);
            float lookAhead = Mathf.Min(remainingDistance,
                _routeSpeed * (CORNER_LOOK_AHEAD_TURN / _cornerTurnRate + rampTime));
            if (lookAhead <= Mathf.Epsilon) return _routeSpeed;
            float limit = _routeSpeed;
            Vector3 forward = _rootTransform.forward;
            for (int i = 1; i <= CORNER_SAMPLE_COUNT; i++)
            {
                float distance = lookAhead * i / CORNER_SAMPLE_COUNT;
                _route.EvaluateNormalizedDistance(_routeProgress + distance / _route.Length, out Vector3 tangent);
                float turn = Vector3.Angle(forward, tangent) - ROUTE_HEADING_TOLERANCE - _turnLead;
                if (turn <= 0f) continue;
                limit = Mathf.Min(limit, distance / (turn / _cornerTurnRate + rampTime));
            }
            return limit;
        }

        private void StepRotation(Vector3 direction, float deltaTime)
        {
            _rootTransform.rotation = ShipRotationKinematics.StepYaw(
                _rootTransform.rotation,
                direction,
                ref _angularVelocity,
                _rotationSpeed,
                _turnAcceleration,
                deltaTime);
            SmoothBank(deltaTime);
        }

        private void SmoothBank(float deltaTime)
        {
            float bank = ShipRotationKinematics.CalculateBankFromYawRate(
                _angularVelocity,
                Mathf.Max(_rotationSpeed, Mathf.Epsilon),
                _maximumBankAngle);
            float currentBank = Mathf.DeltaAngle(
                _bodyRestRotation.eulerAngles.z,
                _bodyTransform.localEulerAngles.z);
            float smoothedBank = Mathf.SmoothDampAngle(
                currentBank,
                bank,
                ref _bankVelocity,
                BANK_SMOOTH_TIME,
                Mathf.Infinity,
                deltaTime);
            _bodyTransform.localRotation =
                _bodyRestRotation * Quaternion.Euler(0f, 0f, smoothedBank);
        }

        private void DisplayRoute(Vector3[] waypoints)
        {
            _lineRenderer.positionCount = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                _lineRenderer.SetPosition(i, waypoints[i]);
            }

            _lineRenderer.enabled = _isSelected && waypoints.Length > 1;
            _displayedRoute = waypoints;
            RouteDisplayChanged?.Invoke();
        }

        private void ClearRoute()
        {
            _currentPathTangent = null;
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
            _displayedRoute = Array.Empty<Vector3>();
            RouteDisplayChanged?.Invoke();
        }
    }
}
