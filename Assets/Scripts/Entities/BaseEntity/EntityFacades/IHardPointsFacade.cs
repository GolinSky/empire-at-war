using System.Collections.Generic;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IHardPointsFacade : IEntityFacade
    {
        IReadOnlyList<IHardPointStatus> HardPoints { get; }
        float MaxShields { get; }
    }
}
