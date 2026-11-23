namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Plays the entity's own destruction explosion, sized to its current hull, without destroying it.</summary>
    public interface IUnitExplosionFacade : IEntityFacade
    {
        void PlayExplosion();
    }
}
