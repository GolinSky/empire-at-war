using EmpireAtWar.Commands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    
    public class EnemySelectionComponent:BaseComponent<SelectionModel>, ISelectionCommand, ISelectionComponent
    {
        private readonly ISelectionService _selectionService;
      
        public IModelObserver ModelObserver { get; }
        
        [Inject]
        public PlayerType PlayerType { get; }


        public EnemySelectionComponent(IModel model, SelectionService selectionService) : base(model)
        {
            _selectionService = selectionService;
            ModelObserver = model;
        }
        

        public void OnSelected(SelectionType selectionType)
        {
            //_selectionService.UpdateSelectable(this, selectionType);
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
        }

  
        public void SetActive(bool isActive)
        {
            // do nothing - bad code here
        }

    }
}