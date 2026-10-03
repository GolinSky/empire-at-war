using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.UnitActions;
using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    public readonly struct UnitOrder
    {
        public UnitActionId Action { get; }
        public PlayerId Issuer { get; }
        public Vector3 Point { get; }
        public IEntity Target { get; }
        public IReadOnlyList<Vector3> Waypoints { get; }
        public int TargetHardPointId { get; }

        public UnitOrder(UnitActionId action, PlayerId issuer, Vector3 point,
            IEntity target = null, IReadOnlyList<Vector3> waypoints = null,
            int targetHardPointId = UnitOrderModel.NO_HARD_POINT)
        {
            Action = action;
            Issuer = issuer;
            Point = point;
            Target = target;
            Waypoints = waypoints;
            TargetHardPointId = targetHardPointId;
        }
    }
}
