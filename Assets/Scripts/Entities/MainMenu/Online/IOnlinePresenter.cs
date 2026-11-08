namespace EmpireAtWar.Entities.MainMenu.Online
{
    public interface IOnlinePresenter
    {
        void HostSkirmish();
        void HostSurvival();
        void Join(string address);
        void AcceptInvite();
        void Close();
    }
}
