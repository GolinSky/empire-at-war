using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands
{
    public interface ISelectionCommand : ICommand
    {
        void OnSelected(SelectionType selectionType);
        void OnSkipSelection(SelectionType selectionType);
    }
}