namespace EmpireAtWar.Entities.MainMenu.ServerBrowser
{
    public interface IServerBrowserPresenter
    {
        void Refresh();
        void JoinLobby(int index);
        void JoinByIp(string address);
        void Close();
    }
}
