namespace EmpireAtWar.Models.Health
{
    public interface IHealthTooltipObserver
    {
        float ShieldRegeneration { get; }
        float ShieldRegenerationInterval { get; }
    }
}
