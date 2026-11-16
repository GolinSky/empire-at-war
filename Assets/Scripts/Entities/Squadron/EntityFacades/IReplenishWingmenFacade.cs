using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public interface IReplenishWingmenFacade : IEntityFacade
    {
        bool CanReplenishWingmen { get; }
        void ReplenishWingmen();
    }
}
