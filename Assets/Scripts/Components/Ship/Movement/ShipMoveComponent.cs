using DG.Tweening;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Mvc;
using EmpireAtWar.Utils;
using UnityEngine;
using Utilities.ScriptUtils.Dotween;
using Utilities.ScriptUtils.Math;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.Ship.Movement
{
    public class ShipMoveComponent : MonoComponent<ShipMoveModel>, IShipMoveComponent, IInitializable,
        ILateDisposable
    {
        private const float BODY_ROTATION_DEFAULT_DURATION = 1f;

        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;

        private ICameraService _cameraService;
        private Vector3 _startPosition;
        private FogOfWarSystem _fogOfWarSystem;
        private PlayerType _playerType;
        private Sequence _moveSequence;
        private Vector3[] _waypoints;
        private Vector3? _pendingTargetPosition;
        private float _duration;
        private bool _canMove;

        public bool CanMove => Model.CanMove;
        public Vector3 CurrentPosition => Model.CurrentPosition;
        public Transform ViewTransform => Model.ViewTransform.Value;
        public bool IsMoving => Model.IsMoving;
        public float HyperSpaceDuration => Model.HyperSpaceDuration;

        public event System.Action<Vector3> TargetPositionChanged
        {
            add => Model.TargetPosition.OnChanged += value;
            remove => Model.TargetPosition.OnChanged -= value;
        }

        public event System.Action<Vector3> LookAtTargetChanged
        {
            add => Model.LookAtTarget.OnChanged += value;
            remove => Model.LookAtTarget.OnChanged -= value;
        }

        public event System.Action Stopped
        {
            add => Model.OnStop += value;
            remove => Model.OnStop -= value;
        }


        [Inject]
        private void Construct(
            ShipMoveModel model,
            ICameraService cameraService,
            Vector3 startPosition,
            FogOfWarSystem fogOfWarSystem,
            PlayerType playerType)
        {
            SetModel(model);
            _cameraService = cameraService;
            startPosition.y = Model.Height;
            _startPosition = startPosition;
            _fogOfWarSystem = fogOfWarSystem;
            _playerType = playerType;
            // Model.TargetPosition = startPosition;
        }

        public void Initialize()
        {
            lineRenderer.enabled = false;
            Model.HyperSpacePosition = _startPosition;

            transform.rotation = Model.StartRotation;
            transform.position = Model.JumpPosition;
            _canMove = false;
            HyperSpaceJump(Model.HyperSpacePosition);

            Model.TargetPosition.OnChanged += UpdateTargetPosition;
            Model.OnStop += StopAllMovement;
            Model.LookAtTarget.OnChanged += LookAt;

            if (_playerType == PlayerType.Player)
            {
                _fogOfWarSystem.RegisterVisionSource(Model.ViewTransform.Value, 80f);
            }
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            Model.TargetPosition.OnChanged -= UpdateTargetPosition;
            Model.OnStop -= StopAllMovement;
            Model.LookAtTarget.OnChanged -= LookAt;
            FallDown();
        }

        public void MoveToPosition(Vector2 screenPosition)
        {
            Vector3 newPosition = GetWorldCoordinate(screenPosition);
            SetTargetPosition(newPosition);
        }

        private void SetTargetPosition(Vector3 newPosition)
        {
            if (!newPosition.IsEqual(Model.TargetPosition.Value))
            {
                Model.TargetPosition.Value = newPosition;
            }
        }
        private Vector3 GetWorldCoordinate(Vector2 screenPosition)
        {
            Vector3 point = _cameraService.GetWorldPoint(screenPosition, Model.CurrentPosition);
            point.y = Model.Height;

            return point;
        }

        public float MoveAround()
        {
            Vector3 backPosition = Model.CurrentPosition - Model.ViewTransform.Value.forward * Random.Range(30, 50f) + Model.ViewTransform.Value.right * Random.Range(-30, 30);
            SetTargetPosition(backPosition);

            return Vector3.Distance(backPosition, Model.CurrentPosition) / Model.Speed;
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
            Model.LookAtTarget.Value = targetPosition;
        }

        public float GetRange(Vector3 targetPosition)
        {
            return Vector3.Distance(Model.CurrentPosition, targetPosition);
        }

        public void Stop()
        {
            Model.Stop();
        }

        public void ApplyMoveCoefficient(float coefficient)
        {
            Model.ApplyMoveCoefficient(coefficient);
        }

        public void HandleSelection(bool isSelected)
        {
            lineRenderer.enabled = isSelected;
        }

        private Vector3 CurrentViewPosition => transform.position;

        private void LookAt(Vector3 targetPosition)
        {
            _moveSequence.KillExt();
            _moveSequence = DOTween.Sequence();

            targetPosition.y = CurrentViewPosition.y;
            Quaternion desiredRotation = Quaternion.LookRotation(targetPosition - CurrentViewPosition);
            float angle = Quaternion.Angle(transform.rotation, desiredRotation);
            float safeSpeed = Mathf.Max(Model.RotationSpeed, 0.01f);
            float rotationDuration = Mathf.Clamp(
                angle / safeSpeed,
                Model.MinRotationDuration,
                Model.MaxRotationDuration);

            _moveSequence.Append(transform.DORotateQuaternion(desiredRotation, rotationDuration).SetEase(lookAtEase));

            float targetZ = GetZRotationOnly(targetPosition);
            Vector3 startEuler = bodyTransform.localEulerAngles;
            Vector3 bodyTargetEuler = new(startEuler.x, startEuler.y, targetZ);

            _moveSequence.Join(bodyTransform.DOLocalRotate(bodyTargetEuler, rotationDuration).SetEase(lookAtEase));
            _moveSequence.Append(bodyTransform.DOLocalRotate(
                    new Vector3(startEuler.x, startEuler.y, 0f),
                    BODY_ROTATION_DEFAULT_DURATION)
                .SetEase(lookAtEase));
        }

        private float GetZRotationOnly(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - CurrentViewPosition;
            float direction = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
            return Mathf.Clamp(-direction * 0.2f, -15f, 15f);
        }

        private void StopAllMovement()
        {
            if (!_canMove)
            {
                _pendingTargetPosition = null;
                return;
            }

            _moveSequence.KillExt();
        }

        private void FallDown()
        {
            Vector3 point = CurrentViewPosition - Model.FallDownDirection;

            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            _moveSequence.Append(transform.DOMove(point, Model.FallDownDuration));
            _moveSequence.Join(transform.DOLocalRotate(Model.FallDownRotation.Value, Model.FallDownDuration));
        }

        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - CurrentViewPosition;

            transform.rotation = Quaternion.LookRotation(lookDirection);
            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            _moveSequence.Append(transform.DOMove(point, Model.HyperSpaceDuration).SetEase(hyperSpaceEase));
            _moveSequence.OnComplete(() =>
            {
                _canMove = true;
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
            if (!_canMove)
            {
                _pendingTargetPosition = targetPosition;
                return;
            }

            targetPosition.y = CurrentViewPosition.y;
            _moveSequence.KillExt();
            _moveSequence = DOTween.Sequence();

            float distance = Vector3.Distance(CurrentViewPosition, targetPosition);
            Vector3 p1 = CurrentViewPosition + transform.forward * distance * IsBehindTarget(targetPosition);
            Vector3 p2 = CurrentViewPosition + IsRightFromTarget(targetPosition) * transform.right * distance *
                IsBehindTarget(targetPosition);

            _waypoints = PathCalculationUtils.GetWayPointsOfBezierPath(
                CurrentViewPosition,
                p1,
                p2,
                targetPosition);

            float curvedDistance = 0f;
            lineRenderer.positionCount = _waypoints.Length;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                lineRenderer.SetPosition(i, _waypoints[i]);
                if (i < _waypoints.Length - 1)
                {
                    curvedDistance += Vector3.Distance(_waypoints[i], _waypoints[i + 1]);
                }
            }

            _duration = curvedDistance / Model.Speed;
            _moveSequence.Append(transform.DOPath(
                    _waypoints,
                    _duration,
                    PathType.CatmullRom,
                    PathMode.Full3D,
                    10)
                .SetOptions(false, AxisConstraint.Y, AxisConstraint.X)
                .SetLookAt(0.01f)
                .SetEase(Ease.Linear));
        }

        private float IsRightFromTarget(Vector3 targetPosition)
        {
            Vector3 positionRelative = transform.InverseTransformPoint(targetPosition);
            return positionRelative.x > 0 ? 1f : -1f;
        }

        private float IsBehindTarget(Vector3 targetPosition)
        {
            Vector3 positionRelative = transform.InverseTransformPoint(targetPosition);
            return positionRelative.z > 0 ? 0.2f : 1f;
        }
    }
}
