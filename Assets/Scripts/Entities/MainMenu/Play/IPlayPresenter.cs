namespace EmpireAtWar.Entities.MainMenu.Play
{
    public interface IPlayPresenter
    {
        void OpenSkirmish();
        void OpenSurvival();
        void OpenSimulation();
        void OpenOnline();
        void OpenTutorial();
        void Close();
        void OpenLoadGame();
    }
}
