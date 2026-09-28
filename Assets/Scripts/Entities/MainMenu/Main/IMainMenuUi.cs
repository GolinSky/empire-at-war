namespace EmpireAtWar.Entities.MainMenu.Main
{
    public interface IMainMenuUi
    {
        void SetModel(IMainMenuModel model);
        void SetNavigation(IMainRouteNavigation navigation);
        void Initialize();
        void Dispose();
    }
}
