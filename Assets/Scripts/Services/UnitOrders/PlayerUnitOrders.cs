using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.PlayerCommands;
using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    public sealed class PlayerUnitOrders : IPlayerUnitOrders
    {
        private readonly IPlayerCommandSink _commands;
        private readonly ILocalPlayer _localPlayer;

        public PlayerUnitOrders(IPlayerCommandSink commands, ILocalPlayer localPlayer)
        {
            _commands = commands;
            _localPlayer = localPlayer;
        }

        public void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point) =>
            Submit(UnitActionId.Move, receivers, point);

        public void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point) =>
            Submit(UnitActionId.AttackMove, receivers, point);

        public void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target) =>
            Submit(UnitActionId.Attack, receivers, target: target);

        public void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId) =>
            Submit(UnitActionId.Attack, receivers, target: target, targetHardPointId: hardPointId);

        public void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly) =>
            Submit(UnitActionId.Guard, receivers, target: friendly);

        public void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints) =>
            Submit(UnitActionId.WaypointMove, receivers, waypoints: waypoints);

        public void IssueStop(IReadOnlyList<IEntity> receivers) => Submit(UnitActionId.Stop, receivers);

        public void IssueHunt(IReadOnlyList<IEntity> receivers) => Submit(UnitActionId.Hunt, receivers);

        public void IssueRetreat(IReadOnlyList<IEntity> receivers) => Submit(UnitActionId.Retreat, receivers);

        private void Submit(UnitActionId action, IReadOnlyList<IEntity> receivers, Vector3 point = default,
            IEntity target = null, int targetHardPointId = UnitOrderModel.NO_HARD_POINT,
            IReadOnlyList<Vector3> waypoints = null) =>
            _commands.Submit(UnitOrderCommand.Create(_localPlayer.Id, action, receivers, point, target,
                targetHardPointId, waypoints));
    }
}
