namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>Remaining active time for saving an ability whose completion is not driven by slot duration.</summary>
    public interface IShipAbilityRemainingTime : IShipAbility
    {
        float RemainingTime { get; }
    }
}
