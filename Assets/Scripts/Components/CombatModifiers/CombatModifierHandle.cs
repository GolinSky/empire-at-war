namespace EmpireAtWar.Components.Combat
{
    /// <summary>One application of a stat modifier; removing it removes exactly this application.</summary>
    public sealed class CombatModifierHandle
    {
        public CombatStatModifier Modifier { get; }
        /// <summary>Applications sharing a non-null key do not stack; only the oldest active one counts.</summary>
        public object StackKey { get; }

        internal CombatModifierHandle(CombatStatModifier modifier, object stackKey)
        {
            Modifier = modifier;
            StackKey = stackKey;
        }
    }
}
