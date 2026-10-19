using System;
using System.Linq;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Services.Saves
{
    /// <summary>
    /// The unit's current order; a load issues it again through the unit's order facades.
    /// Waypoint orders keep only the waypoints not yet reached. An ability approach is not saved.
    /// </summary>
    [Serializable]
    public sealed class UnitOrderSave
    {
        [SerializeField] private UnitOrderType type;
        [SerializeField] private Vector3 destination;
        [SerializeField] private Vector3 offset;
        [SerializeField] private bool hasTarget;
        [SerializeField] private long targetId;
        [SerializeField] private int targetHardPointId = UnitOrderModel.NO_HARD_POINT;
        [SerializeField] private Vector3[] waypoints = Array.Empty<Vector3>();

        public UnitOrderType Type => type;

        public static UnitOrderSave From(UnitOrderModel order)
        {
            if (order.Current == UnitOrderType.UseAbility) return new UnitOrderSave { type = UnitOrderType.None };
            return new UnitOrderSave
            {
                type = order.Current,
                destination = ToVector(order.Destination),
                offset = ToVector(order.Offset),
                hasTarget = order.Target != null,
                targetId = order.Target != null ? order.Target.Id : 0,
                targetHardPointId = order.TargetHardPointId,
                waypoints = order.Waypoints.Skip(order.WaypointIndex).Select(ToVector).ToArray()
            };
        }

        /// <summary>Re-issues the order; an order whose target was not restored leaves the unit idle.</summary>
        public void Apply(IEntity unit, IEntityLocator units)
        {
            IEntity target = null;
            if (hasTarget && !units.TryGetEntity(targetId, out target)) return;
            switch (type)
            {
                case UnitOrderType.Move:
                    unit.GetFacade<IMoveFacade>().MoveTo(destination);
                    break;
                case UnitOrderType.Attack when targetHardPointId != UnitOrderModel.NO_HARD_POINT:
                    unit.GetFacade<IHardPointAttackFacade>().AttackHardPoint(target, targetHardPointId, offset);
                    break;
                case UnitOrderType.Attack:
                    unit.GetFacade<IAttackFacade>().Attack(target, offset);
                    break;
                case UnitOrderType.AttackMove:
                    // Each unit gets its own engagement: the group that shared one is not saved.
                    unit.GetFacade<IAttackMoveFacade>().AttackMoveTo(destination, new AttackMoveEngagement());
                    break;
                case UnitOrderType.Guard:
                    unit.GetFacade<IGuardFacade>().Guard(target, offset);
                    break;
                case UnitOrderType.WaypointMove:
                    unit.GetFacade<IWaypointMoveFacade>().MoveAlong(waypoints);
                    break;
                case UnitOrderType.Hunt:
                    unit.GetFacade<IHuntFacade>().Hunt();
                    break;
                case UnitOrderType.Retreat:
                    unit.GetFacade<IRetreatFacade>().Retreat(destination);
                    break;
            }
        }

        private static Vector3 ToVector(FormationPoint point) => new Vector3(point.X, 0f, point.Z);
    }
}
