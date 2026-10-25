namespace EmpireAtWar.Components.Combat
{
    /// <summary>One application of a stat modifier; removing it removes exactly this application.</summary>
    public sealed class CombatModifierHandle
    {
        public CombatStatModifier Modifier { get; }

        internal CombatModifierHandle(CombatStatModifier modifier)
        {
            Modifier = modifier;
        }
    }
}
