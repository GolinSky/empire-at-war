namespace EmpireAtWar.Views.Menu
{
    public interface IPauseMenuUiView
    {
        void SetNavigation(IPauseMenuRouteNavigation navigation);
        void Initialize();
        void Dispose();
        void SetMenuVisible(bool isVisible);
    }
}
