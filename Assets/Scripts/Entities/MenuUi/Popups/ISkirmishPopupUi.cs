namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISkirmishPopupUi
    {
        void SetModel(ISkirmishPopupModelObserver model);
        void SetPresenter(ISkirmishPopupPresenter presenter);
        void Initialize();
        void Dispose();
        void Show();
        void Hide();
    }
}
