using EmpireAtWar.Commands;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionComponent:BaseComponent<SelectionModel>, ISelectionCommand
    {
        private readonly ISelectionService selectionService;
      


        public EnemySelectionComponent(IModel model ) : base(model)
        {
        }
        

        public void OnSelected(SelectionType selectionType)
        {
            //todo: add ui invoke
            //battleService.AddTarget(healthComponent);
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
            
        }
    }
}