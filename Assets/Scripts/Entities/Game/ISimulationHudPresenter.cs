namespace EmpireAtWar.Entities.Game
{
    public interface ISimulationHudPresenter
    {
        void TogglePause();
        void ToggleSpeed();
        void Restart();
        void New();
    }
}
