using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Battle;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the player selects the unit that arrived last (the marked new unit).</summary>
    public sealed class NewestUnitSelectedHintCondition : IPolledHintCondition
    {
        private readonly HintTargets _targets;
        private readonly ISelectionService _selection;

        public bool IsMet { get; private set; }

        public NewestUnitSelectedHintCondition(HintTargets targets, ISelectionService selection)
        {
            _targets = targets;
            _selection = selection;
        }

        public void Poll(float deltaTime)
        {
            if (IsMet || !_targets.TryGetNewestUnit(out IEntity newest)) return;
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
                if (entity == newest)
                {
                    IsMet = true;
                    return;
                }
        }

        public void Dispose()
        {
        }
    }
}
