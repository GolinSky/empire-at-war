namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public interface ISettingsUi
    {
        void Initialize();

        void Dispose();

        void SetModel(ISettingsModelObserver model);

        void SetNavigation(ISettingsRouteNavigation navigation);

        void Render();

        void Show();

        void Hide();
    }
}
