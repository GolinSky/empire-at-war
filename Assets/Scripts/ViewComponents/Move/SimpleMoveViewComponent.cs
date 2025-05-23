using DG.Tweening;
using EmpireAtWar.Components.Movement;
using UnityEngine;
using Utilities.ScriptUtils.Dotween;

namespace EmpireAtWar.ViewComponents.Move
{
    public class SimpleMoveViewComponent:ViewComponent<IDefaultMoveModelObserver>
    {
        private Sequence _moveSequence;

        protected override void OnInit()
        {
            base.OnInit();
            transform.position = Model.StartPosition;
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            FallDown();
        }

        private void FallDown()
        {
            Vector3 point = transform.position - Model.FallDownDirection;

            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            _moveSequence.Append(transform.DOMove(point, Model.FallDownDuration));
            _moveSequence.Join(transform.DOLocalRotate(
                Model.FallDownRotation.Value,
                Model.FallDownDuration));
        }
        
    }
}