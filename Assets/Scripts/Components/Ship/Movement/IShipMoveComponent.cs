using EmpireAtWar.Components.Movement;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface IShipMoveComponent:IDefaultMoveComponent
    {
        float MoveAround();
        Vector3 CalculateLookDirection(Vector3 targetPosition);
        void MoveToPosition(Vector3 targetPosition);
        void MoveToPositionOnScreen(Vector2 targetPosition);
        void LookAtTarget(Vector3 targetPosition);
    }
}