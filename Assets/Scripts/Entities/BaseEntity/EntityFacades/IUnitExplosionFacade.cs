using EmpireAtWar.Services.UnitExplosion;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Plays an explosion sized to the entity's current hull, without destroying it.</summary>
    public interface IUnitExplosionFacade : IEntityFacade
    {
        void PlayExplosion(UnitExplosionKind kind);
    }
}
