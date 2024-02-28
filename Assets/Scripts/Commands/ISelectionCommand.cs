using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands
{
    public interface ISelectionCommand : ICommand
    {
        void OnSelected(SelectionType selectionType);
        void OnSkipSelection(SelectionType selectionType);
    }
}