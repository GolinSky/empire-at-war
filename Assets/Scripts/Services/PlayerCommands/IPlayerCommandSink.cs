namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>
    /// Where a human's actions go. Offline (and on the online host) commands are validated and run at once;
    /// an online client will send them to the host instead. AI players keep calling services directly.
    /// </summary>
    public interface IPlayerCommandSink
    {
        void Submit(UnitOrderCommand command);
        void Submit(PurchaseUnitCommand command);
        void Submit(CancelPurchaseCommand command);
        void Submit(BuildSiteFacilityCommand command);
        void Submit(FireSuperWeaponCommand command);
        void Submit(ShipAbilityCommand command);
        void Submit(DeployReinforcementCommand command);
        void Submit(PlaceMarkerCommand command);
    }
}
