using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.UnitOrders;
using Mirror;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → issuing client: accepted order feedback, without executing the order again.</summary>
    public struct OrderFeedbackMessage : NetworkMessage
    {
        public UnitActionId Action;
        public PlayerId Issuer;
        public Vector3 Point;
        public long TargetId;
        public int TargetHardPointId;
        public Vector3[] Waypoints;

        public OrderFeedbackMessage(UnitOrder order)
        {
            Action = order.Action;
            Issuer = order.Issuer;
            Point = order.Point;
            TargetId = order.Target == null ? UnitOrderCommand.NO_TARGET : order.Target.Id;
            TargetHardPointId = order.TargetHardPointId;
            Waypoints = order.Waypoints == null ? null : new Vector3[order.Waypoints.Count];
            if (Waypoints != null)
                for (int i = 0; i < Waypoints.Length; i++) Waypoints[i] = order.Waypoints[i];
        }
    }
}
