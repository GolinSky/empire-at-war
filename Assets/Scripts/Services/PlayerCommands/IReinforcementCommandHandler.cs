namespace EmpireAtWar.Services.PlayerCommands
{
    /// <summary>One player's reinforcement reserve on the authority.</summary>
    public interface IReinforcementCommandHandler
    {
        void Deploy(DeployReinforcementCommand command);
    }
}
