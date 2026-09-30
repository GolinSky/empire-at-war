using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Presenters.Reinforcement
{
    public interface IReinforcementPresenter
    {
        void TrySpawnReinforcement(UnitRequest request);
        void Show();
        void Hide();
    }
}
