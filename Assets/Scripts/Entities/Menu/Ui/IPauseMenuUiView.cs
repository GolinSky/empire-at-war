namespace EmpireAtWar.Views.Menu
{
    public interface IPauseMenuUiView
    {
        void Initialize();

        void Dispose();

        void SetNavigation(IPauseMenuRouteNavigation navigation);

        void SetMenuVisible(bool isVisible);
    }
}
