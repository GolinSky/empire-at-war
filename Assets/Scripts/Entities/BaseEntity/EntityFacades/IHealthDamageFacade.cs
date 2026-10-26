using System;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IHealthDamageFacade : IEntityFacade
    {
        event Action Damaged;
    }
}
