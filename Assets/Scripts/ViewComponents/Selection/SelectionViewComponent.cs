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
        protected override void OnRelease() {}

        protected override void OnCommandSet(ICommand command)
        {
            base.OnCommandSet(command);
            command.TryGetCommand(out selectionCommand);
        }

        private void OnMouseDown()
        {
            Debug.Log($"OnMouseDown: {selectionType}");
            selectionCommand?.OnSelected(selectionType);
        }
    }
}