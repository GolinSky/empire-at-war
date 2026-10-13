using System;

namespace EmpireAtWar.Entities.BaseEntity
{
    public interface IPooledEntityEvents
    {
        event Action Respawned;
        event Action Despawned;
    }
}
