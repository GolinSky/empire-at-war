using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the player selects at least a number of units of one selection type (ships, station).</summary>
    public sealed class SelectionHintCondition : IHintCondition, IObserver<ISelectionSubject>
    {
        private readonly ISelectionService _selection;
        private readonly SelectionType _type;
        private readonly int _minimumCount;

        public bool IsMet { get; private set; }

        public SelectionHintCondition(ISelectionService selection, SelectionType type, int minimumCount)
        {
            _selection = selection;
            _type = type;
            _minimumCount = minimumCount;
            _selection.AddObserver(this);
        }

        public void UpdateState(ISelectionSubject value)
        {
            if (value.PlayerSelectionContext.SelectionType == _type &&
                value.PlayerSelectionContext.Count >= _minimumCount)
                IsMet = true;
        }

        public void Dispose() => _selection.RemoveObserver(this);
    }
}
