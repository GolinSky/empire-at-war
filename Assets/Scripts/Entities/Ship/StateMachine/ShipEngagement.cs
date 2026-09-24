using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Entities.Ship.StateMachine
{
    public static class ShipEngagement
    {
        public static bool CanEngage(IEntity enemy, IShipMoveComponent movement,
            IWeaponComponent weapon)
        {
            return !enemy.HealthModel.IsDestroyed && enemy.HealthModel.HasUnits &&
                   weapon.HasEnoughRange(movement.GetRange(
                       enemy.HealthModel.Transform.position));
        }

        /// <summary>
        /// Re-paths towards a moving target once it drifts from the last pursuit
        /// destination, using the same threshold as AttackTargetState.
        /// Returns the pursuit destination currently in use.
        /// </summary>
        public static Vector3 Pursue(IShipMoveComponent movement, IWeaponComponent weapon,
            Vector3 target, Vector3 pursuitDestination)
        {
            float updateDistance = Mathf.Max(movement.NavigationRadius,
                weapon.AttackDistance * 0.1f);
            if (movement.IsMoving &&
                (target - pursuitDestination).sqrMagnitude < updateDistance * updateDistance)
                return pursuitDestination;
            movement.MoveToPosition(target, preserveCourse: true);
            return target;
        }
    }
}
