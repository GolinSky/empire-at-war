using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands
{
    public interface ISelectionCommand : ICommand
    {
        void OnSelected(SelectionType selectionType);
    }
}