using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands
{
    public interface ISelectionCommand : ICommand
    {
        void OnSelected(SelectionType selectionType);
    }

    public class SelectionCommand : Command, ISelectionCommand
    {
        private readonly ISelectable selectable;
        private readonly INavigationService navigationService;

        public SelectionCommand(IController controller, INavigationService navigationService, ISelectable selectable) : base(controller)
        {
            this.navigationService = navigationService;
            this.selectable = selectable;
        }

        public void OnSelected(SelectionType selectionType)
        {
            navigationService.UpdateSelectable(selectable, selectionType);
        }
    }
}