namespace EmpireAtWar.Entities.MainMenu.Online
{
    public interface IOnlinePresenter
    {
        void HostGame();
        void OpenServerBrowser();
        void AcceptInvite();
        void Close();
    }
}
