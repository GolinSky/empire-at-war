using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>Cancel the current item of a production queue (by <c>UnitRequest.Id</c>) and refund it.</summary>
    public readonly struct CancelPurchaseCommand
    {
        public PlayerId Issuer { get; }
        public string RequestId { get; }

        public CancelPurchaseCommand(PlayerId issuer, string requestId)
        {
            Issuer = issuer;
            RequestId = requestId;
        }
    }
}
