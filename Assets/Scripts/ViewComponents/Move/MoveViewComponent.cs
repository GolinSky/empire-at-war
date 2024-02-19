using DG.Tweening;
using EmpireAtWar.Commands.Move;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.ScriptUtils.Dotween;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Move
{
    public class MoveViewComponent:ViewComponent
    {
        private const float FallDownDuration = 260f;
        
        [SerializeField] private RotateMode rotationMode = RotateMode.Fast;
        [SerializeField] private Ease lookAtEase;
        [SerializeField] private Ease moveEase;
        [SerializeField] private Ease hyperSpaceEase;

        private Sequence moveSequence;
        private IMoveModelObserver model;
        private IMoveCommand moveCommand;
        
        protected override void OnInit()
        {
            model = ModelObserver.GetModelObserver<IMoveModelObserver>();
            transform.position = model.HyperSpacePosition - Vector3.right * 1000f;
            HyperSpaceJump(model.HyperSpacePosition);
            model.OnTargetPositionChanged += UpdateTargetPosition;
            model.OnHyperSpaceJump += HyperSpaceJump;
        }

        protected override void OnRelease()
        {
            model.OnTargetPositionChanged -= UpdateTargetPosition;
            model.OnHyperSpaceJump -= HyperSpaceJump;

            FallDown();
        }

      
        protected override void OnCommandSet(ICommand command)
        {
            base.OnCommandSet(command);
            command.TryGetCommand(out moveCommand);
            moveCommand.Assign(transform);
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

            moveSequence.KillIfExist();

            var distance = Vector3.Distance(transform.position, position);
            float duration = distance / model.Speed;
            moveSequence = DOTween.Sequence();

            moveSequence.Append(
                transform.DORotate(
                        Quaternion.LookRotation(lookDirection).eulerAngles,
                        model.RotationDuration,
                        rotationMode)
                    .SetEase(lookAtEase));

            moveSequence.Append(transform.DOMove(position, duration)
                .SetEase(moveEase));
        }
    }
}