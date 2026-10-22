using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Selection;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public sealed class UnitHighlightFacade : IEntityHighlightFacade, IInitializable, ILateDisposable,
        IRespawnable, IDespawnable
    {
        private readonly ISelectionModelObserver _selection;
        private readonly IUnitRenderStateComponent _renderState;
        private UnitHighlight _highlight;

        public UnitHighlightFacade(ISelectionModelObserver selection, IUnitRenderStateComponent renderState)
        {
            _selection = selection;
            _renderState = renderState;
        }

        public void Initialize()
        {
            _selection.OnSelected += HandleSelected;
            HandleSelected(_selection.IsSelected);
        }

        public void SetHovered(bool isHovered)
        {
            SetFlag(UnitHighlight.Hovered, isHovered);
        }

        public void Respawn() => Reset();

        public void Despawn() => Reset();

        public void LateDispose()
        {
            _selection.OnSelected -= HandleSelected;
            Reset();
        }

        private void HandleSelected(bool isSelected) => SetFlag(UnitHighlight.Selected, isSelected);

        private void SetFlag(UnitHighlight flag, bool enabled)
        {
            _highlight = enabled ? _highlight | flag : _highlight & ~flag;
            _renderState.SetHighlight(_highlight);
        }

        private void Reset()
        {
            _highlight = UnitHighlight.None;
            _renderState.SetHighlight(_highlight);
        }
    }
}
