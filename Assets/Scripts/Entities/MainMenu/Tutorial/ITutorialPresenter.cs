using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MainMenu.Tutorial
{
    public interface ITutorialPresenter
    {
        void StartTutorial(FactionType faction);
        void Close();
    }
}
