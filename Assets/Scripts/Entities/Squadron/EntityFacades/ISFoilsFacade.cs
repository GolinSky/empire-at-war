using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public interface ISFoilsFacade : IEntityFacade
    {
        SFoilsModel Model { get; }
    }
}
