using DG.Tweening;
using EmpireAtWar.Models.Movement;
using UnityEngine;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Move
{
    public class MoveViewComponent:ViewComponent
    {
        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;

        private Sequence moveSequence;
        private IMoveModelObserver model;
        
        protected override void OnInit()
        {
            model = ModelObserver.GetModelObserver<IMoveModelObserver>();
            transform.position = model.StartPosition;
            HyperSpaceJump(model.HyperSpacePosition);
            model.OnTargetPositionChanged += UpdateTargetPosition;
            model.OnHyperSpaceJump += HyperSpaceJump;
        }

        protected override void OnRelease()
        {
            model.OnTargetPositionChanged -= UpdateTargetPosition;
            model.OnHyperSpaceJump -= HyperSpaceJump;
        }
        
        private void HyperSpaceJump(Vector3 point)
        {
            point += new Vector3(30, 0, 30);
            Vector3 lookDirection = point - transform.position;

            if (moveSequence != null && moveSequence.IsActive())
            {
                moveSequence.Kill();
            }

            float duration = model.HyperSpaceSpeed;
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

        private void UpdateTargetPosition(Vector3 position)
        {
            position.y = transform.position.y;
            Vector3 lookDirection = position - transform.position;

            if (moveSequence != null && moveSequence.IsActive())
            {
                moveSequence.Kill();
            }

            var distance = Vector3.Distance(transform.position, position);
            float duration = distance / model.Speed;
            moveSequence = DOTween.Sequence();

            moveSequence.Append(
                transform.DORotate(
                        Quaternion.LookRotation(lookDirection).eulerAngles,
                        1f,
                        rotationMode)
                    .SetEase(lookAtEase));

            moveSequence.Append(transform.DOMove(position, duration)
                .SetEase(moveEase));
        }
    }
}