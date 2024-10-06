using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Menu
{
    public interface IMenuCommand : ICommand
    {
        void ExitSkirmish();
        void ResumeGame();
        void OpenMenu();
    }
}