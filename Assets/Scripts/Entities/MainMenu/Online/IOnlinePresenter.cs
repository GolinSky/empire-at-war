namespace EmpireAtWar.Entities.MainMenu.Online
{
    public interface IOnlinePresenter
    {
        void HostSteamLobby();
        void HostLanGame();
        void OpenServerBrowser();
        void AcceptInvite();
        void RetrySteam();
        void Close();
    }
}
