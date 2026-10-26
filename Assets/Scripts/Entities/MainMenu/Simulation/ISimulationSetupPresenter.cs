namespace EmpireAtWar.Entities.MainMenu.Simulation
{
    public interface ISimulationSetupPresenter
    {
        void Close();
        void StartBattle();
        void SelectFaction(int side, int index);
        void AddUnit(int side, int unit, int quantity);
        void RemoveUnit(int side, int index);
        void SetDistance(float value);
        void SetTimeLimit(float value);
        void SetFastSpeed(bool value);
        void SetUseHangars(bool value);
    }
}
