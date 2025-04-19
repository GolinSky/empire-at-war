using EmpireAtWar.Commands;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class PlayerSelectionComponent : BaseComponent<SelectionModel>, ISelectionCommand, ISelectable
    {
        private readonly ISelectionService _selectionService;

        public IModelObserver ModelObserver { get; }
        public IMovable Movable { get; set; }

        public PlayerSelectionComponent(IModel model, ISelectionService selectionService, IMovable movable) : base(model)
        {
            _selectionService = selectionService;
            Movable = movable;
            ModelObserver = model;
        }

        public void OnSelected(SelectionType selectionType)
        {
            _selectionService.UpdateSelectable(this, selectionType);
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
            _selectionService.UpdateSelectable(this, selectionType);
        }

        public void SetActive(bool isActive)
        {
            Model.IsSelected = isActive;
        }
    }
}