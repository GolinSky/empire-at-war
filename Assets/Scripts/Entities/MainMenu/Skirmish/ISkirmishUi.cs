using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public interface ISkirmishUi
    {
        void SetModel(ISkirmishModelObserver model);
        void SetNavigation(ISkirmishRouteNavigation navigation);
        void SetData(TeamColorPalette palette);
        void Initialize();
        void Dispose();
        void Show();
        void Hide();
    }
}
