using EmpireAtWar.Components.Squadrons.Flight;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SFoilsFacade : ISFoilsFacade
    {
        public SFoilsModel Model { get; }
        public SFoilsFacade(SFoilsModel model) { Model = model; }
    }
}
