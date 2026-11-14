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

        private readonly Transform _rootTransform;
        private readonly Transform _bodyTransform;
        private readonly LineRenderer _lineRenderer;
        private Sequence _translationSequence;
        private ShipRoute _route;
        private ShipRouteSpeedProfile _speedProfile;
        private Action _pathCompleted;

        private readonly Ease _hyperSpaceEase;
        private readonly Quaternion _bodyRestRotation;
        private Vector3 _lookDirection;
        private Vector3? _currentPathTangent;

        private float _routeDistance;
        private float _profileTime;
        private int _pieceIndex;
        private int _profilePhaseIndex;
        private int _nextTurnPiece;
        private float _routeSpeed;
        private float _currentSpeed;
        private float _rotationSpeed;
        private float _turnAcceleration;
        private float _maximumBankAngle;
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
        public float CurrentSpeed => _currentSpeed;

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
            if (_routeSpeed == speed) return;
            _routeSpeed = speed;
            if (_route != null) RebuildSpeedProfile();
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
            _routeDistance = 0f;
            _pieceIndex = 0;
            _routeSpeed = speed;
            _rotationSpeed = rotationSpeed;
            _turnAcceleration = turnAcceleration;
            _maximumBankAngle = maximumBankAngle;
            _isBraking = false;
            _isTurningToRoute = plan.TurnDuration > Mathf.Epsilon &&
                Vector3.Angle(_rootTransform.forward, _route.InitialTangent) > ROUTE_HEADING_TOLERANCE;
            if (_isTurningToRoute)
            {
                // The hull turns in place before it moves, so it starts the route from rest.
                _currentSpeed = 0f;
            }
            RebuildSpeedProfile(plan.SpeedProfile);
            FindNextTurn();
            _currentPathTangent = null;
            DisplayRoute(plan.Trajectory);
        }

        public void StopPath()
        {
            _route = null;
            _speedProfile = null;
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
                _route.EvaluateDistance(_routeDistance, ref _pieceIndex,
                    out Vector3 turnTangent, out _);
                StepRotation(turnTangent, deltaTime);
                if (Vector3.Angle(_rootTransform.forward, turnTangent) > ROUTE_HEADING_TOLERANCE)
                    return;
                _isTurningToRoute = false;
            }

            if (_isBraking)
            {
                float previousSpeed = _currentSpeed;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f,
                    _speedProfile.Acceleration * deltaTime);
                _routeDistance = Mathf.Min(_route.Length,
                    _routeDistance + (previousSpeed + _currentSpeed) * 0.5f * deltaTime);
            }
            else
            {
                _profileTime = Mathf.Min(_profileTime + deltaTime, _speedProfile.Duration);
                if (_nextTurnPiece < _route.Pieces.Count &&
                    _profileTime >= _speedProfile.GetPieceStartTime(_nextTurnPiece))
                {
                    _profileTime = _speedProfile.GetPieceStartTime(_nextTurnPiece);
                    _routeDistance = _route.GetPieceStartDistance(_nextTurnPiece);
                    _pieceIndex = _nextTurnPiece;
                    _currentSpeed = 0f;
                    _isTurningToRoute = true;
                    FindNextTurn();
                }
                else
                {
                    _speedProfile.EvaluateTime(_profileTime, ref _profilePhaseIndex,
                        out _routeDistance, out _currentSpeed);
                }
            }

            _rootTransform.position = _route.EvaluateDistance(_routeDistance,
                ref _pieceIndex, out Vector3 routeTangent, out float curvature);
            _currentPathTangent = routeTangent;
            if (!_isTurningToRoute)
            {
                if (_currentSpeed > _speedProfile.GetSpeedLimit(_pieceIndex) + Mathf.Epsilon)
                {
                    // Replans can enter their first arc above its limit; brake without snapping yaw.
                    StepRotation(routeTangent, deltaTime);
                }
                else
                {
                    _rootTransform.rotation = Quaternion.LookRotation(routeTangent, Vector3.up);
                    _angularVelocity = _currentSpeed * curvature * Mathf.Rad2Deg;
                    SmoothBank(deltaTime);
                }
            }

            if (_isBraking)
            {
                if (_currentSpeed <= Mathf.Epsilon || _routeDistance >= _route.Length)
                    StopPath();
                return;
            }
            if (_profileTime < _speedProfile.Duration) return;

            // A replan shorter than the stopping distance must stop at its validated endpoint;
            // carrying residual speed beyond it would leave the clearance-checked route.
            Action completed = _pathCompleted;
            StopPath();
            completed?.Invoke();
        }

        private void RebuildSpeedProfile(ShipRouteSpeedProfile planned = null)
        {
            _speedProfile = planned != null && planned.Matches(_route, _routeSpeed, _rotationSpeed,
                _currentSpeed, _routeDistance)
                ? planned
                : new ShipRouteSpeedProfile(_route, _routeSpeed, _rotationSpeed,
                    _currentSpeed, startDistance: _routeDistance);
            _profileTime = 0f;
            _profilePhaseIndex = 0;
        }

        private void FindNextTurn()
        {
            _nextTurnPiece = _pieceIndex + 1;
            while (_nextTurnPiece < _route.Pieces.Count &&
                   !_route.RequiresTurnAtPiece(_nextTurnPiece))
                _nextTurnPiece++;
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
