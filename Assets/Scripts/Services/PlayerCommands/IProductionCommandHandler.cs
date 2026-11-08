namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>One human's production queue on the authority.</summary>
    public interface IProductionCommandHandler
    {
        void Purchase(PurchaseUnitCommand command);
        void Cancel(CancelPurchaseCommand command);
    }
}
