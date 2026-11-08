namespace EmpireAtWar.Entities.MainMenu.Play
{
    public interface IPlayPresenter
    {
        void OpenSkirmish();
        void OpenSurvival();
        void OpenSimulation();
        void OpenOnline();
        void Close();
        void OpenLoadGame();
    }
}
