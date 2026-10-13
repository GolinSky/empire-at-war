namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Whether the local player's fog of war currently hides the entity.</summary>
    public interface IFogVisibilityFacade : IEntityFacade
    {
        bool IsHiddenByFog { get; }
    }
}
