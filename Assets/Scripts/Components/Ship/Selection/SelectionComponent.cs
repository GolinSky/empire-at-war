using EmpireAtWar.Commands;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Command;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class SelectionComponent : BaseComponent<SelectionModel>, ISelectionCommand, ISelectable
    {
        private readonly INavigationService navigationService;
        public IMovable Movable { get; }

        public SelectionComponent(IModel model, INavigationService navigationService, IMovable movable) : base(model)
        {
            this.navigationService = navigationService;
            Movable = movable;
        }
        

        public void OnSelected(SelectionType selectionType)
        {
            navigationService.UpdateSelectable(this, selectionType);
        }

        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            command = default;
            return false;
        }

        public void SetActive(bool isActive)
        {
            Model.IsSelected = isActive;
        }
    }

    public class SelectionFacade : PlaceholderFactory<IModel,IMovable, SelectionComponent>
    {
        private readonly INavigationService navigationService;

        public SelectionFacade(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }
        
        public override SelectionComponent Create(IModel model, IMovable movable)
        {
            return new SelectionComponent(model,navigationService, movable);
        }
    }
}