namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Instantly destroys the entity. Used by cheats.</summary>
    public interface IEntityDestroyFacade : IEntityFacade
    {
        void Destroy();
    }
}
