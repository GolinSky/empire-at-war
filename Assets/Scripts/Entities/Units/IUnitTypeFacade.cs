using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Entities.Units
{
    public interface IUnitTypeFacade : IEntityFacade
    {
        UnitTypeId UnitTypeId { get; }
    }
}
