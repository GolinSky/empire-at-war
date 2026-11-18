using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IHardPointAttackFacade : IEntityFacade
    {
        /// <param name="pace">Shared by the moving ships of one group order; units that do not pace ignore it.</param>
        void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset, GroupPace pace = null);
    }
}
