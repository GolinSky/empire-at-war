namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>
    /// Icon the pointer shows for the order a world click would give right now.
    /// Target markers reuse it to show which action marked a target.
    /// </summary>
    public enum OrderCursor
    {
        None = 0,
        Attack = 1,
        Invalid = 2,
        Guard = 3,
        AttackMove = 4,
        Waypoint = 5,
        AbilityTarget = 6,
        SuperWeapon = 7
    }
}
