namespace EmpireAtWar.Models.Health
{
    public interface IHealthTooltipObserver
    {
        float Hull { get; }
        float MaxHull { get; }
        float Shields { get; }
        float MaxShields { get; }
        bool IsShieldRecovering { get; }
        float ShieldRegeneration { get; }
        float ShieldRegenerationInterval { get; }
    }
}
