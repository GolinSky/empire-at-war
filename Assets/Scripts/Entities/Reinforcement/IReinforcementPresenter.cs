using EmpireAtWar.Controllers.Factions;

namespace EmpireAtWar.Entities.Reinforcement
{
    public interface IReinforcementPresenter
    {
        void TrySpawnReinforcement(UnitRequest request);

        void Show();

        void Hide();
    }
}
