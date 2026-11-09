using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Selection;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class ShipSelectedHintCondition : IHintCondition, IObserver<ISelectionSubject>
    {
        private readonly ISelectionService _selection;
        private readonly int _minimumCount;

        public bool IsMet { get; private set; }

        public ShipSelectedHintCondition(ISelectionService selection, int minimumCount)
        {
            _selection = selection;
            _minimumCount = minimumCount;
            _selection.AddObserver(this);
        }

        public void UpdateState(ISelectionSubject value)
        {
            if (value.PlayerSelectionContext.SelectionType == SelectionType.Ship &&
                value.PlayerSelectionContext.Count >= _minimumCount)
                IsMet = true;
        }

        public void Dispose() => _selection.RemoveObserver(this);
    }
}
