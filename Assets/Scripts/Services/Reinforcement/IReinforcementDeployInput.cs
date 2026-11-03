namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementDeployInput
    {
        /// <summary>Held when a reinforcement drag starts: deploy every unit of that kind that fits.</summary>
        bool IsDeployAllHeld { get; }
    }
}
