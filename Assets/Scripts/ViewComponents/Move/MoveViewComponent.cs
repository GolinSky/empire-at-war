using DG.Tweening;
using EmpireAtWar.Commands.Move;
using EmpireAtWar.Models.Movement;
using Utilities.ScriptUtils.Dotween;
using Utilities.ScriptUtils.Math;
using UnityEngine;
using LightWeightFramework.Components.ViewComponents;
using Zenject;
using Random = UnityEngine.Random;

namespace EmpireAtWar.ViewComponents.Move
{
    public class MoveViewComponent : ViewComponent<IMoveModelObserver>
    {
        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;
        [SerializeField] private LineRenderer lineRenderer;
        
        private Sequence moveSequence;
        private Vector3[] waypoints;
        private float duration;

        [Inject] private IMoveCommand MoveCommand { get; }

        private Vector3 CurrentPosition => transform.position;

        protected override void OnInit()
        {
            transform.position = Model.HyperSpacePosition - Vector3.right * 1000f; // move magic numbet to model
            HyperSpaceJump(Model.HyperSpacePosition);
            MoveCommand.Assign(transform);// inject transform

            Model.OnTargetPositionChanged += UpdateTargetPosition;
            Model.OnHyperSpaceJump += HyperSpaceJump;
            Model.OnStop += StopAllMovement;
            Model.OnLookAt += LookAt;
        }

        protected override void OnRelease()
        {
            Model.OnTargetPositionChanged -= UpdateTargetPosition;
            Model.OnHyperSpaceJump -= HyperSpaceJump;
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
            float rotationDuration = Mathf.Min(Mathf.Abs(targetRotation.y - transform.rotation.eulerAngles.y) / Model.RotationSpeed, Model.MinRotationDuration);
            
            moveSequence.Append(
                transform.DORotate(
                        targetRotation,
                        rotationDuration,
                        rotationMode)
                    .SetEase(lookAtEase));
        }
        
        private void StopAllMovement()
        {
            moveSequence.KillIfExist();
        }

        private void FallDown()
        {
            Vector3 point = CurrentPosition - Model.FallDownDirection;

            Vector3 randomRotation = new Vector3(Random.Range(-90, 90),
                transform.localRotation.eulerAngles.y + Random.Range(-10, 10), Random.Range(0, 360));

            moveSequence.KillIfExist();
            moveSequence = DOTween.Sequence();
            moveSequence.Append(transform.DOMove(point, Model.FallDownDuration));
            moveSequence.Join(transform.DOLocalRotate(
                randomRotation,
                Model.FallDownDuration));
        }


        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - CurrentPosition;

            moveSequence.KillIfExist();

            float duration = Model.HyperSpaceSpeed;
            moveSequence = DOTween.Sequence();

            moveSequence.Append(
                transform.DORotate(
                        Quaternion.LookRotation(lookDirection).eulerAngles,
                        1f,
                        rotationMode)
                    .SetEase(lookAtEase));

            moveSequence.Append(transform.DOMove(point, duration)
                .SetEase(hyperSpaceEase));
        }

        private void UpdateTargetPosition(Vector3 targetPosition)
        {
            targetPosition.y = CurrentPosition.y;

            moveSequence.KillIfExist();

            var distance = Vector3.Distance(CurrentPosition, targetPosition);
            moveSequence = DOTween.Sequence();


            Vector3 p1 = CurrentPosition + (transform.forward * distance) * IsBehindTarget(targetPosition);
            Vector3 p2 = CurrentPosition + ((IsRightFromTarget(targetPosition) * transform.right) * distance) * IsBehindTarget(targetPosition);
           
            waypoints = PathCalculationUtils.GetWayPointsOfBezierPath(CurrentPosition, p1, p2, targetPosition);

            float curvedDistance = 0;
            lineRenderer.positionCount = waypoints.Length;
            for (var i = 0; i < waypoints.Length; i++)
            {
                lineRenderer.SetPosition(i, waypoints[i]);

                if(i == waypoints.Length - 1) break;
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
                    .SetEase(moveEase));

          
            moveSequence.AppendCallback(() => lineRenderer.positionCount = 0);
        }
        

        private float IsRightFromTarget(Vector3 targetPosition)
        {
            Vector3 positionRelative = transform.InverseTransformPoint(targetPosition);
            return positionRelative.x > 0 ? 1 : -1;
        }
        private float IsBehindTarget(Vector3 targetPosition)
        {
            Vector3 positionRelative = transform.InverseTransformPoint(targetPosition);
            return positionRelative.z  > 0 ? 0.2f : 1f;
        }

    }
}