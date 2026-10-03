using UnityEngine;

namespace EmpireAtWar.Entities.UnitOrderFeedback
{
    public interface IUnitOrderFeedbackUi
    {
        void Initialize();

        void Dispose();

        void SetPresenter(IUnitOrderFeedbackPresenter presenter);

        void PlayAttack(Vector2 screenPosition);

        void PlayMovement(Vector2 screenPosition);

        void SetAttackPosition(Vector2 screenPosition);

        void SetMovementPosition(Vector2 screenPosition);

        void StopAttack();
    }
}
