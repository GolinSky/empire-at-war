namespace EmpireAtWar.Views.Menu
{
    public interface IPauseMenuUiView
    {
        void SetPresenter(IPauseMenuPresenter presenter);
        void Initialize();
        void Dispose();
        void SetMenuVisible(bool isVisible);
    }
}
