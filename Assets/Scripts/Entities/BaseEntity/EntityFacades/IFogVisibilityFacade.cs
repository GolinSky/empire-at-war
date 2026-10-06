namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Whether the local player's fog of war currently hides the entity. Bound only for entities outside the local team.</summary>
    public interface IFogVisibilityFacade : IEntityFacade
    {
        bool IsHiddenByFog { get; }
    }
}
