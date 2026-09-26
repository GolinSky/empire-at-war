using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>What ship behaviour (states, orders, AI) needs from movement.</summary>
    public interface IShipMovement
    {
        Vector3 CurrentPosition { get; }
        bool IsMoving { get; }
        bool IsBlocked { get; }
        float NavigationRadius { get; }
        void MoveToPosition(Vector3 targetPosition, bool preserveCourse = false);
        void LookAtTarget(Vector3 targetPosition);
        float GetRange(Vector3 targetPosition);
        void Stop();
    }
}
