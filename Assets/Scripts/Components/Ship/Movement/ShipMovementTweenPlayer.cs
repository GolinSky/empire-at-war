using System;
using DG.Tweening;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    internal sealed class ShipMovementTweenPlayer
    {
        private const float BODY_STRAIGHTEN_DURATION = 1f;
        private const float LOOK_DIRECTION_TOLERANCE = 1f;

        private readonly Transform _rootTransform;
        private readonly Transform _bodyTransform;
        private readonly LineRenderer _lineRenderer;
        private readonly Ease _lookAtEase;
        private readonly Ease _hyperSpaceEase;
        private readonly Quaternion _bodyRestRotation;

        private Sequence _translationSequence;
        private Sequence _rotationSequence;
        private bool _isSelected;
        private Vector3 _lookDirection;
        private bool _hasLookDirection;
        private Vector3? _currentPathTangent;

        public Vector3? CurrentPathTangent => _currentPathTangent;

        public ShipMovementTweenPlayer(
            Transform rootTransform,
            Transform bodyTransform,
            LineRenderer lineRenderer,
            Ease lookAtEase,
            Ease hyperSpaceEase)
        {
            _rootTransform = rootTransform ??
                throw new ArgumentNullException(nameof(rootTransform));
            _bodyTransform = bodyTransform ??
                throw new ArgumentNullException(nameof(bodyTransform));
            _lineRenderer = lineRenderer ??
                throw new ArgumentNullException(nameof(lineRenderer));
            _lookAtEase = lookAtEase;
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
            float maximumBankAngle)
        {
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            targetDirection.Normalize();
            Vector3 currentDirection = _rootTransform.forward;
            currentDirection.y = 0f;
            if (currentDirection.sqrMagnitude > Mathf.Epsilon &&
                Vector3.Angle(currentDirection, targetDirection) <=
                LOOK_DIRECTION_TOLERANCE)
            {
                return;
            }

            if (_hasLookDirection &&
                Vector3.Angle(_lookDirection, targetDirection) <=
                LOOK_DIRECTION_TOLERANCE)
            {
                return;
            }

            _lookDirection = targetDirection;
            _hasLookDirection = true;
            _rotationSequence.KillExt();
            _rotationSequence = DOTween.Sequence();

            Quaternion desiredRotation = Quaternion.LookRotation(
                targetDirection,
                Vector3.up);
            float rotationDuration =
                ShipRotationKinematics.CalculateTurnDuration(
                    _rootTransform.rotation,
                    targetDirection,
                    Mathf.Max(rotationSpeed, Mathf.Epsilon));
            float bankAngle = ShipRotationKinematics.CalculateLookBankAngle(
                _rootTransform.rotation,
                targetDirection,
                maximumBankAngle);
            Quaternion bodyTargetRotation =
                _bodyRestRotation * Quaternion.Euler(0f, 0f, bankAngle);

            _rotationSequence.Append(_rootTransform
                .DORotateQuaternion(desiredRotation, rotationDuration)
                .SetEase(Ease.Linear));
            _rotationSequence.Join(_bodyTransform
                .DOLocalRotateQuaternion(
                    bodyTargetRotation,
                    rotationDuration)
                .SetEase(_lookAtEase));
            _rotationSequence.Append(_bodyTransform
                .DOLocalRotateQuaternion(
                    _bodyRestRotation,
                    BODY_STRAIGHTEN_DURATION)
                .SetEase(_lookAtEase));
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
            float rotationSpeed,
            float maximumBankAngle,
            Action completed)
        {
            _hasLookDirection = false;
            _currentPathTangent = null;
            _translationSequence.KillExt();
            _rotationSequence.KillExt();
            _translationSequence = DOTween.Sequence();
            DisplayRoute(plan.Trajectory);

            if (plan.TurnDuration > Mathf.Epsilon)
            {
                Vector3 initialDirection = plan.Route.InitialTangent;
                Quaternion desiredRotation = Quaternion.LookRotation(
                    initialDirection,
                    Vector3.up);
                float bankAngle =
                    ShipRotationKinematics.CalculateLookBankAngle(
                        _rootTransform.rotation,
                        initialDirection,
                        maximumBankAngle);
                Quaternion bodyTargetRotation =
                    _bodyRestRotation *
                    Quaternion.Euler(0f, 0f, bankAngle);
                _translationSequence.Append(_rootTransform
                    .DORotateQuaternion(
                        desiredRotation,
                        plan.TurnDuration)
                    .SetEase(Ease.Linear));
                _translationSequence.Join(_bodyTransform
                    .DOLocalRotateQuaternion(
                        bodyTargetRotation,
                        plan.TurnDuration)
                    .SetEase(_lookAtEase));
                _translationSequence.AppendCallback(StraightenBody);
            }

            _translationSequence.Append(DOVirtual.Float(
                    0f,
                    1f,
                    plan.MovementDuration,
                    progress => ApplyRouteProgress(
                        plan.Route,
                        progress,
                        rotationSpeed,
                        maximumBankAngle))
                .SetEase(Ease.Linear));
            _translationSequence.OnComplete(() =>
            {
                ApplyRouteProgress(
                    plan.Route,
                    1f,
                    rotationSpeed,
                    maximumBankAngle);
                StraightenBody();
                ClearRoute();
                completed?.Invoke();
            });
        }

        public void StopPath()
        {
            _hasLookDirection = false;
            _translationSequence.KillExt();
            StraightenBody();
            ClearRoute();
        }

        public void Release()
        {
            _hasLookDirection = false;
            _translationSequence.KillExt();
            _rotationSequence.KillExt();
            ClearRoute();
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

        private void ApplyRouteProgress(
            ShipBezierRoute route,
            float progress,
            float rotationSpeed,
            float maximumBankAngle)
        {
            Vector3 position = route.EvaluateNormalizedDistance(
                progress,
                out Vector3 tangent);
            _currentPathTangent = tangent;
            _rootTransform.position = position;
            RotateAlongRoute(
                tangent,
                rotationSpeed,
                maximumBankAngle);
        }

        private void RotateAlongRoute(
            Vector3 tangent,
            float rotationSpeed,
            float maximumBankAngle)
        {
            Quaternion previousRotation = _rootTransform.rotation;
            float safeRotationSpeed = Mathf.Max(
                rotationSpeed,
                Mathf.Epsilon);
            float bank = ShipRotationKinematics.CalculateBankAngle(
                previousRotation,
                tangent,
                safeRotationSpeed,
                Time.deltaTime,
                maximumBankAngle);
            _rootTransform.rotation = ShipRotationKinematics.Step(
                previousRotation,
                tangent,
                safeRotationSpeed,
                Time.deltaTime);

            Quaternion targetBodyRotation =
                _bodyRestRotation * Quaternion.Euler(0f, 0f, bank);
            _bodyTransform.localRotation = Quaternion.RotateTowards(
                _bodyTransform.localRotation,
                targetBodyRotation,
                safeRotationSpeed * Time.deltaTime);
        }

        private void StraightenBody()
        {
            _bodyTransform.localRotation = _bodyRestRotation;
        }

        private void ClearRoute()
        {
            _currentPathTangent = null;
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }
    }
}
