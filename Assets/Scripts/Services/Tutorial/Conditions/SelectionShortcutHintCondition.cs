using EmpireAtWar.Services.Battle;

namespace EmpireAtWar.Services.Tutorial
{
    /// <summary>Met when the player uses select-visible or select-all.</summary>
    public sealed class SelectionShortcutHintCondition : IHintCondition
    {
        private readonly ISelectionInput _input;
        private readonly bool _isSelectAll;

        public bool IsMet { get; private set; }

        public SelectionShortcutHintCondition(ISelectionInput input, bool isSelectAll)
        {
            _input = input;
            _isSelectAll = isSelectAll;
            if (_isSelectAll) _input.SelectAllRequested += HandleUsed;
            else _input.SelectVisibleRequested += HandleUsed;
        }

        private void HandleUsed() => IsMet = true;

        public void Dispose()
        {
            if (_isSelectAll) _input.SelectAllRequested -= HandleUsed;
            else _input.SelectVisibleRequested -= HandleUsed;
        }
    }
}
