using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.SpaceStation
{
    public interface IStationDurabilityFacade : IEntityFacade
    {
        float MaximumDurability { get; }
    }
}
