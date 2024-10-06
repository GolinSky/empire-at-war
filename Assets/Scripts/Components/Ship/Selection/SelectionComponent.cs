using EmpireAtWar.Commands;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class SelectionComponent : BaseComponent<SelectionModel>, ISelectionCommand, ISelectable
    {
        private readonly INavigationService navigationService;

        public IModelObserver ModelObserver { get; }
        public IMovable Movable { get; set; }

        public SelectionComponent(IModel model, INavigationService navigationService, IMovable movable) : base(model)
        {
            this.navigationService = navigationService;
            Movable = movable;
            ModelObserver = model;
        }

        public void OnSelected(SelectionType selectionType)
        {
            navigationService.UpdateSelectable(this, selectionType);
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
            navigationService.RemoveSelectable(this);
        }

        public void SetActive(bool isActive)
        {
            Model.IsSelected = isActive;
        }
    }
}