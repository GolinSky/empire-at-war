using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface IStationHullFacade : IEntityFacade
    {
        float HullBottom { get; }
        float HullTop { get; }
    }
}
