namespace EmpireAtWar.Components.AttackComponent
{
    public enum WeaponRangeClass
    {
        Standard = 0,
        /// <summary>Shorter reach, more damage per second.</summary>
        Close = 1,
        /// <summary>Longer reach and better accuracy, less damage per second.</summary>
        Long = 2,
    }
}
