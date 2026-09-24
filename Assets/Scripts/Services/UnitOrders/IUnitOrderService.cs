using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using UnityEngine;

namespace EmpireAtWar.Services.UnitOrders
{
    public interface IUnitOrderService
    {
        event Action<UnitOrder> OrderIssued;
        void IssueMove(IReadOnlyList<IEntity> receivers, Vector3 point);
        void IssueMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> destinations);
        void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target);
        void IssueAttack(IReadOnlyList<IEntity> receivers, IEntity target, IReadOnlyList<Vector3> offsets);
        void IssueAttackMove(IReadOnlyList<IEntity> receivers, Vector3 point);
        void IssueStop(IReadOnlyList<IEntity> receivers);
        void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly);
        void IssueGuard(IReadOnlyList<IEntity> receivers, IEntity friendly, IReadOnlyList<Vector3> offsets);
        void IssueWaypointMove(IReadOnlyList<IEntity> receivers, IReadOnlyList<Vector3> waypoints);
        void IssueHunt(IReadOnlyList<IEntity> receivers);
        void IssueRetreat(IReadOnlyList<IEntity> receivers);
        void CancelRetreat(IReadOnlyList<IEntity> receivers);
    }

}
