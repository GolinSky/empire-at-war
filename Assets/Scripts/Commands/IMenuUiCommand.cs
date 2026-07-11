using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands
{
    public interface IMenuUiCommand:ICommand
    {
        void StartDemo();
        void OpenOptions();
        void ExitApplication();
    }
}