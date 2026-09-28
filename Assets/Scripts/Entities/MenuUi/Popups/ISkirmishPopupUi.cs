using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISkirmishPopupUi
    {
        void SetModel(ISkirmishPopupModelObserver model);
        void SetPresenter(ISkirmishPopupPresenter presenter);
        void SetData(TeamColorPalette palette);
        void Initialize();
        void Dispose();
        void Show();
        void Hide();
    }
}
