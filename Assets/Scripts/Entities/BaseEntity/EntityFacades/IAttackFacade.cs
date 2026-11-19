using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IAttackFacade : IEntityFacade
    {
        Vector3 WorldPosition { get; }
        float NavigationRadius { get; }
        float PaceSpeed { get; }

        /// <param name="pace">Shared by the moving units of one spatial cluster; units that do not pace ignore it.</param>
        void Attack(IEntity target, Vector3 formationOffset, PaceCluster pace = null);
    }
}
