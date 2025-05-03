using EmpireAtWar.Commands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionComponent:BaseComponent<SelectionModel>, ISelectionCommand, ISelectable
    {
        private readonly ISelectionService _selectionService;
      
        public IMovable Movable { get; }
        public IModelObserver ModelObserver { get; }
        
        [Inject]
        public PlayerType PlayerType { get; }


        public EnemySelectionComponent(IModel model, IMovable movable, ISelectionService selectionService) : base(model)
        {
            _selectionService = selectionService;
            Movable = movable;
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
            
        }

    }
}