using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IHardPointAttackFacade : IEntityFacade
    {
        void AttackHardPoint(IEntity target, int hardPointId, Vector3 formationOffset);
    }
}
