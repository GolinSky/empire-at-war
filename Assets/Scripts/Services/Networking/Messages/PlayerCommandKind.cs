namespace EmpireAtWar.Services.Networking.Messages
{
    public enum PlayerCommandKind : byte
    {
        UnitOrder,
        Purchase,
        CancelPurchase,
        BuildSiteFacility,
        FireSuperWeapon,
        ShipAbility,
        DeployReinforcement
    }
}
