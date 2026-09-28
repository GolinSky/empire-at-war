namespace EmpireAtWar.Entities.MainMenu.Settings
{
    public interface ISettingsUi
    {
        void SetModel(ISettingsModelObserver model);
        void SetNavigation(ISettingsRouteNavigation navigation);
        void Initialize();
        void Render();
        void Dispose();
        void Show();
        void Hide();
    }
}
