using DG.Tweening;
using EmpireAtWar.Mvc;
using UnityEngine;
using Utilities.ScriptUtils.Dotween;
using Zenject;

namespace EmpireAtWar.Components.Movement
{
    public class SimpleMoveComponent : MonoComponent<DefaultMoveModel>, IComponent, IInitializable,
        ILateDisposable
    {
        private Sequence _moveSequence;

        [Inject]
        private void Construct(DefaultMoveModel model)
        {
            SetModel(model);
        }

        public void Initialize()
        {
            transform.position = Model.StartPosition;
        }

        public void LateDispose()
        {
            Release();
        }

        public override void Release()
        {
            Vector3 point = transform.position - Model.FallDownDirection;

            _moveSequence.KillIfExist();
            _moveSequence = DOTween.Sequence();
            _moveSequence.Append(transform.DOMove(point, Model.FallDownDuration));
            _moveSequence.Join(transform.DOLocalRotate(Model.FallDownRotation.Value, Model.FallDownDuration));
        }
    }
}
