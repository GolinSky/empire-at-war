namespace EmpireAtWar.Presenters.Reinforcement
{
    public interface IReinforcementPresenter
    {
        void TrySpawnReinforcement(string id);
        void Show();
        void Hide();
    }
}
