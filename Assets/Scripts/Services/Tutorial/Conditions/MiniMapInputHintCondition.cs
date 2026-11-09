using EmpireAtWar.Entities.MiniMap;

namespace EmpireAtWar.Services.Tutorial
{
    public sealed class MiniMapInputHintCondition : IHintCondition
    {
        private readonly IMiniMapInputObserver _miniMap;
        private readonly bool _isOrder;

        public bool IsMet { get; private set; }

        /// <param name="isOrder">True waits for a minimap move order; false for a minimap camera jump.</param>
        public MiniMapInputHintCondition(IMiniMapInputObserver miniMap, bool isOrder)
        {
            _miniMap = miniMap;
            _isOrder = isOrder;
            if (_isOrder) _miniMap.MoveOrdered += HandleInput;
            else _miniMap.CameraMoved += HandleInput;
        }

        private void HandleInput() => IsMet = true;

        public void Dispose()
        {
            if (_isOrder) _miniMap.MoveOrdered -= HandleInput;
            else _miniMap.CameraMoved -= HandleInput;
        }
    }
}
