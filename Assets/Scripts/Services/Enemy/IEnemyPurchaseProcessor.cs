using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyPurchaseProcessor
    {
        /// <summary>A station upgrade is building; another cannot be bought until it completes.</summary>
        bool IsUpgradingStation { get; }

        void Purchase(UnitRequest unitRequest);
    }
}
