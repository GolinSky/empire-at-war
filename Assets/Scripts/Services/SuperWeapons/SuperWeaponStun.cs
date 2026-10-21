using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>A running disable effect on one entity's combat modifiers.</summary>
    public sealed class SuperWeaponStun
    {
        public IEntity Target { get; }
        public CombatModifiers Modifiers { get; }
        public CombatStatModifier Modifier { get; }
        public float TimeLeft { get; set; }

        public SuperWeaponStun(IEntity target, CombatModifiers modifiers, CombatStatModifier modifier, float timeLeft)
        {
            Target = target;
            Modifiers = modifiers;
            Modifier = modifier;
            TimeLeft = timeLeft;
        }
    }
}
