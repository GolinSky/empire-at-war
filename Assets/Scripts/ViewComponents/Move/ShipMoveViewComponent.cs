using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using EmpireAtWar.Commands.Move;
using EmpireAtWar.Models.Movement;
using Utilities.ScriptUtils.Dotween;
using Utilities.ScriptUtils.Math;
using UnityEngine;
using LightWeightFramework.Components.ViewComponents;
using Zenject;

namespace EmpireAtWar.ViewComponents.Move
{
    public class ShipMoveViewComponent : ViewComponent<IShipMoveModelObserver>
    {
        private const float BodyRotationDefaultDuration = 1f;

        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform bodyTransform;

        private Sequence moveSequence;
        private Sequence bodyRotationSequence;
        private Vector3[] waypoints;
        private float duration;

        [Inject] private IMoveCommand MoveCommand { get; }

        private Vector3 CurrentPosition => transform.position;

        protected override void OnInit()
        {
            transform.position = Model.HyperSpacePosition - Vector3.right * 1000f; // move magic number to model
            HyperSpaceJump(Model.HyperSpacePosition);
            MoveCommand.Assign(transform); // inject transform

            Model.OnTargetPositionChanged += UpdateTargetPosition;
            Model.OnStop += StopAllMovement;
            Model.OnLookAt += LookAt;
        }

        protected override void OnRelease()
        {
            Model.OnTargetPositionChanged -= UpdateTargetPosition;
            Model.OnStop -= StopAllMovement;
            Model.OnLookAt -= LookAt;
            FallDown();
        }

        private void LookAt(Vector3 targetPosition)
        {
            moveSequence.KillIfExist();
            moveSequence = DOTween.Sequence();
            targetPosition.y = CurrentPosition.y;
            Vector3 targetRotation = Quaternion.LookRotation(targetPosition - CurrentPosition).eulerAngles;
            float rotationDuration =
                Mathf.Min(Mathf.Abs(targetRotation.y - transform.rotation.eulerAngles.y) / Model.RotationSpeed,
                    Model.MinRotationDuration);
            bodyRotationSequence.KillIfExist();

            moveSequence.Append(
                transform.DORotate(
                        targetRotation,
                        rotationDuration,
                        rotationMode)
                    .SetEase(lookAtEase));
            moveSequence.Join(GetRotationSequence(targetPosition, rotationDuration));
            moveSequence.Append(bodyTransform.DOLocalRotate(Vector3.zero, BodyRotationDefaultDuration)
                .SetEase(lookAtEase));
        }

        private void StopAllMovement()
        {
            moveSequence.KillIfExist();
        }

        private void FallDown()
        {
            Vector3 point = CurrentPosition - Model.FallDownDirection;

            moveSequence.KillIfExist();
            moveSequence = DOTween.Sequence();
            moveSequence.Append(transform.DOMove(point, Model.FallDownDuration));
            moveSequence.Join(transform.DOLocalRotate(
                Model.FallDownRotation.Value,
                Model.FallDownDuration));
            moveSequence.AppendCallback(DestroyView);
        }

        private void DestroyView()
        {
            Destroy(View.Transform.parent.gameObject);
        }

        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - CurrentPosition;

            transform.rotation = Quaternion.LookRotation(lookDirection);
            moveSequence.KillIfExist();
            moveSequence = DOTween.Sequence();
            moveSequence.Append(transform.DOMove(point, Model.HyperSpaceSpeed)
                .SetEase(hyperSpaceEase));
        }

        private void UpdateTargetPosition(Vector3 targetPosition)
        {
            targetPosition.y = CurrentPosition.y;

            moveSequence.KillIfExist();

            var distance = Vector3.Distance(CurrentPosition, targetPosition);
            moveSequence = DOTween.Sequence();

            Vector3 p1 = CurrentPosition + (transform.forward * distance) * IsBehindTarget(targetPosition);
            Vector3 p2 = CurrentPosition + ((IsRightFromTarget(targetPosition) * transform.right) * distance) *
                IsBehindTarget(targetPosition);

            waypoints = PathCalculationUtils.GetWayPointsOfBezierPath(CurrentPosition, p1, p2, targetPosition);

            float curvedDistance = 0;
            lineRenderer.positionCount = waypoints.Length;
            for (var i = 0; i < waypoints.Length; i++)
            {
                lineRenderer.SetPosition(i, waypoints[i]);

                if (i == waypoints.Length - 1) break;
                curvedDistance += Vector3.Distance(waypoints[i], waypoints[i + 1]);
            }

            duration = curvedDistance / Model.Speed;
            moveSequence.Append(
                transform.DOPath
                    (waypoints,
                        duration,
                        PathType.CatmullRom,
                        PathMode.Full3D,
                        10)
                    .SetLookAt(0.01f)
                    .SetOptions(AxisConstraint.Y, AxisConstraint.X | AxisConstraint.Z)
                    .OnWaypointChange(HandleWaypoints)
                    .SetEase(moveEase));

            bodyRotationSequence.KillIfExist();
            moveSequence.Append(bodyTransform.DOLocalRotate(Vector3.zero, BodyRotationDefaultDuration)
                .SetEase(lookAtEase));
            moveSequence.AppendCallback(() => lineRenderer.positionCount = 0);
        }

        private void HandleWaypoints(int index)
        {
            if (bodyTransform == null) return;

            bodyRotationSequence.KillIfExist();
            bodyRotationSequence = DOTween.Sequence();
            bodyRotationSequence.Append(GetRotationSequence(waypoints[index], duration / waypoints.Length));
        }

        private TweenerCore<Quaternion, Vector3, QuaternionOptions> GetRotationSequence(Vector3 targetPosition,
            float duration)
        {
            float modifier = -IsRightFromTarget(targetPosition);
            Vector3 targetRotation = Vector3.forward * Model.BodyRotationMaxAngle * modifier;
            return bodyTransform.DOLocalRotate(targetRotation, duration).SetEase(lookAtEase);
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