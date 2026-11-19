using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    /// <summary>What ship behaviour (states, orders, AI) needs from movement.</summary>
    public interface IShipMovement
    {
        Vector3 CurrentPosition { get; }
        bool IsMoving { get; }
        bool IsArriving { get; }
        float NavigationRadius { get; }
        float CurrentSpeed { get; }
        /// <summary>Own uncapped top speed for group pacing; PositiveInfinity while the ship cannot move.</summary>
        float PaceSpeed { get; }

        void MoveToPosition(Vector3 targetPosition, bool preserveCourse = false);

        void LookAtTarget(IEntity target);
        void LookInDirection(Vector3 direction);

        float GetRange(Vector3 targetPosition);

        void Stop();

        // Combat holds allow budgeted spacing; explicit Stop never does.
        void HoldPosition(IEntity target);
        void CancelCombatHold();

        /// <summary>Limits top speed to a group pace; PositiveInfinity removes the cap.</summary>
        void SetSpeedCap(float cap);
    }
}
