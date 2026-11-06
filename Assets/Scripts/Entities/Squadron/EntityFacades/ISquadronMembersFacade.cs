using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Health;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public interface ISquadronMembersFacade : IEntityFacade
    {
        IReadOnlyList<IHardPointModel> Members { get; }
    }
}
