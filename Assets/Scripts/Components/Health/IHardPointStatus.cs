namespace EmpireAtWar.Models.Health
{
    public interface IHardPointStatus : IHardPointModel
    {
        float Health { get; }
        float MaxHealth { get; }
    }
}
