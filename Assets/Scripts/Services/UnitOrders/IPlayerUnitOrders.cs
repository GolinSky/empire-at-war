using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>Unit orders from the human at this machine; each becomes a <c>UnitOrderCommand</c>.</summary>
    public interface IPlayerUnitOrders
    {
        void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point);
        void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point);
        void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target);
        void IssueHardPointAttack(IReadOnlyList<IEntity> receivers, IEntity target, int hardPointId);
        void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly);
        void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints);
        void IssueStop(IReadOnlyList<IEntity> receivers);
        void IssueHunt(IReadOnlyList<IEntity> receivers);
        void IssueRetreat(IReadOnlyList<IEntity> receivers);
    }
}
