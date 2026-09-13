namespace EmpireAtWar.Models.Health
{
    public interface IHealthState
    {
        bool HasShields { get; }
        float Dexterity { get; }
        float Shields { get; }
    }
}
