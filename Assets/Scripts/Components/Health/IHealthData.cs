namespace EmpireAtWar.Models.Health
{
    public interface IHealthData
    {
        bool HasShields { get; }
        float Dexterity { get; }
        float Shields { get; }
        
    }
}