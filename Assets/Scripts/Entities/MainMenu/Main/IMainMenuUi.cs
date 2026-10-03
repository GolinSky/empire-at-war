namespace EmpireAtWar.Entities.MainMenu.Main
{
    public interface IMainMenuUi
    {
        void Initialize();

        void Dispose();

        void SetModel(IMainMenuModel model);

        void SetNavigation(IMainRouteNavigation navigation);
    }
}
