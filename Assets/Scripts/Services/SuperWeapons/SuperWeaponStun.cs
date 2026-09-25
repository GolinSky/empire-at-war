using EmpireAtWar.Components.Combat;

namespace EmpireAtWar.Services.SuperWeapons
{
    /// <summary>A running disable effect on one entity's combat modifiers.</summary>
    public sealed class SuperWeaponStun
    {
        public CombatModifiers Modifiers { get; }
        public CombatStatModifier Modifier { get; }
        public float TimeLeft { get; set; }

        public SuperWeaponStun(CombatModifiers modifiers, CombatStatModifier modifier, float timeLeft)
        {
            Modifiers = modifiers;
            Modifier = modifier;
            TimeLeft = timeLeft;
        }
    }
}
