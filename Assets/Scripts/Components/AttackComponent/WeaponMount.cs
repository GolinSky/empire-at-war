namespace EmpireAtWar.Components.AttackComponent
{
    /// <summary>Barrels per hardpoint. The value is the barrel count; ship weapon damage per barrel comes from <see cref="WeaponMountRule"/>.</summary>
    public enum WeaponMount
    {
        Single = 1,
        Dual = 2,
        Triple = 3,
        Quad = 4,
        Octuple = 8,
    }
}
