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
        private const int ROUTE_STEP_SEARCH_ITERATIONS = 8;

        private readonly Transform _rootTransform;
        private readonly Transform _bodyTransform;
        private readonly LineRenderer _lineRenderer;
        private readonly Ease _hyperSpaceEase;
        private readonly Quaternion _bodyRestRotation;

        private Sequence _translationSequence;
        private bool _isSelected;
        private Vector3 _lookDirection;
        private bool _hasLookDirection;
        private Vector3? _currentPathTangent;
        private ShipBezierRoute _route;
        private Action _pathCompleted;
        private float _routeProgress;
        private float _routeSpeed;
        private float _rotationSpeed;
        private float _turnAcceleration;
        private float _maximumBankAngle;
        private float _angularVelocity;
        private float _bankVelocity;
        private bool _isTurningToRoute;

        public Vector3? CurrentPathTangent => _currentPathTangent;

        public void SetRouteSpeed(float speed) => _routeSpeed = speed;

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

        public void SetSelected(bool isSelected, bool isMoving)
        {
            _isSelected = isSelected;
            _lineRenderer.enabled =
                isSelected && isMoving && _lineRenderer.positionCount > 1;
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
            _isTurningToRoute = plan.TurnDuration > Mathf.Epsilon;
            _currentPathTangent = null;
            DisplayRoute(plan.Trajectory);
        }

        public void StopPath()
        {
            _route = null;
            _pathCompleted = null;
            _currentPathTangent = null;
            _isTurningToRoute = false;
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

            float requestedStep = _routeSpeed * deltaTime /
                Mathf.Max(_route.Length, Mathf.Epsilon);
            float nextProgress = Mathf.Min(1f, _routeProgress + requestedStep);
            float allowedTurn = Mathf.Max(
                MINIMUM_TANGENT_STEP,
                Mathf.Abs(_angularVelocity) * deltaTime +
                0.5f * _turnAcceleration * deltaTime * deltaTime);
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

            _routeProgress = low;
            _rootTransform.position = _route.EvaluateNormalizedDistance(
                _routeProgress,
                out Vector3 routeTangent);
            _currentPathTangent = routeTangent;
            StepRotation(routeTangent, deltaTime);
            if (_routeProgress < 1f - Mathf.Epsilon)
            {
                return;
            }

            Action completed = _pathCompleted;
            StopPath();
            completed?.Invoke();
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
        }

        private void ClearRoute()
        {
            _currentPathTangent = null;
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }
    }
}
