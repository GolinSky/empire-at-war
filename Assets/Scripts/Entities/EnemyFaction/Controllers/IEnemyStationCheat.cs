namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
    /// <summary>Cheat access to an AI player's station level.</summary>
    public interface IEnemyStationCheat
    {
        /// <summary>Raises the station one level for free; false at the maximum level or without a production base.</summary>
        bool ForceUpgradeStation();
    }
}
