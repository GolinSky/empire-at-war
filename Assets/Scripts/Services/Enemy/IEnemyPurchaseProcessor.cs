using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyPurchaseProcessor
    {
        /// <summary>A station upgrade is building; another cannot be bought until it completes.</summary>
        bool IsUpgradingStation { get; }
        /// <summary>A second-station upgrade is building; the next one waits until it completes.</summary>
        bool IsUpgradingSecondStation => false;
        bool HasActiveProduction => false;
        bool PriorityConstructionPending { get; set; }

        bool CanQueue(string requestId, int maxCount);
        float GetQueueRemainingTime(string requestId);

        /// <summary>
        /// A paid build of this ID waits behind the one in production. Builds of one ID run one at a time, so another
        /// purchase would only hold money and unit capacity until the queue reaches it.
        /// </summary>
        bool HasWaitingBuild(string requestId) => false;

        void ReserveExternalPipeline(string requestId);
        void ReleaseExternalPipeline(string requestId);

        /// <summary>A station-bought platform is queued or under on-site construction (W50: one at a time).</summary>
        bool HasPlatformInProgress => false;

        void Purchase(UnitRequest unitRequest);

        /// <summary>
        /// Buys a platform; when its build completes, placement uses these purchase-time flags (see
        /// <see cref="IEnemyStructurePlacementService.TryGetDefensePosition(bool, bool, bool, out UnityEngine.Vector3)"/>).
        /// </summary>
        void PurchaseDefense(UnitRequest unitRequest, bool allowExtra, bool firstOnly) => Purchase(unitRequest);
    }
}
