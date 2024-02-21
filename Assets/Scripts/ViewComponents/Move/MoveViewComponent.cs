using DG.Tweening;
using EmpireAtWar.Commands.Move;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.ScriptUtils.Dotween;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.ViewComponents;
using Zenject;

namespace EmpireAtWar.ViewComponents.Move
{
    public class MoveViewComponent:ViewComponent<IMoveModelObserver>
    {
        private const float FallDownDuration = 260f;
        
        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;

        private Sequence moveSequence;
        [Inject]
        private IMoveCommand MoveCommand { get; }
        
        protected override void OnInit()
        {
            transform.position = Model.HyperSpacePosition - Vector3.right * 1000f;
            HyperSpaceJump(Model.HyperSpacePosition);
            Model.OnTargetPositionChanged += UpdateTargetPosition;
            Model.OnHyperSpaceJump += HyperSpaceJump;
            MoveCommand.Assign(transform);
        }

        protected override void OnRelease()
        {
            Model.OnTargetPositionChanged -= UpdateTargetPosition;
            Model.OnHyperSpaceJump -= HyperSpaceJump;
            FallDown();
        }
        
        private void FallDown()
        {
            Vector3 point = transform.position - Vector3.up * 40f;
            
            Vector3 randomRotation = new Vector3(Random.Range(-90, 90), transform.localRotation.eulerAngles.y + Random.Range(-10, 10), Random.Range(0, 360));
            
            moveSequence.KillIfExist();
            moveSequence = DOTween.Sequence();
            moveSequence.Append(transform.DOMove(point, FallDownDuration));
            moveSequence.Join(transform.DOLocalRotate(
                randomRotation,
                FallDownDuration));
        }


        private void HyperSpaceJump(Vector3 point)
        {
            Vector3 lookDirection = point - transform.position;

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

        private void UpdateTargetPosition(Vector3 position)
        {
            position.y = transform.position.y;
            Vector3 lookDirection = position - transform.position;

            moveSequence.KillIfExist();

            var distance = Vector3.Distance(transform.position, position);
            float duration = distance / Model.Speed;
            moveSequence = DOTween.Sequence();

            Vector3 targetRotation = Quaternion.LookRotation(lookDirection).eulerAngles;
            float rotationDuration = Mathf.Min(Mathf.Abs(targetRotation.y - transform.rotation.eulerAngles.y) / Model.RotationSpeed, Model.MinRotationDuration);
            moveSequence.Append(
                transform.DORotate(
                        targetRotation,
                        rotationDuration,
                        rotationMode)
                    .SetEase(lookAtEase));

            moveSequence.Append(transform.DOMove(position, duration)
                .SetEase(moveEase));
        }
    }
}