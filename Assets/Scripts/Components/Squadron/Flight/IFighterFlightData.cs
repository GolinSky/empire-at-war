namespace EmpireAtWar.Components.Squadrons.Flight
{
    public interface IFighterFlightData
    {
        float CruiseSpeed { get; }
        float CombatSpeed { get; }
        float Acceleration { get; }
        float TurnRate { get; }
        float MaxBankAngle { get; }
        float BankResponse { get; }
        float Height { get; }
        float FormationSpacing { get; }
        float LoiterRadius { get; }
        float BreakDistance { get; }
        float ExtendDistance { get; }
    }
}
