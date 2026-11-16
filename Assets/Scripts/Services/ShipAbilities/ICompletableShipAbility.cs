namespace EmpireAtWar.Services.ShipAbilities
{
    public interface ICompletableShipAbility : IStoppableShipAbility
    {
        bool IsComplete { get; }
    }
}
