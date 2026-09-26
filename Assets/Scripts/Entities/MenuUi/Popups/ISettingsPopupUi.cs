namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISettingsPopupUi
    {
        void SetModel(ISettingsPopupModelObserver model);
        void SetPresenter(ISettingsPopupPresenter presenter);
        void Initialize();
        void Render();
        void Dispose();
        void Show();
        void Hide();
    }
}
