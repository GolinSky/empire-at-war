namespace EmpireAtWar.Components.AttackComponent
{
    public enum AttackType
    {
        Base = 0,
        MainTarget = 1,
        /// <summary>A main target from a direct attack order: every gun may fire at it, strikecraft included.</summary>
        OrderedTarget = 2,
    }
}