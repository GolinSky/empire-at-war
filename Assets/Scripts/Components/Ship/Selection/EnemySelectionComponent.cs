using EmpireAtWar.Commands;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionComponent:BaseComponent<SelectionModel>, ISelectionCommand
    {
        private readonly IBattleService battleService;
        private readonly IHealthComponent healthComponent;


        public EnemySelectionComponent(IModel model, IBattleService battleService, IHealthComponent healthComponent) : base(model)
        {
            this.battleService = battleService;
            this.healthComponent = healthComponent;
        }
        

        public void OnSelected(SelectionType selectionType)
        {
            battleService.AddTarget(healthComponent);
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
            
        }
    }
}