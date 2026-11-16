namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IAdvancingShipAbility : ICompletableShipAbility
    {
        void Advance(float deltaTime);
    }
}
