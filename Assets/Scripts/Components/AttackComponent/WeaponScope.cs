namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>Who may mount a weapon: every unit, one faction's units, or one unit type.</summary>
    public enum WeaponScope
    {
        Shared = 0,
        Faction = 1,
        Unit = 2,
    }
}
