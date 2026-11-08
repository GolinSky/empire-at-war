namespace EmpireAtWar.Entities.MainMenu.Online
{
    /// <summary>
    /// A lobby setup screen; <see cref="OnlineRouteUiController"/> shows it while this PC is in a lobby of its mode.
    /// Leaving goes through the online session, never through this screen.
    /// </summary>
    public interface IOnlineLobbyScreen
    {
        void Show();
        void Hide();
    }
}
