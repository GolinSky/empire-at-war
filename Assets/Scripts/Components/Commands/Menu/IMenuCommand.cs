using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.Menu
{
    public interface IMenuCommand : ICommand
    {
        void ExitSkirmish();
        void ResumeGame();
        void OpenMenu();
    }
}