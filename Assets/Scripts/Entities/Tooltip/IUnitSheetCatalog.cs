using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Entities.Tooltip
{
    public interface IUnitSheetCatalog
    {
        UnitSheet Get(UnitTypeId unitType);
    }
}
