using EmpireAtWar.Entities.Faction.Ui;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the player cancels an item in the build queue (credits come back).</summary>
    public sealed class UnitCancelledHintCondition : IHintCondition
    {
        private readonly IBuildQueueCancelObserver _queue;

        public bool IsMet { get; private set; }

        public UnitCancelledHintCondition(IBuildQueueCancelObserver queue)
        {
            _queue = queue;
            _queue.Canceled += HandleCanceled;
        }

        private void HandleCanceled() => IsMet = true;

        public void Dispose() => _queue.Canceled -= HandleCanceled;
    }
}
