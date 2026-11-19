using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IAttackMoveFacade : IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        float PaceSpeed { get; }

        void AttackMoveTo(Vector3 worldPosition, AttackMoveEngagement engagement, PaceCluster pace = null);
    }
}
