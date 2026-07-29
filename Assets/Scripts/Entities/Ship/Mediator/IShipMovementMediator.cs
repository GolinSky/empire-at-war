using UnityEngine;

namespace EmpireAtWar.Entities.Ship.Mediator
{
    public interface IShipMovementMediator
    {
        void OnPositionChanged(Vector3 position);
        void OnLookAtTarget(Vector3 targetPosition);
        void OnStopped();
    }
}
