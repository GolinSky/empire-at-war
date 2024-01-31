using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands
{
    public interface ISelectionCommand : ICommand
    {
        void OnSelected(SelectionType selectionType);
    }
}