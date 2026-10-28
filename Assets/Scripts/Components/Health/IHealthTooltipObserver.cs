namespace EmpireAtWar.Models.Health
{
    public interface IHealthTooltipObserver
    {
        bool IsShieldRecovering { get; }
        float ShieldRegeneration { get; }
        float ShieldRegenerationInterval { get; }
    }
}
