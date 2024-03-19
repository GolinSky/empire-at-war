using UnityEngine;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface IShipMoveComponent:ISimpleMoveComponent
    {
        float MoveAround();
        Vector3 CalculateLookDirection(Vector3 targetPosition);
        void MoveToPosition(Vector3 targetPosition);
        void LookAtTarget(Vector3 targetPosition);
    }
}