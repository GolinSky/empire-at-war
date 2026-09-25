using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;

namespace EmpireAtWar.Entities.Ship.EntityCommands.Combat
{
    public class CombatModifiersCommand : ICombatModifiersCommand
    {
        public CombatModifiers Modifiers { get; }

        public CombatModifiersCommand(CombatModifiers modifiers)
        {
            Modifiers = modifiers;
        }
    }
}
