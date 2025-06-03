using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using EmpireAtWar.Commands.Move;
using EmpireAtWar.Components.Ship.Movement;
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
        private Sequence _bodyRotationSequence;
        private Vector3[] _waypoints;
        private float _duration;

        [Inject] private IMoveCommand MoveCommand { get; }
        
        private Vector3 CurrentPosition => transform.position;

        protected override void OnInit()
        {
            transform.rotation = Model.StartRotation;
            transform.position = Model.JumpPosition;
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
            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            targetPosition.y = CurrentPosition.y;
            Vector3 targetRotation = Quaternion.LookRotation(targetPosition - CurrentPosition).eulerAngles;
            float rotationDuration =
                Mathf.Min(Mathf.Abs(targetRotation.y - transform.rotation.eulerAngles.y) / Model.RotationSpeed,
                    Model.MinRotationDuration);// todo: rebuild this 

            if (rotationDuration < 1f)
            {
                Debug.Log($"rotationDuration:{rotationDuration}");
            }
            _moveSequence.Append(
                transform.DORotate(
                        targetRotation,
                        rotationDuration,
                        rotationMode)
                    .SetEase(lookAtEase));
            _moveSequence.Join(bodyTransform.DOLocalRotate(GetRotation(targetPosition), rotationDuration).SetEase(lookAtEase));

            _moveSequence.Append(bodyTransform.DOLocalRotate(Vector3.zero, BODY_ROTATION_DEFAULT_DURATION)
                .SetEase(lookAtEase));
        }

        private void StopAllMovement()
        {
            _moveSequence.KillIfExist();
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
            _moveSequence.AppendCallback(DestroyView);
        }

        private void DestroyView()
        {
            //- use command
            //todo: invoke move model that fall flow is finished 
            //Destroy(View.Transform.parent.gameObject);
        }

        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - CurrentPosition;

            transform.rotation = Quaternion.LookRotation(lookDirection);
            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            _moveSequence.Append(transform.DOMove(point, Model.HyperSpaceSpeed)
                .SetEase(hyperSpaceEase));
        }

        private void UpdateTargetPosition(Vector3 targetPosition)
        {
            targetPosition.y = CurrentPosition.y;

            _moveSequence.KillIfExist();

            var distance = Vector3.Distance(CurrentPosition, targetPosition);
            _moveSequence = DOTween.Sequence();

            Vector3 p1 = CurrentPosition + (transform.forward * distance) * IsBehindTarget(targetPosition);
            Vector3 p2 = CurrentPosition + ((IsRightFromTarget(targetPosition) * transform.right) * distance) *
                IsBehindTarget(targetPosition);

            _waypoints = PathCalculationUtils.GetWayPointsOfBezierPath(CurrentPosition, p1, p2, targetPosition);

            float curvedDistance = 0;
            //lineRenderer.positionCount = _waypoints.Length;
            for (var i = 0; i < _waypoints.Length; i++)
            {
               // lineRenderer.SetPosition(i, _waypoints[i]);

                if (i == _waypoints.Length - 1) break;
                curvedDistance += Vector3.Distance(_waypoints[i], _waypoints[i + 1]);
            }

            _duration = curvedDistance / Model.Speed;
            _moveSequence.Append(
                transform.DOPath
                    (_waypoints,
                        _duration,
                        PathType.CatmullRom,
                        PathMode.Full3D,
                        10)
                    .SetLookAt(0.01f)
                    .SetOptions(AxisConstraint.Y, AxisConstraint.X | AxisConstraint.Z)
                    //.OnWaypointChange(HandleWaypoints)
                    .SetEase(moveEase));

            _bodyRotationSequence.KillIfExist();
            _moveSequence.Append(bodyTransform.DOLocalRotate(Vector3.zero, BODY_ROTATION_DEFAULT_DURATION)
                .SetEase(lookAtEase));
            // _moveSequence.AppendCallback(() => lineRenderer.positionCount = 0);
        }

        private void HandleWaypoints(int index)
        {
            if (bodyTransform == null) return;

            _bodyRotationSequence.KillIfExist();
            _bodyRotationSequence = DOTween.Sequence();
            _bodyRotationSequence.Append(GetRotationSequence(_waypoints[index], _duration / _waypoints.Length));
        }

        private TweenerCore<Quaternion, Vector3, QuaternionOptions> GetRotationSequence(Vector3 targetPosition,
            float duration)
        {
            float modifier = -IsRightFromTarget(targetPosition);
            Vector3 targetRotation = Vector3.forward * Model.BodyRotationMaxAngle * modifier;
            return bodyTransform.DOLocalRotate(targetRotation, duration).SetEase(lookAtEase);
        }
        
        
        private Vector3 GetRotation(Vector3 targetPosition)
        {
            float modifier = -IsRightFromTarget(targetPosition);
            return Vector3.forward * Model.BodyRotationMaxAngle * modifier;
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