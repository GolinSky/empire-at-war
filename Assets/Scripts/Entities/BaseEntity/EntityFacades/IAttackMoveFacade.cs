using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IAttackMoveFacade : IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        float PaceSpeed { get; }
        /// <summary>Hull size order (<c>ShipClassExtensions.HullTier</c>); 0 for strikecraft.</summary>
        int HullTier { get; }
        /// <summary>Artillery or anti-strikecraft ship below BattleCruiser: never in the front row of a group.</summary>
        bool IsBackRow { get; }

        void AttackMoveTo(Vector3 worldPosition, AttackMoveEngagement engagement, PaceCluster pace = null,
            FormationRow row = FormationRow.Front);
    }
}
