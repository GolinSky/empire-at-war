namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IUnitCombatObserverFacade : IEntityFacade
    {
        float WeaponRange { get; }
        float RadarRange { get; }
        IEntity Target { get; }
        bool IsAttacking(IEntity target);
    }
}
