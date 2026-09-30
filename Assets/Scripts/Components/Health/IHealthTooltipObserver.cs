namespace EmpireAtWar.Models.Health
{
    public interface IHealthTooltipObserver
    {
        float MaxHull { get; }
        float MaxShields { get; }
        float ShieldRegeneration { get; }
        float ShieldRegenerationInterval { get; }
    }
}
