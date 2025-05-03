using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Services.NavigationService;

namespace EmpireAtWar.Entities.Ship.EntityCommands.Selection
{
    public class SelectionCommand: IEntitySelectionCommand
    {
        public SelectionType SelectionType { get; set; }


        public SelectionCommand(SelectionType selectionType)
        {
            SelectionType = selectionType;
        }
        
        public void Select(bool isSelected)
        {
            // invoke selection components -> last one change model - model invokes event - view updates visuals
        }
    }
}