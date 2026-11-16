namespace EmpireAtWar.Services.ShipAbilities
{
    public interface IShipAbilityInterruptionPolicy : IShipAbility
    {
        bool SurvivesCasterDeath { get; }
        bool SurvivesHazardSuppression { get; }
    }
}
