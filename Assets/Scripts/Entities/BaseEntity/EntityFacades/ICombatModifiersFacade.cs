using EmpireAtWar.Components.Combat;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface ICombatModifiersFacade : IEntityFacade
    {
        CombatModifiers Modifiers { get; }
    }
}
