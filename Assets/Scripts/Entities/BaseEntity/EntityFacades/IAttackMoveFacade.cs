using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IAttackMoveFacade : IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        void AttackMoveTo(Vector3 worldPosition, AttackMoveEngagement engagement);
    }
}
