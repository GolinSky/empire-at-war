using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.Ship.EntityFacades.Combat
{
    public class CombatModifiersFacade : ICombatModifiersFacade
    {
        public CombatModifiers Modifiers { get; }

        public CombatModifiersFacade(CombatModifiers modifiers)
        {
            Modifiers = modifiers;
        }
    }
}
