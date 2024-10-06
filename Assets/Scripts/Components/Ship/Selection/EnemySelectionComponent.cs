using EmpireAtWar.Commands;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionComponent:BaseComponent<SelectionModel>, ISelectionCommand
    {
        private readonly IModel model;
        private readonly IBattleService battleService;
        private readonly ISelectionService selectionService;
      


        public EnemySelectionComponent(IModel model, IBattleService battleService) : base(model)
        {
            this.model = model;
            this.battleService = battleService;
        }
        

        public void OnSelected(SelectionType selectionType)
        {
            battleService.NotifyAttack(model); 
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
            
        }
    }
}