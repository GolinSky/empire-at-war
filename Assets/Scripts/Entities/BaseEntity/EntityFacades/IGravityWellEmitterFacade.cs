namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>Hull-mounted gravity well generator effects of a ship that carries the Interdict ability.</summary>
    public interface IGravityWellEmitterFacade : IEntityFacade
    {
        void SetEmitting(bool emitting);
    }
}
