using EmpireAtWar.Commands;
using EmpireAtWar.Services.NavigationService;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.ViewComponents;

namespace EmpireAtWar.ViewComponents.Selection
{
    public class SelectionViewComponent:ViewComponent
    {
        [SerializeField] private SelectionType selectionType;
        
        private ISelectionCommand selectionCommand;
        
        protected override void OnInit() {}

        protected override void OnRelease()
        {
            selectionCommand?.OnSkipSelection(selectionType);

            selectionCommand = null;
        }

        protected override void OnCommandSet(ICommand command)
        {
            base.OnCommandSet(command);
            command.TryGetCommand(out selectionCommand);
        }
        
        private void OnMouseUp()
        {
        }

        public void OnSelected()
        {
            selectionCommand?.OnSelected(selectionType);
        }
    }
}