using EmpireAtWar.Components.Combat;

namespace EmpireAtWar.Entities.BaseEntity.EntityCommands
{
    public interface ICombatModifiersCommand : IEntityCommand
    {
        CombatModifiers Modifiers { get; }
    }
}
