using EmpireAtWar.Services.Selection;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands
{
    public interface ISelectionCommand : ICommand
    {
        void OnSelected(SelectionType selectionType);
        void OnSkipSelection(SelectionType selectionType);
    }
}