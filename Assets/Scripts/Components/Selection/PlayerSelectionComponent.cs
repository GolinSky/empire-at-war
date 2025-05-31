using EmpireAtWar.Commands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Components.Components;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public interface ISelectionComponent: IComponent
    {
        void SetActive(bool isActive);
    }
    
    public class PlayerSelectionComponent : BaseComponent<SelectionModel>, ISelectionCommand, ISelectionComponent
    {
        private readonly ISelectionService _selectionService;

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
        }

    }
}