using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public interface ISkirmishUi
    {
        void Initialize();

        void Dispose();

        void SetModel(ISkirmishModelObserver model);

        void SetNavigation(ISkirmishRouteNavigation navigation);

        void SetData(TeamColorPalette palette);

        void Show();

        void Hide();
    }
}
