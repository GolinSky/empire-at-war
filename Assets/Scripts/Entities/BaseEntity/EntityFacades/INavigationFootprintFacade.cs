namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>
    /// Radius around the entity that ship navigation keeps other hulls out of.
    /// </summary>
    public interface INavigationFootprintFacade : IEntityFacade
    {
        float NavigationRadius { get; }
    }
}
