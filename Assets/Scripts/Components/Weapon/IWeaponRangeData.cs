namespace EmpireAtWar.Components.Weapon
{
    /// <summary>How far the unit's hardpoints can fire. Independent of radar, which only detects.</summary>
    public interface IWeaponRangeData
    {
        float WeaponRange { get; }
    }
}
