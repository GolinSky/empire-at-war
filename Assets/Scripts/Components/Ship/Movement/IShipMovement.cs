using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>What ship behaviour (states, orders, AI) needs from movement.</summary>
    public interface IShipMovement
    {
        Vector3 CurrentPosition { get; }
        bool IsMoving { get; }
        float NavigationRadius { get; }

        void MoveToPosition(Vector3 targetPosition, bool preserveCourse = false);

        void LookAtTarget(IEntity target);
        void LookInDirection(Vector3 direction);

        float GetRange(Vector3 targetPosition);

        void Stop();
    }
}
