using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyPurchaseProcessor
    {
        /// <summary>A station upgrade is building; another cannot be bought until it completes.</summary>
        bool IsUpgradingStation { get; }

        bool CanQueue(string requestId, int maxCount);

        void ReserveExternalPipeline(string requestId);
        void ReleaseExternalPipeline(string requestId);

        void Purchase(UnitRequest unitRequest);
    }
}
