using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    public interface IPlayerOrderInputHandler
    {
        void FinishWaypoints();
        bool TryIssueMove(Vector3 worldPoint);
    }
}
