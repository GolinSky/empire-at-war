namespace EmpireAtWar.Entities.MainMenu.Main
{
    public interface IMainMenuPresenter
    {
        void OpenPlay();

        void OpenSettings();

        void ExitApplication();

        void AnswerUsageDataConsent(bool isShared);
    }
}
