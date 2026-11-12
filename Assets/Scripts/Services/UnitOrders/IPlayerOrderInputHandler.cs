using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    public interface IPlayerOrderInputHandler
    {
        void FinishWaypoints();

        bool CanIssueAttackMove(Vector2 screen);

        bool TryIssueMove(Vector3 worldPoint);
    }
}
