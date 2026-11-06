namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IPhasedShipAbility : IStoppableShipAbility
    {
        bool IsComplete { get; }
        bool SurvivesCasterDeath { get; }
        void Advance(float deltaTime);
    }
}
