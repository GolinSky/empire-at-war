using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>A human's unit order as plain data: entity ids instead of references, so it can cross the network.</summary>
    public readonly struct UnitOrderCommand
    {
        public const long NO_TARGET = 0;

        public PlayerId Issuer { get; }
        public UnitActionId Action { get; }
        public long[] ReceiverIds { get; }
        public Vector3 Point { get; }
        public long TargetId { get; }
        public int TargetHardPointId { get; }
        public Vector3[] Waypoints { get; }

        public UnitOrderCommand(PlayerId issuer, UnitActionId action, long[] receiverIds, Vector3 point,
            long targetId = NO_TARGET, int targetHardPointId = UnitOrderModel.NO_HARD_POINT, Vector3[] waypoints = null)
        {
            Issuer = issuer;
            Action = action;
            ReceiverIds = receiverIds;
            Point = point;
            TargetId = targetId;
            TargetHardPointId = targetHardPointId;
            Waypoints = waypoints;
        }

        public static UnitOrderCommand Create(PlayerId issuer, UnitActionId action, IReadOnlyList<IEntity> receivers,
            Vector3 point = default, IEntity target = null, int targetHardPointId = UnitOrderModel.NO_HARD_POINT,
            IReadOnlyList<Vector3> waypoints = null)
        {
            long[] ids = new long[receivers.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = receivers[i].Id;
            Vector3[] route = null;
            if (waypoints != null)
            {
                route = new Vector3[waypoints.Count];
                for (int i = 0; i < route.Length; i++) route[i] = waypoints[i];
            }

            return new UnitOrderCommand(issuer, action, ids, point, target == null ? NO_TARGET : target.Id,
                targetHardPointId, route);
        }
    }
}
