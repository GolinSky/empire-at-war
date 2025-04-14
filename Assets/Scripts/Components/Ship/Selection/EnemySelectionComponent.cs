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
        private readonly IModel _model;
        private readonly IBattleService _battleService;
        private readonly ISelectionService _selectionService;
      


        public EnemySelectionComponent(IModel model, IBattleService battleService) : base(model)
        {
            _model = model;
            _battleService = battleService;
        }
        

        public void OnSelected(SelectionType selectionType)
        {
            _battleService.NotifyAttack(_model); 
        }

        public void OnSkipSelection(SelectionType selectionType)
        {
            
        }
    }
}