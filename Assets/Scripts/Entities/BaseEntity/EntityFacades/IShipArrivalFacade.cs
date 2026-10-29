namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IShipArrivalFacade : IEntityFacade
    {
        bool IsArriving { get; }
    }
}
