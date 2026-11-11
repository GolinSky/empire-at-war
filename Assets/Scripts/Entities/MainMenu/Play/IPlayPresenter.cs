namespace EmpireAtWar.Entities.MainMenu.Play
{
    public interface IPlayPresenter
    {
        void OpenSinglePlayer();
        void OpenMultiplayer();
        void OpenSimulation();
        void OpenTutorial();
        void Close();
    }
}
