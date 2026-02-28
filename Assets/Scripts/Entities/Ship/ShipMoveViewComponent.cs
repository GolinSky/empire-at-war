using DG.Tweening;
using EmpireAtWar.Commands.Move;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents;
using Utilities.ScriptUtils.Dotween;
using Utilities.ScriptUtils.Math;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Move
{
    public class ShipMoveViewComponent : ViewComponent<IShipMoveModelObserver>
    {
        private const float BODY_ROTATION_DEFAULT_DURATION = 1f;

        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;

        private Sequence _moveSequence;
        private Vector3[] _waypoints;
        private float _duration;
        private bool _canMove;

        [Inject] private IMoveCommand MoveCommand { get; }
        
        private Vector3 CurrentPosition => transform.position;

        protected override void OnInit()
        {
            transform.rotation = Model.StartRotation;
            transform.position = Model.JumpPosition;
            _canMove = false;
            HyperSpaceJump(Model.HyperSpacePosition);

            Model.TargetPositionObserver.OnChanged += UpdateTargetPosition;
            Model.OnStop += StopAllMovement;
            Model.LookAtTargetObserver.OnChanged += LookAt;
        }

        protected override void OnRelease()
        {
            Model.TargetPositionObserver.OnChanged -= UpdateTargetPosition;
            Model.OnStop -= StopAllMovement;
            Model.LookAtTargetObserver.OnChanged -= LookAt;
            FallDown();
        }

        private void LookAt(Vector3 targetPosition)
        {
            _moveSequence.KillExt();
            _moveSequence = DOTween.Sequence();

            // Flatten target Y to current level
            targetPosition.y = CurrentPosition.y;

            // Calculate world rotation
            Quaternion desiredRotation = Quaternion.LookRotation(targetPosition - CurrentPosition);
            float angle = Quaternion.Angle(transform.rotation, desiredRotation);

            float safeSpeed = Mathf.Max(Model.RotationSpeed, 0.01f);
            float rotationDuration = Mathf.Clamp(angle / safeSpeed, Model.MinRotationDuration, Model.MaxRotationDuration);

            // World rotation (Y-axis)
            _moveSequence.Append(transform.DORotateQuaternion(desiredRotation, rotationDuration).SetEase(lookAtEase));

            // Local Z-only rotation for body
            float targetZ = GetZRotationOnly(targetPosition); // You'll define this
            Vector3 startEuler = bodyTransform.localEulerAngles;
            Vector3 bodyTargetEuler = new Vector3(startEuler.x, startEuler.y, targetZ);

            _moveSequence.Join(
                bodyTransform.DOLocalRotate(bodyTargetEuler, rotationDuration).SetEase(lookAtEase)
            );

            _moveSequence.Append(
                bodyTransform.DOLocalRotate(new Vector3(startEuler.x, startEuler.y, 0f), BODY_ROTATION_DEFAULT_DURATION)
                    .SetEase(lookAtEase)
            );
        }
        
        private float GetZRotationOnly(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - CurrentPosition;
            float direction = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
            return Mathf.Clamp(-direction * 0.2f, -15f, 15f); // lean effect
        }

        private void StopAllMovement()
        {
            _moveSequence.KillExt();
        }

        private void FallDown()
        {
            Vector3 point = CurrentPosition - Model.FallDownDirection;

            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            _moveSequence.Append(transform.DOMove(point, Model.FallDownDuration));
            _moveSequence.Join(transform.DOLocalRotate(
                Model.FallDownRotation.Value,
                Model.FallDownDuration));
        }
        

        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - CurrentPosition;

            transform.rotation = Quaternion.LookRotation(lookDirection);
            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            _moveSequence.Append(transform.DOMove(point, Model.HyperSpaceDuration)
                .SetEase(hyperSpaceEase));
            _moveSequence.OnComplete((() =>
            {
                Debug.Log("Hyper Space Jump Complete");
                _canMove = true;
            }));
        }

        private void UpdateTargetPosition(Vector3 targetPosition)
        {
            targetPosition.y = CurrentPosition.y;

            _moveSequence.KillExt();
            _moveSequence = DOTween.Sequence();
            
            var distance = Vector3.Distance(CurrentPosition, targetPosition);

            Vector3 p1 = CurrentPosition + (transform.forward * distance) * IsBehindTarget(targetPosition);
            Vector3 p2 = CurrentPosition + ((IsRightFromTarget(targetPosition) * transform.right) * distance) *
                IsBehindTarget(targetPosition);

            _waypoints = PathCalculationUtils.GetWayPointsOfBezierPath(CurrentPosition, p1, p2, targetPosition);

            float curvedDistance = 0;
            lineRenderer.positionCount = _waypoints.Length;
            for (var i = 0; i < _waypoints.Length; i++)
            {
                lineRenderer.SetPosition(i, _waypoints[i]);

                if (i == _waypoints.Length - 1) break;
                curvedDistance += Vector3.Distance(_waypoints[i], _waypoints[i + 1]);
            }

            _duration = curvedDistance / Model.Speed;

            _moveSequence.Append(
                
                transform.DOPath(_waypoints,
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
            return positionRelative.x > 0 ? 1 : -1;
        }

        private float IsBehindTarget(Vector3 targetPosition)
        {
            Vector3 positionRelative = transform.InverseTransformPoint(targetPosition);
            return positionRelative.z > 0 ? 0.2f : 1f;
        }
    }
}