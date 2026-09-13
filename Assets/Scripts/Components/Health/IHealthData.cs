namespace EmpireAtWar.Models.Health
{
    public interface IHealthData
    {
        float Armor { get; }
        float Dexterity { get; }
        float Shields { get; }
        float ShieldRegenerateValue { get; }
        float ShieldRegenerateDelay { get; }
    }
}
