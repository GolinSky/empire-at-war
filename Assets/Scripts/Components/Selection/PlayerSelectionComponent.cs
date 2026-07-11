using EmpireAtWar.Commands;
using EmpireAtWar.Entities.Ship.Mediator;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Mvc;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface ISelectionComponent: IComponent, IUnitComponent
    {
        void SetActive(bool isActive);
    }
    
    public class PlayerSelectionComponent : BaseComponent<SelectionModel>, ISelectionCommand, ISelectionComponent
    {
        private readonly ISelectionService _selectionService;
        private IUnitMediator _mediator;

        public IModelObserver ModelObserver { get; }

        
        [Inject]
        public PlayerType PlayerType { get; }
        
        
        public PlayerSelectionComponent(IModel model, ISelectionService selectionService) : base(model)
        {
            _selectionService = selectionService;
            ModelObserver = model;
        }

        public void OnSelected(SelectionType selectionType)
        {
           // _selectionService.UpdateSelectable(this, selectionType);
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
            //_selectionService.UpdateSelectable(this, selectionType);
        }

        public void SetActive(bool isActive)
        {
            Model.IsSelected = isActive;
            _mediator?.OnSelect(isActive);
        }

        public void SetMediator(IUnitMediator mediator)
        {
            _mediator = mediator;
        }
    }
}