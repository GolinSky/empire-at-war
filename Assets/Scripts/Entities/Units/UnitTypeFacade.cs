namespace EmpireAtWar.Entities.Units
{
    public sealed class UnitTypeFacade : IUnitTypeFacade
    {
        public UnitTypeId UnitTypeId { get; }

        public UnitTypeFacade(UnitTypeId unitTypeId)
        {
            UnitTypeId = unitTypeId;
        }
    }
}
