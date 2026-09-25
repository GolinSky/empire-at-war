namespace EmpireAtWar.Entities.EnemyFaction.Controllers
{
    public interface IEnemyReinforcementObserver
    {
        bool HasPendingReinforcement { get; }
    }
}
