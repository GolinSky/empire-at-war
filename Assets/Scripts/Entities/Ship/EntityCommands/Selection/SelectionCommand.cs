using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Services.NavigationService;

namespace EmpireAtWar.Entities.Ship.EntityCommands.Selection
{
    public class SelectionCommand: IEntitySelectionCommand
    {
        private readonly ISelectionComponent _selectionComponent;
        public SelectionType SelectionType { get; set; }


        public SelectionCommand(SelectionType selectionType, ISelectionComponent selectionComponent)
        {
            _selectionComponent = selectionComponent;
            SelectionType = selectionType;
        }
        
        public void Select(bool isSelected)
        {
            _selectionComponent.SetActive(isSelected);
            // invoke selection components -> last one change model - model invokes event - view updates visuals
        }
    }
}