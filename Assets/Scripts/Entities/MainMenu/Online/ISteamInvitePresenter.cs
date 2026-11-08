namespace EmpireAtWar.Entities.MainMenu.Online
{
    public interface ISteamInvitePresenter
    {
        void Refresh();
        void Invite(int index);
        void Close();
    }
}
