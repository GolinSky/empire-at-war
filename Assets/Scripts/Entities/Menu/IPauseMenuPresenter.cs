namespace EmpireAtWar.Entities.Menu
{
    public interface IPauseMenuPresenter
    {
        void Close();

        void RestartBattle();

        void OpenSettings();

        void ExitSkirmish();
        void OpenSaveGame();
    }
}
